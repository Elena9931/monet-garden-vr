using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MonetGarden.Editor
{
    public static class GardenFinalUpgrade
    {
        const string Root="Assets/MonetGarden";
        [MenuItem("Tools/Monet Garden/Apply Final Design")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/MonetGarden_Prototype.unity")throw new InvalidOperationException("Prototype in Edit mode only.");
            var flow=UnityEngine.Object.FindObjectOfType<GardenExperience>();if(!flow)throw new InvalidOperationException("Existing prototype required.");
            var scene=SceneManager.GetActiveScene();
            // Explicit latest user request: remove human objects, including inactive placeholders.
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t && (t.name.StartsWith("Gardener_") || t.name.StartsWith("GardenerWaypoint_")))UnityEngine.Object.DestroyImmediate(t.gameObject);
            // Keep the detailed source lilies; coarse cylinder placeholders do not match the garden.
            foreach(var t in flow.dynamicEnvironment.GetComponentsInChildren<Transform>(true))
                if(t && t.name.StartsWith("FloatingLily_"))UnityEngine.Object.DestroyImmediate(t.gameObject);
            ConfigureTexture(Root+"/References/Reference03_MonetOriginal.png",true,4096);
            ConfigureTexture(Root+"/References/Reference02_ImpressionistEarth.png",false,2048);
            ConfigureTexture(Root+"/Rendering/ImpressionistEarth_Surface.png",false,2048);
            var artwork=flow.artworks[0];artwork.title="Claude Monet — user-selected Japanese bridge painting";artwork.year="";
            artwork.sourceAndRights="Exact Reference03 supplied by the user on 2026-10-09 for this local educational demo. Title/year and source institution unconfirmed. No replacement image generated.";
            artwork.originalPainting=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/References/Reference03_MonetOriginal.png");EditorUtility.SetDirty(artwork);
            var rendererPath=Root+"/Rendering/MonetGarden_OffscreenRenderer.asset";
            var offscreen=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if(!offscreen){offscreen=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Rendering/MonetGarden_Renderer.asset"));offscreen.name="MonetGarden_OffscreenRenderer";offscreen.rendererFeatures.Clear();AssetDatabase.CreateAsset(offscreen,rendererPath);}
            var map=new SerializedObject(offscreen);map.FindProperty("m_RendererFeatureMap").ClearArray();map.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(offscreen);
            var pipeline=new SerializedObject(flow.prototypePipeline);var renderers=pipeline.FindProperty("m_RendererDataList");renderers.arraySize=2;renderers.GetArrayElementAtIndex(1).objectReferenceValue=offscreen;pipeline.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(flow.prototypePipeline);
            flow.globeInterface=flow.GetComponent<GardenGlobe>();if(!flow.globeInterface)flow.globeInterface=flow.gameObject.AddComponent<GardenGlobe>();
            flow.globeInterface.surfaceTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Rendering/ImpressionistEarth_Surface.png");flow.globeInterface.globeShader=Shader.Find("MonetGarden/ImpressionistEarth");
            flow.reassembly=flow.GetComponent<GardenReassembly>();if(!flow.reassembly)flow.reassembly=flow.gameObject.AddComponent<GardenReassembly>();flow.reassembly.pigmentShader=Shader.Find("MonetGarden/PigmentParticles");
            // Components with runtime world roots must not share the experience transform.
            var globeRoot=GetOrCreate("ImpressionistGlobeInterface");var globe=globeRoot.GetComponent<GardenGlobe>();if(!globe)globe=globeRoot.AddComponent<GardenGlobe>();
            globe.surfaceTexture=flow.globeInterface.surfaceTexture;globe.globeShader=flow.globeInterface.globeShader;UnityEngine.Object.DestroyImmediate(flow.globeInterface);flow.globeInterface=globe;
            var paintRoot=GetOrCreate("PigmentReassemblyInterface");var paint=paintRoot.GetComponent<GardenReassembly>();if(!paint)paint=paintRoot.AddComponent<GardenReassembly>();paint.pigmentShader=flow.reassembly.pigmentShader;UnityEngine.Object.DestroyImmediate(flow.reassembly);flow.reassembly=paint;
            flow.requireConfirmation=false;flow.transitionSeconds=9;flow.entrySeconds=2.4f;flow.returnSeconds=2.4f;
            flow.player.viewCamera.cullingMask&=~((1<<30)|(1<<29));
            flow.viewpoint.name="Approximate Artist Viewpoint";flow.viewpoint.radius=.36f;flow.viewpoint.yawTolerance=8;flow.viewpoint.pitchTolerance=5;flow.viewpoint.holdSeconds=3;
            flow.viewpoint.transform.SetPositionAndRotation(new Vector3(.72f,.8f,.16f),Quaternion.Euler(-5,188,0));
            var guide=GetOrCreate("QuietViewpointLights");guide.transform.position=flow.viewpoint.transform.position-Vector3.up*.73f;
            var ps=guide.GetComponent<ParticleSystem>();if(!ps)ps=guide.AddComponent<ParticleSystem>();var main=ps.main;main.startLifetime=3;main.startSpeed=.018f;main.startSize=.025f;main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
            var render=ps.GetComponent<ParticleSystemRenderer>();var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/QuietLight.mat");if(!mat){mat=new Material(Shader.Find("MonetGarden/PigmentParticles"));AssetDatabase.CreateAsset(mat,Root+"/Materials/QuietLight.mat");}render.sharedMaterial=mat;
            var guideLogic=guide.GetComponent<GardenViewpointLight>();if(!guideLogic)guideLogic=guide.AddComponent<GardenViewpointLight>();guideLogic.lights=ps;
            foreach(string sound in new[]{"Wind","Water","Birds"})
            {var go=GetOrCreateChild(flow.dynamicEnvironment.transform,"SynthesizedAmbient_"+sound);var a=go.GetComponent<AudioSource>();if(!a)a=go.AddComponent<AudioSource>();a.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Synthesized_"+sound+".wav");a.loop=true;a.playOnAwake=true;a.volume=.035f;a.spatialBlend=0;}
            var water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WaterRipples.mat");if(water){water.SetColor("_Tint",new Color(.62f,.81f,.72f,.42f));water.SetFloat("_Speed",.4f);EditorUtility.SetDirty(water);}
            AddLocalFlowers(flow.dynamicEnvironment.transform);
            ConfigurePaths();
            foreach(var f in AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Rendering/MonetGarden_Renderer.asset").rendererFeatures)if(f is GardenPainterlyFeature painterly){painterly.shader=Shader.Find("MonetGarden/PainterlyTransition");EditorUtility.SetDirty(painterly);}
            EditorUtility.SetDirty(flow);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Documentation/Validation/final_upgrade.txt",$"{DateTime.Now:O}\nPrototype updated. Human placeholders removed. Exact user original {artwork.originalPainting.width}x{artwork.originalPainting.height}. Separate offscreen renderer configured. Approximate viewpoint requires visual inspection.\n");
            Debug.Log("MONET_FINAL_UPGRADE_SAVED");
        }
        static void ConfigureTexture(string path,bool readable,int size)
        {var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new InvalidOperationException("Texture not imported: "+path);importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.sRGBTexture=true;importer.isReadable=readable;importer.maxTextureSize=size;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.wrapMode=path.Contains("Surface")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.SaveAndReimport();}
        static GameObject GetOrCreate(string name){return GameObject.Find(name)??new GameObject(name);}
        static GameObject GetOrCreateChild(Transform parent,string name){var t=parent.Find(name);if(t)return t.gameObject;var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
        static void AddLocalFlowers(Transform parent)
        {
            var group=GetOrCreateChild(parent,"LocalFlowerSway");if(group.transform.childCount>0)return;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/LeafRibbon.asset");var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Willow.mat");
            for(int j=0;j<12;j++){var f=new GameObject("SmallFlowerLeaf_"+j);f.transform.SetParent(group.transform);f.transform.position=new Vector3(2.15f+(j%4)*.08f,.08f,-2.8f-(j/4)*.15f);f.transform.rotation=Quaternion.Euler(0,j*71,65+j%3*6);f.transform.localScale=Vector3.one*.18f;f.AddComponent<MeshFilter>().sharedMesh=mesh;f.AddComponent<MeshRenderer>().sharedMaterial=material;var motion=f.AddComponent<GardenAmbientMotion>();motion.amplitude=.025f;motion.phase=j*1.917f;motion.frequency=.45f+(j%3)*.09f;}
        }
        static void ConfigurePaths()
        {
            var root=GameObject.Find("GardenProxyColliders_PROVISIONAL").transform;
            // Retain the tested approach. Open only the locally sampled gravel branch.
            var bank=GameObject.Find("BankPath_Floor");bank.transform.position=new Vector3(1.62f,-.16f,-1.4f);bank.GetComponent<BoxCollider>().size=new Vector3(1.25f,.15f,3.3f);
            var end=GameObject.Find("BankEndBoundary");end.transform.position=new Vector3(2.58f,.65f,-4.32f);end.GetComponent<BoxCollider>().size=new Vector3(.9f,1.6f,.16f);
            var pond=GameObject.Find("BankPondBoundary");pond.transform.position=new Vector3(.91f,.65f,-1.64f);pond.GetComponent<BoxCollider>().size=new Vector3(.16f,1.6f,2.5f);
            var plants=GameObject.Find("BankVegetationBoundary");plants.transform.position=new Vector3(2.34f,.65f,-1.2f);plants.GetComponent<BoxCollider>().size=new Vector3(.18f,1.6f,3.0f);
            Segment(root,"GravelBend_0",new Vector3(1.62f,-.16f,-2.65f),new Vector3(2.58f,-.16f,-3.18f),.75f);
            Segment(root,"GravelBend_1",new Vector3(2.58f,-.16f,-3.18f),new Vector3(2.58f,-.16f,-4.25f),.72f);
        }
        static void Segment(Transform parent,string name,Vector3 a,Vector3 b,float width)
        {
            var floor=GetOrCreateChild(parent,name);floor.transform.position=(a+b)*.5f;floor.transform.rotation=Quaternion.LookRotation((b-a).normalized);var c=floor.GetComponent<BoxCollider>();if(!c)c=floor.AddComponent<BoxCollider>();c.size=new Vector3(width,.15f,Vector3.Distance(a,b)+.2f);if(!floor.GetComponent<GardenProxyGizmo>())floor.AddComponent<GardenProxyGizmo>();
            for(int sign=-1;sign<=1;sign+=2){var wall=GetOrCreateChild(parent,name+"_Edge_"+sign);wall.transform.SetPositionAndRotation(floor.transform.position+floor.transform.right*sign*(width*.5f+.035f)+Vector3.up*.65f,floor.transform.rotation);var box=wall.GetComponent<BoxCollider>();if(!box)box=wall.AddComponent<BoxCollider>();box.size=new Vector3(.07f,1.45f,Vector3.Distance(a,b));if(!wall.GetComponent<GardenProxyGizmo>())wall.AddComponent<GardenProxyGizmo>();}
        }
    }
}
