using UnityEngine;
namespace MonetGarden
{
    public class GardenAmbientMotion : MonoBehaviour
    {
        public enum Motion { Sway, Float, Bird }
        public Motion motion;
        public float amplitude = 0.025f, frequency = 0.65f, phase;
        public Vector3 orbit = new Vector3(2, 0.15f, 1);
        public Transform leftWing, rightWing;
        Vector3 origin;
        Quaternion rest;
        float elapsed;
        void Awake() { origin = transform.localPosition; rest = transform.localRotation; }
        public void ResetMotion() { elapsed = 0; transform.localPosition = origin; transform.localRotation = rest; }
        void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed * frequency + phase;
            float breeze = (Mathf.PerlinNoise(t*.37f,phase+3)*2-1)*.7f+Mathf.Sin(t*.81f+phase)*.2f+Mathf.Sin(t*1.37f)*.1f;
            if (motion == Motion.Bird)
            {
                transform.localPosition = origin + new Vector3(Mathf.Cos(t) * orbit.x, Mathf.Sin(t * 2) * orbit.y, Mathf.Sin(t) * orbit.z);
                Vector3 tangent = new Vector3(-Mathf.Sin(t) * orbit.x, 2 * Mathf.Cos(2 * t) * orbit.y, Mathf.Cos(t) * orbit.z);
                transform.localRotation = Quaternion.LookRotation(tangent.normalized);
                float flap = Mathf.Sin(t * 16) * 32;
                if (leftWing) leftWing.localRotation = Quaternion.Euler(0, 0, flap);
                if (rightWing) rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
            }
            else if (motion == Motion.Float)
            {
                transform.localPosition = origin + Vector3.up * breeze * amplitude;
                transform.localRotation = rest * Quaternion.Euler(breeze * 2, 0, Mathf.Cos(t * 0.8f) * 3);
            }
            else transform.localRotation = rest * Quaternion.Euler(breeze * amplitude * 30, 0, Mathf.Cos(t * 0.7f) * amplitude * 30);
        }
    }
}
