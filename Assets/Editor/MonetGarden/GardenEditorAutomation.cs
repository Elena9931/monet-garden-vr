using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonetGarden.Editor
{
    [InitializeOnLoad]
    public static class GardenEditorAutomation
    {
        const string RequestPath="Documentation/Validation/request.json";
        static double nextPoll;
        [Serializable] class Request { public string id; public string action; }
        static GardenEditorAutomation()
        {
            EditorApplication.update+=Poll;
            Application.logMessageReceived+=Log;
            CompilationPipeline.assemblyCompilationFinished+=CompilerFinished;
        }
        static void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert && !message.StartsWith("MONET_")) return;
            Directory.CreateDirectory("Documentation/Validation");
            File.AppendAllText("Documentation/Validation/editor_messages.log",$"{DateTime.Now:O} [{type}] {message}\n{stack}\n");
        }
        static void CompilerFinished(string assembly,CompilerMessage[] messages)
        {
            Directory.CreateDirectory("Documentation/Validation");
            foreach(var m in messages) File.AppendAllText("Documentation/Validation/compiler.log",$"{assembly}: {m.type}: {m.message}\n");
        }
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll=EditorApplication.timeSinceStartup+.5;
            if(!File.Exists(RequestPath))return;
            Request request;
            try { request=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath)); } catch { return; }
            if(request==null || string.IsNullOrEmpty(request.id) || request.id==SessionState.GetString("MonetRequest",""))return;
            SessionState.SetString("MonetRequest",request.id);
            try
            {
                if(SceneManager.GetActiveScene().path!="Assets/Scenes/MonetGarden_Prototype.unity")
                    throw new InvalidOperationException("Automation only operates on the prototype scene.");
                if(request.action=="validate")RunValidation();
                else if(request.action=="final-upgrade" && !EditorApplication.isPlaying)GardenFinalUpgrade.Apply();
                else if(request.action=="play")EditorApplication.isPlaying=true;
                else if(request.action=="stop")EditorApplication.isPlaying=false;
                else if(request.action=="save" && !EditorApplication.isPlaying)EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                else if(request.action=="explore" && GardenExperience.Active)GardenExperience.Active.EnterGarden();
                else if(request.action=="map" && GardenExperience.Active)GardenExperience.Active.ResetExperience(true);
                else if(request.action=="audit")Audit();
                else if(request.action=="calibrate" && !EditorApplication.isPlaying)Calibrate();
                else if(request.action=="evidence-collision" && !EditorApplication.isPlaying)ShowCollisionEvidence();
                else if(request.action=="evidence-viewpoint" && EditorApplication.isPlaying)ShowViewpointEvidence();
                else if(request.action=="evidence-dynamics" && EditorApplication.isPlaying)
                    Selection.activeGameObject=GameObject.Find("LocalDynamicEnvironment");
                else if(request.action=="build" && !EditorApplication.isPlaying)EditorApplication.delayCall+=GardenDesktopBuild.Build;
                else if(request.action=="clean-legacy" && !EditorApplication.isPlaying)GardenPrototypeBuilder.DetachDisabledTimelineAssets();
                else throw new InvalidOperationException("Unknown action or invalid Play/Edit state: "+request.action);
                File.WriteAllText("Documentation/Validation/automation.txt",request.id+" / "+request.action+" / completed "+DateTime.Now.ToString("O"));
            }
            catch(Exception e){Debug.LogException(e);}
        }

        static void ShowCollisionEvidence()
        {
            Selection.activeGameObject=GameObject.Find("player");
            var view=SceneView.lastActiveSceneView;
            if(view){view.LookAt(new Vector3(.7f,0,-1.8f),Quaternion.Euler(65,150,0),4.5f);view.drawGizmos=true;view.Focus();view.Repaint();}
        }
        static void ShowViewpointEvidence()
        {
            Selection.activeGameObject=UnityEngine.Object.FindObjectOfType<GardenViewpoint>().gameObject;
            // Selection only: never fabricate a detection result or alter the live camera.
        }

        static void Calibrate()
        {
            GameObject.Find("BankPath_Floor").transform.position=new Vector3(1.62f,-.16f,-2.45f);
            var ramp=GameObject.Find("Approach_Ramp");ramp.transform.position=new Vector3(1.02f,-.06f,.14f);
            ramp.transform.rotation=Quaternion.Euler(0,0,-4);ramp.GetComponent<BoxCollider>().size=new Vector3(.7f,.05f,1.1f);
            GameObject.Find("Gardener_PROCEDURAL_PLACEHOLDER").transform.position=new Vector3(2.8f,-.06f,-2.4f);
            GameObject.Find("GardenerWaypoint_0").transform.position=new Vector3(2.6f,-.08f,-2);
            GameObject.Find("GardenerWaypoint_1").transform.position=new Vector3(3,-.06f,-3.2f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("MONET_CALIBRATION: bank height from local PLY sample, actor moved onto sampled bank path; visual approval still pending.");
        }

        [MenuItem("Tools/Monet Garden/Run Play Validation")]
        public static void RunValidation()
        {
            if(EditorApplication.isPlaying){
                if(!UnityEngine.Object.FindObjectOfType<GardenValidationProbe>())new GameObject("MonetValidationProbe").AddComponent<GardenValidationProbe>();
            }
            else { SessionState.SetBool("MonetValidateOnPlay",true);EditorApplication.isPlaying=true; }
        }
        [InitializeOnEnterPlayMode]
        static void EnterPlay(EnterPlayModeOptions options)
        {
            EditorApplication.delayCall+=()=>
            {
                if(!SessionState.GetBool("MonetValidateOnPlay",false))return;
                SessionState.SetBool("MonetValidateOnPlay",false);
                if(EditorApplication.isPlaying)new GameObject("MonetValidationProbe").AddComponent<GardenValidationProbe>();
            };
        }
        [MenuItem("Tools/Monet Garden/Audit Saved Scene")]
        public static void Audit()
        {
            Directory.CreateDirectory("Documentation/Validation");
            var text=$"Unity {Application.unityVersion}\nScene {SceneManager.GetActiveScene().path}\nPlay {EditorApplication.isPlaying}\nGPU {SystemInfo.graphicsDeviceName} / {SystemInfo.graphicsDeviceType}\n";
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                int missing=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if(missing>0)text+=$"MISSING {t.name}: {missing}\n";
            }
            foreach(var name in new[]{"MonetGarden/PainterlyTransition","MonetGarden/LocalWaterRipples","MonetGarden/LocalFoliageSway","MonetGarden/ImpressionistEarth","MonetGarden/PigmentParticles"})
            {
                var shader=Shader.Find(name);text+=$"Shader {name}: found={shader!=null}, supported={(shader ? shader.isSupported : false)}\n";
                if(shader)foreach(var m in ShaderUtil.GetShaderMessages(shader))text+=$"Shader diagnostic {m.severity}: {m.message}\n";
            }
            var flow=UnityEngine.Object.FindObjectOfType<GardenExperience>();
            if(flow)text+=$"State={flow.State}, progress={flow.Progress}, effectRendered={flow.EffectRendered}\n";
            File.WriteAllText("Documentation/Validation/audit.txt",text);
            Debug.Log("MONET_AUDIT_WRITTEN");
        }
    }
}
