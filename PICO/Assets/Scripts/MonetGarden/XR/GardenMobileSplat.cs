using UnityEngine;
using UnityEngine.Rendering;
namespace MonetGarden {
[DefaultExecutionOrder(-450)] public class GardenMobileSplat:MonoBehaviour {
 public Gsplat.GsplatAsset mobileAsset;public Shader shader;public ComputeShader sorter;public bool previewInEditor;
 [Range(1,32)]public int sortStagesPerFrame=12;[Range(.5f,3)]public float sizeMultiplier=2.8f;
 public int RenderedCount=>mobileAsset?(int)mobileAsset.SplatCount:0;
 GraphicsBuffer positions,scales,rotations,colours,work,order;Material mat;MaterialPropertyBlock properties;int count,init,sort,publish;int stageK=2,stageJ=1;bool sorting;Bounds bounds;
 void OnEnable(){if(!mobileAsset||!shader||!sorter)return;if(!SystemInfo.supportsComputeShaders){Debug.LogError("MONET_PICO_COMPUTE_UNSUPPORTED: Vulkan compute is required for mobile Gaussian sorting.");return;}count=(int)mobileAsset.SplatCount;
 positions=Buffer(count,12,mobileAsset.Positions);scales=Buffer(count,12,mobileAsset.Scales);rotations=Buffer(count,16,mobileAsset.Rotations);colours=Buffer(count,16,mobileAsset.Colors);work=new GraphicsBuffer(GraphicsBuffer.Target.Structured,count,8);order=new GraphicsBuffer(GraphicsBuffer.Target.Structured,count,4);
 uint[] ids=new uint[count];for(uint i=0;i<count;i++)ids[i]=i;order.SetData(ids);
 mat=new Material(shader);properties=new MaterialPropertyBlock();properties.SetBuffer("_PositionBuffer",positions);properties.SetBuffer("_ScaleBuffer",scales);properties.SetBuffer("_RotationBuffer",rotations);properties.SetBuffer("_ColorBuffer",colours);properties.SetBuffer("_OrderBuffer",order);
 init=sorter.FindKernel("Init");sort=sorter.FindKernel("Sort");publish=sorter.FindKernel("Publish");
 foreach(var k in new[]{init,sort,publish}){sorter.SetBuffer(k,"_Positions",positions);sorter.SetBuffer(k,"_Work",work);sorter.SetBuffer(k,"_Order",order);}
 bounds=new Bounds(transform.position,Vector3.one*200);Debug.Log("MONET_PICO_MOBILE_GAUSSIANS: "+count+" anisotropic 3D Gaussians; SH0; no subgroup instructions.");
 }
 static GraphicsBuffer Buffer<T>(int n,int stride,T[] data){var b=new GraphicsBuffer(GraphicsBuffer.Target.Structured,n,stride);b.SetData(data);return b;}
 void Update(){if(mat==null)return;var f=GardenExperience.Active;var cam=f?f.player.viewCamera:Camera.main;if(!cam)return;if(f&&f.State==GardenExperience.ExperienceState.Selection)return;
 if(!sorting){sorter.SetInt("_Count",count);sorter.SetMatrix("_ViewModel",cam.worldToCameraMatrix*transform.localToWorldMatrix);sorter.Dispatch(init,(count+127)/128,1,1);stageK=2;stageJ=1;sorting=true;}
 for(int n=0;n<sortStagesPerFrame && sorting;n++){sorter.SetInt("_K",stageK);sorter.SetInt("_J",stageJ);sorter.Dispatch(sort,(count+127)/128,1,1);stageJ>>=1;if(stageJ==0){stageK<<=1;stageJ=stageK>>1;}if(stageK>count){sorter.Dispatch(publish,(count+127)/128,1,1);sorting=false;}}
 properties.SetFloat("_SceneOpacity",f&&f.State==GardenExperience.ExperienceState.Entering?f.StateProgress:f&&f.State==GardenExperience.ExperienceState.Returning?1-f.StateProgress:1);
 properties.SetMatrix("_MATRIX_M",transform.localToWorldMatrix);properties.SetFloat("_SizeMultiplier",sizeMultiplier);properties.SetFloat("_PointSize",f&&f.gaussianTransition?f.gaussianTransition.pointSize:.004f);
 Graphics.DrawProcedural(mat,bounds,MeshTopology.Triangles,6,count,null,properties,ShadowCastingMode.Off,false,gameObject.layer);
 if(f && f.player.VRControlled && f.State==GardenExperience.ExperienceState.Transition)f.EffectRendered=true;
 }
 void OnDisable(){positions?.Dispose();scales?.Dispose();rotations?.Dispose();colours?.Dispose();work?.Dispose();order?.Dispose();if(mat)Destroy(mat);mat=null;sorting=false;}
}
}
