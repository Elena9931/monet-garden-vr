using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using Gsplat;
namespace MonetGarden.Editor {
public static class GardenPICOSetup {
 const string Dir="Assets/MonetGarden/XR";
 static InputActionAsset actions;
 static InputActionProperty Action(InputActionMap map,string name,string path,string type="Button",string layout="Button") {
  var a=map.AddAction(name,type=="Value"?InputActionType.Value:InputActionType.Button,expectedControlLayout:layout);a.AddBinding(path);
  return new InputActionProperty(a);
 }
 static InputActionProperty Ref(InputAction a) {
  string path=Dir+"/Input_"+a.actionMap.name+"_"+a.name+".asset";
  var r=AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
  if(!r){r=InputActionReference.Create(a);AssetDatabase.CreateAsset(r,path);}else r.Set(a);
  return new InputActionProperty(r);
 }
 static GameObject Child(Transform p,string n){var t=p.Find(n);if(t)return t.gameObject;var g=new GameObject(n);g.transform.SetParent(p,false);return g;}
 static T Get<T>(GameObject g) where T:Component{var c=g.GetComponent<T>();return c?c:g.AddComponent<T>();}
 [MenuItem("Tools/Monet Garden/PICO/Configure Existing Scene")]
 public static void Apply() {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
  Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
  var earth=AssetImporter.GetAtPath("Assets/MonetGarden/Rendering/Earth_Impressionist_Albedo.png") as TextureImporter;
  if(earth){var android=earth.GetPlatformTextureSettings("Android");if(!android.overridden||android.maxTextureSize!=4096){android.name="Android";android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_4x4;android.compressionQuality=100;earth.SetPlatformTextureSettings(android);earth.anisoLevel=8;earth.SaveAndReimport();}}
  var p=UnityEngine.Object.FindObjectOfType<GardenDesktopController>();
  var f=UnityEngine.Object.FindObjectOfType<GardenExperience>();
  if(!p||!f||!p.viewCamera)throw new InvalidOperationException("Existing player/experience missing.");
  var eye=p.viewCamera.transform.position;var look=p.viewCamera.transform.rotation;
  var root=p.gameObject;var cc=root.GetComponent<CharacterController>();cc.enabled=false;
  if(!root.GetComponent<XROrigin>()){root.transform.position=eye-Vector3.up*.82f;root.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);}
  foreach(var m in root.GetComponents<MonoBehaviour>())if(m && m.GetType().Name=="GardenWalkController")m.enabled=false;
  Get<Unity.XR.PXR.PXR_Manager>(root);var origin=Get<XROrigin>(root);origin.Camera=p.viewCamera;p.viewCamera.nearClipPlane=.01f;
  var offset=Child(root.transform,"XR_FloorOffset");var tracking=Child(offset.transform,"XR_TrackingSpace");
  tracking.transform.localScale=Vector3.one;p.viewCamera.transform.SetParent(tracking.transform,true);
  p.viewCamera.transform.SetPositionAndRotation(eye,look);
  origin.CameraFloorOffsetObject=offset;origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;origin.CameraYOffset=.82f;
  string inputPath=Dir+"/MonetPICO.inputactions";
  actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
  if(!actions) {
   var tmp=ScriptableObject.CreateInstance<InputActionAsset>();tmp.name="Monet PICO";
   var head=new InputActionMap("Head");tmp.AddActionMap(head);
   Action(head,"Position","<XRHMD>/centerEyePosition","Value","Vector3");
   Action(head,"Rotation","<XRHMD>/centerEyeRotation","Value","Quaternion");
   Action(head,"Tracking","<XRHMD>/trackingState","Value","Integer");
   foreach(var hand in new[]{"Left","Right"}) {
    var map=new InputActionMap(hand);tmp.AddActionMap(map);string device="<XRController>{"+hand+"Hand}/";
    Action(map,"Position",device+"devicePosition","Value","Vector3");Action(map,"Rotation",device+"deviceRotation","Value","Quaternion");
    Action(map,"Tracked",device+"isTracked");Action(map,"Tracking",device+"trackingState","Value","Integer");
    Action(map,"Select",device+"triggerPressed");Action(map,"SelectValue",device+"trigger","Value","Axis");
    Action(map,"Grip",device+"gripPressed");Action(map,"GripValue",device+"grip","Value","Axis");
    var stick=map.AddAction("Stick",InputActionType.Value,expectedControlLayout:"Vector2");stick.AddBinding(device+"primary2DAxis").WithProcessor("stickDeadzone");
   }
   File.WriteAllText(inputPath,tmp.ToJson());UnityEngine.Object.DestroyImmediate(tmp);AssetDatabase.ImportAsset(inputPath);
   actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
  }
  var driver=Get<TrackedPoseDriver>(p.viewCamera.gameObject);
  driver.positionInput=Ref(actions.FindAction("Head/Position"));driver.rotationInput=Ref(actions.FindAction("Head/Rotation"));driver.trackingStateInput=Ref(actions.FindAction("Head/Tracking"));driver.enabled=false;
  var manager=UnityEngine.Object.FindObjectOfType<XRInteractionManager>();if(!manager)manager=new GameObject("XR_InteractionManager").AddComponent<XRInteractionManager>();
  var controllers=new ActionBasedController[2];int hi=0;
  foreach(var hand in new[]{"Left","Right"}) {
   var g=Child(tracking.transform,"XR_"+hand+"Controller");
   var oldController=g.GetComponent<ActionBasedController>();if(oldController && !(oldController is GardenPICOController))UnityEngine.Object.DestroyImmediate(oldController);var c=Get<GardenPICOController>(g);c.hand=hand=="Left"?UnityEngine.XR.XRNode.LeftHand:UnityEngine.XR.XRNode.RightHand;controllers[hi++]=c;
   c.positionAction=Ref(actions.FindAction(hand+"/Position"));c.rotationAction=Ref(actions.FindAction(hand+"/Rotation"));
   c.isTrackedAction=Ref(actions.FindAction(hand+"/Tracked"));c.trackingStateAction=Ref(actions.FindAction(hand+"/Tracking"));
   c.selectAction=Ref(actions.FindAction(hand+"/Select"));c.selectActionValue=Ref(actions.FindAction(hand+"/SelectValue"));
   c.uiPressAction=c.selectAction;c.uiPressActionValue=c.selectActionValue;
   c.activateAction=Ref(actions.FindAction(hand+"/Grip"));c.activateActionValue=Ref(actions.FindAction(hand+"/GripValue"));
   var ray=Get<XRRayInteractor>(g);ray.interactionManager=manager;ray.enableUIInteraction=true;ray.maxRaycastDistance=12;ray.selectActionTrigger=XRBaseControllerInteractor.InputTriggerType.StateChange;ray.blockUIOnInteractableSelection=false;ray.hoverToSelect=false;ray.raycastMask=(1<<30)|(1<<5);ray.hitClosestOnly=true;
   c.modelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.unity.xr.picoxr/Assets/Resources/Controller/PICO 4U "+(hand=="Left"?"L":"R")+".prefab").transform;
   c.enableInputTracking=true;c.enableInputActions=true;
   var line=Get<LineRenderer>(g);line.widthMultiplier=.003f;
   string rayPath=Dir+"/M_ControllerRay.mat";
   var rayMaterial=AssetDatabase.LoadAssetAtPath<Material>(rayPath);
   if(!rayMaterial){rayMaterial=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonetGarden/XR/GardenVRUI.shader"));rayMaterial.color=new Color(1,.85f,.42f,1);rayMaterial.renderQueue=5000;AssetDatabase.CreateAsset(rayMaterial,rayPath);}
   rayMaterial.renderQueue=5000;EditorUtility.SetDirty(rayMaterial);line.sharedMaterial=rayMaterial;line.sortingOrder=100;
   var visual=Get<XRInteractorLineVisual>(g);visual.lineWidth=.003f;
   g.SetActive(false);
  }
  var loc=Get<LocomotionSystem>(root);loc.xrOrigin=origin;
  foreach(var old in root.GetComponents<ActionBasedContinuousMoveProvider>())if(!(old is GardenXRMove))UnityEngine.Object.DestroyImmediate(old);var move=Get<GardenXRMove>(root);move.system=loc;move.forwardSource=p.viewCamera.transform;move.moveSpeed=.45f;move.enableStrafe=true;move.useGravity=true;move.leftHandMoveAction=Ref(actions.FindAction("Left/Stick"));move.rightHandMoveAction=new InputActionProperty((InputAction)null);move.enabled=false;
  foreach(var old in root.GetComponents<ActionBasedSnapTurnProvider>())if(!(old is GardenXRSnapTurn))UnityEngine.Object.DestroyImmediate(old);var turn=Get<GardenXRSnapTurn>(root);turn.system=loc;turn.rightHandSnapTurnAction=Ref(actions.FindAction("Right/Stick"));turn.leftHandSnapTurnAction=new InputActionProperty((InputAction)null);turn.turnAmount=30;turn.enableTurnAround=false;turn.enabled=false;
  var ev=UnityEngine.Object.FindObjectOfType<EventSystem>();if(!ev)ev=new GameObject("XR_UIEventSystem").AddComponent<EventSystem>();
  Get<StandaloneInputModule>(ev.gameObject).enabled=true;var uim=Get<XRUIInputModule>(ev.gameObject);uim.enableXRInput=true;uim.uiCamera=p.viewCamera;uim.enableMouseInput=false;uim.enableTouchInput=false;uim.enableGamepadInput=false;uim.enableJoystickInput=false;uim.enabled=false;
  var source=UnityEngine.Object.FindObjectOfType<GsplatRenderer>();if(!source||!source.GsplatAsset)throw new Exception("Original GSplat asset missing.");
  string mobilePath=Dir+"/MonetGarden_Mobile262144.asset";
  var asset=AssetDatabase.LoadAssetAtPath<GsplatAsset>(mobilePath);
  if(!asset){var src=source.GsplatAsset;int n=262144;asset=ScriptableObject.CreateInstance<GsplatAsset>();asset.name="MonetGarden Mobile 262144";asset.SplatCount=(uint)n;asset.SHBands=0;asset.Bounds=src.Bounds;
   asset.Positions=new Vector3[n];asset.Scales=new Vector3[n];asset.Rotations=new Vector4[n];asset.Colors=new Vector4[n];asset.SHs=Array.Empty<Vector3>();
   for(int i=0;i<n;i++){int j=(int)((long)i*src.SplatCount/n);asset.Positions[i]=src.Positions[j];asset.Scales[i]=src.Scales[j];asset.Rotations[i]=src.Rotations[j];asset.Colors[i]=src.Colors[j];}
   AssetDatabase.CreateAsset(asset,mobilePath);
  }
  var mobile=Get<GardenMobileSplat>(source.gameObject);mobile.mobileAsset=asset;mobile.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonetGarden/XR/GardenMobileGaussian.shader");mobile.sorter=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/MonetGarden/XR/GardenMobileSort.compute");mobile.sizeMultiplier=1.4f;mobile.sortStagesPerFrame=24;mobile.enabled=false;
  var bridges=root.GetComponents<GardenXRBridge>();for(int i=1;i<bridges.Length;i++)UnityEngine.Object.DestroyImmediate(bridges[i]);var b=Get<GardenXRBridge>(root);b.simulateInEditor=false;b.origin=origin;b.trackingSpace=tracking.transform;b.poseDriver=driver;b.move=move;b.turn=turn;b.controllers=controllers;b.mobileSplat=mobile;b.actions=actions;
  Get<GardenXRDiagnostics>(root);
  b.presentation=Get<GardenXRPresentation>(f.gameObject);b.presentation.bridge=b;b.presentation.uiShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonetGarden/XR/GardenVRUI.shader");b.presentation.revealShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonetGarden/XR/GardenVRPainting.shader");b.presentation.glassShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonetGarden/XR/GardenVRGlass.shader");
  cc.enabled=true;foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true)){int missing=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);if(missing>0)Debug.LogWarning("MONET_PICO_MISSING_SCRIPT "+t.name+": "+missing);}
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  Debug.Log("MONET_PICO_SETUP_COMPLETE");
 }
}
public class GardenPICOSceneBuild: IProcessSceneWithReport {
 public int callbackOrder=>100;
 public void OnProcessScene(Scene scene,BuildReport report) {
  if(report==null||report.summary.platform!=BuildTarget.Android)return;
  foreach(var root in scene.GetRootGameObjects()){foreach(var b in root.GetComponentsInChildren<GardenXRBridge>(true))b.simulateInEditor=false;}
  foreach(var root in scene.GetRootGameObjects())foreach(var s in root.GetComponentsInChildren<GsplatRenderer>(true)){s.enabled=false;s.GsplatAsset=null;var mobile=s.GetComponent<GardenMobileSplat>();if(mobile)mobile.enabled=true;}
 }
}
}




