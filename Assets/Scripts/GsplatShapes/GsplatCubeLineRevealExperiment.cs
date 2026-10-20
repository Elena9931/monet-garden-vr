using System;
using System.Collections.Generic;
using Gsplat;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GsplatExtensions
{
    [ExecuteAlways]
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class GsplatCubeLineRevealExperiment : MonoBehaviour
    {
        public enum AdvanceMode
        {
            CameraMovement,
            Automatic
        }

        const float ShC0 = 0.28209479177387814f;

        [Header("Independent Source")]
        [SerializeField] GsplatRenderer sourceRenderer;
        [SerializeField] GsplatAsset sourceAsset;
        [SerializeField] Camera drivingCamera;
        [Tooltip("Build the independent cube output whenever this component is enabled.")]
        [SerializeField] bool buildOnEnable;
        [Tooltip("In Play Mode, build the experiment if needed and start the reveal automatically.")]
        [SerializeField] bool startRevealOnPlay = true;
        [Tooltip("While this experiment exists, hide the source Gaussian renderer and old Generated Gsplat Shapes that use the same source. Their previous states are restored when the experiment is cleared or disabled.")]
        [SerializeField] bool isolateCompetingDisplays = true;

        [Header("Cube Sampling")]
        [Tooltip("World appearance size of every cube. This now updates the existing output immediately; rebuilding is not required.")]
        [Min(0.0001f)]
        [SerializeField] float cubeSize = 0.03f;
        [Min(1)]
        [SerializeField] int sampleStep = 1;
        [Min(1)]
        [SerializeField] int maxCubes = 30000;
        [Range(0f, 1f)]
        [SerializeField] float minimumOpacity = 0.01f;
        [SerializeField] bool multiplyColorByOpacity = true;
        [Min(1000)]
        [SerializeField] int maxVerticesPerMesh = 400000;

        [Header("Forward Reveal")]
        [SerializeField] AdvanceMode advanceMode = AdvanceMode.CameraMovement;
        [FormerlySerializedAs("cameraMovementMultiplier")]
        [Tooltip("In Camera Movement mode, the reveal plane follows the current Timeline/Game camera and stays this many world units in front of it.")]
        [Min(0f)]
        [SerializeField] float cameraScanAheadDistance = 2f;
        [Min(0f)]
        [SerializeField] float automaticSpeed = 2f;
        [InspectorName("End Padding Distance")]
        [Tooltip("Extra travel after the final cubes before an automatic reveal stops or loops.")]
        [Min(0.01f)]
        [SerializeField] float movingBandLength = 10f;
        [Tooltip("Thickness used to find points around the glowing reveal cross-section. All rendered connections are projected back onto the exact scan plane.")]
        [Min(0.01f)]
        [SerializeField] float connectionPlaneThickness = 1f;
        [Tooltip("Automatic mode only: forward travel required before the nearest cubes begin to appear.")]
        [Min(0f)]
        [SerializeField] float initialHiddenDistance = 0.5f;
        [Min(0.0001f)]
        [SerializeField] float revealEdgeWidth = 0.35f;
        [ColorUsage(true, true)]
        [SerializeField] Color revealEdgeColor = new Color(0.1f, 0.8f, 1f, 1f);
        [Min(0f)]
        [SerializeField] float revealEdgeIntensity = 2f;
        [SerializeField] bool loopAutomaticReveal;
        [SerializeField] bool previewInEditMode = true;

        [Header("Moving Connections (Maximum 10)")]
        [Range(1, 10)]
        [SerializeField] int connectionCount = 8;
        [Tooltip("Endpoints prefer a similar world-space Y coordinate.")]
        [Min(0.01f)]
        [SerializeField] float sameHeightTolerance = 0.5f;
        [Tooltip("Minimum world-space distance between connected points.")]
        [Min(0f)]
        [SerializeField] float minimumConnectionDistance = 2f;
        [Tooltip("Maximum world-space line length. This prevents connections from jumping to another model or to isolated Gaussian outliers.")]
        [Min(0.01f)]
        [SerializeField] float maximumConnectionDistance = 4f;
        [Min(0.001f)]
        [SerializeField] float lineWidth = 0.025f;
        [ColorUsage(true, true)]
        [SerializeField] Color lineColor = new Color(0.1f, 0.75f, 1f, 1f);
        [Min(0.01f)]
        [SerializeField] float lineFlowSpeed = 1.5f;
        [Range(0.02f, 0.8f)]
        [SerializeField] float pulseLength = 0.2f;
        [Range(0.05f, 2f)]
        [SerializeField] float reconnectInterval = 0.35f;

        Transform outputRoot;
        Transform cubeRoot;
        Transform lineRoot;
        Material cubeMaterial;
        Material lineMaterial;
        readonly List<Mesh> generatedMeshes = new List<Mesh>();
        readonly List<Vector3> localCenters = new List<Vector3>();
        readonly List<int> candidateIndices = new List<int>();
        readonly List<ConnectionSlot> connectionSlots = new List<ConnectionSlot>();
        readonly List<GameObject> isolatedGeneratedRoots = new List<GameObject>();
        readonly List<bool> isolatedGeneratedRootStates = new List<bool>();

        bool revealRunning;
        bool isolationStateCaptured;
        bool sourceRendererWasEnabled;
        float builtCubeSize = 0.03f;
        float minimumDepth;
        float maximumDepth;
        float currentFrontDepth;
        float revealStartTime;
        float lastReconnectTime = float.NegativeInfinity;
        int reconnectRevision;
        Vector3 scanOrigin;
        Vector3 scanDirection = Vector3.forward;
        Camera cachedMainCamera;

        public int GeneratedCubeCount => localCenters.Count;
        public bool RevealRunning => revealRunning;

        sealed class ConnectionSlot
        {
            public LineRenderer BaseLine;
            public LineRenderer PulseLine;
            public Vector3 Start;
            public Vector3 End;
            public float Phase;
        }

        static readonly Vector3[] CubeVertices =
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f)
        };

        static readonly int[] CubeIndices =
        {
            0, 2, 1, 0, 3, 2,
            1, 2, 6, 1, 6, 5,
            5, 6, 7, 5, 7, 4,
            4, 7, 3, 4, 3, 0,
            3, 7, 6, 3, 6, 2,
            4, 0, 1, 4, 1, 5
        };

        void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
