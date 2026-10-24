// Runtime-only, non-destructive point-shape visualizer.
// The source GsplatAsset is never modified. Concepts are adapted from the
// public-domain Pcx point-cloud renderer/deformation samples by Keijiro Takahashi.

using System;
using System.Collections.Generic;
using Gsplat;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GsplatExtensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GsplatShapeVisualizer : MonoBehaviour
    {
        public enum DisplayMode
        {
            OriginalGaussian,
            Cubes,
            Spheres
        }

        public enum ShapeAnimationMode
        {
            None,
            Twist,
            Swirl,
            Rain,
            Wind,
            Wave,
            Pulse,
            Orbit
        }

        [Header("Source")]
        [Tooltip("Optional. Assigning this lets the component switch the original Gaussian renderer on and off.")]
        [SerializeField] GsplatRenderer sourceRenderer;

        [Tooltip("Optional when Source Renderer is assigned. This is the imported PLY's Gsplat Asset.")]
        [SerializeField] GsplatAsset sourceAsset;

        [Header("Display")]
        [SerializeField] DisplayMode displayMode = DisplayMode.OriginalGaussian;
        [SerializeField] bool buildOnEnable;

        [Tooltip("Optional override. When empty, the Gsplat Shape URP Lit vertex-color material is created.")]
        [SerializeField] Material overrideMaterial;

        [Header("Shape Lighting")]
        [Range(0f, 2f)]
        [SerializeField] float flatColorBrightness = 1f;
        [Tooltip("Off keeps the extracted Gaussian colors flat. Enable only when directional face shading is wanted.")]
        [SerializeField] bool useDirectionalLight;
        [Range(0f, 2f)]
        [SerializeField] float mainLightStrength = 1f;
        [Tooltip("Allows ordinary URP Point and Spot Lights to brighten generated particles.")]
        [SerializeField] bool useUnityPointLights = true;
        [Range(0f, 4f)]
        [SerializeField] float pointLightStrength = 1f;
        [Tooltip("0 keeps lighting flat. 1 uses face normals for stronger 3D shading.")]
        [Range(0f, 1f)]
        [SerializeField] float lightingDepth;
        [Range(0f, 10f)]
        [SerializeField] float localEmissionMultiplier = 1f;

        [Min(0.0001f)]
        [SerializeField] float shapeSize = 0.03f;

        [Tooltip("Off: every generated object uses a uniform Shape Size. On: use each Gaussian's anisotropic scale and rotation. Gaussian ellipsoids are often extremely flat, so cubes/spheres can intentionally look like plates or fragments.")]
        [SerializeField] bool useGaussianTransform;

        [Min(0.0001f)]
        [SerializeField] float gaussianScaleMultiplier = 2f;

        [Range(3, 16)]
        [SerializeField] int sphereSegments = 6;

        [Range(2, 12)]
        [SerializeField] int sphereRings = 4;

        [Header("Sampling / Voxelization")]
        [Tooltip("Greater than zero merges points in the same voxel and averages their position and color. Set to zero for one shape per sampled Gaussian.")]
        [Min(0f)]
        [SerializeField] float voxelSize = 0.05f;

        [Tooltip("Reads every Nth Gaussian before voxelization.")]
        [Min(1)]
        [SerializeField] int sampleStep = 1;

        [Tooltip("Safety cap. Large sphere/cube meshes consume substantial CPU and memory.")]
        [Min(1)]
        [SerializeField] int maxShapes = 50000;

        [Tooltip("Gaussians below this opacity are ignored.")]
        [Range(0f, 1f)]
        [SerializeField] float minimumOpacity = 0.01f;

        [Tooltip("Multiplies base color by opacity to approximate the perceived Gaussian color in an opaque shape material.")]
        [SerializeField] bool multiplyColorByOpacity = true;

        [Min(1000)]
        [SerializeField] int maxVerticesPerMesh = 400000;

        [Header("Dynamic Shape Animation (Scene + Game View)")]
        [Tooltip("Animates generated shape centers without rebuilding the mesh.")]
        [SerializeField] bool animateShapes;

        [Tooltip("Map the Source Renderer's Gsplat Effect Type to the closest shape animation mode and reuse its intensity/speed/frequency.")]
        [SerializeField] bool followSourceGsplatEffect;

        [SerializeField] ShapeAnimationMode animationMode = ShapeAnimationMode.Swirl;

        [Range(1f, 30f)]
        [SerializeField] float animationFramesPerSecond = 15f;

        [Min(0f)]
        [SerializeField] float animationSpeed = 1f;

        [Min(0f)]
        [SerializeField] float animationStrength = 0.25f;

        [Min(0.0001f)]
        [SerializeField] float animationFrequency = 1f;

        [SerializeField] Vector3 windDirection = new Vector3(1f, 0.15f, 0f);

        Transform generatedRoot;
        Material generatedMaterial;
        bool ownsGeneratedMaterial;
        bool originalStateCaptured;
        bool originalRendererWasEnabled;
        DisplayMode builtMode;
        int generatedShapeCount;
        Bounds animationBounds;
        float animationStartTime;
        float lastAnimationUpdateTime = float.NegativeInfinity;
        bool animationWasApplied;

        readonly List<DynamicMeshChunk> dynamicMeshChunks = new List<DynamicMeshChunk>();

        const float ShC0 = 0.28209479177387814f;

        public DisplayMode Mode => displayMode;
        public int GeneratedShapeCount => generatedShapeCount;

        struct ShapeSample
        {
            public Vector3 Position;
            public Vector3 Scale;
            public Quaternion Rotation;
            public Color Color;
        }

        struct VoxelAccumulator
        {
            public Vector3 PositionSum;
            public Vector3 ScaleSum;
            public Vector4 ColorSum;
            public Quaternion FirstRotation;
            public int Count;
        }

        struct ShapeTemplate
        {
            public Vector3[] Vertices;
            public int[] Indices;
        }

        sealed class DynamicMeshChunk
        {
            public Mesh Mesh;
            public Vector3[] BaseVertices;
            public Vector3[] AnimatedVertices;
            public Vector3[] BaseCenters;
            public int VerticesPerShape;
        }

        void OnEnable()
        {
            CaptureOriginalRendererState();
            RestartAnimation();
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorAnimationUpdate;
            EditorApplication.update += OnEditorAnimationUpdate;
#endif
            if (buildOnEnable && displayMode != DisplayMode.OriginalGaussian)
                Rebuild();
            else
                ApplyDisplayState();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorAnimationUpdate;
#endif
            RestoreOriginalRendererState();
            ClearGeneratedShapes();
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorAnimationUpdate;
#endif
            RestoreOriginalRendererState();
            ClearGeneratedShapes();
        }

        void Update()
        {
            if (Application.isPlaying)
                TickAnimation(Time.time);
        }

#if UNITY_EDITOR
        void OnEditorAnimationUpdate()
        {
            if (Application.isPlaying || !isActiveAndEnabled)
                return;

            TickAnimation((float)EditorApplication.timeSinceStartup);

            bool hasVolumeInfluence =
                GsplatShapeInfluenceVolume.HasActiveVolumesFor(this);

            if ((animateShapes || hasVolumeInfluence) &&
                displayMode != DisplayMode.OriginalGaussian)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
#endif

        void OnValidate()
        {
            shapeSize = Mathf.Max(0.0001f, shapeSize);
            gaussianScaleMultiplier = Mathf.Max(0.0001f, gaussianScaleMultiplier);
            sampleStep = Mathf.Max(1, sampleStep);
            maxShapes = Mathf.Max(1, maxShapes);
            maxVerticesPerMesh = Mathf.Max(1000, maxVerticesPerMesh);
            sphereSegments = Mathf.Clamp(sphereSegments, 3, 16);
            sphereRings = Mathf.Clamp(sphereRings, 2, 12);
            animationFramesPerSecond = Mathf.Clamp(animationFramesPerSecond, 1f, 30f);
            animationSpeed = Mathf.Max(0f, animationSpeed);
            animationStrength = Mathf.Max(0f, animationStrength);
            animationFrequency = Mathf.Max(0.0001f, animationFrequency);
            flatColorBrightness =
                Mathf.Clamp(flatColorBrightness, 0f, 2f);
            mainLightStrength = Mathf.Clamp(mainLightStrength, 0f, 2f);
            pointLightStrength =
                Mathf.Clamp(pointLightStrength, 0f, 4f);
            lightingDepth = Mathf.Clamp01(lightingDepth);
            localEmissionMultiplier =
                Mathf.Clamp(localEmissionMultiplier, 0f, 10f);
            ApplyGeneratedMaterialSettings();
        }

        public void ShowOriginalGaussian()
        {
            displayMode = DisplayMode.OriginalGaussian;
            ApplyDisplayState();
        }

        public void ShowCubes()
        {
            displayMode = DisplayMode.Cubes;
            EnsureCurrentModeIsBuilt();
        }

        public void ShowSpheres()
        {
            displayMode = DisplayMode.Spheres;
            EnsureCurrentModeIsBuilt();
        }

        public void Rebuild()
        {
            if (displayMode == DisplayMode.OriginalGaussian)
            {
                ApplyDisplayState();
                return;
            }

            var asset = ResolveSourceAsset();
            if (asset == null || asset.Positions == null || asset.Colors == null)
            {
                Debug.LogError("GsplatShapeVisualizer requires a valid Gsplat Asset with position and color data.", this);
                ShowOriginalGaussian();
                return;
            }

            ClearGeneratedShapes();

            var material = ResolveMaterial();
            if (material == null)
            {
                ShowOriginalGaussian();
                return;
            }

            var samples = BuildSamples(asset);
            if (samples.Count == 0)
            {
                Debug.LogWarning("No visible Gaussian points passed the current sampling/opacity settings.", this);
                ShowOriginalGaussian();
                return;
            }

            CreateGeneratedRoot();
            var template = displayMode == DisplayMode.Cubes
                ? CreateCubeTemplate()
                : CreateSphereTemplate(sphereSegments, sphereRings);

            BuildMeshChunks(samples, template, material);
            builtMode = displayMode;
            generatedShapeCount = samples.Count;
            animationBounds = asset.Bounds;
            RestartAnimation();
            ApplyDisplayState();

            Debug.Log(
                $"GsplatShapeVisualizer built {generatedShapeCount:N0} {displayMode} from {asset.SplatCount:N0} Gaussians.",
                this);
        }

        public void ClearGeneratedShapes()
        {
            generatedShapeCount = 0;
            dynamicMeshChunks.Clear();
            animationWasApplied = false;

            if (generatedRoot != null)
            {
                var filters = generatedRoot.GetComponentsInChildren<MeshFilter>(true);
                foreach (var filter in filters)
                    if (filter != null && filter.sharedMesh != null)
                        DestroyGeneratedObject(filter.sharedMesh);

                DestroyGeneratedObject(generatedRoot.gameObject);
                generatedRoot = null;
            }

            if (ownsGeneratedMaterial && generatedMaterial != null)
                DestroyGeneratedObject(generatedMaterial);

            generatedMaterial = null;
            ownsGeneratedMaterial = false;
        }

        public void RestartAnimation()
        {
            animationStartTime = GetCurrentTime();
            lastAnimationUpdateTime = float.NegativeInfinity;
            animationWasApplied = false;
        }

        void EnsureCurrentModeIsBuilt()
        {
            if (generatedRoot == null || builtMode != displayMode)
                Rebuild();
            else
                ApplyDisplayState();
        }

        GsplatAsset ResolveSourceAsset()
        {
            if (sourceAsset != null)
                return sourceAsset;

            if (sourceRenderer != null)
                return sourceRenderer.GsplatAsset;

            return null;
        }

        void CaptureOriginalRendererState()
        {
            if (originalStateCaptured || sourceRenderer == null)
                return;

            originalRendererWasEnabled = sourceRenderer.enabled;
            originalStateCaptured = true;
        }

        void RestoreOriginalRendererState()
        {
            if (!originalStateCaptured || sourceRenderer == null)
                return;

            sourceRenderer.enabled = originalRendererWasEnabled;
            originalStateCaptured = false;
        }

        void ApplyDisplayState()
        {
            CaptureOriginalRendererState();

            bool generatedModeIsReady =
                generatedRoot != null &&
                generatedShapeCount > 0 &&
                builtMode == displayMode;

            // Never hide the valid Gaussian renderer before a replacement mesh exists.
            bool showOriginal =
                displayMode == DisplayMode.OriginalGaussian ||
                !generatedModeIsReady;

            if (sourceRenderer != null)
                sourceRenderer.enabled = showOriginal;

            if (generatedRoot != null)
                generatedRoot.gameObject.SetActive(!showOriginal && generatedModeIsReady);
        }

        Material ResolveMaterial()
        {
            if (overrideMaterial != null)
            {
                generatedMaterial = overrideMaterial;
                ownsGeneratedMaterial = false;
                return generatedMaterial;
            }

            var shader = Shader.Find("GsplatExtensions/URP Shape Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError(
                    "Could not find the Gsplat Shape URP shader or URP Particles/Unlit. Assign an override material that reads vertex colors.",
                    this);
                return null;
            }

            generatedMaterial = CreateGeneratedMaterial(shader);
            ownsGeneratedMaterial = true;
            ApplyGeneratedMaterialSettings();
            return generatedMaterial;
        }

        Material CreateGeneratedMaterial(Shader shader)
        {
            var material = new Material(shader)
            {
                name = "Gsplat Shapes (Runtime)",
                hideFlags = HideFlags.DontSave
            };
            material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            return material;
        }

        public static void RefreshAllLightingMaterials()
        {
            var visualizers =
                FindObjectsOfType<GsplatShapeVisualizer>(true);
            for (int i = 0; i < visualizers.Length; i++)
                visualizers[i].RefreshLightingMaterial();
        }

        public void RefreshLightingMaterial()
        {
            if (overrideMaterial != null || generatedRoot == null)
                return;

            Shader shader =
                Shader.Find("GsplatExtensions/URP Shape Lit");
            if (shader == null)
                return;

            if (generatedMaterial != null &&
                generatedMaterial.shader == shader)
            {
                ApplyGeneratedMaterialSettings();
                return;
            }

            Material previousMaterial =
                ownsGeneratedMaterial ? generatedMaterial : null;
            generatedMaterial = CreateGeneratedMaterial(shader);
            ownsGeneratedMaterial = true;
            ApplyGeneratedMaterialSettings();

            var renderers =
                generatedRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = generatedMaterial;

            if (previousMaterial != null)
                DestroyGeneratedObject(previousMaterial);
        }

        void ApplyGeneratedMaterialSettings()
        {
            if (!ownsGeneratedMaterial || generatedMaterial == null)
                return;

            if (generatedMaterial.HasProperty("_FlatColorBrightness"))
                generatedMaterial.SetFloat(
                    "_FlatColorBrightness",
                    flatColorBrightness);
            if (generatedMaterial.HasProperty("_DirectionalLightStrength"))
                generatedMaterial.SetFloat(
                    "_DirectionalLightStrength",
                    useDirectionalLight ? mainLightStrength : 0f);
            if (generatedMaterial.HasProperty("_PointLightStrength"))
                generatedMaterial.SetFloat(
                    "_PointLightStrength",
                    useUnityPointLights ? pointLightStrength : 0f);
            if (generatedMaterial.HasProperty("_LightingDepth"))
                generatedMaterial.SetFloat(
                    "_LightingDepth",
                    lightingDepth);
            if (generatedMaterial.HasProperty("_EmissionMultiplier"))
                generatedMaterial.SetFloat(
                    "_EmissionMultiplier",
                    localEmissionMultiplier);
        }

        void CreateGeneratedRoot()
        {
            var rootObject = new GameObject("Generated Gsplat Shapes")
            {
                hideFlags = HideFlags.DontSave
            };

            generatedRoot = rootObject.transform;
            var parent = sourceRenderer != null ? sourceRenderer.transform : transform;
            generatedRoot.SetParent(parent, false);
            rootObject.layer = parent.gameObject.layer;
        }

        List<ShapeSample> BuildSamples(GsplatAsset asset)
        {
            int count = Mathf.Min(asset.Positions.Length, asset.Colors.Length);
            int step = Mathf.Max(1, sampleStep);

            if (voxelSize <= 0f)
            {
                step = Mathf.Max(step, Mathf.CeilToInt(count / (float)maxShapes));
                var direct = new List<ShapeSample>(Mathf.Min(maxShapes, Mathf.CeilToInt(count / (float)step)));

                for (int i = 0; i < count && direct.Count < maxShapes; i += step)
                {
                    if (asset.Colors[i].w < minimumOpacity)
                        continue;

                    direct.Add(CreateSample(asset, i, asset.Positions[i], asset.Colors[i]));
                }

                return direct;
            }

            float inverseVoxelSize = 1f / voxelSize;
            var voxels = new Dictionary<Vector3Int, VoxelAccumulator>();

            for (int i = 0; i < count; i += step)
            {
                Vector4 rawColor = asset.Colors[i];
                if (rawColor.w < minimumOpacity)
                    continue;

                Vector3 position = asset.Positions[i];
                var key = new Vector3Int(
                    Mathf.FloorToInt(position.x * inverseVoxelSize),
                    Mathf.FloorToInt(position.y * inverseVoxelSize),
                    Mathf.FloorToInt(position.z * inverseVoxelSize));

                voxels.TryGetValue(key, out var accumulator);
                accumulator.PositionSum += position;
                accumulator.ColorSum += rawColor;
                accumulator.ScaleSum += GetSourceScale(asset, i);
                if (accumulator.Count == 0)
                    accumulator.FirstRotation = GetSourceRotation(asset, i);
                accumulator.Count++;
                voxels[key] = accumulator;
            }

            var all = new List<ShapeSample>(voxels.Count);
            foreach (var pair in voxels)
            {
                var accumulator = pair.Value;
                float inverseCount = 1f / accumulator.Count;
                Vector4 rawColor = accumulator.ColorSum * inverseCount;

                all.Add(new ShapeSample
                {
                    Position = accumulator.PositionSum * inverseCount,
                    Scale = ResolveShapeScale(accumulator.ScaleSum * inverseCount),
                    Rotation = useGaussianTransform ? accumulator.FirstRotation : Quaternion.identity,
                    Color = DecodeBaseColor(rawColor)
                });
            }

            if (all.Count <= maxShapes)
                return all;

            int voxelStep = Mathf.CeilToInt(all.Count / (float)maxShapes);
            var limited = new List<ShapeSample>(maxShapes);
            for (int i = 0; i < all.Count && limited.Count < maxShapes; i += voxelStep)
                limited.Add(all[i]);
            return limited;
        }

        ShapeSample CreateSample(GsplatAsset asset, int index, Vector3 position, Vector4 rawColor)
        {
            return new ShapeSample
            {
                Position = position,
                Scale = ResolveShapeScale(GetSourceScale(asset, index)),
                Rotation = useGaussianTransform ? GetSourceRotation(asset, index) : Quaternion.identity,
                Color = DecodeBaseColor(rawColor)
            };
        }

        Vector3 GetSourceScale(GsplatAsset asset, int index)
        {
            if (asset.Scales != null && index < asset.Scales.Length)
                return asset.Scales[index];
            return Vector3.one * shapeSize;
        }

        Quaternion GetSourceRotation(GsplatAsset asset, int index)
        {
            if (asset.Rotations == null || index >= asset.Rotations.Length)
                return Quaternion.identity;

            // Imported Gaussian quaternion layout is (w, x, y, z).
            Vector4 q = asset.Rotations[index];
            var rotation = new Quaternion(q.y, q.z, q.w, q.x);
            return Quaternion.Dot(rotation, rotation) > 0.000001f
                ? Quaternion.Normalize(rotation)
                : Quaternion.identity;
        }

        Vector3 ResolveShapeScale(Vector3 sourceScale)
        {
            if (!useGaussianTransform)
                return Vector3.one * shapeSize;

            return new Vector3(
                Mathf.Max(0.0001f, sourceScale.x * gaussianScaleMultiplier),
                Mathf.Max(0.0001f, sourceScale.y * gaussianScaleMultiplier),
                Mathf.Max(0.0001f, sourceScale.z * gaussianScaleMultiplier));
        }

        Color DecodeBaseColor(Vector4 rawColor)
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

        void TickAnimation(float now)
        {
            if (generatedRoot == null ||
                generatedShapeCount == 0 ||
                displayMode == DisplayMode.OriginalGaussian ||
                dynamicMeshChunks.Count == 0)
                return;

            bool hasVolumeInfluence =
                GsplatShapeInfluenceVolume.HasActiveVolumesFor(this);

            if (!animateShapes && !hasVolumeInfluence)
            {
                if (animationWasApplied)
                    RestoreBaseMeshVertices();
                return;
            }

            float interval = 1f / Mathf.Max(1f, animationFramesPerSecond);
            if (now - lastAnimationUpdateTime < interval)
                return;

            lastAnimationUpdateTime = now;
            AnimateMeshVertices(
                now - animationStartTime,
                now,
                hasVolumeInfluence);
            animationWasApplied = true;
        }

        void AnimateMeshVertices(
            float animationTime,
            float absoluteTime,
            bool applyVolumes)
        {
            ShapeAnimationMode mode = animateShapes
                ? ResolveAnimationMode()
                : ShapeAnimationMode.None;
            bool hasGlobalAnimation =
                animateShapes && mode != ShapeAnimationMode.None;

            if (!hasGlobalAnimation && !applyVolumes)
            {
                RestoreBaseMeshVertices();
                return;
            }

            float strength = animationStrength;
            float speed = animationSpeed;
            float frequency = animationFrequency;

            if (hasGlobalAnimation &&
                followSourceGsplatEffect &&
                sourceRenderer != null)
            {
                strength = Mathf.Max(0.0001f, sourceRenderer.intensity);
                speed = Mathf.Max(0.0001f, sourceRenderer.waveSpeed);
                frequency = Mathf.Max(0.0001f, sourceRenderer.waveFrequency);
            }

            foreach (var chunk in dynamicMeshChunks)
            {
                int vertexCursor = 0;

                for (int shape = 0; shape < chunk.BaseCenters.Length; shape++)
                {
                    Vector3 baseCenter = chunk.BaseCenters[shape];
                    Vector3 animatedCenter = hasGlobalAnimation
                        ? AnimateCenter(
                            baseCenter,
                            mode,
                            animationTime,
                            strength,
                            speed,
                            frequency)
                        : baseCenter;

                    if (applyVolumes)
                    {
                        Vector3 worldPosition =
                            generatedRoot.TransformPoint(animatedCenter);
                        worldPosition =
                            GsplatShapeInfluenceVolume.ApplyAll(
                                this,
                                worldPosition,
                                absoluteTime);
                        animatedCenter =
                            generatedRoot.InverseTransformPoint(worldPosition);
                    }

                    Vector3 delta = animatedCenter - baseCenter;

                    for (int vertex = 0; vertex < chunk.VerticesPerShape; vertex++)
                    {
                        chunk.AnimatedVertices[vertexCursor] =
                            chunk.BaseVertices[vertexCursor] + delta;
                        vertexCursor++;
                    }
                }

                chunk.Mesh.SetVertices(
                    chunk.AnimatedVertices,
                    0,
                    chunk.AnimatedVertices.Length,
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontValidateIndices |
                    MeshUpdateFlags.DontNotifyMeshUsers);
            }
        }

        Vector3 AnimateCenter(
            Vector3 position,
            ShapeAnimationMode mode,
            float time,
            float strength,
            float speed,
            float frequency)
        {
            Vector3 center = animationBounds.center;
            Vector3 local = position - center;
            float phase = Hash01(position) * Mathf.PI * 2f;

            switch (mode)
            {
                case ShapeAnimationMode.Twist:
                {
                    float angle = Mathf.Sin(
                        time * speed + local.y * frequency) * strength;
                    float sine = Mathf.Sin(angle);
                    float cosine = Mathf.Cos(angle);
                    local = new Vector3(
                        local.x * cosine - local.z * sine,
                        local.y,
                        local.x * sine + local.z * cosine);
                    return center + local;
                }

                case ShapeAnimationMode.Swirl:
                {
                    float radius = new Vector2(local.x, local.z).magnitude;
                    float angle = Mathf.Sin(
                        radius * frequency - time * speed) * strength;
                    float sine = Mathf.Sin(angle);
                    float cosine = Mathf.Cos(angle);
                    local = new Vector3(
                        local.x * cosine - local.z * sine,
                        local.y + Mathf.Sin(radius * frequency - time * speed) * strength * 0.2f,
                        local.x * sine + local.z * cosine);
                    return center + local;
                }

                case ShapeAnimationMode.Rain:
                {
                    float height = Mathf.Max(0.0001f, animationBounds.size.y);
                    float fallDistance = Mathf.Repeat(
                        position.y - animationBounds.min.y - time * speed - phase * strength,
                        height);
                    Vector3 rainPosition = position;
                    rainPosition.y = animationBounds.min.y + fallDistance;
                    rainPosition.x += Mathf.Sin(time * speed + phase) * strength * 0.15f;
                    rainPosition.z += Mathf.Cos(time * speed * 0.8f + phase) * strength * 0.15f;
                    return rainPosition;
                }

                case ShapeAnimationMode.Wind:
                {
                    Vector3 direction = windDirection.sqrMagnitude > 0.000001f
                        ? windDirection.normalized
                        : Vector3.right;
                    float gust = Mathf.Sin(
                        time * speed + Vector3.Dot(local, direction) * frequency + phase);
                    float sway = Mathf.Sin(time * speed * 1.7f + local.y * frequency + phase);
                    return position +
                           direction * gust * strength +
                           Vector3.up * sway * strength * 0.2f;
                }

                case ShapeAnimationMode.Wave:
                {
                    float wave = Mathf.Sin(
                        (local.x + local.z) * frequency + time * speed);
                    return position + Vector3.up * wave * strength;
                }

                case ShapeAnimationMode.Pulse:
                {
                    float scale = 1f + Mathf.Sin(time * speed) * strength;
                    return center + local * Mathf.Max(0.05f, scale);
                }

                case ShapeAnimationMode.Orbit:
                {
                    float angle = time * speed * strength;
                    float sine = Mathf.Sin(angle);
                    float cosine = Mathf.Cos(angle);
                    local = new Vector3(
                        local.x * cosine - local.z * sine,
                        local.y,
                        local.x * sine + local.z * cosine);
                    return center + local;
                }

                default:
                    return position;
            }
        }

        ShapeAnimationMode ResolveAnimationMode()
        {
            if (!followSourceGsplatEffect || sourceRenderer == null)
                return animationMode;

            switch (sourceRenderer.effectType)
            {
                case GsplatEffectType.DeepMeditation:
                case GsplatEffectType.Flare:
                case GsplatEffectType.GlowDissolve:
                case GsplatEffectType.RadialExpansion:
                    return ShapeAnimationMode.Pulse;

                case GsplatEffectType.Waves:
                case GsplatEffectType.PerlinWave:
                    return ShapeAnimationMode.Wave;

                case GsplatEffectType.Disintegrate:
                case GsplatEffectType.Wind:
                case GsplatEffectType.FlyDissolve:
                    return ShapeAnimationMode.Wind;

                case GsplatEffectType.Magic:
                case GsplatEffectType.Unroll:
                case GsplatEffectType.Twister:
                    return ShapeAnimationMode.Twist;

                case GsplatEffectType.Spread:
                case GsplatEffectType.GalaxyGlitter:
                    return ShapeAnimationMode.Swirl;

                case GsplatEffectType.Rain:
                    return ShapeAnimationMode.Rain;

                default:
                    return ShapeAnimationMode.None;
            }
        }

        void RestoreBaseMeshVertices()
        {
            foreach (var chunk in dynamicMeshChunks)
            {
                Array.Copy(
                    chunk.BaseVertices,
                    chunk.AnimatedVertices,
                    chunk.BaseVertices.Length);
                chunk.Mesh.SetVertices(
                    chunk.AnimatedVertices,
                    0,
                    chunk.AnimatedVertices.Length,
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontValidateIndices |
                    MeshUpdateFlags.DontNotifyMeshUsers);
            }

            animationWasApplied = false;
        }

        static float Hash01(Vector3 value)
        {
            float hash = Mathf.Sin(
                Vector3.Dot(value, new Vector3(12.9898f, 78.233f, 37.719f))) *
                43758.5453f;
            return hash - Mathf.Floor(hash);
        }

        static float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }

        void BuildMeshChunks(List<ShapeSample> samples, ShapeTemplate template, Material material)
        {
            int verticesPerShape = template.Vertices.Length;
            int instancesPerChunk = Mathf.Max(1, maxVerticesPerMesh / verticesPerShape);
            int chunkCount = Mathf.CeilToInt(samples.Count / (float)instancesPerChunk);

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int first = chunkIndex * instancesPerChunk;
                int instanceCount = Mathf.Min(instancesPerChunk, samples.Count - first);
                int vertexCount = instanceCount * verticesPerShape;
                int indexCount = instanceCount * template.Indices.Length;

                var vertices = new List<Vector3>(vertexCount);
                var colors = new List<Color>(vertexCount);
                var indices = new List<int>(indexCount);
                var baseCenters = new Vector3[instanceCount];

                for (int instance = 0; instance < instanceCount; instance++)
                {
                    ShapeSample sample = samples[first + instance];
                    baseCenters[instance] = sample.Position;
                    int vertexOffset = vertices.Count;

                    for (int vertex = 0; vertex < verticesPerShape; vertex++)
                    {
                        Vector3 local = Vector3.Scale(template.Vertices[vertex], sample.Scale);
                        vertices.Add(sample.Position + sample.Rotation * local);
                        colors.Add(sample.Color);
                    }

                    for (int index = 0; index < template.Indices.Length; index++)
                        indices.Add(vertexOffset + template.Indices[index]);
                }

                var mesh = new Mesh
                {
                    name = $"Gsplat {displayMode} {chunkIndex + 1}",
                    indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                    hideFlags = HideFlags.DontSave
                };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(indices, 0, true);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var expandedBounds = mesh.bounds;
                expandedBounds.Expand(Mathf.Max(10f, animationStrength * 10f));
                mesh.bounds = expandedBounds;

                var chunk = new GameObject($"Chunk {chunkIndex + 1:D3}")
                {
                    hideFlags = HideFlags.DontSave,
                    layer = generatedRoot.gameObject.layer
                };
                chunk.transform.SetParent(generatedRoot, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                chunk.AddComponent<MeshRenderer>().sharedMaterial = material;

                Vector3[] baseVertices = vertices.ToArray();
                dynamicMeshChunks.Add(new DynamicMeshChunk
                {
                    Mesh = mesh,
                    BaseVertices = baseVertices,
                    AnimatedVertices = (Vector3[])baseVertices.Clone(),
                    BaseCenters = baseCenters,
                    VerticesPerShape = verticesPerShape
                });
            }
        }

        static ShapeTemplate CreateCubeTemplate()
        {
            return new ShapeTemplate
            {
                Vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3( 0.5f, -0.5f, -0.5f),
                    new Vector3( 0.5f,  0.5f, -0.5f),
                    new Vector3(-0.5f,  0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f,  0.5f),
                    new Vector3( 0.5f, -0.5f,  0.5f),
                    new Vector3( 0.5f,  0.5f,  0.5f),
                    new Vector3(-0.5f,  0.5f,  0.5f)
                },
                Indices = new[]
                {
                    0, 2, 1, 0, 3, 2,
                    1, 2, 6, 1, 6, 5,
                    5, 6, 7, 5, 7, 4,
                    4, 7, 3, 4, 3, 0,
                    3, 7, 6, 3, 6, 2,
                    4, 0, 1, 4, 1, 5
                }
            };
        }

        static ShapeTemplate CreateSphereTemplate(int segments, int rings)
        {
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var indices = new int[rings * segments * 6];
            int vertexIndex = 0;

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = ring / (float)rings;
                float latitude = v * Mathf.PI;
                float y = Mathf.Cos(latitude) * 0.5f;
                float radius = Mathf.Sin(latitude) * 0.5f;

                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float longitude = u * Mathf.PI * 2f;
                    vertices[vertexIndex++] = new Vector3(
                        Mathf.Cos(longitude) * radius,
                        y,
                        Mathf.Sin(longitude) * radius);
                }
            }

            int index = 0;
            int rowLength = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int a = ring * rowLength + segment;
                    int b = a + rowLength;
                    indices[index++] = a;
                    indices[index++] = a + 1;
                    indices[index++] = b;
                    indices[index++] = a + 1;
                    indices[index++] = b + 1;
                    indices[index++] = b;
                }
            }

            return new ShapeTemplate { Vertices = vertices, Indices = indices };
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
    [CustomEditor(typeof(GsplatShapeVisualizer))]
    sealed class GsplatShapeVisualizerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var visualizer = (GsplatShapeVisualizer)target;
            var gaussianTransformProperty =
                serializedObject.FindProperty("useGaussianTransform");

            if (gaussianTransformProperty != null &&
                gaussianTransformProperty.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Use Gaussian Transform preserves every splat's anisotropic scale and rotation. " +
                    "Most Gaussian splats are very thin ellipsoids, so generated cubes/spheres can look like " +
                    "flat plates or fragments. Turn it off for uniform solid shapes.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Display Actions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The generated shapes automatically follow the Source Renderer's position, rotation and scale. " +
                "Dynamic Shape Animation runs continuously in both Scene and Game view and does not require rebuilding.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Original Gaussian", GUILayout.Height(28f)))
                    InvokeWithUndo(visualizer, "Show Original Gaussian", visualizer.ShowOriginalGaussian);

                if (GUILayout.Button("Cubes", GUILayout.Height(28f)))
                    InvokeWithUndo(visualizer, "Show Gsplat Cubes", visualizer.ShowCubes);

                if (GUILayout.Button("Spheres", GUILayout.Height(28f)))
                    InvokeWithUndo(visualizer, "Show Gsplat Spheres", visualizer.ShowSpheres);
            }

            if (GUILayout.Button("Apply Settings / Rebuild", GUILayout.Height(32f)))
                InvokeWithUndo(visualizer, "Rebuild Gsplat Shapes", visualizer.Rebuild);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Restart Animation"))
                    InvokeWithUndo(visualizer, "Restart Gsplat Shape Animation", visualizer.RestartAnimation);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Generated Shapes"))
                    InvokeWithUndo(visualizer, "Clear Gsplat Shapes", visualizer.ClearGeneratedShapes);
            }

            EditorGUILayout.LabelField(
                "Generated Shapes",
                visualizer.GeneratedShapeCount.ToString("N0"));
        }

        static void InvokeWithUndo(
            GsplatShapeVisualizer visualizer,
            string undoName,
            Action action)
        {
            Undo.RecordObject(visualizer, undoName);
            action();
            EditorUtility.SetDirty(visualizer);
        }
    }
#endif
}
