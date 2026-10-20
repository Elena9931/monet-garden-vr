using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace MonetGarden
{
    public class GardenReassembly : MonoBehaviour
    {
        [Range(500,9000)]public int particleCount=5600;
        public Shader pigmentShader;
        [Range(.2f,2)]public float dispersion=1;
        public int VisibleParticleCount=>system?system.particleCount:0;
        public RenderTexture View {get;private set;}
        ParticleSystem system;
        ParticleSystem.Particle[] particles;
        Vector2[] uv,origins;
        Color[] colours;
        float[] seeds;
        Camera paintCamera;
        GardenExperience flow;
        Material material;
        float aspect;
        public void Initialize(GardenExperience experience)
        {
            flow=experience;transform.position=new Vector3(2000,0,0);
            var emitter=new GameObject("PigmentReassembly_ParticleSystem");emitter.transform.SetParent(transform,false);emitter.layer=29;
            system=emitter.AddComponent<ParticleSystem>();var main=system.main;main.playOnAwake=false;main.loop=false;main.maxParticles=particleCount;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startSpeed=0;main.startSize3D=true;
            var emission=system.emission;emission.enabled=false;var shape=system.shape;shape.enabled=false;
            var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            material=new Material(pigmentShader);renderer.sharedMaterial=material;
            var go=new GameObject("PigmentCamera_Offscreen");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,0,-10);
            paintCamera=go.AddComponent<Camera>();paintCamera.orthographic=true;paintCamera.orthographicSize=1;paintCamera.nearClipPlane=.1f;paintCamera.farClipPlane=15;paintCamera.cullingMask=1<<29;
            paintCamera.clearFlags=CameraClearFlags.SolidColor;paintCamera.backgroundColor=Color.clear;paintCamera.allowHDR=false;paintCamera.allowMSAA=false;
            var extra=paintCamera.GetUniversalAdditionalCameraData();extra.SetRenderer(1);extra.renderPostProcessing=false;extra.requiresColorOption=CameraOverrideOption.Off;
            View=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);View.Create();paintCamera.targetTexture=View;aspect=1280f/720;paintCamera.aspect=aspect;paintCamera.enabled=false;
        }
        public void Begin()
        {
            Clear();if(!flow.SelectedArtwork || !flow.SelectedArtwork.originalPainting)return;
            var texture=flow.SelectedArtwork.originalPainting;
            particles=new ParticleSystem.Particle[particleCount];uv=new Vector2[particleCount];origins=new Vector2[particleCount];colours=new Color[particleCount];seeds=new float[particleCount];
            var random=new System.Random(271828);
            for(int i=0;i<particleCount;i++)
            {
                uv[i]=new Vector2((float)random.NextDouble(),(float)random.NextDouble());
                origins[i]=new Vector2((float)random.NextDouble(),(float)random.NextDouble());seeds[i]=(float)random.NextDouble();
                colours[i]=texture.GetPixelBilinear(uv[i].x,uv[i].y);
                particles[i].startLifetime=1000;particles[i].remainingLifetime=1000;particles[i].randomSeed=(uint)(i+1);particles[i].rotation=seeds[i]*60-30;
            }
            system.Pause();paintCamera.enabled=true;
        }
        public void Tick(float progress)
        {
            if(particles==null)return;
            aspect=(float)Screen.width/Mathf.Max(1,Screen.height);paintCamera.aspect=aspect;
            float imageAspect=(float)flow.SelectedArtwork.originalPainting.width/flow.SelectedArtwork.originalPainting.height;
            float width=Mathf.Min(aspect,imageAspect),height=width/imageAspect;
            for(int i=0;i<particles.Length;i++)
            {
                float s=seeds[i],collect=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.3f-s*.16f)/.48f));
                Vector2 start=(origins[i]-Vector2.one*.5f)*new Vector2(aspect*2,2);
                Vector2 target=(uv[i]-Vector2.one*.5f)*new Vector2(width*2,height*2);
                Vector2 swirl=new Vector2(Mathf.Sin(s*33+progress*5),Mathf.Cos(s*24+progress*4))*.35f*dispersion*Mathf.Sin(progress*Mathf.PI)*(1-collect);
                Vector2 pos=Vector2.Lerp(start,target,collect)+swirl;
                particles[i].position=new Vector3(pos.x,pos.y,0);
                float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.1f)/.18f))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.79f)/.2f)));
                Color c=colours[i];c.a=fade*.82f;particles[i].startColor=c;
                float size=Mathf.Lerp(.045f,.014f,collect)*(.6f+s*.7f);particles[i].startSize3D=new Vector3(size*2.1f,size*.62f,size);
            }
            system.SetParticles(particles,particles.Length);
        }
        public void Clear(){if(system)system.Clear();particles=null;if(paintCamera)paintCamera.enabled=false;}
        public void Draw(){if(View && paintCamera.enabled)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),View,ScaleMode.StretchToFill,true);}
        void OnDestroy(){if(View){View.Release();Destroy(View);}if(material)Destroy(material);}
    }
}
