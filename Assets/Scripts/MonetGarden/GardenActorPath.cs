using UnityEngine;
namespace MonetGarden
{
    public class GardenActorPath : MonoBehaviour
    {
        public Transform[] waypoints;
        public float speed = 0.14f, pauseSeconds = 2.5f;
        public Animator animator;
        public string State { get; private set; } = "Idle";
        int target;
        float pause;
        Vector3 origin;
        Quaternion rest;
        void Awake() { origin = transform.position; rest = transform.rotation; ResetPath(); }
        public void ResetPath()
        {
            target = waypoints != null && waypoints.Length > 1 ? 1 : 0; pause = pauseSeconds;
            transform.SetPositionAndRotation(origin, rest);
            State = "Idle";
            if (animator) { animator.SetFloat("Speed", 0); animator.SetBool("Paused", false); }
        }
        void Update()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            if (pause > 0)
            {
                pause -= Time.deltaTime;
                State = target == 0 ? "Idle" : "Pause";
                if (animator) { animator.SetFloat("Speed", 0); animator.SetBool("Paused", target != 0); }
                return;
            }
            if (!waypoints[target]) return;
            Vector3 destination = waypoints[target].position;
            Vector3 delta = destination - transform.position;
            delta.y = 0;
            State = "Walk";
            if (animator) { animator.SetFloat("Speed", speed); animator.SetBool("Paused", false); }
            if (delta.magnitude < 0.04f)
            { target = (target + 1) % waypoints.Length; pause = pauseSeconds; return; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 3);
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }
    }
}
