using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonetGarden
{
    public class GardenExperience : MonoBehaviour
    {
        public enum ExperienceState { Selection, Entering, Exploration, Transition, Painting, Returning }
        public GardenDesktopController player;
        public GardenViewpoint viewpoint;
        public GardenArtwork[] artworks;
        public GameObject dynamicEnvironment;
        public RenderPipelineAsset prototypePipeline;
        public Shader painterlyShader;
        public GardenGlobe globeInterface;
        public GardenReassembly reassembly;
        [Range(4,20)] public float transitionSeconds=9;
        [Range(1,5)] public float entrySeconds=2.4f, returnSeconds=2.4f;
        public float explorationDistance=.4f;
        public bool requireConfirmation=false; // Retained for serialization; final experience always auto-triggers.
        public const double PaintingHoldSeconds=10.0;
        public ExperienceState State {get;private set;}=ExperienceState.Selection;
        public float Progress {get;private set;}
        public bool HasExplored {get;private set;}
        public GardenArtwork SelectedArtwork {get;private set;}
        public static GardenExperience Active {get;private set;}
        public bool EffectRendered {get;set;}
        public bool CaptureRequested {get;set;}
        public double PaintingStartedAt {get;private set;}=-1;
        public double LastPaintingHold {get;private set;}
        public int CompletedLoops {get;private set;}
        public float StateProgress {get;private set;}
        public bool ShowGuide=true;
        [Range(0,.5f)]public float ambientVolume=.12f;
        [Range(15,50)]public float compositionFieldOfView=24;
        [Range(.5f,5)]public float lensResponse=2;
        public bool IsMapVisible=>State==ExperienceState.Selection || State==ExperienceState.Entering || State==ExperienceState.Returning;
        RenderPipelineAsset previousPipeline;
        bool ownsPipeline;
        float stateElapsed;
        float explorationFieldOfView;
        int cycle;
        GUIStyle caption;

        void Start()
        {
            if(!player || !viewpoint){Debug.LogError("Monet: missing player/viewpoint.",this);enabled=false;return;}
            Active=this;
            explorationFieldOfView=player.viewCamera.fieldOfView;
            previousPipeline=QualitySettings.renderPipeline;
            if(prototypePipeline){QualitySettings.renderPipeline=prototypePipeline;ownsPipeline=true;}
            if(artworks!=null && artworks.Length>0)SelectedArtwork=artworks[0];
            if(globeInterface)globeInterface.Initialize(this);
            if(reassembly)reassembly.Initialize(this);
            ResetExperience(true);
        }
        public void EnterGarden(int index=0)
        {
            if(State!=ExperienceState.Selection || artworks==null || index<0 || index>=artworks.Length || !artworks[index])return;
            SelectedArtwork=artworks[index];Debug.Log("MONET_GARDEN_ENTRY: second selection confirmed");
            ResetWorld();
            State=ExperienceState.Entering;stateElapsed=0;StateProgress=0;
        }
        void ResetWorld()
        {
            cycle++;Progress=0;HasExplored=false;EffectRendered=false;CaptureRequested=false;PaintingStartedAt=-1;
            viewpoint.ResetDetection();player.ResetToSpawn();player.SetInput(false);
            player.viewCamera.fieldOfView=explorationFieldOfView;
            if(reassembly)reassembly.Clear();
            if(dynamicEnvironment){dynamicEnvironment.SetActive(true);foreach(var motion in dynamicEnvironment.GetComponentsInChildren<GardenAmbientMotion>())motion.ResetMotion();}
        }
        public void ResetExperience(bool returnToMap)
        {
            ResetWorld();stateElapsed=0;StateProgress=0;
            State=returnToMap?ExperienceState.Selection:ExperienceState.Exploration;
            player.SetInput(!returnToMap);
            if(globeInterface && returnToMap)globeInterface.ResetGlobe();
        }
        public bool BeginTransition()
        {
            if(State!=ExperienceState.Exploration || !HasExplored || !viewpoint.Ready)return false;
            if(!SelectedArtwork || !SelectedArtwork.HasDocumentedOriginal){Debug.LogWarning("MONET_ORIGINAL_MISSING: selected image required.");return false;}
            State=ExperienceState.Transition;player.SetInput(false);Progress=0;CaptureRequested=true;
            if(reassembly)reassembly.Begin();
            return true;
        }
        void Update()
        {
            if(State==ExperienceState.Exploration)
            {
                if(Input.GetKeyDown(KeyCode.R)){ResetExperience(false);return;}
                if(Input.GetKeyDown(KeyCode.M)){ResetExperience(true);return;}
                if(Input.GetKeyDown(KeyCode.G) && dynamicEnvironment)dynamicEnvironment.SetActive(!dynamicEnvironment.activeSelf);
                var d=player.transform.position-player.SpawnPosition;
                if(new Vector2(d.x,d.z).magnitude>=explorationDistance)HasExplored=true;
                float proximity=HasExplored?1-Mathf.Clamp01(Vector3.Distance(player.viewCamera.transform.position,viewpoint.transform.position)/.9f):0;
                float targetLens=Mathf.Lerp(explorationFieldOfView,compositionFieldOfView,Mathf.SmoothStep(0,1,proximity));
                player.viewCamera.fieldOfView=Mathf.Lerp(player.viewCamera.fieldOfView,targetLens,1-Mathf.Exp(-Time.deltaTime*lensResponse));
                viewpoint.Tick(player.viewCamera,Time.deltaTime,HasExplored);
                if(viewpoint.Ready)BeginTransition();
            }
            else if(State==ExperienceState.Entering)
            {
                stateElapsed+=Time.unscaledDeltaTime;StateProgress=Mathf.Clamp01(stateElapsed/entrySeconds);
                if(StateProgress>=1){State=ExperienceState.Exploration;StateProgress=0;player.SetInput(true);}
            }
            else if(State==ExperienceState.Transition)
            {
                Progress=Mathf.Clamp01(Progress+Time.unscaledDeltaTime/Mathf.Max(.5f,transitionSeconds));
                if(reassembly)reassembly.Tick(Progress);
                if(Progress>=1){State=ExperienceState.Painting;StartCoroutine(StartPaintingClock(cycle));}
            }
            else if(State==ExperienceState.Painting && PaintingStartedAt>=0)
            {
                // Real-time clock starts AFTER the first completely formed original frame.
                double elapsed=Time.realtimeSinceStartupAsDouble-PaintingStartedAt;
                if(elapsed>=PaintingHoldSeconds)
                {
                    LastPaintingHold=elapsed;CompletedLoops++;
                    Debug.Log($"MONET_HOLD_COMPLETE: {elapsed:F4}s; loop={CompletedLoops}");
                    State=ExperienceState.Returning;stateElapsed=0;StateProgress=0;
                    if(globeInterface)globeInterface.ResetGlobe();
                }
            }
            else if(State==ExperienceState.Returning)
            {
                stateElapsed+=Time.unscaledDeltaTime;StateProgress=Mathf.Clamp01(stateElapsed/returnSeconds);
                if(StateProgress>=1)ResetExperience(true);
            }
            if(dynamicEnvironment)
            {
                foreach(var audio in dynamicEnvironment.GetComponentsInChildren<AudioSource>(true))
                    audio.volume=State==ExperienceState.Exploration?ambientVolume:State==ExperienceState.Entering?ambientVolume*StateProgress:State==ExperienceState.Transition?ambientVolume*(1-Progress):0;
            }
        }
        IEnumerator StartPaintingClock(int token)
        {
            yield return new WaitForEndOfFrame();
            if(State==ExperienceState.Painting && token==cycle)
            {PaintingStartedAt=Time.realtimeSinceStartupAsDouble;Debug.Log("MONET_ORIGINAL_FULL_FRAME: clock started.");}
        }
        public void DrawOriginal(float alpha=1)
        {
            if(!SelectedArtwork || !SelectedArtwork.originalPainting)return;
            var texture=SelectedArtwork.originalPainting;
            float aspect=(float)texture.width/texture.height;
            float width=Mathf.Min(Screen.width,Screen.height*aspect),height=width/aspect;
            GUI.color=new Color(1,1,1,alpha);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),globeInterface?globeInterface.Background:Texture2D.blackTexture,ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height),texture,ScaleMode.StretchToFill,false);
            GUI.color=Color.white;
        }
        void OnGUI()
        {
            if(State==ExperienceState.Painting){DrawOriginal();return;}
            if(State==ExperienceState.Returning)
            {DrawOriginal();if(globeInterface)globeInterface.Draw(StateProgress,false);return;}
            if(State==ExperienceState.Selection || State==ExperienceState.Entering)
            {if(globeInterface)globeInterface.Draw(State==ExperienceState.Selection?1:1-StateProgress,State==ExperienceState.Selection);return;}
            if(State==ExperienceState.Transition){if(reassembly)reassembly.Draw();return;}
            if(caption==null){caption=new GUIStyle(GUI.skin.label){fontSize=Mathf.Clamp(Screen.height/42,12,20),alignment=TextAnchor.MiddleCenter};caption.normal.textColor=new Color(.93f,.9f,.77f);}
            if(ShowGuide)
            {
                string text=!HasExplored?"Explore the garden paths. Let the bridge guide your attention.":
                    viewpoint.Aligned?"Approximate Artist Viewpoint  ·  Hold your gaze":"Find the quiet lights along the bank.";
                GUI.Label(new Rect(0,Screen.height-65,Screen.width,30),text,caption);
                if(!HasExplored)GUI.Label(new Rect(0,Screen.height-35,Screen.width,25),"WASD · Shift · click to look · Esc release",caption);
            }
            // Soft peripheral pigment cue; no arrow, crosshair or task counter.
            if(viewpoint.Aligned){GUI.color=new Color(1,.83f,.38f,viewpoint.HoldProgress*.035f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
        }
        void OnDestroy()
        {
            if(Active==this)Active=null;
            if(ownsPipeline && QualitySettings.renderPipeline==prototypePipeline)QualitySettings.renderPipeline=previousPipeline;
            if(player)player.ReleaseCursor();
        }
    }
}
