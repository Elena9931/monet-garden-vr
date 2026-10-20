using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GsplatExtensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GsplatShapeInfluenceVolume : MonoBehaviour
    {
        public enum VolumeShape
        {
            Sphere,
            Box
        }

        public enum LocalEffect
        {
            OutwardPush,
            InwardPull,
            Twist,
            Vortex,
            Explosion,
            Implosion,
            Turbulence
        }

        [Header("Target")]
        [Tooltip("Optional. Assign one visualizer to avoid affecting other generated shape clouds. Leave empty to affect all active visualizers.")]
        [SerializeField] GsplatShapeVisualizer targetVisualizer;

        [Header("Volume")]
        [Tooltip("Use a SphereCollider or BoxCollider on this GameObject as the influence boundary.")]
        [SerializeField] bool useAttachedCollider = true;

        [SerializeField] VolumeShape volumeShape = VolumeShape.Sphere;
        [SerializeField] Vector3 localCenter;

        [Min(0.0001f)]
        [SerializeField] float sphereRadius = 0.5f;

        [SerializeField] Vector3 boxSize = Vector3.one;

        [Tooltip("Disable the Renderer on this GameObject while the influence component is enabled. The Scene gizmo remains visible.")]
        [SerializeField] bool hideAttachedRenderer = true;

        [Header("Local Effect")]
        [SerializeField] LocalEffect effect = LocalEffect.Explosion;

        [Tooltip("World-space displacement strength. Twist interprets this as turns of approximately 90 degrees.")]
        [Min(0f)]
        [SerializeField] float strength = 1f;

        [Tooltip("Higher values keep the effect concentrated near the volume center.")]
        [Min(0.01f)]
        [SerializeField] float falloffPower = 2f;

        [SerializeField] bool animateEffect = true;
        [SerializeField] bool loopAnimation = true;

        [Min(0.01f)]
        [SerializeField] float animationSpeed = 1f;

        [Min(0.01f)]
        [SerializeField] float animationFrequency = 1f;

        [Min(0.05f)]
        [SerializeField] float burstDuration = 1.25f;

        static readonly List<GsplatShapeInfluenceVolume> ActiveVolumes =
            new List<GsplatShapeInfluenceVolume>();

        Renderer attachedRenderer;
        SphereCollider cachedSphereCollider;
        BoxCollider cachedBoxCollider;
        bool rendererStateCaptured;
        bool rendererWasEnabled;
        float burstStartTime;

        public static bool HasActiveVolumesFor(GsplatShapeVisualizer visualizer)
        {
            for (int i = 0; i < ActiveVolumes.Count; i++)
            {
                var volume = ActiveVolumes[i];
                if (volume != null &&
                    volume.isActiveAndEnabled &&
                    (volume.targetVisualizer == null ||
                     volume.targetVisualizer == visualizer))
                    return true;
            }

            return false;
        }

        public static Vector3 ApplyAll(
            GsplatShapeVisualizer visualizer,
            Vector3 worldPosition,
            float absoluteTime)
        {
            Vector3 result = worldPosition;

            for (int i = 0; i < ActiveVolumes.Count; i++)
            {
                var volume = ActiveVolumes[i];
                if (volume == null ||
                    !volume.isActiveAndEnabled ||
                    (volume.targetVisualizer != null &&
                     volume.targetVisualizer != visualizer))
                    continue;

                result = volume.Apply(result, absoluteTime);
            }

            return result;
        }

        void OnEnable()
        {
            CacheComponents();
            if (!ActiveVolumes.Contains(this))
                ActiveVolumes.Add(this);

            CaptureAndApplyRendererState();
            TriggerBurst();
        }

        void OnDisable()
        {
            ActiveVolumes.Remove(this);
            RestoreRendererState();
        }

        void OnDestroy()
        {
            ActiveVolumes.Remove(this);
            RestoreRendererState();
        }

        void OnValidate()
        {
            sphereRadius = Mathf.Max(0.0001f, sphereRadius);
            boxSize = new Vector3(
                Mathf.Max(0.0001f, boxSize.x),
                Mathf.Max(0.0001f, boxSize.y),
                Mathf.Max(0.0001f, boxSize.z));
            strength = Mathf.Max(0f, strength);
            falloffPower = Mathf.Max(0.01f, falloffPower);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);
            animationFrequency = Mathf.Max(0.01f, animationFrequency);
            burstDuration = Mathf.Max(0.05f, burstDuration);

            CacheComponents();
            if (isActiveAndEnabled)
                CaptureAndApplyRendererState();
        }

        void CacheComponents()
        {
            if (cachedSphereCollider == null)
                cachedSphereCollider = GetComponent<SphereCollider>();
            if (cachedBoxCollider == null)
                cachedBoxCollider = GetComponent<BoxCollider>();
            if (attachedRenderer == null)
                attachedRenderer = GetComponent<Renderer>();
        }

        public void TriggerBurst()
        {
            burstStartTime = GetCurrentTime();
        }

        Vector3 Apply(Vector3 worldPosition, float absoluteTime)
        {
            if (!TryGetNormalizedDistance(worldPosition, out float normalizedDistance))
                return worldPosition;

            float falloff = Mathf.Pow(
                Mathf.Clamp01(1f - normalizedDistance),
                falloffPower);

            if (falloff <= 0f || strength <= 0f)
                return worldPosition;

            Vector3 center = GetWorldCenter();
            Vector3 radial = worldPosition - center;
            Vector3 direction = radial.sqrMagnitude > 0.000001f
                ? radial.normalized
                : transform.up;

            float motion = GetMotionMultiplier(normalizedDistance, absoluteTime);
            float amount = strength * falloff * motion;

            switch (effect)
            {
                case LocalEffect.OutwardPush:
                    return worldPosition + direction * amount;

                case LocalEffect.InwardPull:
                    return worldPosition - direction * amount;

                case LocalEffect.Twist:
                {
                    float angle = amount * 90f;
                    Vector3 twisted = Quaternion.AngleAxis(
                        angle,
                        transform.up) * radial;
                    return center + twisted;
                }

                case LocalEffect.Vortex:
                {
                    Vector3 tangent = Vector3.Cross(
                        transform.up,
                        direction);
                    if (tangent.sqrMagnitude < 0.000001f)
                        tangent = transform.right;
                    return worldPosition + tangent.normalized * amount;
                }

                case LocalEffect.Explosion:
                    return worldPosition + direction * amount;

                case LocalEffect.Implosion:
                    return worldPosition - direction * amount;

                case LocalEffect.Turbulence:
                {
                    Vector3 local = transform.InverseTransformPoint(worldPosition);
                    float t = animateEffect ? absoluteTime * animationSpeed : 0f;
                    Vector3 noise = new Vector3(
                        Mathf.Sin(local.y * animationFrequency + t * 1.17f),
                        Mathf.Sin(local.z * animationFrequency + t * 1.43f),
                        Mathf.Sin(local.x * animationFrequency + t * 1.79f));
                    return worldPosition + transform.TransformDirection(noise) * amount;
                }

                default:
                    return worldPosition;
            }
        }

        float GetMotionMultiplier(float normalizedDistance, float absoluteTime)
        {
            if (!animateEffect)
                return 1f;

            bool burstEffect =
                effect == LocalEffect.Explosion ||
                effect == LocalEffect.Implosion;

            if (burstEffect)
            {
                float normalizedTime;
                if (loopAnimation)
                {
                    normalizedTime = Mathf.Repeat(
                        absoluteTime * animationSpeed,
                        1f);
                }
                else
                {
                    normalizedTime =
                        (absoluteTime - burstStartTime) / burstDuration;
                    if (normalizedTime < 0f || normalizedTime > 1f)
                        return 0f;
                }

                float waveFront = normalizedTime * 1.35f;
                float distanceFromFront = Mathf.Abs(
                    normalizedDistance - waveFront);
                return Mathf.Pow(
                    Mathf.Clamp01(1f - distanceFromFront * 5f),
                    2f);
            }

            return 0.65f +
                   0.35f * Mathf.Sin(
                       absoluteTime * animationSpeed * Mathf.PI * 2f);
        }

        bool TryGetNormalizedDistance(
            Vector3 worldPosition,
            out float normalizedDistance)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPosition);

            if (useAttachedCollider)
            {
                if (cachedSphereCollider != null)
                {
                    normalizedDistance =
                        Vector3.Distance(localPoint, cachedSphereCollider.center) /
                        Mathf.Max(0.0001f, cachedSphereCollider.radius);
                    return normalizedDistance <= 1f;
                }

                if (cachedBoxCollider != null)
                {
                    normalizedDistance = GetBoxNormalizedDistance(
                        localPoint - cachedBoxCollider.center,
                        cachedBoxCollider.size);
                    return normalizedDistance <= 1f;
                }
            }

            if (volumeShape == VolumeShape.Sphere)
            {
                normalizedDistance =
                    Vector3.Distance(localPoint, localCenter) /
                    sphereRadius;
                return normalizedDistance <= 1f;
            }

            normalizedDistance = GetBoxNormalizedDistance(
                localPoint - localCenter,
                boxSize);
            return normalizedDistance <= 1f;
        }

        static float GetBoxNormalizedDistance(
            Vector3 centeredPoint,
            Vector3 size)
        {
            Vector3 halfSize = size * 0.5f;
            return Mathf.Max(
                Mathf.Abs(centeredPoint.x) /
                Mathf.Max(0.0001f, halfSize.x),
                Mathf.Max(
                    Mathf.Abs(centeredPoint.y) /
                    Mathf.Max(0.0001f, halfSize.y),
                    Mathf.Abs(centeredPoint.z) /
                    Mathf.Max(0.0001f, halfSize.z)));
        }

        Vector3 GetWorldCenter()
        {
            if (useAttachedCollider)
            {
                if (cachedSphereCollider != null)
                    return transform.TransformPoint(cachedSphereCollider.center);

                if (cachedBoxCollider != null)
                    return transform.TransformPoint(cachedBoxCollider.center);
            }

            return transform.TransformPoint(localCenter);
        }

        void CaptureAndApplyRendererState()
        {
            CacheComponents();

            if (attachedRenderer == null)
                return;

            if (!rendererStateCaptured)
            {
                rendererWasEnabled = attachedRenderer.enabled;
                rendererStateCaptured = true;
            }

            if (hideAttachedRenderer)
                attachedRenderer.enabled = false;
            else
                attachedRenderer.enabled = rendererWasEnabled;
        }

        void RestoreRendererState()
        {
            if (!rendererStateCaptured || attachedRenderer == null)
                return;

            attachedRenderer.enabled = rendererWasEnabled;
            rendererStateCaptured = false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = GetGizmoColor();
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (TryGetSphereGizmo(out Vector3 center, out float radius))
                Gizmos.DrawWireSphere(center, radius);
            else if (TryGetBoxGizmo(out center, out Vector3 size))
                Gizmos.DrawWireCube(center, size);

            Gizmos.matrix = previousMatrix;
        }

        bool TryGetSphereGizmo(out Vector3 center, out float radius)
        {
            if (useAttachedCollider)
            {
                if (cachedSphereCollider != null)
                {
                    center = cachedSphereCollider.center;
                    radius = cachedSphereCollider.radius;
                    return true;
                }

                if (cachedBoxCollider != null)
                {
                    center = default;
                    radius = 0f;
                    return false;
                }
            }

            center = localCenter;
            radius = sphereRadius;
            return volumeShape == VolumeShape.Sphere;
        }

        bool TryGetBoxGizmo(out Vector3 center, out Vector3 size)
        {
            if (useAttachedCollider)
            {
                if (cachedBoxCollider != null)
                {
                    center = cachedBoxCollider.center;
                    size = cachedBoxCollider.size;
                    return true;
                }
            }

            center = localCenter;
            size = boxSize;
            return volumeShape == VolumeShape.Box;
        }

        Color GetGizmoColor()
        {
            switch (effect)
            {
                case LocalEffect.InwardPull:
                case LocalEffect.Implosion:
                    return new Color(0.2f, 0.65f, 1f, 0.9f);
                case LocalEffect.Twist:
                case LocalEffect.Vortex:
                    return new Color(0.75f, 0.35f, 1f, 0.9f);
                case LocalEffect.Turbulence:
                    return new Color(1f, 0.75f, 0.2f, 0.9f);
                default:
                    return new Color(1f, 0.25f, 0.15f, 0.9f);
            }
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
    [CustomEditor(typeof(GsplatShapeInfluenceVolume))]
    sealed class GsplatShapeInfluenceVolumeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Move, rotate or scale this volume in the Scene view. Only generated shape points inside the wireframe are affected.",
                MessageType.Info);

            var volume = (GsplatShapeInfluenceVolume)target;
            if (GUILayout.Button("Trigger Burst", GUILayout.Height(28f)))
            {
                Undo.RecordObject(volume, "Trigger Gsplat Shape Burst");
                volume.TriggerBurst();
                EditorUtility.SetDirty(volume);
            }
        }
    }
#endif
}
