#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
namespace MonetGarden.Editor {
[DefaultExecutionOrder(-1000)]
public class GardenXRRegressionProbe:MonoBehaviour {
 StringBuilder report=new StringBuilder("Unified XR actual ray / event simulation. Synthetic controller poses and trigger states; NOT PICO hardware acceptance.\n");
 int pass,fail;bool pressed;ActionBasedController right;Vector3 origin,target;GardenExperience f;GardenXRBridge b;float started;
 void Check(string label,bool ok){report.AppendLine((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;Debug.Log("MONET_REGRESSION "+(ok?"PASS ":"FAIL ")+label);}
 void OnEnable(){Application.logMessageReceived+=LogError;}
 void OnDisable(){Application.logMessageReceived-=LogError;}
 void LogError(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){report.AppendLine("ERROR "+message+" "+stack);fail++;}}
 void Update(){
  if(right&&(target-origin).sqrMagnitude>.00001f){var s=right.currentControllerState;s.position=right.transform.parent.InverseTransformPoint(origin);s.rotation=Quaternion.Inverse(right.transform.parent.rotation)*Quaternion.LookRotation(target-origin);s.isTracked=true;s.inputTrackingState=InputTrackingState.Position|InputTrackingState.Rotation;s.ResetFrameDependentStates();s.selectInteractionState.SetFrameState(pressed,pressed?1:0);s.uiPressInteractionState.SetFrameState(pressed,pressed?1:0);}
  if(started>0&&Time.realtimeSinceStartup-started>170){Check("Timeout",false);Finish();}
 }
 IEnumerator Frames(int count=4){for(int i=0;i<count;i++)yield return null;}
 void Aim(Transform t){origin=f.player.viewCamera.transform.position-f.player.viewCamera.transform.up*.07f;target=t is RectTransform r?t.TransformPoint(r.rect.center):t.position;}
 IEnumerator Click(Transform t,string label){
  Aim(t);yield return Frames();Canvas.ForceUpdateCanvases();
  var ray=right.GetComponent<XRRayInteractor>();bool hit=ray.TryGetCurrentUIRaycastResult(out var h)&&h.gameObject==t.gameObject;
  Check(label+" UI ray top hit is button",hit);GardenXRDiagnostics.TraceRay(right);
  pressed=true;yield return Frames(2);pressed=false;yield return Frames(3);
 }
 IEnumerator Start(){
  Application.runInBackground=true;EditorApplication.isPaused=false;started=Time.realtimeSinceStartup;yield return new WaitForSeconds(1);f=GardenExperience.Active;b=GardenXRBridge.Active;
  right=b.controllers[1];foreach(var c in b.controllers){c.enableInputActions=false;c.enableInputTracking=false;}
  b.controllers[0].transform.localPosition=new Vector3(-5,0,-5);
  
#if ENABLE_LEGACY_INPUT_MANAGER
 Check("Android equivalent New Input only",false);
#else
 Check("Android equivalent New Input only",true);
#endif
 Check("Mobile quality count",b.mobileSplat.RenderedCount==262144);
 Check("One EventSystem",FindObjectsOfType<EventSystem>().Length==1);
  int modules=0;foreach(var m in FindObjectsOfType<BaseInputModule>())if(m.isActiveAndEnabled)modules++;
  Check("Exactly one active XRUIInputModule",modules==1&&EventSystem.current.currentInputModule is XRUIInputModule);
  Check("XR Origin and stereo target configured",b.origin.enabled&&b.origin.Camera.stereoTargetEye==StereoTargetEyeMask.Both);
  Check("Menu world space",b.presentation.menu.renderMode==RenderMode.WorldSpace);
  Check("No transparent Earth trigger drag handler",!b.presentation.menu.GetComponentInChildren<GardenXRGlobeSurface>());
  for(int cycle=0;cycle<2;cycle++){
   right=b.controllers[cycle==0?1:0];var other=b.controllers[cycle==0?0:1];other.currentControllerState.inputTrackingState=InputTrackingState.None;other.transform.localPosition=new Vector3(-5,0,-5);
   yield return Frames();Check("Cycle "+cycle+" map ready",f.State==GardenExperience.ExperienceState.Selection&&!f.globeInterface.Selected);
   var sphere=b.presentation.menu.transform.Find("XR_StereoEarth_ExistingArt");var pin=sphere.Find("Giverny_SELECTABLE");
   var interactable=pin.GetComponent<XRSimpleInteractable>();Check("Pin Collider and XR Interactable",interactable&&pin.GetComponentInChildren<Collider>());
   var before=f.globeInterface.Sphere.rotation;f.globeInterface.RotateBy(new Vector2(5,0));yield return Frames();Check("Existing rotation propagates to stereo globe",Quaternion.Angle(before,sphere.rotation)>.1f);f.globeInterface.ResetGlobe();yield return Frames();
   Aim(pin.Find("RedPin"));yield return Frames();var ray=right.GetComponent<XRRayInteractor>();
   Check("Ray visual enabled with live positions",right.GetComponent<LineRenderer>().enabled&&right.GetComponent<LineRenderer>().positionCount>=2);
   foreach(var marker in sphere.GetComponentsInChildren<GardenLandmark>())if(!marker.selectable)Check("Future landmark has no collider/interactable",marker.GetComponentsInChildren<Collider>().Length==0&&!marker.GetComponent<XRSimpleInteractable>());
   Check("3D physics ray hits actual Giverny pin",ray.TryGetCurrent3DRaycastHit(out var ph)&&ph.collider.transform.IsChildOf(pin));
   pressed=true;yield return Frames(2);pressed=false;yield return new WaitForSeconds(1.9f);
   Check("Pin trigger opens existing information card",f.globeInterface.Selected);
   var card=b.presentation.menu.transform.Find("GivernyInformationCard");Check("Card visible and not a Button",card.gameObject.activeSelf&&!card.GetComponent<Button>());
   Check("Fallback selection button hidden behind card",!b.presentation.menu.transform.Find("SelectGiverny").gameObject.activeSelf);
   Check("Paper decoration does not intercept",!card.GetComponent<Image>().raycastTarget);
   Check("Selection released rather than sticky",!ray.hasSelection);
   var enter=card.Find("Enter");yield return Click(enter,"Enter");
   Check("Original EnterGarden called",f.State==GardenExperience.ExperienceState.Entering);
   yield return new WaitForSeconds(f.entrySeconds+.3f);Check("Exploration reached",f.State==GardenExperience.ExperienceState.Exploration);
   var guide=f.guide.CanvasView;var continueButton=guide.transform.Find("FrostedGuidePanel/ContinueOrConfirm");
   Check("Guide world raycaster only",guide.renderMode==RenderMode.WorldSpace&&guide.GetComponent<TrackedDeviceGraphicRaycaster>().enabled&&!guide.GetComponent<GraphicRaycaster>().enabled);
   Check("Quest still gated before reading",!f.guide.QuestConfirmed);
   yield return Click(guide.transform.Find("FrostedGuidePanel/Collapse"),"Collapse guide");Check("Guide collapsed",!f.guide.Expanded&&!guide.transform.Find("FrostedGuidePanel").gameObject.activeSelf&&guide.transform.Find("ReopenGuide").gameObject.activeSelf);
   yield return Click(guide.transform.Find("ReopenGuide"),"Reopen guide");Check("Guide reopened",f.guide.Expanded);yield return new WaitForSeconds(.3f);
   yield return Click(guide.transform.Find("FrostedGuidePanel/Close"),"Close guide");Check("Guide closed",!f.guide.Expanded&&!guide.transform.Find("FrostedGuidePanel").gameObject.activeSelf);
   yield return Click(guide.transform.Find("ReopenGuide"),"Reopen closed guide");Check("Closed guide reopened",f.guide.Expanded);yield return new WaitForSeconds(.3f);
   yield return Click(continueButton,"Continue page 1");Check("Page 2 through XR pointer",f.guide.Page==1);
   yield return new WaitForSeconds(.3f);yield return Click(continueButton,"Continue page 2");Check("Page 3 through XR pointer",f.guide.Page==2&&!f.guide.QuestConfirmed);
   yield return new WaitForSeconds(.3f);yield return Click(continueButton,"Confirm");Check("Quest confirmed via XR pointer",f.guide.QuestConfirmed&&!f.guide.Expanded&&!guide.transform.Find("FrostedGuidePanel").gameObject.activeSelf&&!f.player.UIInteraction);
   b.presentation.ToggleGuide();yield return Frames();Check("Controller shortcut opens guide",f.guide.Expanded&&guide.transform.Find("FrostedGuidePanel").gameObject.activeSelf);b.presentation.ToggleGuide();yield return Frames();Check("Controller shortcut closes guide",!f.guide.Expanded&&!guide.transform.Find("FrostedGuidePanel").gameObject.activeSelf&&!f.player.UIInteraction);
   var reopen=guide.transform.Find("ReopenGuide");if(!reopen)reopen=guide.transform.Find("GuideIcon");
   // UI naming is audited below; the main Continue/Confirm flow must pass before any downstream assertions.
   if(!f.guide.QuestConfirmed||f.State!=GardenExperience.ExperienceState.Exploration){Check("Stop downstream on failed entry/guide",false);Finish();yield break;}
   var cc=f.player.GetComponent<CharacterController>();cc.enabled=false;f.player.transform.position+=Vector3.right*.5f;cc.enabled=true;yield return Frames();Check("Exploration retained",f.HasExplored);
   cc.enabled=false;var cam=f.player.viewCamera.transform;f.player.transform.position+=f.viewpoint.transform.position-cam.position;cam.rotation=Quaternion.LookRotation(-f.viewpoint.transform.forward);cc.enabled=true;
   yield return new WaitForSeconds(.4f);Check("Wrong viewpoint angle rejected",f.State==GardenExperience.ExperienceState.Exploration&&f.viewpoint.HoldProgress==0);
   cam.rotation=f.viewpoint.transform.rotation;yield return new WaitForSeconds(f.viewpoint.holdSeconds+.3f);
   Check("Position plus angle hold starts transition",f.State==GardenExperience.ExperienceState.Transition&&f.gaussianTransition.Running);
   Check("Locomotion locked",!b.move.enabled&&!b.turn.enabled);
   while(f.State==GardenExperience.ExperienceState.Transition)yield return null;
   Check("Original painting reached",f.State==GardenExperience.ExperienceState.Painting&&f.Progress==1);
   while(f.State==GardenExperience.ExperienceState.Painting)yield return null;
   Check("Ten seconds after fully formed frame",f.LastPaintingHold>=10&&f.LastPaintingHold<10.2);
   while(f.State!=GardenExperience.ExperienceState.Selection)yield return null;
   Check("Loop reset",!f.guide.QuestConfirmed&&!f.globeInterface.Selected&&!f.gaussianTransition.Running);
  }
  Finish();
 }
 void Finish(){
  started=0;report.AppendLine("Passed="+pass+" Failed="+fail);Directory.CreateDirectory("Documentation/Validation");File.WriteAllText("Documentation/Validation/UnifiedXR_Editor_Regression.txt",report.ToString());
  Debug.Log("MONET_REGRESSION_DONE "+pass+" pass "+fail+" fail");EditorApplication.Exit(fail==0?0:1);
 }
}
}
#endif
