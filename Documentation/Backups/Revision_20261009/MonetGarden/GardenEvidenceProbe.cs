#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
namespace MonetGarden
{
    public class GardenEvidenceProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var flow=GardenExperience.Active;if(!flow)yield break;
            flow.EnterGarden();flow.player.SetInput(false);
            // Exercise real exploration and return, then let the normal hold detector run.
            flow.player.GetComponent<CharacterController>().Move(Vector3.left*.5f);
            yield return null;yield return null;
            flow.player.ResetToSpawn();
            yield return new WaitForSeconds(flow.viewpoint.holdSeconds+.3f);
            ScreenCapture.CaptureScreenshot("Documentation/ProcessShots/S04_Viewpoint_Game.png");
            yield return new WaitForSeconds(.3f);
            flow.player.SetInput(true);Destroy(gameObject);
        }
    }
}
#endif
