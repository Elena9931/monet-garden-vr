using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
namespace MonetGarden.Editor {
[InitializeOnLoad] public static class GardenXRRegression {
 static GardenXRRegression(){EditorApplication.playModeStateChanged+=Changed;}
 public static void BuildUnified(){PlayerSettings.bundleVersion="0.1.2-fieldfix";PlayerSettings.Android.bundleVersionCode=3;try{GardenPICOBuild.Build();}finally{RestoreBoth();}}
 public static void PrepareInputOnly(){var o=new SerializedObject(Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);o.FindProperty("activeInputHandler").intValue=1;o.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}
 public static void RestoreBoth(){var o=new SerializedObject(Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);o.FindProperty("activeInputHandler").intValue=2;o.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/MonetGarden_Prototype.unity");GardenPICOSetup.Apply();
  UnityEngine.Object.FindObjectOfType<GardenXRBridge>().simulateInEditor=true;
  SessionState.SetBool("UnifiedXRTesting",true);
  EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.isPlaying=true;
 }
 static void Changed(PlayModeStateChange s){
  if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("UnifiedXRTesting",false)){SessionState.SetBool("UnifiedXRTesting",false);new GameObject("XRFullRegression").AddComponent<GardenXRRegressionProbe>();}
 }
}
}
