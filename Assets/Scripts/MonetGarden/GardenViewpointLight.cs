using UnityEngine;
namespace MonetGarden
{
    public class GardenViewpointLight : MonoBehaviour
    {
        public ParticleSystem lights;
        public float visibilityDistance=2.5f;
        void Update()
        {
            var f=GardenExperience.Active;if(!f || !lights)return;
            bool explore=f.State==GardenExperience.ExperienceState.Exploration;
            float near=1-Mathf.Clamp01(Vector3.Distance(f.player.viewCamera.transform.position,f.viewpoint.transform.position)/visibilityDistance);
            var emission=lights.emission;emission.rateOverTime=explore?Mathf.Lerp(1,9,near):0;
            var main=lights.main;main.startColor=new Color(1,.8f,.4f,explore?Mathf.Lerp(.05f,.3f,near)+f.viewpoint.HoldProgress*.16f:0);
            if(!explore)lights.Clear();
        }
    }
}
