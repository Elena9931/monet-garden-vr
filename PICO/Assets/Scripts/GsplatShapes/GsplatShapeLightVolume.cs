using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GsplatExtensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GsplatShapeLightVolume : MonoBehaviour
    {
        public enum VolumeShape
        {
            Sphere,
            Box
        }

        public enum LightMode
        {
            SurfaceLight,
            Emission,
            SurfaceAndEmission
        }

        const int MaxVolumes = 8;
        const string CountProperty = "_GsplatShapeLightCount";
        const string WorldToVolumeProperty = "_GsplatShapeLightWorldToVolume";
        const string CenterProperty = "_GsplatShapeLightCenter";
        const string ColorProperty = "_GsplatShapeLightColor";
        const string ParamsProperty = "_GsplatShapeLightParams";

        static readonly List<GsplatShapeLightVolume> ActiveVolumes =
            new List<GsplatShapeLightVolume>(MaxVolumes);
        static readonly Matrix4x4[] WorldToVolume = new Matrix4x4[MaxVolumes];
        static readonly Vector4[] LightCenters = new Vector4[MaxVolumes];
        static readonly Vector4[] LightColors = new Vector4[MaxVolumes];
        static readonly Vector4[] LightParams = new Vector4[MaxVolumes];

        [Header("Volume")]
        [Tooltip("Use a SphereCollider or BoxCollider on this GameObject as the lighting boundary.")]
        [SerializeField] bool useAttachedCollider = true;
        [SerializeField] VolumeShape volumeShape = VolumeShape.Sphere;
        [SerializeField] Vector3 localCenter;
        [Min(0.0001f)]
        [SerializeField] float sphereRadius = 0.5f;
        [SerializeField] Vector3 boxSize = Vector3.one;
        [SerializeField] bool hideAttachedRenderer = true;

        [Header("Particle Lighting")]
        [ColorUsage(true, true)]
        [SerializeField] Color lightColor = new Color(1f, 0.35f, 0.08f, 1f);
        [Min(0f)]
        [SerializeField] float intensity = 3f;
        [Min(0.01f)]
        [SerializeField] float falloffPower = 2f;
        [SerializeField] LightMode lightMode = LightMode.SurfaceAndEmission;

        [Header("Animation")]
        [SerializeField] bool pulse;
        [Min(0f)]
        [SerializeField] float pulseAmount = 0.5f;
        [Min(0.01f)]
        [SerializeField] float pulseSpeed = 1f;
        [SerializeField] bool previewInEditMode = true;

        SphereCollider cachedSphereCollider;
        BoxCollider cachedBoxCollider;
        Renderer cachedRenderer;
        bool rendererStateCaptured;
        bool rendererWasEnabled;

        void OnEnable()
        {
            CacheComponents();
            if (!ActiveVolumes.Contains(this))
                ActiveVolumes.Add(this);

            GsplatShapeVisualizer.RefreshAllLightingMaterials();
            ApplyRendererVisibility();
            UploadAll(GetCurrentTime());

#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
#endif
        }

        void OnDisable()
        {
            ActiveVolumes.Remove(this);
            RestoreRendererVisibility();
            UploadAll(GetCurrentTime());

#if UNITY_EDITOR
            if (ActiveVolumes.Count == 0)
                EditorApplication.update -= OnEditorUpdate;
#endif
        }

        void OnDestroy()
        {
            ActiveVolumes.Remove(this);
            RestoreRendererVisibility();
            UploadAll(GetCurrentTime());
        }

        void OnValidate()
        {
            sphereRadius = Mathf.Max(0.0001f, sphereRadius);
            boxSize = new Vector3(
                Mathf.Max(0.0001f, boxSize.x),
                Mathf.Max(0.0001f, boxSize.y),
                Mathf.Max(0.0001f, boxSize.z));
            intensity = Mathf.Max(0f, intensity);
            falloffPower = Mathf.Max(0.01f, falloffPower);
            pulseAmount = Mathf.Max(0f, pulseAmount);
            pulseSpeed = Mathf.Max(0.01f, pulseSpeed);

            CacheComponents();
            ApplyRendererVisibility();
            UploadAll(GetCurrentTime());
        }

        void Update()
        {
            if (Application.isPlaying &&
                ActiveVolumes.Count > 0 &&
                ActiveVolumes[0] == this)
                UploadAll(Time.time);
        }

#if UNITY_EDITOR
        static void OnEditorUpdate()
        {
            if (Application.isPlaying || ActiveVolumes.Count == 0)
                return;

            UploadAll((float)EditorApplication.timeSinceStartup);

            bool needsRepaint = false;
            for (int i = 0; i < ActiveVolumes.Count; i++)
            {
                var volume = ActiveVolumes[i];
                if (volume != null && volume.pulse && volume.previewInEditMode)
                {
                    needsRepaint = true;
                    break;
                }
            }

            if (needsRepaint)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
#endif

        static void UploadAll(float time)
        {
            int count = 0;
            for (int i = 0; i < ActiveVolumes.Count && count < MaxVolumes; i++)
            {
                var volume = ActiveVolumes[i];
                if (volume == null || !volume.isActiveAndEnabled)
                    continue;

                volume.GetVolumeTransform(
                    out Vector3 center,
                    out Quaternion rotation,
                    out Vector3 size,
                    out bool isBox);

                size.x = Mathf.Max(0.0001f, Mathf.Abs(size.x));
                size.y = Mathf.Max(0.0001f, Mathf.Abs(size.y));
                size.z = Mathf.Max(0.0001f, Mathf.Abs(size.z));

                WorldToVolume[count] =
                    Matrix4x4.TRS(center, rotation, size).inverse;
                LightCenters[count] =
                    new Vector4(center.x, center.y, center.z, 1f);

                float animatedIntensity = volume.intensity;
                if (volume.pulse &&
                    (Application.isPlaying || volume.previewInEditMode))
                {
                    float wave = 0.5f +
                                 0.5f * Mathf.Sin(
                                     time * volume.pulseSpeed *
                                     Mathf.PI * 2f);
                    animatedIntensity *=
                        Mathf.Lerp(1f - volume.pulseAmount, 1f, wave);
                }

                float surface = volume.lightMode == LightMode.Emission ? 0f : 1f;
                float emission = volume.lightMode == LightMode.SurfaceLight ? 0f : 1f;

                Color color =
                    QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? volume.lightColor.linear
                        : volume.lightColor;
                LightColors[count] = new Vector4(color.r, color.g, color.b, 1f);
                LightParams[count] = new Vector4(
                    animatedIntensity,
                    volume.falloffPower,
                    isBox ? 1f : 0f,
                    surface + emission * 2f);
                count++;
            }

            Shader.SetGlobalInt(CountProperty, count);
            Shader.SetGlobalMatrixArray(WorldToVolumeProperty, WorldToVolume);
            Shader.SetGlobalVectorArray(CenterProperty, LightCenters);
            Shader.SetGlobalVectorArray(ColorProperty, LightColors);
            Shader.SetGlobalVectorArray(ParamsProperty, LightParams);
        }

        void GetVolumeTransform(
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 size,
            out bool isBox)
        {
            rotation = transform.rotation;
            Vector3 scale = transform.lossyScale;

            if (useAttachedCollider && cachedSphereCollider != null)
            {
                center = transform.TransformPoint(cachedSphereCollider.center);
                float diameter = cachedSphereCollider.radius * 2f;
                size = Vector3.Scale(scale, Vector3.one * diameter);
                isBox = false;
                return;
            }

            if (useAttachedCollider && cachedBoxCollider != null)
            {
                center = transform.TransformPoint(cachedBoxCollider.center);
                size = Vector3.Scale(scale, cachedBoxCollider.size);
                isBox = true;
                return;
            }

            center = transform.TransformPoint(localCenter);
            if (volumeShape == VolumeShape.Sphere)
            {
                size = Vector3.Scale(
                    scale,
                    Vector3.one * sphereRadius * 2f);
                isBox = false;
            }
            else
            {
                size = Vector3.Scale(scale, boxSize);
                isBox = true;
            }
        }

        void CacheComponents()
        {
            if (cachedSphereCollider == null)
                cachedSphereCollider = GetComponent<SphereCollider>();
            if (cachedBoxCollider == null)
                cachedBoxCollider = GetComponent<BoxCollider>();
            if (cachedRenderer == null)
                cachedRenderer = GetComponent<Renderer>();
        }

        void ApplyRendererVisibility()
        {
            if (cachedRenderer == null)
                return;

            if (!rendererStateCaptured)
            {
                rendererWasEnabled = cachedRenderer.enabled;
                rendererStateCaptured = true;
            }

            cachedRenderer.enabled =
                hideAttachedRenderer ? false : rendererWasEnabled;
        }

        void RestoreRendererVisibility()
        {
            if (!rendererStateCaptured || cachedRenderer == null)
                return;

            cachedRenderer.enabled = rendererWasEnabled;
            rendererStateCaptured = false;
        }

        void OnDrawGizmos()
        {
            CacheComponents();
            GetVolumeTransform(
                out Vector3 center,
                out Quaternion rotation,
                out Vector3 size,
                out bool isBox);

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.color = new Color(
                lightColor.r,
                lightColor.g,
                lightColor.b,
                0.9f);
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, size);

            if (isBox)
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            else
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);

            Gizmos.matrix = previous;
        }

        static float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GsplatShapeLightVolume))]
    sealed class GsplatShapeLightVolumeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "This lights only generated Gsplat cube/sphere meshes using the Gsplat Shape URP shader. Emission becomes a visible glow when URP Bloom is enabled.",
                MessageType.Info);
        }
    }
#endif
}
