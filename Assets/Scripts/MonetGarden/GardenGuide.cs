using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MonetGarden {
[DefaultExecutionOrder(-200)] public class GardenGuide : MonoBehaviour {
 public Shader glassShader;
 [Range(.2f,.25f)]public float widthFraction=.24f;
 public bool QuestConfirmed{get;private set;} public bool Expanded{get;private set;} public bool InteractionMode{get;private set;} public int Page{get;private set;}
 public const string Page01="Monet’s Garden is located in Giverny, a small town around 70 km northwest of Paris, beside the Epte River, a tributary of the Seine. It consists of two distinct zones: the formal Clos Normand flower garden in front of the house, and the Water Garden on the other side of the road, accessible via an underground passage. The Water Garden features a man‑made lily pond with a Japanese‑style bridge, surrounded by willows and bamboo, serving as the source for Monet’s late Water Lilies paintings";
 public const string Page02="Impressionism. Explore the garden as light shifts through time, and feel the ephemeral beauty Monet painted.";
 public const string Page03="Try to locate where the painter once stood to create his works. A special surprise awaits you there.";
 GardenExperience flow;Canvas canvas;RectTransform panel,content;CanvasGroup group;Text heading,body,pageLabel,buttonLabel;Button previous;ScrollRect scroll;GameObject icon;Material glass;float fade;
 public void Initialize(GardenExperience experience){
 flow=experience;var root=new GameObject("GardenGuideCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
 var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
 if(!FindObjectOfType<EventSystem>())new GameObject("GardenUIEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
 panel=Rect("FrostedGuidePanel",root.transform,true,new Vector2(-24,-24),new Vector2(1280*widthFraction,440));var image=panel.gameObject.AddComponent<Image>();glass=new Material(glassShader);image.material=glass;image.color=Color.white;group=panel.gameObject.AddComponent<CanvasGroup>();
 heading=Label("Heading",panel,new Vector2(20,-26),new Vector2(1280*widthFraction-120,54),18,true);pageLabel=Label("PageProgress",panel,new Vector2(20,-78),new Vector2(200,24),12,false);
 ButtonAt("Collapse",panel,new Vector2(-58,-16),new Vector2(25,25),"−",Collapse,true);ButtonAt("Close",panel,new Vector2(-22,-16),new Vector2(25,25),"×",Close,true);
 var viewport=Rect("ScrollViewport",panel,false,new Vector2(20,-112),new Vector2(1280*widthFraction-40,206));viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,.001f);scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=22;scroll.viewport=viewport;
 content=Rect("ScrollablePageText",viewport,false,Vector2.zero,new Vector2(1280*widthFraction-40,206));body=content.gameObject.AddComponent<Text>();body.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");body.fontSize=16;body.lineSpacing=1.15f;body.alignment=TextAnchor.UpperLeft;body.color=new Color(.13f,.23f,.22f);body.horizontalOverflow=HorizontalWrapMode.Wrap;body.verticalOverflow=VerticalWrapMode.Overflow;body.raycastTarget=false;scroll.content=content;
 var next=ButtonAt("ContinueOrConfirm",panel,new Vector2(20,-334),new Vector2(1280*widthFraction-40,34),"Continue →",NextPage,false);buttonLabel=next.GetComponentInChildren<Text>();previous=ButtonAt("PreviousPage",panel,new Vector2(20,-376),new Vector2(1280*widthFraction-40,24),"← Previous",PreviousPage,false);
 Label("MouseModeHint",panel,new Vector2(20,-412),new Vector2(1280*widthFraction-40,20),11,false).text="Tab · switch between look and guide";
 icon=ButtonAt("ReopenGuide",root.transform,new Vector2(-24,-24),new Vector2(80,34),"Guide",Open,true).gameObject;ResetGuide(); }
 static RectTransform Rect(string name,Transform parent,bool right,Vector2 pos,Vector2 size){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(right?1:0,1);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,bool bold){var r=Rect(name,parent,false,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.color=new Color(.14f,.24f,.23f);t.raycastTarget=false;return t;}
 static Button ButtonAt(string name,Transform parent,Vector2 pos,Vector2 size,string text,UnityEngine.Events.UnityAction action,bool right){var r=Rect(name,parent,right,pos,size);var i=r.gameObject.AddComponent<Image>();i.color=new Color(.87f,.93f,.88f,.65f);var b=r.gameObject.AddComponent<Button>();b.onClick.AddListener(action);var t=Label("Label",r,Vector2.zero,size,14,false);t.alignment=TextAnchor.MiddleCenter;t.text=text;return b;}
 public void HideForSequence(){if(canvas)canvas.enabled=false;}
 public void ResetGuide(){QuestConfirmed=false;Page=0;Expanded=true;InteractionMode=true;fade=0;RefreshPage();}
 public void Open(){Expanded=true;SetInteraction(true);RefreshPage();} public void Collapse(){Expanded=false;SetInteraction(false);} public void Close(){Collapse();}
 public void PreviousPage(){Page=Mathf.Max(0,Page-1);fade=0;RefreshPage();}
 public void NextPage(){if(Page<2){Page++;fade=0;RefreshPage();}else{QuestConfirmed=true;Collapse();Debug.Log("MONET_GUIDE_CONFIRMED");}}
 public void SetInteraction(bool value){InteractionMode=value;if(flow&&flow.player){flow.player.UIInteraction=value;if(value)flow.player.ReleaseCursor();else if(flow.State==GardenExperience.ExperienceState.Exploration)flow.player.CaptureCursor();}}
 void RefreshPage(){if(!body)return;heading.text=new[]{"About Monet's Garden","Impressionism","Find the Painter's View"}[Page];body.text=new[]{Page01,Page02,Page03}[Page];pageLabel.text=(Page+1).ToString("00")+" / 03";buttonLabel.text=Page==2?"Confirm":"Continue →";previous.gameObject.SetActive(Page>0);Canvas.ForceUpdateCanvases();content.sizeDelta=new Vector2(1280*widthFraction-40,Mathf.Max(206,body.preferredHeight+10));scroll.verticalNormalizedPosition=1;}
 void Update(){if(!flow||!canvas)return;bool explore=flow.State==GardenExperience.ExperienceState.Exploration;canvas.enabled=explore;flow.player.UIInteraction=explore&&InteractionMode;if(!explore)return;if(Input.GetKeyDown(KeyCode.Tab))SetInteraction(!InteractionMode);if(Input.GetKeyDown(KeyCode.Escape))SetInteraction(true);fade=Mathf.MoveTowards(fade,1,Time.unscaledDeltaTime*5);panel.gameObject.SetActive(Expanded);icon.SetActive(!Expanded);group.alpha=fade;group.interactable=InteractionMode;group.blocksRaycasts=InteractionMode;panel.anchoredPosition=new Vector2(-24+(1-fade)*12,-24);}
 void OnDestroy(){if(glass)Destroy(glass);}
}}
