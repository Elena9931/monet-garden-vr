using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
namespace MonetGarden {
/// <summary>Uses SDK-provided native XR features when an action has no resolved control.</summary>
public class GardenPICOController:ActionBasedController {
 public XRNode hand=XRNode.RightHand;
 protected override void UpdateTrackingInput(XRControllerState state){
  base.UpdateTrackingInput(state);
  if(state==null || (positionAction.action?.activeControl!=null && state.isTracked))return;
  var device=InputDevices.GetDeviceAtXRNode(hand);
  device.TryGetFeatureValue(CommonUsages.isTracked,out state.isTracked);
  device.TryGetFeatureValue(CommonUsages.trackingState,out state.inputTrackingState);
  bool position=device.TryGetFeatureValue(CommonUsages.devicePosition,out var p);
  bool rotation=device.TryGetFeatureValue(CommonUsages.deviceRotation,out var q);
  if(position){state.position=p;state.inputTrackingState|=InputTrackingState.Position;}
  if(rotation){state.rotation=q;state.inputTrackingState|=InputTrackingState.Rotation;}
  state.isTracked=device.isValid && position && rotation;
  if(Application.platform==RuntimePlatform.Android && (!position||!rotation)){
   var sdkHand=hand==XRNode.LeftHand?Unity.XR.PXR.PXR_Input.Controller.LeftController:Unity.XR.PXR.PXR_Input.Controller.RightController;
   if(Unity.XR.PXR.PXR_Input.IsControllerConnected(sdkHand)){
    double time=Unity.XR.PXR.PXR_Plugin.System.UPxr_GetPredictedDisplayTime();
    var rawPos=Unity.XR.PXR.PXR_Input.GetControllerPredictPosition(sdkHand,time);
    var rawRot=Unity.XR.PXR.PXR_Input.GetControllerPredictRotation(sdkHand,time);
    var status=Unity.XR.PXR.PXR_Input.GetControllerStatus(sdkHand);
    if(status!=Unity.XR.PXR.PXR_Input.ControllerStatus.Sleep && (rawRot.x*rawRot.x+rawRot.y*rawRot.y+rawRot.z*rawRot.z+rawRot.w*rawRot.w)>.5f){
     state.position=new Vector3(rawPos.x,rawPos.y,-rawPos.z);
     state.rotation=new Quaternion(rawRot.x,rawRot.y,-rawRot.z,-rawRot.w);
     state.inputTrackingState=InputTrackingState.Position|InputTrackingState.Rotation;
     state.isTracked=true;
    }
   }
  }
 }
 protected override void UpdateInput(XRControllerState state){
  if(Application.platform!=RuntimePlatform.Android){base.UpdateInput(state);return;}
  if(state==null)return;
  state.ResetFrameDependentStates();
  var d=InputDevices.GetDeviceAtXRNode(hand);
  d.TryGetFeatureValue(CommonUsages.trigger,out float trigger);
  d.TryGetFeatureValue(CommonUsages.triggerButton,out bool pressed);
  d.TryGetFeatureValue(CommonUsages.grip,out float grip);
  d.TryGetFeatureValue(CommonUsages.gripButton,out bool gripped);
  d.TryGetFeatureValue(CommonUsages.primary2DAxis,out var scroll);
  state.selectInteractionState.SetFrameState(pressed,trigger);
  state.uiPressInteractionState.SetFrameState(pressed,trigger);
  state.activateInteractionState.SetFrameState(gripped,grip);
  state.uiScrollValue=scroll;
 }
}
}




