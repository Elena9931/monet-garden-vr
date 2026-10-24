using UnityEngine;

namespace MonetGarden
{
    public class GardenViewpoint : MonoBehaviour
    {
        [Tooltip("A proposed composition anchor, not an authenticated historical position.")]
        public float radius = 0.32f;
        public float verticalTolerance = 0.2f, yawTolerance = 10f, pitchTolerance = 8f, holdSeconds = 3f;
        public bool requirePitch = true;
        public float HoldProgress { get; private set; }
        public float Distance { get; private set; }
        public float YawError { get; private set; }
        public float PitchError { get; private set; }
        public bool Aligned { get; private set; }
        public bool Ready => HoldProgress >= 1f;

        public static bool IsAligned(Vector3 eye, Vector3 forward, Vector3 target, Vector3 targetForward,
            float radius, float verticalTolerance, float yawTolerance, float pitchTolerance, bool requirePitch)
        {
            Vector3 delta = eye - target;
            Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up);
            Vector3 desired = Vector3.ProjectOnPlane(targetForward, Vector3.up);
            float pitch = Mathf.Asin(Mathf.Clamp(forward.normalized.y, -1, 1)) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Asin(Mathf.Clamp(targetForward.normalized.y, -1, 1)) * Mathf.Rad2Deg;
            return new Vector2(delta.x, delta.z).magnitude <= radius && Mathf.Abs(delta.y) <= verticalTolerance &&
                flat.sqrMagnitude > 0.001f && desired.sqrMagnitude > 0.001f &&
                Vector3.Angle(flat, desired) <= yawTolerance && (!requirePitch || Mathf.Abs(pitch - targetPitch) <= pitchTolerance);
        }

        public void Tick(Camera camera, float deltaTime, bool canDetect)
        {
            if (!camera || !canDetect) { ResetDetection(); return; }
            Vector3 delta = camera.transform.position - transform.position;
            Distance = new Vector2(delta.x, delta.z).magnitude;
            YawError = Vector3.Angle(Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up),
                Vector3.ProjectOnPlane(transform.forward, Vector3.up));
            PitchError = Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.x, transform.eulerAngles.x));
            Aligned = IsAligned(camera.transform.position, camera.transform.forward, transform.position, transform.forward,
                radius, verticalTolerance, yawTolerance, pitchTolerance, requirePitch);
            HoldProgress = Aligned ? Mathf.Clamp01(HoldProgress + deltaTime / Mathf.Max(0.1f, holdSeconds)) : 0;
        }
        public void ResetDetection() { HoldProgress = 0; Aligned = false; }
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0.77f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawRay(transform.position, transform.forward * 1.2f);
            Gizmos.DrawLine(transform.position, transform.position - Vector3.up * 0.82f);
        }
    }
}
