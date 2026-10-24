using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace MonetGarden {
/// <summary>Reuses the existing globe mesh/material and rotation/focus state in stereo world space.</summary>
public class GardenXRStereoGlobe:MonoBehaviour {
 GardenGlobe source;Transform globe;float homeDistance;public float rotationSpeed=60f;
 public void Initialize(GardenExperience f,Canvas menu){
  source=f.globeInterface;homeDistance=4.3f;
  globe=Instantiate(source.Sphere.gameObject,menu.transform).transform;
  globe.name="XR_StereoEarth_ExistingArt";
  globe.localPosition=new Vector3(0,65f,0);globe.localScale=Vector3.one*205f;
  f.player.viewCamera.cullingMask|=1<<30;
  f.player.viewCamera.stereoTargetEye=StereoTargetEyeMask.Both;
  foreach(var landmark in globe.GetComponentsInChildren<GardenLandmark>()){
   if(!landmark.selectable){foreach(var collider in landmark.GetComponentsInChildren<Collider>())Destroy(collider);continue;}
   var head=landmark.transform.Find("RedPin").GetComponent<Collider>();
   foreach(var collider in landmark.GetComponentsInChildren<Collider>())if(collider!=head)Destroy(collider);
   var interactable=landmark.gameObject.AddComponent<XRSimpleInteractable>();interactable.colliders.Clear();interactable.colliders.Add(head);
   interactable.selectEntered.AddListener(args=>{
    Debug.Log("MONET_XR_LANDMARK_SELECT "+args.interactorObject.transform.name);
    source.SelectLandmark(source.ActiveMarker);
   });
  }
 }
 void LateUpdate(){
  if(!globe||!source.Sphere)return;
  var flow=GardenExperience.Active;var b=GardenXRBridge.Active;
  if(b&&b.IsVR&&flow&&flow.State==GardenExperience.ExperienceState.Selection&&!source.Selected){
   var d=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
   if(d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out var stick)&&stick.magnitude>.2f)source.RotateBy(new Vector2(stick.x,-stick.y)*rotationSpeed*Time.unscaledDeltaTime);
  }
  foreach(var selectable in globe.GetComponentsInChildren<XRSimpleInteractable>(true))selectable.enabled=!source.Selected;
  globe.localRotation=source.Sphere.localRotation;
  globe.localScale=Vector3.one*(205f*homeDistance/Mathf.Max(2.8f,-source.GlobeCamera.transform.localPosition.z));
  var a=globe.Find("Giverny_SELECTABLE");if(a)a.localScale=source.ActiveMarker.transform.localScale;
 }
}
}



