using UnityEngine;
namespace MonetGarden
{
    [RequireComponent(typeof(BoxCollider))]
    public class GardenProxyGizmo : MonoBehaviour
    {
        public bool waterOrBoundary;
        public bool provisional = true;
        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = waterOrBoundary ? new Color(0.2f, 0.65f, 1, 0.8f) : new Color(0.3f, 1, 0.5f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
