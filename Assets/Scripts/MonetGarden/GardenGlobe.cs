using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MonetGarden
{
    public class GardenLandmark : MonoBehaviour { public bool selectable; }
    public class GardenGlobe : MonoBehaviour
    {
        public Texture2D surfaceTexture;
        public Shader globeShader;
        [Range(.05f,.6f)] public float dragSensitivity=.18f;
        [Range(1,10)] public float inertiaFriction=3.2f;
        public float focusSeconds=1.6f;
        public bool Selected {get;private set;}
        public bool Hovered {get;private set;}
        public float FocusProgress {get;private set;}
        public Transform Sphere {get;private set;}
        public Camera GlobeCamera {get;private set;}
        public GardenLandmark ActiveMarker {get;private set;}
        public Texture2D Background {get;private set;}
        public RenderTexture View {get;private set;}
        public int MarkerCount=>markers.Count;
        GardenExperience flow;
        List<GardenLandmark> markers=new List<GardenLandmark>();
        Vector2 lastMouse,downMouse,velocity;
        Quaternion focusFrom,homeRotation;
        Vector3 homeCamera;
        bool dragging;
        float focusElapsed;
        GUIStyle title,body,small;
        Material earthMaterial,pinMaterial;
        const int Layer=30;
        public static Vector3 Geographic(float latitude,float longitude)
        {float a=latitude*Mathf.Deg2Rad,b=longitude*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Sin(a),-Mathf.Cos(a)*Mathf.Cos(b));}

        public void Initialize(GardenExperience experience)
        {
            flow=experience;flow.player.viewCamera.cullingMask&=~((1<<30)|(1<<29));
            transform.position=new Vector3(1000,0,0);
            Background=new Texture2D(128,128,TextureFormat.RGB24,false);
            Color[] bg=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {float light=Mathf.Exp(-((x-90)*(x-90)+(y-100)*(y-100))/4800f);float noise=Mathf.PerlinNoise(x*.06f,y*.06f);bg[y*128+x]=new Color(.009f+light*.016f,.018f+light*.043f,.055f+light*.085f)+new Color(.005f,.01f,.014f)*noise;}
            Background.SetPixels(bg);Background.Apply();
            var sphere=new GameObject("ImpressionistEarth_3D");sphere.transform.SetParent(transform,false);Sphere=sphere.transform;sphere.layer=Layer;
            sphere.AddComponent<MeshFilter>().sharedMesh=BuildSphere(128,64);
            earthMaterial=new Material(globeShader);earthMaterial.SetTexture("_MainTex",surfaceTexture);
            sphere.AddComponent<MeshRenderer>().sharedMaterial=earthMaterial;
            pinMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));pinMaterial.color=new Color(.94f,.105f,.045f);
            AddMarker(49.075f,1.534f,true,"Giverny_SELECTABLE");
            AddMarker(51.5f,-.12f,false,"London_FUTURE");AddMarker(48.21f,16.37f,false,"Vienna_FUTURE");
            AddMarker(41.9f,12.5f,false,"Rome_FUTURE");AddMarker(40.42f,-3.7f,false,"Madrid_FUTURE");
            AddMarker(40.7f,-74,false,"NewYork_FUTURE");AddMarker(35.68f,139.7f,false,"Tokyo_FUTURE");
            var c=new GameObject("GlobeCamera_Offscreen");c.transform.SetParent(transform,false);GlobeCamera=c.AddComponent<Camera>();c.layer=Layer;
            GlobeCamera.transform.localPosition=new Vector3(0,0,-4.3f);GlobeCamera.transform.localRotation=Quaternion.identity;
            GlobeCamera.fieldOfView=36;GlobeCamera.nearClipPlane=.1f;GlobeCamera.farClipPlane=10;
            GlobeCamera.cullingMask=1<<Layer;GlobeCamera.clearFlags=CameraClearFlags.SolidColor;GlobeCamera.backgroundColor=new Color(.015f,.03f,.095f,0);
            GlobeCamera.allowHDR=false;GlobeCamera.allowMSAA=false;
            var extra=GlobeCamera.GetUniversalAdditionalCameraData();extra.SetRenderer(1);extra.renderPostProcessing=false;extra.requiresColorOption=CameraOverrideOption.Off;extra.requiresDepthOption=CameraOverrideOption.Off;
            View=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);View.Create();GlobeCamera.targetTexture=View;
            homeRotation=Quaternion.FromToRotation(Geographic(28,-8),Vector3.back);homeCamera=GlobeCamera.transform.localPosition;
            ResetGlobe();
        }
        void AddMarker(float lat,float lon,bool selectable,string name)
        {
            var pin=new GameObject(name);pin.transform.SetParent(Sphere,false);pin.layer=Layer;
            Vector3 point=Geographic(lat,lon);pin.transform.localPosition=point*1.005f;pin.transform.localRotation=Quaternion.FromToRotation(Vector3.up,point);
            var marker=pin.AddComponent<GardenLandmark>();marker.selectable=selectable;markers.Add(marker);if(selectable)ActiveMarker=marker;
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="RedPin";head.transform.SetParent(pin.transform,false);head.transform.localPosition=Vector3.up*.065f;head.transform.localScale=Vector3.one*(selectable? .065f:.046f);head.layer=Layer;head.GetComponent<Renderer>().sharedMaterial=pinMaterial;
            if(!selectable)Destroy(head.GetComponent<Collider>());
            var stem=new GameObject("PinStem");stem.transform.SetParent(pin.transform,false);stem.layer=Layer;stem.AddComponent<MeshFilter>().sharedMesh=BuildCone();stem.AddComponent<MeshRenderer>().sharedMaterial=pinMaterial;
            if(selectable)
            {
                var light=GameObject.CreatePrimitive(PrimitiveType.Sphere);light.name="WarmHighlight";light.transform.SetParent(head.transform,false);light.transform.localPosition=new Vector3(-.16f,.17f,-.36f);light.transform.localScale=Vector3.one*.24f;light.layer=Layer;Destroy(light.GetComponent<Collider>());
                var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=new Color(1,.9f,.53f);light.GetComponent<Renderer>().sharedMaterial=mat;
            }
        }
        public void ResetGlobe(){Selected=false;Hovered=false;FocusProgress=0;focusElapsed=0;dragging=false;velocity=Vector2.zero;if(Sphere)Sphere.localRotation=homeRotation;if(GlobeCamera)GlobeCamera.transform.localPosition=homeCamera;if(ActiveMarker)ActiveMarker.transform.localScale=Vector3.one;}
        public bool SelectLandmark(GardenLandmark marker)
        {
            if(!marker || !marker.selectable || flow.State!=GardenExperience.ExperienceState.Selection)return false;
            Debug.Log("MONET_LANDMARK_SELECTED: Giverny");
            Selected=true;focusElapsed=0;focusFrom=Sphere.localRotation;velocity=Vector2.zero;dragging=false;return true;
        }
        public void RotateBy(Vector2 degrees)
        {Sphere.rotation=Quaternion.AngleAxis(degrees.x,Vector3.up)*Quaternion.AngleAxis(degrees.y,Vector3.right)*Sphere.rotation;}
        public GardenLandmark Hit(Vector2 screen)
        {
            if(!GlobeCamera)return null;
            Ray ray=GlobeCamera.ViewportPointToRay(new Vector3(screen.x/Screen.width,screen.y/Screen.height,0));
            if(Physics.Raycast(ray,out var hit,10,1<<Layer))
            {
                var marker=hit.collider.GetComponentInParent<GardenLandmark>();
                // Surface-facing check prevents selecting a pin through the solid globe.
                if(marker && Vector3.Dot((marker.transform.position-Sphere.position).normalized,(GlobeCamera.transform.position-Sphere.position).normalized)>.08f)return marker;
            }
            return null;
        }
        void Update()
        {
            if(!flow || !GlobeCamera)return;
            GlobeCamera.enabled=flow.IsMapVisible;
            GlobeCamera.aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            if(flow.State!=GardenExperience.ExperienceState.Selection)return;
            if(Selected)
            {
                focusElapsed+=Time.unscaledDeltaTime;FocusProgress=Mathf.Clamp01(focusElapsed/Mathf.Max(.1f,focusSeconds));
                float t=Mathf.SmoothStep(0,1,FocusProgress);
                Sphere.localRotation=Quaternion.Slerp(focusFrom,Quaternion.FromToRotation(Geographic(49.075f,1.534f),Vector3.back),t);
                GlobeCamera.transform.localPosition=Vector3.Lerp(homeCamera,new Vector3(.32f,.08f,-2.95f),t);
                ActiveMarker.transform.localScale=Vector3.one*Mathf.Lerp(1,1.65f,t);
                return;
            }
            Hovered=Hit(Input.mousePosition)==ActiveMarker;
            ActiveMarker.transform.localScale=Vector3.one*(Hovered?1.22f:1+Mathf.Sin(Time.unscaledTime*2)*.035f);
            if(!dragging){RotateBy(velocity*Time.unscaledDeltaTime);velocity*=Mathf.Exp(-inertiaFriction*Time.unscaledDeltaTime);}
        }
        void OnGUI()
        {
            if(!flow || flow.State!=GardenExperience.ExperienceState.Selection || Selected)return;
            var e=Event.current;if(e.button!=0)return;
            Vector2 point=new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y);
            if(e.type==EventType.MouseDown)
            {dragging=true;downMouse=lastMouse=point;velocity=Vector2.zero;e.Use();}
            else if(e.type==EventType.MouseDrag && dragging)
            {
                Vector2 d=point-lastMouse;lastMouse=point;
                var turn=new Vector2(d.x,-d.y)*dragSensitivity;RotateBy(turn);
                velocity=Vector2.Lerp(velocity,Vector2.ClampMagnitude(turn/Mathf.Max(Time.unscaledDeltaTime,.005f),100),.4f);e.Use();
            }
            else if(e.type==EventType.MouseUp && dragging)
            {
                dragging=false;float distance=Vector2.Distance(downMouse,point);
                Debug.Log("MONET_GLOBE_POINTER_RELEASE: "+distance.ToString("F1")+" px");
                if(distance<7)SelectLandmark(Hit(point));e.Use();
            }
        }
        public Vector2 ActiveScreenPoint()
        {var p=GlobeCamera.WorldToViewportPoint(ActiveMarker.transform.position);return new Vector2(p.x*Screen.width,(1-p.y)*Screen.height);}
        public Rect CardRect=>new Rect(Screen.width*.58f,Screen.height*.36f,Screen.width*.37f,Screen.height*.25f);
        public void Draw(float alpha,bool interactive)
        {
            if(!Background || !View)return;
            GUI.color=new Color(1,1,1,Mathf.SmoothStep(0,1,alpha));
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Background,ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),View,ScaleMode.StretchToFill,true);
            if(title==null)
            {
                var paperFont=Font.CreateDynamicFontFromOSFont("Georgia",24);
                title=new GUIStyle(GUI.skin.label){font=paperFont,fontSize=26,fontStyle=FontStyle.Bold,wordWrap=true};title.normal.textColor=new Color(.94f,.9f,.72f);
                body=new GUIStyle(GUI.skin.label){font=paperFont,fontSize=17,wordWrap=true};body.normal.textColor=new Color(.21f,.27f,.29f);
                small=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};small.normal.textColor=new Color(.7f,.76f,.8f);
            }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            title.fontSize=Mathf.Max(16,(int)(27*scale));body.fontSize=Mathf.Max(12,(int)(17*scale));small.fontSize=Mathf.Max(10,(int)(13*scale));
            if(!Selected)
            {
                GUI.Label(new Rect(Screen.width*.07f,Screen.height*.08f,Screen.width*.8f,45*scale),"From Viewing a Painting to Entering the Painting",title);
                GUI.Label(new Rect(Screen.width*.07f,Screen.height*.88f,Screen.width*.85f,40),"Drag the earth · Find the glowing red pin at Giverny · Other destinations are future studies",small);
            }
            else if(FocusProgress>.8f)
            {
                Rect card=CardRect;
                Vector3 point=GlobeCamera.WorldToViewportPoint(ActiveMarker.transform.GetChild(0).position);
                Vector2 from=new Vector2(point.x*Screen.width,(1-point.y)*Screen.height);
                Vector2 elbow=new Vector2(card.x-24*scale,card.y+card.height*.4f);
                DrawLeader(from,elbow,alpha);DrawLeader(elbow,new Vector2(card.x,elbow.y),alpha);
                GUI.color=new Color(.95f,.92f,.81f,alpha);GUI.DrawTexture(card,Texture2D.whiteTexture);GUI.color=new Color(1,1,1,alpha);
                Rect thumb=new Rect(card.x+card.width*.62f,card.y+8*scale,card.width*.36f,card.height-16*scale);
                if(flow.SelectedArtwork && flow.SelectedArtwork.originalPainting)GUI.DrawTexture(thumb,flow.SelectedArtwork.originalPainting,ScaleMode.ScaleToFit,false);
                var cardTitle=new GUIStyle(body){fontStyle=FontStyle.Bold,fontSize=Mathf.Max(13,(int)(20*scale))};
                GUI.Label(new Rect(card.x+15*scale,card.y+16*scale,card.width*.58f,50*scale),"Claude Monet's Garden",cardTitle);
                GUI.Label(new Rect(card.x+15*scale,card.y+70*scale,card.width*.58f,62*scale),"84 Rue Claude Monet\n27620 Giverny, France",body);
                GUI.Label(new Rect(card.x+15*scale,card.yMax-30*scale,card.width*.58f,24*scale),"Enter the water garden →",body);
                // The whole paper card is the explicit second click target.
                if(interactive && GUI.Button(card,GUIContent.none,GUIStyle.none))flow.EnterGarden();
            }
            GUI.color=Color.white;
        }
        static void DrawLeader(Vector2 from,Vector2 to,float alpha)
        {
            var saved=GUI.matrix;GUI.color=new Color(.94f,.91f,.76f,alpha*.8f);
            var delta=to-from;GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);
            GUI.DrawTexture(new Rect(from.x,from.y,delta.magnitude,1.5f),Texture2D.whiteTexture);GUI.matrix=saved;
        }
        public static Mesh BuildSphere(int columns,int rows)
        {
            var mesh=new Mesh{name="Geographic sphere"};var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];var triangles=new int[columns*rows*6];int k=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){int i=y*(columns+1)+x;uv[i]=new Vector2((float)x/columns,(float)y/rows);vertices[i]=Geographic(uv[i].y*180-90,uv[i].x*360-180);}
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int a=y*(columns+1)+x,b=a+columns+1;triangles[k++]=a;triangles[k++]=b;triangles[k++]=b+1;triangles[k++]=a;triangles[k++]=b+1;triangles[k++]=a+1;}
            mesh.vertices=vertices;mesh.normals=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
        static Mesh BuildCone()
        {
            var v=new Vector3[18];var t=new int[48];v[0]=Vector3.zero;v[17]=Vector3.up*.056f;
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;v[i+1]=new Vector3(Mathf.Cos(a)*.012f,.056f,Mathf.Sin(a)*.012f);int j=i*3;t[j]=0;t[j+1]=(i+1)%16+1;t[j+2]=i+1;}
            var m=new Mesh{name="Landmark tapered stem"};m.vertices=v;m.triangles=t;m.RecalculateNormals();return m;
        }
        void OnDestroy(){if(View){View.Release();Destroy(View);}if(Background)Destroy(Background);if(earthMaterial)Destroy(earthMaterial);if(pinMaterial)Destroy(pinMaterial);}
    }
}
