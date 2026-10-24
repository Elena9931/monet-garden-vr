using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit;
namespace MonetGarden {
public class GardenXRDiagnostics:MonoBehaviour {
 readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
 readonly List<XRInputSubsystem> inputs=new List<XRInputSubsystem>();
 float next;bool leftPress,rightPress;string state;
 void Update(){
  var b=GetComponent<GardenXRBridge>();if(!b||!b.IsVR)return;
  foreach(var node in new[]{XRNode.LeftHand,XRNode.RightHand}){
   var d=InputDevices.GetDeviceAtXRNode(node);d.TryGetFeatureValue(CommonUsages.triggerButton,out bool pressed);
   bool old=node==XRNode.LeftHand?leftPress:rightPress;
   if(pressed!=old){Debug.Log("MONET_XR_TRIGGER "+node+" "+pressed);if(node==XRNode.LeftHand)leftPress=pressed;else rightPress=pressed;
    foreach(var c in b.controllers)TraceRay(c);
   }
  }
  var f=GardenExperience.Active;if(f && state!=f.State.ToString()){state=f.State.ToString();Debug.Log("MONET_XR_STATE "+state);}
  if(Time.unscaledTime<next)return;next=Time.unscaledTime+5;
  SubsystemManager.GetInstances(displays);SubsystemManager.GetInstances(inputs);
  foreach(var d in displays)Debug.Log("MONET_XR_DISPLAY "+d.SubsystemDescriptor.id+" running="+d.running+" passes="+(d.running?d.GetRenderPassCount():0));
  foreach(var i in inputs)Debug.Log("MONET_XR_INPUT "+i.SubsystemDescriptor.id+" running="+i.running);
  var manager=XRGeneralSettings.Instance?XRGeneralSettings.Instance.Manager:null;
  Debug.Log("MONET_XR_CAMERA stereo="+b.origin.Camera.stereoEnabled+" target="+b.origin.Camera.stereoTargetEye+" origin="+b.origin.enabled+" loader="+(manager&&manager.activeLoader?manager.activeLoader.name:"none"));
  foreach(var node in new[]{XRNode.LeftHand,XRNode.RightHand}){
 var d=InputDevices.GetDeviceAtXRNode(node);
 bool pos=d.TryGetFeatureValue(CommonUsages.devicePosition,out var p);
 bool rot=d.TryGetFeatureValue(CommonUsages.deviceRotation,out var q);
 bool flags=d.TryGetFeatureValue(CommonUsages.trackingState,out var t);
 Debug.Log("MONET_XR_NATIVE "+node+" valid="+d.isValid+" name="+d.name+" positionAvailable="+pos+" position="+p+" rotationAvailable="+rot+" flagsAvailable="+flags+" flags="+t);
}
if(f&&f.State==GardenExperience.ExperienceState.Exploration)Debug.Log("MONET_XR_VIEWPOINT quest="+f.guide.QuestConfirmed+" explored="+f.HasExplored+" ui="+f.player.UIInteraction+" distance="+f.viewpoint.Distance+" radius="+f.viewpoint.radius+" heightError="+Mathf.Abs(f.player.viewCamera.transform.position.y-f.viewpoint.transform.position.y)+" heightTolerance="+f.viewpoint.verticalTolerance+" yaw="+f.viewpoint.YawError+" pitch="+f.viewpoint.PitchError+" hold="+f.viewpoint.HoldProgress+" guideExpanded="+f.guide.Expanded);
 foreach(var c in b.controllers){TraceRay(c);var line=c.GetComponent<LineRenderer>();Debug.Log("MONET_XR_RAY "+c.name+" enabled="+(line&&line.enabled)+" points="+(line?line.positionCount:0)+" origin="+c.transform.position+" shader="+(line&&line.sharedMaterial?line.sharedMaterial.shader.name:"none"));Debug.Log("MONET_XR_CONTROLLER "+c.name+" active="+c.isActiveAndEnabled+" tracked="+c.currentControllerState.isTracked+" pressControl="+c.uiPressAction.action?.activeControl?.path);}
 }
 public static void TraceRay(ActionBasedController c){
  var ray=c.GetComponent<XRRayInteractor>();if(!ray)return;
  string prefix="MONET_XR_ROUTE hand="+c.name;
  Debug.Log(prefix+" active="+ray.isActiveAndEnabled+" UISelect="+c.currentControllerState.uiPressInteractionState.active+" selected3D="+ray.hasSelection+" tracked="+c.currentControllerState.isTracked);
  if(ray.TryGetCurrent3DRaycastHit(out var physics))Debug.Log(prefix+" PHYSICS="+GardenXRButtonTrace.Path(physics.transform)+" distance="+physics.distance);
  if(ray.TryGetCurrentUIRaycastResult(out var ui))Debug.Log(prefix+" CHOSEN_UI="+GardenXRButtonTrace.Path(ui.gameObject.transform)+" depth="+ui.depth+" distance="+ui.distance);
  if(EventSystem.current&&ray.TryGetUIModel(out var model)){
   var data=new TrackedDeviceEventData(EventSystem.current){pointerId=model.pointerId,rayPoints=new List<Vector3>(model.raycastPoints),layerMask=model.raycastLayerMask};
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
   for(int i=0;i<Mathf.Min(hits.Count,6);i++)Debug.Log(prefix+" UI_CANDIDATE rank="+i+" path="+GardenXRButtonTrace.Path(hits[i].gameObject.transform)+" depth="+hits[i].depth+" order="+hits[i].sortingOrder+" distance="+hits[i].distance);
   if(hits.Count==0)Debug.Log(prefix+" UI_CANDIDATE none");
  }
 }
}
}