#endif
            if (buildOnEnable || (Application.isPlaying && startRevealOnPlay))
                BuildExperiment();

            if (Application.isPlaying && startRevealOnPlay)
                StartReveal();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
            ClearExperimentOutput();
            RestoreCompetingDisplays();
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif
            ClearExperimentOutput();
            RestoreCompetingDisplays();
        }

        void OnValidate()
        {
            cubeSize = Mathf.Max(0.0001f, cubeSize);
            sampleStep = Mathf.Max(1, sampleStep);
            maxCubes = Mathf.Max(1, maxCubes);
            maxVerticesPerMesh = Mathf.Max(1000, maxVerticesPerMesh);
            cameraScanAheadDistance = Mathf.Max(0f, cameraScanAheadDistance);
            automaticSpeed = Mathf.Max(0f, automaticSpeed);
            movingBandLength = Mathf.Max(0.01f, movingBandLength);
            connectionPlaneThickness =
                Mathf.Max(0.01f, connectionPlaneThickness);
            initialHiddenDistance = Mathf.Max(0f, initialHiddenDistance);
            revealEdgeWidth = Mathf.Max(0.0001f, revealEdgeWidth);
            revealEdgeIntensity = Mathf.Max(0f, revealEdgeIntensity);
            connectionCount = Mathf.Clamp(connectionCount, 1, 10);
            sameHeightTolerance = Mathf.Max(0.01f, sameHeightTolerance);
            minimumConnectionDistance = Mathf.Max(0f, minimumConnectionDistance);
            maximumConnectionDistance = Mathf.Max(
                minimumConnectionDistance,
                maximumConnectionDistance);
            lineWidth = Mathf.Max(0.001f, lineWidth);
            lineFlowSpeed = Mathf.Max(0.01f, lineFlowSpeed);
            reconnectInterval = Mathf.Clamp(reconnectInterval, 0.05f, 2f);
            if (!isolateCompetingDisplays && isolationStateCaptured)
                RestoreCompetingDisplays();
            ApplyCubeMaterialProperties();
            ApplyLineAppearance();
        }

        void Update()
        {
            MaintainDisplayIsolation();
            if (Application.isPlaying)
                Tick(Time.time);
        }

