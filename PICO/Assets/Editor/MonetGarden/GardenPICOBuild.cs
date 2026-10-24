using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace MonetGarden.Editor {
 public static class GardenPICOBuild {
 [MenuItem("Tools/Monet Garden/PICO/Build APK")]
 public static void Build() {
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/MonetGarden_Prototype.unity")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MonetGarden_Prototype.unity");
  GardenPICOSetup.Apply();
  Directory.CreateDirectory("Documentation/Validation");Directory.CreateDirectory("Builds/PICO4");
  var stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");string log="Documentation/Validation/PICO_Build_"+stamp+".txt";
  var inputSettings=new SerializedObject(Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);var inputProperty=inputSettings.FindProperty("activeInputHandler");int previousInput=inputProperty!=null?inputProperty.intValue:2;
  try {
   if(inputProperty!=null){inputProperty.intValue=1;inputSettings.ApplyModifiedPropertiesWithoutUndo();}
   if(EditorApplication.isPlaying)throw new Exception("Stop Play before building.");
   PlayerSettings.productName="MonetGarden PICO";PlayerSettings.companyName="Monet Garden";
   PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.monetgarden.pico4");
   PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
   PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
   PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
   PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
   PlayerSettings.graphicsJobs=false;PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
   PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,ManagedStrippingLevel.Low);
   PlayerSettings.Android.useCustomKeystore=false;EditorUserBuildSettings.buildAppBundle=false;
   AssetDatabase.SaveAssets();
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MonetGarden_Prototype.unity"},locationPathName="Builds/PICO4/MonetGarden_PICO4.apk",target=BuildTarget.Android,options=BuildOptions.None});
   string text="Time: "+DateTime.Now.ToString("O")+"\nResult: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSize: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+"\n";
   foreach(var step in report.steps)foreach(var m in step.messages)if(m.type==LogType.Error||m.type==LogType.Exception||m.type==LogType.Warning)text+=m.type+": "+m.content+"\n";
   File.WriteAllText(log,text);File.WriteAllText("Documentation/Validation/PICO_Build_Latest.txt",text);
   Debug.Log("MONET_PICO_BUILD_RESULT: "+report.summary.result);
  }catch(Exception e){File.WriteAllText(log,e.ToString());Debug.LogException(e);}
  finally{if(inputProperty!=null){inputSettings.Update();inputProperty.intValue=previousInput;inputSettings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}}
 }
 }
}