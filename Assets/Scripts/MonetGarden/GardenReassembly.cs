using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MonetGarden {
// Static billboard mesh, animated entirely in the vertex shader. Interface retained.
public class GardenReassembly:MonoBehaviour {
 [Range(2000,40000)]public int particleCount=18000;public Shader pigmentShader;[Range(.2f,2)]public float dispersion=1;
 public int VisibleParticleCount=>paintCamera&&paintCamera.enabled?particleCount:0;public RenderTexture View{get;private set;}
 Camera paintCamera;GardenExperience flow;Material material;Mesh mesh;
 public void Initialize(GardenExperience experience){flow=experience;transform.position=new Vector3(2000,0,0);var emitter=new GameObject("GPU_PigmentReassembly");emitter.transform.SetParent(transform,false);emitter.layer=29;mesh=new Mesh(){name="Static pigment billboards",indexFormat=IndexFormat.UInt32};var v=new Vector3[particleCount*4];var uv=new Vector2[v.Length];var seeds=new Vector2[v.Length];var indices=new int[particleCount*6];var random=new System.Random(271828);for(int i=0;i<particleCount;i++){var seed=new Vector2((float)random.NextDouble(),(float)random.NextDouble());for(int j=0;j<4;j++){int k=i*4+j;v[k]=Vector3.zero;uv[k]=new Vector2(j==0||j==3?-1:1,j<2?-1:1);seeds[k]=seed;}int s=i*6,b=i*4;indices[s]=b;indices[s+1]=b+2;indices[s+2]=b+1;indices[s+3]=b;indices[s+4]=b+3;indices[s+5]=b+2;}mesh.vertices=v;mesh.uv=uv;mesh.uv2=seeds;mesh.triangles=indices;mesh.bounds=new Bounds(Vector3.zero,Vector3.one*20);emitter.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(pigmentShader);emitter.AddComponent<MeshRenderer>().sharedMaterial=material;
 var g=new GameObject("PigmentCamera_Offscreen");g.transform.SetParent(transform,false);g.transform.localPosition=new Vector3(0,0,-10);paintCamera=g.AddComponent<Camera>();paintCamera.orthographic=true;paintCamera.orthographicSize=1;paintCamera.nearClipPlane=.1f;paintCamera.farClipPlane=15;paintCamera.cullingMask=1<<29;paintCamera.clearFlags=CameraClearFlags.SolidColor;paintCamera.backgroundColor=Color.clear;paintCamera.allowHDR=false;paintCamera.allowMSAA=false;var extra=paintCamera.GetUniversalAdditionalCameraData();extra.SetRenderer(1);extra.renderPostProcessing=false;extra.requiresColorOption=CameraOverrideOption.Off;View=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);View.Create();paintCamera.targetTexture=View;paintCamera.enabled=false; }
 public void Begin(){Clear();if(!flow.SelectedArtwork||!flow.SelectedArtwork.originalPainting)return;material.SetTexture("_Painting",flow.SelectedArtwork.originalPainting);material.SetFloat("_Progress",0);paintCamera.enabled=true;}
 public void Tick(float progress){if(!paintCamera.enabled)return;float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);paintCamera.aspect=aspect;material.SetFloat("_Progress",progress);material.SetFloat("_Dispersion",dispersion);material.SetFloat("_ViewAspect",aspect);material.SetFloat("_PaintingAspect",(float)flow.SelectedArtwork.originalPainting.width/flow.SelectedArtwork.originalPainting.height);}
 public void Clear(){if(paintCamera)paintCamera.enabled=false;}
 public void Draw(){if(View&&paintCamera.enabled)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),View,ScaleMode.StretchToFill,true);}
 void OnDestroy(){if(View){View.Release();Destroy(View);}if(material)Destroy(material);if(mesh)Destroy(mesh);}
}}