#if UNITY_EDITOR
        void OnEditorUpdate()
        {
            if (Application.isPlaying || !isActiveAndEnabled)
                return;

            MaintainDisplayIsolation();
            if (previewInEditMode)
                Tick((float)EditorApplication.timeSinceStartup);

            if (revealRunning)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
#endif

        public void BuildExperiment()
        {
            GsplatAsset asset = ResolveAsset();
            if (asset == null || asset.Positions == null || asset.Colors == null)
            {
                Debug.LogError(
                    "Cube Line Reveal Experiment requires a Gsplat Renderer or Gsplat Asset.",
                    this);
                return;
            }

            ClearExperimentOutput();
            ApplyDisplayIsolation();
            CreateRoots();
            CreateMaterials();
            BuildCubes(asset);
            CreateConnectionLines();
            PrepareScanFrame();
            ResetReveal();

            Debug.Log(
                $"Cube Line Reveal Experiment built {localCenters.Count:N0} independent cubes.",
                this);
        }

        public void StartReveal()
        {
            if (localCenters.Count == 0)
                BuildExperiment();
            if (localCenters.Count == 0)
                return;

            ApplyDisplayIsolation();
            PrepareScanFrame();
            currentFrontDepth = advanceMode == AdvanceMode.CameraMovement
                ? cameraScanAheadDistance
                : minimumDepth - initialHiddenDistance;
            revealStartTime = GetCurrentTime();
            lastReconnectTime = float.NegativeInfinity;
            reconnectRevision = 0;
            revealRunning = true;
            SetLinesVisible(false);
            ApplyCubeMaterialProperties();
            RefreshConnections();
            UpdateLinePulses(revealStartTime);
            lastReconnectTime = revealStartTime;
        }

        public void ResetReveal()
        {
            ApplyDisplayIsolation();
            revealRunning = false;
            currentFrontDepth = minimumDepth - initialHiddenDistance;
            SetLinesVisible(false);
            ApplyCubeMaterialProperties();
        }

        public void ShowAllCubes()
        {
            ApplyDisplayIsolation();
            revealRunning = false;
            currentFrontDepth = maximumDepth + movingBandLength;
            SetLinesVisible(false);
            ApplyCubeMaterialProperties();
        }

        public void ClearExperiment()
        {
            ClearExperimentOutput();
            RestoreCompetingDisplays();
        }

        void ClearExperimentOutput()
        {
            revealRunning = false;
            localCenters.Clear();
            candidateIndices.Clear();
            connectionSlots.Clear();

            for (int i = 0; i < generatedMeshes.Count; i++)
                DestroyGeneratedObject(generatedMeshes[i]);
            generatedMeshes.Clear();

            DestroyGeneratedObject(cubeMaterial);
            DestroyGeneratedObject(lineMaterial);
            cubeMaterial = null;
            lineMaterial = null;

            if (outputRoot != null)
                DestroyGeneratedObject(outputRoot.gameObject);
            outputRoot = null;
            cubeRoot = null;
            lineRoot = null;
        }

        void ApplyDisplayIsolation()
        {
            if (!isolateCompetingDisplays)
                return;

            if (!isolationStateCaptured)
            {
                isolationStateCaptured = true;
                sourceRendererWasEnabled =
                    sourceRenderer != null && sourceRenderer.enabled;
            }

            if (sourceRenderer != null)
                sourceRenderer.enabled = false;

            CaptureAndHideGeneratedRoots();
        }

        void MaintainDisplayIsolation()
        {
            if (!isolationStateCaptured || !isolateCompetingDisplays)
                return;

            if (sourceRenderer != null && sourceRenderer.enabled)
                sourceRenderer.enabled = false;

            CaptureAndHideGeneratedRoots();
        }

        void CaptureAndHideGeneratedRoots()
        {
            Transform sourceTransform = SourceTransform;
            for (int childIndex = 0;
                 childIndex < sourceTransform.childCount;
                 childIndex++)
            {
                GameObject child = sourceTransform.GetChild(childIndex).gameObject;
                if (child.name != "Generated Gsplat Shapes")
                    continue;

                int capturedIndex = isolatedGeneratedRoots.IndexOf(child);
                if (capturedIndex < 0)
                {
                    isolatedGeneratedRoots.Add(child);
                    isolatedGeneratedRootStates.Add(child.activeSelf);
                }

                if (child.activeSelf)
                    child.SetActive(false);
            }
        }

        void RestoreCompetingDisplays()
        {
            if (!isolationStateCaptured)
                return;

            if (sourceRenderer != null)
                sourceRenderer.enabled = sourceRendererWasEnabled;

            for (int i = 0; i < isolatedGeneratedRoots.Count; i++)
            {
                GameObject generatedRoot = isolatedGeneratedRoots[i];
                if (generatedRoot != null)
                    generatedRoot.SetActive(isolatedGeneratedRootStates[i]);
            }

            isolatedGeneratedRoots.Clear();
            isolatedGeneratedRootStates.Clear();
            isolationStateCaptured = false;
        }

        void Tick(float now)
        {
            if (!revealRunning || localCenters.Count == 0 || cubeMaterial == null)
                return;

            if (advanceMode == AdvanceMode.CameraMovement)
            {
                Camera cameraToUse = ResolveCamera();
                Transform cameraTransform = cameraToUse != null
                    ? cameraToUse.transform
                    : transform;

                // Timeline/Cinemachine can move and rotate the camera. Keeping
                // the scan frame attached to the current camera guarantees the
                // glowing cross-section remains visible in Game view instead
                // of being left behind at the reveal's starting transform.
                scanOrigin = cameraTransform.position;
                scanDirection = cameraTransform.forward.normalized;
                currentFrontDepth = cameraScanAheadDistance;
            }
            else
            {
                currentFrontDepth =
                    minimumDepth - initialHiddenDistance +
                    (now - revealStartTime) * automaticSpeed;

                if (loopAutomaticReveal &&
                    currentFrontDepth > maximumDepth + movingBandLength)
                {
                    revealStartTime = now;
                    currentFrontDepth =
                        minimumDepth - initialHiddenDistance;
                }
            }

            ApplyCubeMaterialProperties();

            if (advanceMode == AdvanceMode.CameraMovement ||
                currentFrontDepth <= maximumDepth + movingBandLength)
            {
                if (now - lastReconnectTime >= reconnectInterval)
                {
                    RefreshConnections();
                    lastReconnectTime = now;
                }
                UpdateLinePulses(now);
            }
            else
            {
                SetLinesVisible(false);
                if (!loopAutomaticReveal)
                    revealRunning = false;
            }
        }

        GsplatAsset ResolveAsset()
        {
            if (sourceAsset != null)
                return sourceAsset;
            if (sourceRenderer != null)
                return sourceRenderer.GsplatAsset;
            return null;
        }

        Camera ResolveCamera()
        {
            if (drivingCamera != null)
                return drivingCamera;

            Camera currentMainCamera = Camera.main;
            if (currentMainCamera != null)
                cachedMainCamera = currentMainCamera;
            return cachedMainCamera;
        }

        Transform SourceTransform => sourceRenderer != null
            ? sourceRenderer.transform
            : transform;

        void CreateRoots()
        {
            var root = new GameObject("Cube Line Reveal Experiment Output")
            {
                hideFlags = HideFlags.DontSave
            };
            outputRoot = root.transform;
            outputRoot.SetParent(SourceTransform, false);

            var cubes = new GameObject("Independent Cubes")
            {
                hideFlags = HideFlags.DontSave
            };
            cubeRoot = cubes.transform;
            cubeRoot.SetParent(outputRoot, false);

            var lines = new GameObject("Moving Connections")
            {
                hideFlags = HideFlags.DontSave
            };
            lineRoot = lines.transform;
            lineRoot.SetParent(outputRoot, false);
        }

        void CreateMaterials()
        {
            Shader cubeShader = Shader.Find(
                "GsplatExtensions/URP Cube Line Reveal");
            Shader lineShader = Shader.Find(
                "GsplatExtensions/URP Connection Line");

            if (cubeShader == null || lineShader == null)
            {
                Debug.LogError(
                    "Cube Line Reveal shaders are missing or still compiling.",
                    this);
                return;
            }

            cubeMaterial = new Material(cubeShader)
            {
                name = "Cube Line Reveal (Runtime)",
                hideFlags = HideFlags.DontSave
            };
            lineMaterial = new Material(lineShader)
            {
                name = "Connection Line (Runtime)",
                hideFlags = HideFlags.DontSave
            };
            ApplyCubeMaterialProperties();
            ApplyLineAppearance();
        }

        void BuildCubes(GsplatAsset asset)
        {
            if (cubeMaterial == null)
                return;

            builtCubeSize = Mathf.Max(0.0001f, cubeSize);

            int sourceCount = Mathf.Min(
                asset.Positions.Length,
                asset.Colors.Length);
            int step = Mathf.Max(
                sampleStep,
                Mathf.CeilToInt(sourceCount / (float)maxCubes));

            var sampledColors = new List<Color>(maxCubes);
            for (int sourceIndex = 0;
                 sourceIndex < sourceCount && localCenters.Count < maxCubes;
                 sourceIndex += step)
            {
                Vector4 rawColor = asset.Colors[sourceIndex];
                if (rawColor.w < minimumOpacity)
                    continue;

                localCenters.Add(asset.Positions[sourceIndex]);
                sampledColors.Add(DecodeColor(rawColor));
            }

            int cubesPerChunk = Mathf.Max(
                1,
                maxVerticesPerMesh / CubeVertices.Length);
            int chunkCount = Mathf.CeilToInt(
                localCenters.Count / (float)cubesPerChunk);

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int first = chunkIndex * cubesPerChunk;
                int cubeCount = Mathf.Min(
                    cubesPerChunk,
                    localCenters.Count - first);
                var vertices = new List<Vector3>(cubeCount * CubeVertices.Length);
                var colors = new List<Color>(cubeCount * CubeVertices.Length);
                var centers = new List<Vector3>(cubeCount * CubeVertices.Length);
                var indices = new List<int>(cubeCount * CubeIndices.Length);

                for (int cubeIndex = 0; cubeIndex < cubeCount; cubeIndex++)
                {
                    int sampleIndex = first + cubeIndex;
                    Vector3 center = localCenters[sampleIndex];
                    int vertexOffset = vertices.Count;

                    for (int vertexIndex = 0;
                         vertexIndex < CubeVertices.Length;
                         vertexIndex++)
                    {
                        vertices.Add(
                            center + CubeVertices[vertexIndex] * builtCubeSize);
                        colors.Add(sampledColors[sampleIndex]);
                        centers.Add(center);
                    }

                    for (int index = 0; index < CubeIndices.Length; index++)
                        indices.Add(vertexOffset + CubeIndices[index]);
                }

                var mesh = new Mesh
                {
                    name = $"Independent Reveal Cubes {chunkIndex + 1}",
                    indexFormat = vertices.Count > 65535
                        ? IndexFormat.UInt32
                        : IndexFormat.UInt16,
                    hideFlags = HideFlags.DontSave
                };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetUVs(1, centers);
                mesh.SetTriangles(indices, 0, true);
                mesh.RecalculateBounds();
                generatedMeshes.Add(mesh);

                var chunk = new GameObject($"Chunk {chunkIndex + 1:D3}")
                {
                    hideFlags = HideFlags.DontSave,
                    layer = SourceTransform.gameObject.layer
                };
                chunk.transform.SetParent(cubeRoot, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                chunk.AddComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
            }
        }

        Color DecodeColor(Vector4 rawColor)
        {
            var color = new Color(
                Mathf.Max(0f, rawColor.x * ShC0 + 0.5f),
                Mathf.Max(0f, rawColor.y * ShC0 + 0.5f),
                Mathf.Max(0f, rawColor.z * ShC0 + 0.5f),
                Mathf.Clamp01(rawColor.w));

            if (multiplyColorByOpacity)
            {
                color.r *= color.a;
                color.g *= color.a;
                color.b *= color.a;
                color.a = 1f;
            }
            return color;
        }

        void CreateConnectionLines()
        {
            if (lineMaterial == null)
                return;

            for (int i = 0; i < connectionCount; i++)
            {
                var slotRoot = new GameObject($"Connection {i + 1:D2}")
                {
                    hideFlags = HideFlags.DontSave
                };
                slotRoot.transform.SetParent(lineRoot, false);

                LineRenderer baseLine = CreateLineRenderer(
                    slotRoot.transform,
                    "Base");
                LineRenderer pulseLine = CreateLineRenderer(
                    slotRoot.transform,
                    "Flow Pulse");
                connectionSlots.Add(new ConnectionSlot
                {
                    BaseLine = baseLine,
                    PulseLine = pulseLine,
                    Phase = i / (float)Mathf.Max(1, connectionCount)
                });
            }
            ApplyLineAppearance();
            SetLinesVisible(false);
        }

        LineRenderer CreateLineRenderer(Transform parent, string objectName)
        {
            var lineObject = new GameObject(objectName)
            {
                hideFlags = HideFlags.DontSave
            };
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.sharedMaterial = lineMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void CalculateDepthRange()
        {
            minimumDepth = float.PositiveInfinity;
            maximumDepth = float.NegativeInfinity;
            Transform sourceTransform = SourceTransform;

            for (int i = 0; i < localCenters.Count; i++)
            {
                Vector3 world = sourceTransform.TransformPoint(localCenters[i]);
                float depth = Vector3.Dot(world - scanOrigin, scanDirection);
                minimumDepth = Mathf.Min(minimumDepth, depth);
                maximumDepth = Mathf.Max(maximumDepth, depth);
            }

            if (float.IsNaN(minimumDepth) ||
                float.IsInfinity(minimumDepth))
            {
                minimumDepth = 0f;
                maximumDepth = 0f;
            }
        }

        void PrepareScanFrame()
        {
            Camera cameraToUse = ResolveCamera();
            Transform directionSource = cameraToUse != null
                ? cameraToUse.transform
                : transform;
            scanOrigin = directionSource.position;
            scanDirection = directionSource.forward.normalized;
            CalculateDepthRange();
        }

        void RefreshConnections()
        {
            candidateIndices.Clear();
            Transform sourceTransform = SourceTransform;
            // Select only points touching the glowing cross-section. Endpoints
            // are projected onto the exact plane below, so every connection is
            // coplanar with the reveal light instead of filling a deep volume.
            float halfThickness = connectionPlaneThickness * 0.5f;
            float connectionFrontDepth = GetConnectionFrontDepth();
            float bandStart = connectionFrontDepth - halfThickness;
            float bandEnd = connectionFrontDepth + halfThickness;

            for (int i = 0; i < localCenters.Count; i++)
            {
                Vector3 world = sourceTransform.TransformPoint(localCenters[i]);
                float depth = Vector3.Dot(world - scanOrigin, scanDirection);
                if (depth >= bandStart && depth <= bandEnd)
                    candidateIndices.Add(i);
            }

            if (candidateIndices.Count < 2)
            {
                SetLinesVisible(false);
                return;
            }

            reconnectRevision++;
            for (int slotIndex = 0;
                 slotIndex < connectionSlots.Count;
                 slotIndex++)
            {
                int seed = HashIndex(reconnectRevision * 31 + slotIndex * 101);
                int firstCandidate = candidateIndices[
                    PositiveModulo(seed, candidateIndices.Count)];
                Vector3 start = sourceTransform.TransformPoint(
                    localCenters[firstCandidate]);

                int bestIndex = -1;
                float bestScore = float.NegativeInfinity;
                int checks = Mathf.Min(128, candidateIndices.Count);
                int stride = Mathf.Max(1, candidateIndices.Count / checks);
                int offset = PositiveModulo(seed * 17, candidateIndices.Count);

                for (int check = 0; check < checks; check++)
                {
                    int candidate = candidateIndices[
                        (offset + check * stride) % candidateIndices.Count];
                    if (candidate == firstCandidate)
                        continue;

                    Vector3 end = sourceTransform.TransformPoint(
                        localCenters[candidate]);
                    Vector3 delta = end - start;
                    float distance = delta.magnitude;
                    if (distance < minimumConnectionDistance)
                        continue;
                    if (distance > maximumConnectionDistance)
                        continue;

                    float heightDifference = Mathf.Abs(end.y - start.y);
                    if (heightDifference > sameHeightTolerance)
                        continue;

                    Vector3 transverse = delta -
                        Vector3.Project(delta, scanDirection);
                    float diagonalScore = transverse.magnitude +
                                          Mathf.Abs(Vector3.Dot(delta, scanDirection)) * 0.35f;
                    if (diagonalScore > bestScore)
                    {
                        bestScore = diagonalScore;
                        bestIndex = candidate;
                    }
                }

                ConnectionSlot slot = connectionSlots[slotIndex];
                if (bestIndex < 0)
                {
                    slot.BaseLine.enabled = false;
                    slot.PulseLine.enabled = false;
                    continue;
                }

                Vector3 endPoint = sourceTransform.TransformPoint(
                    localCenters[bestIndex]);
                slot.Start = ProjectOntoScanPlane(start);
                slot.End = ProjectOntoScanPlane(endPoint);
                slot.BaseLine.SetPosition(0, slot.Start);
                slot.BaseLine.SetPosition(1, slot.End);
                slot.BaseLine.enabled = true;
                slot.PulseLine.enabled = true;
            }
        }

        Vector3 ProjectOntoScanPlane(Vector3 worldPosition)
        {
            float pointDepth = Vector3.Dot(
                worldPosition - scanOrigin,
                scanDirection);
            float connectionFrontDepth = GetConnectionFrontDepth();
            return worldPosition +
                   scanDirection * (connectionFrontDepth - pointDepth);
        }

        float GetConnectionFrontDepth()
        {
            return advanceMode == AdvanceMode.CameraMovement
                ? currentFrontDepth
                : Mathf.Clamp(currentFrontDepth, minimumDepth, maximumDepth);
        }

        void UpdateLinePulses(float now)
        {
            for (int i = 0; i < connectionSlots.Count; i++)
            {
                ConnectionSlot slot = connectionSlots[i];
                if (!slot.BaseLine.enabled)
                    continue;

                float progress = Mathf.Repeat(
                    now * lineFlowSpeed + slot.Phase,
                    1f);
                float halfLength = pulseLength * 0.5f;
                float startT = Mathf.Clamp01(progress - halfLength);
                float endT = Mathf.Clamp01(progress + halfLength);
                slot.PulseLine.SetPosition(
                    0,
                    Vector3.Lerp(slot.Start, slot.End, startT));
                slot.PulseLine.SetPosition(
                    1,
                    Vector3.Lerp(slot.Start, slot.End, endT));
            }
        }

        void ApplyCubeMaterialProperties()
        {
            if (cubeMaterial == null)
                return;

            cubeMaterial.SetFloat("_RevealEnabled", 1f);
            cubeMaterial.SetVector(
                "_ScanOrigin",
                new Vector4(scanOrigin.x, scanOrigin.y, scanOrigin.z, 1f));
            cubeMaterial.SetVector(
                "_ScanDirection",
                new Vector4(scanDirection.x, scanDirection.y, scanDirection.z, 0f));
            cubeMaterial.SetFloat("_RevealFront", currentFrontDepth);
            cubeMaterial.SetFloat(
                "_CubeScale",
                cubeSize / Mathf.Max(0.0001f, builtCubeSize));
            cubeMaterial.SetFloat("_RevealEdgeWidth", revealEdgeWidth);
            cubeMaterial.SetColor("_RevealEdgeColor", revealEdgeColor);
            cubeMaterial.SetFloat("_RevealEdgeIntensity", revealEdgeIntensity);
        }

        void ApplyLineAppearance()
        {
            if (lineMaterial != null)
                lineMaterial.SetColor("_BaseColor", lineColor);

            Color faint = Color.white;
            faint.a = 0.22f;
            for (int i = 0; i < connectionSlots.Count; i++)
            {
                ConnectionSlot slot = connectionSlots[i];
                slot.BaseLine.startWidth = lineWidth;
                slot.BaseLine.endWidth = lineWidth;
                slot.BaseLine.startColor = faint;
                slot.BaseLine.endColor = faint;
                slot.PulseLine.startWidth = lineWidth * 2.4f;
                slot.PulseLine.endWidth = lineWidth * 2.4f;
                slot.PulseLine.startColor = Color.white;
                slot.PulseLine.endColor = Color.white;
            }
        }

        void SetLinesVisible(bool visible)
        {
            for (int i = 0; i < connectionSlots.Count; i++)
            {
                connectionSlots[i].BaseLine.enabled = visible;
                connectionSlots[i].PulseLine.enabled = visible;
            }
        }

        static int HashIndex(int value)
        {
            unchecked
            {
                uint x = (uint)value;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (int)x;
            }
        }

        static int PositiveModulo(int value, int modulus)
        {
            return (int)((uint)value % (uint)modulus);
        }

        static float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }

        static void DestroyGeneratedObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GsplatCubeLineRevealExperiment))]
    sealed class GsplatCubeLineRevealExperimentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var experiment = (GsplatCubeLineRevealExperiment)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Independent scan experiment: it temporarily hides the source Gaussian and old Generated Gsplat Shapes so only the scanned cubes appear. Clear or disable this component to restore their previous states.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                "Cube Size updates the existing cubes immediately. Sample Step, Max Cubes and opacity settings still require Build Independent Experiment. Maximum Connection Distance prevents lines from reaching another model or isolated outlier points.",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "Camera Movement follows the current Driving Camera (or Main Camera) position and rotation. Camera Scan Ahead Distance controls how far in front of the Game camera the glowing reveal plane stays.",
                MessageType.None);

            if (GUILayout.Button("Build Independent Experiment", GUILayout.Height(30f)))
                Invoke(experiment, "Build Cube Line Reveal", experiment.BuildExperiment);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Start Reveal"))
                    Invoke(experiment, "Start Cube Line Reveal", experiment.StartReveal);
                if (GUILayout.Button("Reset / Hide Cubes"))
                    Invoke(experiment, "Reset Cube Line Reveal", experiment.ResetReveal);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show All Cubes"))
                    Invoke(experiment, "Show All Reveal Cubes", experiment.ShowAllCubes);
                if (GUILayout.Button("Clear Experiment"))
                    Invoke(experiment, "Clear Cube Line Reveal", experiment.ClearExperiment);
            }

            EditorGUILayout.LabelField(
                "Independent Cubes",
                experiment.GeneratedCubeCount.ToString("N0"));
            EditorGUILayout.LabelField(
                "Reveal State",
                experiment.RevealRunning ? "Running" : "Stopped");
        }

        static void Invoke(
            GsplatCubeLineRevealExperiment experiment,
            string undoName,
            Action action)
        {
            Undo.RecordObject(experiment, undoName);
            action();
            EditorUtility.SetDirty(experiment);
        }
    }
#endif
}
