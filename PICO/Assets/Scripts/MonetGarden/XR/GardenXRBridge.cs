using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
namespace MonetGarden {
[DefaultExecutionOrder(-500)] public class GardenXRBridge:MonoBehaviour {
 public bool simulateInEditor; public XROrigin origin;public Transform trackingSpace;public TrackedPoseDriver poseDriver;
 public ActionBasedContinuousMoveProvider move;public ActionBasedSnapTurnProvider turn;public ActionBasedController[] controllers;
 public GardenXRPresentation presentation;public GardenMobileSplat mobileSplat;public InputActionAsset actions;
 [Range(.3f,.7f)]public float metresToGarden=.48235f;
 public bool IsVR{get;private set;} public static GardenXRBridge Active{get;private set;}
 public Vector3 SpawnEye{get;private set;}
 Vector3 spawn;Quaternion spawnRotation;bool configuredViewpoint,leftPrimary,rightPrimary;
 public float explorationDistanceMetres=.1f;public float viewpointRadiusMetres=1.5f,viewpointHeightToleranceMetres=.9f;public float viewpointYaw=20f,viewpointPitch=14f;
 void Awake(){Active=this;IsVR=Application.platform==RuntimePlatform.Android || simulateInEditor;var p=GetComponent<GardenDesktopController>();p.VRControlled=IsVR;SpawnEye=p.viewCamera.transform.position;spawn=transform.position;spawnRotation=transform.rotation;
 if(actions&&IsVR)actions.Enable();if(poseDriver)poseDriver.enabled=IsVR && (!Application.isEditor || !simulateInEditor);
 var ev=FindObjectOfType<UnityEngine.EventSystems.EventSystem>();if(ev){var xr=ev.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();var pc=ev.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();if(xr)xr.enabled=IsVR;if(pc)pc.enabled=!IsVR;}
 trackingSpace.localScale=Vector3.one*(IsVR?metresToGarden:1);
 if(IsVR&&simulateInEditor)p.viewCamera.transform.localPosition/=metresToGarden;
 foreach(var c in controllers){
 if(IsVR){c.positionAction.action?.Enable();c.rotationAction.action?.Enable();c.isTrackedAction.action?.Enable();c.trackingStateAction.action?.Enable();c.selectAction.action?.Enable();c.selectActionValue.action?.Enable();c.uiPressAction.action?.Enable();c.uiPressActionValue.action?.Enable();}
 c.gameObject.SetActive(IsVR);
}
 if(move)move.enabled=false;if(turn)turn.enabled=false;
 if(!IsVR && origin)origin.enabled=false;
 if(IsVR && mobileSplat){var original=mobileSplat.GetComponent<Gsplat.GsplatRenderer>();if(original)original.enabled=false;mobileSplat.enabled=true;}
 else if(mobileSplat)mobileSplat.enabled=false;
 }
 void Update(){if(!IsVR)return;var f=GardenExperience.Active;if(!f)return;
 if(!configuredViewpoint){f.explorationDistance=explorationDistanceMetres*metresToGarden;f.viewpoint.radius=viewpointRadiusMetres*metresToGarden;f.viewpoint.verticalTolerance=viewpointHeightToleranceMetres*metresToGarden;f.viewpoint.yawTolerance=viewpointYaw;f.viewpoint.pitchTolerance=viewpointPitch;configuredViewpoint=true;}
 bool lp=false,rp=false;UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out lp);UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out rp);
 if((lp&&!leftPrimary)||(rp&&!rightPrimary)){if(f.State==GardenExperience.ExperienceState.Selection)presentation.RecenterMenu();else presentation.ToggleGuide();}leftPrimary=lp;rightPrimary=rp;
 if(f.dynamicEnvironment)f.dynamicEnvironment.SetActive((f.State==GardenExperience.ExperienceState.Exploration || f.State==GardenExperience.ExperienceState.Entering || f.State==GardenExperience.ExperienceState.Transition));bool walking=f.State==GardenExperience.ExperienceState.Exploration && f.player.InputEnabled && !f.player.UIInteraction;
 move.enabled=walking;turn.enabled=walking;
 var cc=GetComponent<CharacterController>();float h=Mathf.Clamp(origin.CameraInOriginSpaceHeight,.35f,1.15f);cc.height=h+.06f;Vector3 head=transform.InverseTransformPoint(origin.Camera.transform.position);cc.center=new Vector3(head.x,cc.height*.5f,head.z);
 
 }
 void LateUpdate(){
 if(IsVR && Application.platform==RuntimePlatform.Android && poseDriver && poseDriver.positionInput.action?.activeControl==null){
  var head=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
  if(head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out var pos))origin.Camera.transform.localPosition=pos;
  if(head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var rot))origin.Camera.transform.localRotation=rot;
 }
 if(IsVR&&presentation&&GardenExperience.Active)presentation.Tick(GardenExperience.Active);}
 public void ResetRig(){if(!IsVR)return;var cc=GetComponent<CharacterController>();cc.enabled=false;transform.SetPositionAndRotation(spawn,spawnRotation);var delta=SpawnEye-origin.Camera.transform.position;transform.position+=new Vector3(delta.x,0,delta.z);cc.enabled=true;Physics.SyncTransforms();}
 void OnDestroy(){if(actions&&IsVR)actions.Disable();if(Active==this)Active=null;}
}
}


