using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MonetGarden.Editor {
public static class GardenRound2Upgrade {
const string Root="Assets/MonetGarden";
public static void BatchApply(){EditorSceneManager.OpenScene("Assets/Scenes/MonetGarden_Prototype.unity");Apply();}
[MenuItem("Tools/Monet Garden/Apply Round 2 Incremental Upgrade")]
public static void Apply(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/MonetGarden_Prototype.unity")throw new InvalidOperationException("Prototype edit mode required");var f=UnityEngine.Object.FindObjectOfType<GardenExperience>();if(!f)throw new InvalidOperationException("Existing experience missing");
string path=Root+"/Rendering/Earth_Impressionist_Albedo.png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=4096;ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.npotScale=TextureImporterNPOTScale.None;ti.wrapModeU=TextureWrapMode.Repeat;ti.wrapModeV=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.SaveAndReimport();
f.globeInterface.surfaceTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);f.globeInterface.surfaceMaterial=Material(Root+"/Materials/M_Earth_Impressionist.mat","MonetGarden/ImpressionistEarth");f.globeInterface.surfaceMaterial.SetTexture("_MainTex",f.globeInterface.surfaceTexture);EditorUtility.SetDirty(f.globeInterface.surfaceMaterial);f.globeInterface.atmosphereMaterial=Material(Root+"/Materials/M_Earth_Atmosphere.mat","MonetGarden/Atmosphere");
f.guide=f.GetComponent<GardenGuide>();if(!f.guide)f.guide=f.gameObject.AddComponent<GardenGuide>();f.guide.glassShader=Shader.Find("MonetGarden/FrostedGuide");f.gaussianTransition=f.GetComponent<GardenGaussianTransition>();if(!f.gaussianTransition)f.gaussianTransition=f.gameObject.AddComponent<GardenGaussianTransition>();f.gaussianTransition.rendererSource=UnityEngine.Object.FindObjectOfType<Gsplat.GsplatRenderer>();f.transitionSeconds=7;
f.reassembly.particleCount=18000;f.reassembly.pigmentShader=Shader.Find("MonetGarden/GPUReassembly");
// Preserve calibrated eye anchor and collisions. Scale cue relative to existing 0.82-unit eye height.
var cue=UnityEngine.Object.FindObjectOfType<GardenViewpointLight>();cue.markerSize=.11f;cue.groundClearance=.13f;cue.floatAmplitude=.035f;cue.floatPeriod=2;var shape=cue.lights.shape;shape.radius=.065f;var main=cue.lights.main;main.startSize=.015f;main.startLifetime=2;
f.viewpoint.yawTolerance=16;f.viewpoint.pitchTolerance=8;f.viewpoint.holdSeconds=3;
foreach(var o in new UnityEngine.Object[]{f,f.guide,f.gaussianTransition,f.globeInterface,f.reassembly,f.viewpoint,cue})EditorUtility.SetDirty(o);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Directory.CreateDirectory("Documentation/Validation");File.WriteAllText("Documentation/Validation/round2_upgrade.txt",DateTime.Now.ToString("O")+" Incremental upgrade saved. Compile/Play verification separate.");}
static Material Material(string path,string shaderName){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shaderName));AssetDatabase.CreateAsset(m,path);}return m;}
}}
