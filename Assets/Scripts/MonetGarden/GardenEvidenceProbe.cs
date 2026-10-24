#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;
namespace MonetGarden
{
 public class GardenEvidenceProbe : MonoBehaviour
 {
  IEnumerator Start()
  {
   var f=GardenExperience.Active;if(!f)yield break;
   f.ResetExperience(true);f.EnterGarden();
   yield return new WaitForSecondsRealtime(f.entrySeconds+.3f);
   f.guide.NextPage();f.guide.NextPage();f.guide.NextPage();
   var p=f.player;var cc=p.GetComponent<CharacterController>();
   cc.Move(Vector3.left*.6f);yield return null;
   cc.enabled=false;
   p.transform.SetPositionAndRotation(f.viewpoint.transform.position-Vector3.up*p.eyeHeight,Quaternion.Euler(0,f.viewpoint.transform.eulerAngles.y,0));
   p.viewCamera.transform.localPosition=Vector3.up*p.eyeHeight;
   p.viewCamera.transform.localRotation=Quaternion.Euler(f.viewpoint.transform.eulerAngles.x,0,0);
   cc.enabled=true;Physics.SyncTransforms();
   while(f.State==GardenExperience.ExperienceState.Exploration)yield return null;
   while(f.State==GardenExperience.ExperienceState.Transition && f.Progress<.36f)yield return null;
   Selection.activeGameObject=f.gaussianTransition.rendererSource.gameObject;
   Debug.Log("MONET_EVIDENCE: actual sequence paused for Uniform Sphere Points Inspector capture.");
   EditorApplication.isPaused=true;
   Destroy(gameObject);
  }
 }
}
#endif
