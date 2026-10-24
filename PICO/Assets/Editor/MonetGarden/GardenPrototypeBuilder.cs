using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Gsplat;

namespace MonetGarden.Editor
{
    [InitializeOnLoad]
    public static class GardenPrototypeBuilder
    {
        const string ScenePath = "Assets/Scenes/MonetGarden_Prototype.unity";
        const string Root = "Assets/MonetGarden";
        static GardenPrototypeBuilder() { EditorApplication.delayCall += AutoConfigure; }
        static void AutoConfigure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (SceneManager.GetActiveScene().path == ScenePath && !GameObject.Find("MonetGardenExperience"))
            {
                try { Configure(); } catch(Exception e) { Debug.LogException(e); }
            }
        }

        [MenuItem("Tools/Monet Garden/Configure Prototype")]
        public static void Configure()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Open MonetGarden_Prototype in Edit mode. The original scene is protected.");
            if (GameObject.Find("MonetGardenExperience")) { Debug.Log("Monet: prototype already configured. Edit its Inspector values."); return; }
            Directory.CreateDirectory("Documentation/Backups");
            File.Copy(ScenePath, "Documentation/Backups/RecoveredGarden_BeforeChanges.unity", false);
            foreach(var folder in new []{Root,Root+"/Rendering",Root+"/Meshes",Root+"/Materials",Root+"/Animation",Root+"/Artwork"}) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            int missing = 0;
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count > 0) { missing += count; GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject); }
            }
            foreach(var name in new[]{"WalkableGround_Test","Dolly Track","Virtual Camera","CameraSequence","Gsplat Shape View","Cube Line Reveal Experiment"})
            { var obj = GameObject.Find(name); if(obj) obj.SetActive(false); }
            DetachDisabledTimelineAssets();
            var gs = UnityEngine.Object.FindObjectOfType<GsplatRenderer>();
            if (!gs || !gs.GsplatAsset) throw new InvalidOperationException("The recovered scene needs its existing GSplat asset.");
            gs.enabled = true;
            var playerObject = GameObject.Find("player");
            var camera = playerObject ? playerObject.GetComponentInChildren<Camera>() : null;
            if (!camera) throw new InvalidOperationException("The recovered player/Main Camera hierarchy is missing.");
            var oldWalk = playerObject.GetComponent<GardenWalkController>();
            if (oldWalk) oldWalk.enabled = false;
            var cc = playerObject.GetComponent<CharacterController>();
            cc.height = .9f; cc.radius = .16f; cc.center = new Vector3(0,.45f,0);
            cc.stepOffset = .1f; cc.skinWidth = .018f; cc.minMoveDistance = 0; cc.slopeLimit = 45;
            var walk = playerObject.AddComponent<GardenDesktopController>();
            walk.viewCamera = camera;
            var vp = new GameObject("MonetViewpoint_PROPOSED").AddComponent<GardenViewpoint>();
            vp.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);

            var proxies = new GameObject("GardenProxyColliders_PROVISIONAL").transform;
            // A narrow crossing/approach around the imported initial viewpoint, plus a bank route.
            // These are explicit editable proxies, never a floor covering the whole pond.
            Box("InitialApproach_Floor",proxies,new Vector3(.0f,-.11f,.16f),new Vector3(2.8f,.15f,1.15f),false);
            Box("BankPath_Floor",proxies,new Vector3(1.62f,-.16f,-2.45f),new Vector3(1.25f,.15f,5.4f),false);
            var ramp=Box("Approach_Ramp",proxies,new Vector3(1.02f,-.06f,.14f),new Vector3(.7f,.05f,1.1f),false);
            ramp.transform.rotation=Quaternion.Euler(0,0,-4);
            Box("NorthWaterBoundary",proxies,new Vector3(.0f,.5f,.82f),new Vector3(3.1f,1.6f,.18f),true);
            Box("SouthPondBoundary",proxies,new Vector3(-.18f,.5f,-.48f),new Vector3(2.4f,1.6f,.18f),true);
            Box("WestBoundary",proxies,new Vector3(-1.49f,.5f,.17f),new Vector3(.18f,1.6f,1.48f),true);
            Box("BankPondBoundary",proxies,new Vector3(.91f,.65f,-2.62f),new Vector3(.16f,1.6f,4.45f),true);
            Box("BankVegetationBoundary",proxies,new Vector3(2.34f,.65f,-2.4f),new Vector3(.18f,1.6f,6.4f),true);
            Box("BankEndBoundary",proxies,new Vector3(1.61f,.65f,-5.23f),new Vector3(1.6f,1.6f,.16f),true);
            // Close the outer lip while retaining the connection from the approach to the bank.
            Box("ApproachEastBoundary",proxies,new Vector3(1.63f,.65f,.55f),new Vector3(1.4f,1.6f,.16f),true);

            var ambient = new GameObject("LocalDynamicEnvironment");
            var foliageMaterial=MaterialAsset("Willow",Shader.Find("MonetGarden/LocalFoliageSway"),new Color(.32f,.49f,.13f));
            var waterMaterial=MaterialAsset("WaterRipples",Shader.Find("MonetGarden/LocalWaterRipples"),new Color(.62f,.81f,.72f));
            var leafMesh=LeafMesh();AssetDatabase.CreateAsset(leafMesh,Root+"/Meshes/LeafRibbon.asset");
            for(int j=0;j<4;j++)
            {
                var stem=new GameObject("Willow_Sway_"+j).transform;stem.SetParent(ambient.transform);
                stem.position=new Vector3(-1.05f-j*.05f,1.65f,-1.2f-j*.16f);
                for(int i=0;i<14;i++)
                {
                    var leaf=new GameObject("Leaf_"+i);leaf.transform.SetParent(stem,false);
                    leaf.transform.localPosition=new Vector3(Mathf.Sin(i*.7f)*.04f,-i*.07f,Mathf.Cos(i)*.015f);
                    leaf.transform.localRotation=Quaternion.Euler(0,i*43,0);
                    leaf.AddComponent<MeshFilter>().sharedMesh=leafMesh;leaf.AddComponent<MeshRenderer>().sharedMaterial=foliageMaterial;
                }
                var sway=stem.gameObject.AddComponent<GardenAmbientMotion>();sway.amplitude=.04f;sway.phase=j*1.2f;
            }
            for(int i=0;i<3;i++)
            {
                var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="LocalRipplePatch_"+i;
                UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
                water.transform.SetParent(ambient.transform);water.transform.position=new Vector3(-.3f-i*.22f,-.17f,-1.6f-i*.5f);
                water.transform.localScale=Vector3.one*.1f;water.GetComponent<Renderer>().sharedMaterial=waterMaterial;
            }
            var lilyMaterial=MaterialAsset("Lily",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.27f,.39f,.12f));
            for(int i=0;i<5;i++)
            {
                var lily=Primitive("FloatingLily_"+i,ambient.transform,PrimitiveType.Cylinder,new Vector3(-.58f+i*.19f,-.155f,-2.25f-(i%2)*.21f),new Vector3(.14f,.007f,.13f),lilyMaterial);
                var motion=lily.AddComponent<GardenAmbientMotion>();motion.motion=GardenAmbientMotion.Motion.Float;motion.amplitude=.007f;motion.phase=i;
            }
            var birdMaterial=MaterialAsset("Bird",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.18f,.22f,.24f));
            var wing=new Mesh {name="BirdWing"};wing.vertices=new[]{Vector3.zero,new Vector3(.11f,0,-.035f),new Vector3(.09f,0,.055f)};
            wing.triangles=new[]{0,1,2,2,1,0};wing.RecalculateNormals();AssetDatabase.CreateAsset(wing,Root+"/Meshes/BirdWing.asset");
            for(int i=0;i<3;i++)
            {
                var bird=new GameObject("OrbitBird_"+i).transform;bird.SetParent(ambient.transform);bird.position=new Vector3(.4f,2.8f+i*.2f,-3.2f);
                Primitive("Body",bird,PrimitiveType.Sphere,Vector3.zero,new Vector3(.035f,.03f,.09f),birdMaterial,true);
                var l=MeshObject("LeftWing",bird,wing,birdMaterial);l.localScale=new Vector3(-1,1,1);
                var r=MeshObject("RightWing",bird,wing,birdMaterial);
                var flight=bird.gameObject.AddComponent<GardenAmbientMotion>();flight.motion=GardenAmbientMotion.Motion.Bird;flight.phase=i*2;flight.frequency=.45f;flight.leftWing=l;flight.rightWing=r;
            }
            // Human-free final design; old generation method retained only as source history.

            var renderer = BuildRenderer();
            var originalPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!originalPipeline) throw new InvalidOperationException("Current renderer must remain the installed URP pipeline.");
            var pipeline=UnityEngine.Object.Instantiate(originalPipeline);pipeline.name="MonetGarden_URP";
            var serializedPipeline=new SerializedObject(pipeline);
            var dataList=serializedPipeline.FindProperty("m_RendererDataList");dataList.arraySize=1;dataList.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue=0;serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline,Root+"/Rendering/MonetGarden_URP.asset");
            var artwork=ScriptableObject.CreateInstance<GardenArtwork>();
            AssetDatabase.CreateAsset(artwork,Root+"/Artwork/Giverny.asset");
            var flow=new GameObject("MonetGardenExperience").AddComponent<GardenExperience>();
            flow.player=walk;flow.viewpoint=vp;flow.artworks=new[]{artwork};flow.dynamicEnvironment=ambient;flow.prototypePipeline=pipeline;flow.painterlyShader=Shader.Find("MonetGarden/PainterlyTransition");
            var previewLight=new GameObject("PrototypeFillLight").AddComponent<Light>();previewLight.type=LightType.Directional;
            previewLight.intensity=.65f;previewLight.transform.rotation=Quaternion.Euler(45,-30,0);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Documentation/Validation");
            File.WriteAllText("Documentation/Validation/setup.txt",$"Configured {DateTime.Now:O}\nRecovered GSplat count: {gs.SplatCount}\nPreserved GSplat transform: {gs.transform.position} / {gs.transform.rotation.eulerAngles} / {gs.transform.localScale}\nOriginal camera eye: {camera.transform.position} / {camera.transform.eulerAngles}\nMissing legacy components removed in COPY: {missing}\nProxy layout is provisional; metric scene scale is unverified.\nOriginal artwork/model/XR device not supplied.\n");
            Debug.Log("MONET_SETUP_COMPLETE: prototype scene configured; original SampleScene and plugin preserved.");
        }

        public static void DetachDisabledTimelineAssets()
        {
            // Keep disabled sample objects and all source timeline assets, but avoid packaging
            // their unresolved Cinemachine tracks through the demo scene's Director references.
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var director in root.GetComponentsInChildren<UnityEngine.Playables.PlayableDirector>(true))
                {
                    if(director.gameObject.activeInHierarchy)continue;
                    director.playableAsset=null;
                    var serialized=new SerializedObject(director);
                    serialized.FindProperty("m_SceneBindings").ClearArray();
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,bool boundary)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;
            go.AddComponent<BoxCollider>().size=size;go.AddComponent<GardenProxyGizmo>().waterOrBoundary=boundary;return go;
        }
        static Material MaterialAsset(string name,Shader shader,Color tint)
        {
            if(!shader) throw new InvalidOperationException("Missing shader for "+name);
            var m=new Material(shader){name=name};
            if(m.HasProperty("_Tint"))m.SetColor("_Tint",tint);
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",tint);
            AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;
        }
        static Mesh LeafMesh()
        {
            var m=new Mesh{name="LeafRibbon"};m.vertices=new[]{new Vector3(0,0,0),new Vector3(-.016f,-.07f,0),new Vector3(0,-.16f,0),new Vector3(.016f,-.07f,0)};
            m.uv=new[]{new Vector2(.5f,0),new Vector2(0,.5f),new Vector2(.5f,1),new Vector2(1,.5f)};
            m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();return m;
        }
        static Transform MeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
        static GameObject Primitive(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool local=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            if(local)go.transform.localPosition=position;else go.transform.position=position;
            go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }

        static void BuildActor(Transform parent)
        {
            var coat=MaterialAsset("GardenerCoat",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.27f,.3f,.21f));
            var trousers=MaterialAsset("GardenerTrousers",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.15f,.18f,.17f));
            var skin=MaterialAsset("GardenerSkin",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.6f,.48f,.36f));
            var hat=MaterialAsset("GardenerHat",Shader.Find("Universal Render Pipeline/Unlit"),new Color(.58f,.48f,.28f));
            var actor=new GameObject("Gardener_PROCEDURAL_PLACEHOLDER").transform;actor.SetParent(parent);actor.position=new Vector3(2.8f,-.06f,-2.4f);
            Primitive("Coat",actor,PrimitiveType.Capsule,new Vector3(0,.48f,0),new Vector3(.2f,.18f,.14f),coat,true);
            Primitive("Head",actor,PrimitiveType.Sphere,new Vector3(0,.75f,0),Vector3.one*.13f,skin,true);
            Primitive("HatBrim",actor,PrimitiveType.Cylinder,new Vector3(0,.82f,0),new Vector3(.24f,.015f,.24f),hat,true);
            Primitive("HatTop",actor,PrimitiveType.Cylinder,new Vector3(0,.86f,0),new Vector3(.14f,.04f,.14f),hat,true);
            foreach(var pair in new[]{new[]{"LeftArm","LeftLeg"},new[]{"RightArm","RightLeg"}})
            {
                float sign=pair[0].StartsWith("Left")?-1:1;
                var arm=new GameObject(pair[0]).transform;arm.SetParent(actor,false);arm.localPosition=new Vector3(sign*.11f,.61f,0);
                Primitive("Mesh",arm,PrimitiveType.Capsule,new Vector3(0,-.14f,0),new Vector3(.06f,.15f,.06f),coat,true);
                var leg=new GameObject(pair[1]).transform;leg.SetParent(actor,false);leg.localPosition=new Vector3(sign*.06f,.38f,0);
                Primitive("Mesh",leg,PrimitiveType.Capsule,new Vector3(0,-.17f,0),new Vector3(.065f,.17f,.065f),trousers,true);
            }
            var controller=new AnimatorController();AssetDatabase.CreateAsset(controller,Root+"/Animation/Gardener.controller");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Paused",AnimatorControllerParameterType.Bool);controller.AddLayer("Base");
            var machine=controller.layers[0].stateMachine;
            var idle=machine.AddState("Idle");var walking=machine.AddState("Walk");var pause=machine.AddState("Pause");machine.defaultState=idle;
            idle.motion=ActorClip("Idle",2,.8f);walking.motion=ActorClip("Walk",1,19);pause.motion=ActorClip("Pause",2,2);
            Transition(idle,walking,"Speed",AnimatorConditionMode.Greater,.01f);
            Transition(pause,walking,"Speed",AnimatorConditionMode.Greater,.01f);
            Transition(walking,pause,"Speed",AnimatorConditionMode.Less,.01f);
            Transition(idle,pause,"Paused",AnimatorConditionMode.If,0);
            Transition(pause,idle,"Paused",AnimatorConditionMode.IfNot,0);
            var animator=actor.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var route=actor.gameObject.AddComponent<GardenActorPath>();route.animator=animator;
            route.waypoints=new Transform[2];
            for(int i=0;i<2;i++)
            {var point=new GameObject("GardenerWaypoint_"+i).transform;point.SetParent(parent);point.position=i==0?new Vector3(2.6f,-.08f,-2):new Vector3(3,-.06f,-3.2f);route.waypoints[i]=point;}
        }
        static void Transition(AnimatorState from,AnimatorState to,string parameter,AnimatorConditionMode mode,float threshold)
        {
            var transition=from.AddTransition(to);transition.hasExitTime=false;transition.duration=.15f;transition.AddCondition(mode,threshold,parameter);
        }
        static AnimationClip ActorClip(string name,float duration,float amplitude)
        {
            var clip=new AnimationClip{name=name};
            foreach(var part in new[]{"LeftArm","RightArm","LeftLeg","RightLeg"})
            {
                float sign=part.StartsWith("Left")?1:-1;if(part.Contains("Arm"))sign=-sign;
                var curve=new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.25f,sign*amplitude),new Keyframe(duration*.5f,0),new Keyframe(duration*.75f,-sign*amplitude),new Keyframe(duration,0));
                clip.SetCurve(part,typeof(Transform),"localEulerAnglesRaw.x",curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,Root+"/Animation/"+name+".anim");return clip;
        }
        static UniversalRendererData BuildRenderer()
        {
            var original=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");
            var renderer=UnityEngine.Object.Instantiate(original);renderer.name="MonetGarden_Renderer";
            renderer.rendererFeatures.Clear();
            AssetDatabase.CreateAsset(renderer,Root+"/Rendering/MonetGarden_Renderer.asset");
            foreach(var originalFeature in original.rendererFeatures)
            {
                var feature=UnityEngine.Object.Instantiate(originalFeature);feature.name=originalFeature.name;
                AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);
            }
            var painterly=ScriptableObject.CreateInstance<GardenPainterlyFeature>();painterly.name="MonetPainterly";painterly.shader=Shader.Find("MonetGarden/PainterlyTransition");
            AssetDatabase.AddObjectToAsset(painterly,renderer);renderer.rendererFeatures.Add(painterly);
            // Synchronise URP's serialized local file-id map so features survive a project reopen.
            var serialized=new SerializedObject(renderer);var map=serialized.FindProperty("m_RendererFeatureMap");map.arraySize=renderer.rendererFeatures.Count;
            for(int i=0;i<renderer.rendererFeatures.Count;i++)
            {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string guid,out long id);map.GetArrayElementAtIndex(i).longValue=id;}
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);return renderer;
        }
    }
}
