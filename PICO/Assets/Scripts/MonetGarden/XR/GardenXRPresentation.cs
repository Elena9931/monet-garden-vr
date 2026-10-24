using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
namespace MonetGarden {
public class GardenXRPresentation:MonoBehaviour {
 public GardenXRBridge bridge;public Shader uiShader;public Shader revealShader;public Shader glassShader;
 Canvas directions;Text directionLabel;public Canvas menu;public Canvas painting;RawImage globe,original,pigments;GameObject card;Text title;Material reveal,uiMaterial,guideMaterial;CanvasGroup menuFade;
 Vector3 anchor;Quaternion rotation;bool sequence,menuAnchored;int headReadyFrames;GardenExperience.ExperienceState previous;
 public Transform PaintingAnchor=>painting?painting.transform:null;
 public Vector3 PaintingRight=>painting.transform.right*painting.GetComponent<RectTransform>().rect.width*painting.transform.localScale.x;
 public Vector3 PaintingUp=>painting.transform.up*painting.GetComponent<RectTransform>().rect.height*painting.transform.localScale.y;
 public void Build(GardenExperience f){
 uiMaterial=new Material(uiShader);menu=CanvasAt("XR_GlobeEntry",f.player.viewCamera,900,620,.001f);menuFade=menu.gameObject.AddComponent<CanvasGroup>();var backdrop=menu.gameObject.AddComponent<Image>();backdrop.color=Color.clear;backdrop.material=uiMaterial;backdrop.raycastTarget=false;globe=Raw("Earth",menu.transform,new Vector2(0,35),new Vector2(880,490),f.globeInterface.View);globe.raycastTarget=false;globe.color=Color.clear;menu.gameObject.AddComponent<GardenXRStereoGlobe>().Initialize(f,menu);
 Button("SelectGiverny",menu.transform,new Vector2(0,-225),new Vector2(700,42),"Claude Monet's Garden · Giverny",()=>f.globeInterface.SelectLandmark(f.globeInterface.ActiveMarker));
 card=new GameObject("GivernyInformationCard",typeof(RectTransform));card.transform.SetParent(menu.transform,false);var cr=(RectTransform)card.transform;cr.anchorMin=cr.anchorMax=cr.pivot=Vector2.one*.5f;cr.sizeDelta=new Vector2(850,180);cr.anchoredPosition=new Vector2(0,-180);var bg=card.AddComponent<Image>();bg.color=new Color(.9f,.89f,.77f,1);bg.raycastTarget=false;
 title=Label(card.transform,new Vector2(-70,34),new Vector2(570,80),"Claude Monet's Garden\n84 Rue Claude Monet\n27620 Giverny, France",23);
 var thumb=Raw("OriginalThumbnail",card.transform,new Vector2(325,0),new Vector2(140,140*2293f/2400f),f.SelectedArtwork.originalPainting);thumb.raycastTarget=false;
 Button("Enter",card.transform,new Vector2(-70,-48),new Vector2(570,45),"Enter the water garden →",()=>f.EnterGarden());
 painting=CanvasAt("XR_OriginalPainting",f.player.viewCamera,700,700*2293f/2400f,.001f);original=Raw("SpecifiedOriginal",painting.transform,Vector2.zero,new Vector2(700,700*2293f/2400f),f.SelectedArtwork.originalPainting);reveal=new Material(revealShader);original.material=reveal;original.raycastTarget=false;
 pigments=Raw("GPU_Reassembly",painting.transform,Vector2.zero,new Vector2(880,600),f.reassembly.View);pigments.raycastTarget=false;painting.gameObject.SetActive(false);
 foreach(var graphic in menu.GetComponentsInChildren<Graphic>(true)){graphic.material=uiMaterial;graphic.gameObject.layer=5;}menu.gameObject.layer=5;
 directions=CanvasAt("XR_ViewpointHint",f.player.viewCamera,600,50,.0007f);directionLabel=Label(directions.transform,Vector2.zero,new Vector2(600,50),"Look towards the Japanese bridge.",21);directionLabel.color=new Color(1,.9f,.64f);directionLabel.material=uiMaterial;
 var guide=f.guide.CanvasView;guide.renderMode=RenderMode.WorldSpace;guide.worldCamera=f.player.viewCamera;var r=guide.GetComponent<RectTransform>();r.sizeDelta=new Vector2(1280,720);guide.transform.localScale=Vector3.one*.00105f;guide.transform.SetParent(null,true);if(!guide.GetComponent<TrackedDeviceGraphicRaycaster>())guide.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();var legacy=guide.GetComponent<GraphicRaycaster>();if(legacy)legacy.enabled=false;
 ConfigureRaycaster(guide);var image=guide.transform.Find("FrostedGuidePanel").GetComponent<Image>();image.raycastTarget=false;image.material=uiMaterial;image.color=new Color(.86f,.93f,.9f,.9f);foreach(var graphic in guide.GetComponentsInChildren<Graphic>(true))if(graphic!=image)graphic.material=uiMaterial;var hint=guide.transform.Find("FrostedGuidePanel/MouseModeHint").GetComponent<Text>();hint.text="Point with controller · press trigger";
 foreach(var t in guide.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
 foreach(var button in guide.GetComponentsInChildren<Button>(true)){button.targetGraphic=button.GetComponent<Image>();if(!button.GetComponent<GardenXRButtonTrace>())button.gameObject.AddComponent<GardenXRButtonTrace>();}
 guide.transform.position=f.player.viewCamera.transform.position+f.player.viewCamera.transform.forward*.85f;guide.transform.rotation=f.player.viewCamera.transform.rotation;
 }
 Canvas CanvasAt(string n,Camera head,float width,float height,float scale){var g=new GameObject(n,typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));g.transform.SetParent(transform,false);var c=g.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=head;ConfigureRaycaster(c);var r=g.GetComponent<RectTransform>();r.sizeDelta=new Vector2(width,height);g.transform.localScale=Vector3.one*scale;g.transform.position=head.transform.position+head.transform.forward*.85f;g.transform.rotation=head.transform.rotation;return c;}
 static void ConfigureRaycaster(Canvas c){
  var ray=c.GetComponent<TrackedDeviceGraphicRaycaster>();
  ray.ignoreReversedGraphics=false;ray.checkFor2DOcclusion=false;ray.checkFor3DOcclusion=false;
  // Exhibit panels deliberately render above the garden; decorative graphics do not receive input.
 } static RawImage Raw(string name,Transform parent,Vector2 pos,Vector2 size,Texture texture){var g=new GameObject(name,typeof(RectTransform),typeof(RawImage));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;var x=g.GetComponent<RawImage>();x.texture=texture;return x;}
 static Text Label(Transform parent,Vector2 pos,Vector2 size,string text,int font){var g=new GameObject("Label",typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;var t=g.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=font;t.alignment=TextAnchor.MiddleCenter;t.text=text;t.color=new Color(.1f,.2f,.18f);t.raycastTarget=false;return t;}
 static void Button(string name,Transform parent,Vector2 pos,Vector2 size,string text,UnityEngine.Events.UnityAction action){var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;g.GetComponent<Image>().color=new Color(.85f,.91f,.86f,1);g.AddComponent<GardenXRButtonTrace>();g.GetComponent<Button>().targetGraphic=g.GetComponent<Image>();g.GetComponent<Button>().onClick.AddListener(()=>{Debug.Log("MONET_XR_BUTTON "+name);action();});Label(g.transform,Vector2.zero,size,text,22);}
 public void Tick(GardenExperience f){if(!menu)Build(f);bool map=f.IsMapVisible;menu.gameObject.SetActive(map);card.SetActive(f.globeInterface.Selected);menu.transform.Find("SelectGiverny").gameObject.SetActive(!f.globeInterface.Selected);globe.raycastTarget=false;globe.color=Color.clear;
 menuFade.alpha=f.State==GardenExperience.ExperienceState.Entering?1-f.StateProgress:f.State==GardenExperience.ExperienceState.Returning?f.StateProgress:1;
 if(f.State==GardenExperience.ExperienceState.Selection){
  if(previous!=f.State){menuAnchored=false;headReadyFrames=0;}
  bool ready=Application.platform!=RuntimePlatform.Android||UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var headRotation);
  if(!menuAnchored&&ready&&++headReadyFrames>=12){RecenterMenu();menuAnchored=true;}
 }
 menuFade.interactable=f.State==GardenExperience.ExperienceState.Selection;menuFade.blocksRaycasts=menuFade.interactable;
 bool art=f.State==GardenExperience.ExperienceState.Transition || f.State==GardenExperience.ExperienceState.Painting || f.State==GardenExperience.ExperienceState.Returning;
 if(art&&!sequence){var head=f.player.viewCamera.transform;painting.transform.SetPositionAndRotation(head.position+head.forward*.9f,head.rotation);sequence=true;}
 painting.gameObject.SetActive(art);if(!art)sequence=false;
 if(art){reveal.SetFloat("_Progress",f.Progress);original.color=new Color(1,1,1,f.State==GardenExperience.ExperienceState.Returning?1-f.StateProgress:1);pigments.gameObject.SetActive(f.State==GardenExperience.ExperienceState.Transition);f.EffectRendered=true;}
 // Keep the guide world anchored while reading. Re-anchor on each new garden entry.
 if(f.State==GardenExperience.ExperienceState.Exploration&&previous!=f.State){var head=f.player.viewCamera.transform;var c=f.guide.CanvasView;c.transform.SetPositionAndRotation(head.position+head.forward*.85f,head.rotation);}
 if(f.State==GardenExperience.ExperienceState.Exploration&&!f.guide.Expanded)AnchorGuide();
 bool hint=f.State==GardenExperience.ExperienceState.Exploration&&f.guide.QuestConfirmed&&f.viewpoint.Distance<=f.viewpoint.radius&&!f.viewpoint.Aligned;directions.gameObject.SetActive(hint);if(hint){var h=f.player.viewCamera.transform;directions.transform.SetPositionAndRotation(h.position+h.forward*.6f-h.up*.18f,h.rotation);}
 previous=f.State;
 }
 public void RecenterMenu(){
  var f=GardenExperience.Active;if(!f||!menu)return;var h=f.player.viewCamera.transform;
  menu.transform.SetPositionAndRotation(h.position+h.forward*.85f-h.up*.065f,h.rotation);
  Debug.Log("MONET_XR_RECENTER head="+h.position+" globeCenter="+menu.transform.Find("XR_StereoEarth_ExistingArt").position);
 }
 public void AnchorGuide(){var f=GardenExperience.Active;if(!f||!f.guide.CanvasView)return;var h=f.player.viewCamera.transform;f.guide.CanvasView.transform.SetPositionAndRotation(h.position+h.forward*.85f,h.rotation);}
 public void ToggleGuide(){var f=GardenExperience.Active;if(!f||f.State!=GardenExperience.ExperienceState.Exploration)return;if(f.guide.Expanded)f.guide.Close();else{AnchorGuide();f.guide.Open();}Debug.Log("MONET_XR_GUIDE_TOGGLE expanded="+f.guide.Expanded);} void OnDestroy(){if(reveal)Destroy(reveal);if(uiMaterial)Destroy(uiMaterial);if(guideMaterial)Destroy(guideMaterial);if(directions)Destroy(directions.gameObject);if(menu)Destroy(menu.gameObject);if(painting)Destroy(painting.gameObject);}
}
}




