#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace MonetGarden
{
    public class GardenValidationProbe : MonoBehaviour
    {
        string report;int failures,checks;
        void Check(string name,bool passed,string detail="")
        {checks++;report+=$"{(passed?"PASS":"FAIL")} {name} {detail}\n";if(!passed)failures++;File.WriteAllText("Documentation/Validation/round2_play_validation.txt",report);}
        void Align(GardenExperience f)
        {
            var p=f.player;var cc=p.GetComponent<CharacterController>();p.SetInput(false);cc.enabled=false;
            p.transform.SetPositionAndRotation(f.viewpoint.transform.position-Vector3.up*p.eyeHeight,Quaternion.Euler(0,f.viewpoint.transform.eulerAngles.y,0));
            p.viewCamera.transform.localPosition=Vector3.up*p.eyeHeight;p.viewCamera.transform.localRotation=Quaternion.Euler(f.viewpoint.transform.eulerAngles.x,0,0);cc.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator Capture(string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Documentation/ProcessShots/"+name+".png");yield return new WaitForEndOfFrame();}
        IEnumerator Start()
        {
            report=$"Round 2 real Unity Play validation {DateTime.Now:O}\nUnity {Application.unityVersion}, {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}\n";
            yield return null;yield return null;var f=GardenExperience.Active;Check("Experience loaded",f);if(!f)yield break;
            var p=f.player;var vp=f.viewpoint;var cc=p.GetComponent<CharacterController>();var g=f.globeInterface;
            Check("Starts on 3D earth",f.State==GardenExperience.ExperienceState.Selection && g.Sphere && g.GlobeCamera && g.View);
            Check("Exact user image bound at full imported size",f.SelectedArtwork.originalPainting && f.SelectedArtwork.originalPainting.width==2400 && f.SelectedArtwork.originalPainting.height==2293);
            Check("No human GameObjects or actor behaviours",!GameObject.Find("Gardener_PROCEDURAL_PLACEHOLDER") && FindObjectsOfType<GardenActorPath>(true).Length==0 && f.dynamicEnvironment.GetComponentsInChildren<Animator>(true).Length==0);
            Check("Multiple pins with only one valid destination",g.MarkerCount>=6);
            var pins=g.Sphere.GetComponentsInChildren<GardenLandmark>();int valid=0;foreach(var pin in pins){if(pin.selectable)valid++;else Check("Future marker rejected "+pin.name,!g.SelectLandmark(pin));}
            Check("Exactly one interactive marker",valid==1);
            var initial=g.Sphere.rotation;g.RotateBy(new Vector2(15,7));Check("Real sphere rotation changes",Quaternion.Angle(initial,g.Sphere.rotation)>10);g.ResetGlobe();
            foreach(var pin in pins)if(!pin.selectable)
            {
                Vector3 point=g.GlobeCamera.WorldToViewportPoint(pin.transform.GetChild(0).position);
                if(Vector3.Dot((pin.transform.position-g.Sphere.position).normalized,(g.GlobeCamera.transform.position-g.Sphere.position).normalized)>.1f)
                    Check("Future pin pointer cannot enter "+pin.name,g.Hit(new Vector2(point.x*Screen.width,point.y*Screen.height))!=g.ActiveMarker);
            }
            yield return Capture("R2_F01_Globe_Game");
            // Ray hit is tested through the same projection and Physics path as mouse clicks.
            var world=g.ActiveMarker.transform.GetChild(0).position;var screen=g.GlobeCamera.WorldToViewportPoint(world);
            Check("Giverny collider raycast",g.Hit(new Vector2(screen.x*Screen.width,screen.y*Screen.height))==g.ActiveMarker);
            Check("Giverny selection accepted",g.SelectLandmark(g.ActiveMarker));yield return new WaitForSecondsRealtime(g.focusSeconds+.2f);
            Check("Focus, zoom and pin enlargement",g.Selected && g.FocusProgress==1 && g.ActiveMarker.transform.localScale.x>1.5f && g.GlobeCamera.transform.localPosition.z>-3.1f);
            Check("Selected pin remains outside information card",!g.CardRect.Contains(g.ActiveScreenPoint()));
            yield return Capture("R2_F02_LandmarkCard_Game");
            f.EnterGarden();Check("Artistic entry is timed rather than a cut",f.State==GardenExperience.ExperienceState.Entering && !p.InputEnabled);
            yield return new WaitForSecondsRealtime(f.entrySeconds+.2f);Check("Exploration and WASD input enabled",f.State==GardenExperience.ExperienceState.Exploration && p.InputEnabled);
            yield return new WaitForSeconds(.4f);Check("Guide interaction pauses navigation",p.UIInteraction && !f.guide.QuestConfirmed);
            var sounds=f.dynamicEnvironment.GetComponentsInChildren<AudioSource>();bool audioReady=sounds.Length==3;
            foreach(var sound in sounds)audioReady&=sound.clip && sound.isPlaying && sound.volume>0;
            Check("Three synthesized ambience sources playing",audioReady);
            Check("No trigger before exploration",!f.BeginTransition());
            Check("Guide starts on exact first page",f.guide && f.guide.Page==0 && f.guide.Expanded && !f.guide.QuestConfirmed);
            var marker=FindObjectOfType<GardenViewpointLight>();Check("Marker hidden before confirmation",!marker.MarkerVisible);
            yield return Capture("R2_02_GuideExpanded_Game");
            f.guide.Close();yield return null;Check("Guide closes without confirming quest",!f.guide.Expanded && !f.guide.QuestConfirmed);yield return Capture("R2_02_GuideCollapsed_Game");
            f.guide.Open();f.guide.NextPage();Check("Second guide page",f.guide.Page==1);f.guide.PreviousPage();Check("Guide back navigation",f.guide.Page==0);f.guide.NextPage();f.guide.NextPage();Check("Third page alone does not confirm",f.guide.Page==2 && !f.guide.QuestConfirmed);f.guide.NextPage();yield return null;
            Check("Confirmation starts quest and closes panel",f.guide.QuestConfirmed && !f.guide.Expanded && !p.UIInteraction);Check("Marker shown only after confirmation",marker.MarkerVisible);yield return new WaitForSecondsRealtime(.4f);Check("Gravity lands on proxy",cc.isGrounded,$"feet={p.transform.position:F3}");
            p.ReleaseCursor();p.viewCamera.transform.LookAt(marker.transform.position+Vector3.up*marker.groundClearance);yield return Capture("R2_03_GoldMarker_Game");p.ResetToSpawn();yield return Capture("R2_F03_NoPeople_Garden_Game");
            for(int i=0;i<35;i++){cc.Move(Vector3.forward*.045f);yield return null;}Check("North water blocks player",p.transform.position.z<.62f);
            p.ResetToSpawn();for(int i=0;i<35;i++){cc.Move(Vector3.back*.045f);yield return null;}Check("South pond blocks player",p.transform.position.z>-.3f);
            p.ResetToSpawn();for(int i=0;i<75;i++){cc.Move(Vector3.left*.045f);yield return null;}Check("West obstacle blocks player",p.transform.position.x>-1.3f);
            p.ResetToSpawn();
            foreach(var stop in new[]{new Vector3(1.62f,0,.15f),new Vector3(1.62f,0,-1.3f),new Vector3(1.62f,0,-2.65f),new Vector3(2.58f,0,-3.18f),new Vector3(2.58f,0,-4f)})
            {
                for(int step=0;step<140;step++)
                {
                    Vector3 delta=stop-p.transform.position;delta.y=0;if(delta.magnitude<.08f)break;
                    cc.Move(Vector3.ClampMagnitude(delta,.035f));yield return null;
                }
                var diff=stop-p.transform.position;diff.y=0;
                Check("Connected bank/gravel route "+stop,diff.magnitude<.18f,$"actual={p.transform.position:F3}");
            }
            Check("Exploration from actual movement",f.HasExplored);p.ResetToSpawn();
            Check("Position alone insufficient",!GardenViewpoint.IsAligned(vp.transform.position,-vp.transform.forward,vp.transform.position,vp.transform.forward,vp.radius,vp.verticalTolerance,vp.yawTolerance,vp.pitchTolerance,true));
            Check("Angle alone insufficient",!GardenViewpoint.IsAligned(vp.transform.position+Vector3.right*2,vp.transform.forward,vp.transform.position,vp.transform.forward,vp.radius,vp.verticalTolerance,vp.yawTolerance,vp.pitchTolerance,true));
            vp.Tick(p.viewCamera,1,true);vp.Tick(p.viewCamera,.1f,false);Check("Interrupted dwell resets",vp.HoldProgress==0);
            Align(f);yield return new WaitForSeconds(vp.holdSeconds*.55f);Check("Dwell has not triggered early",f.State==GardenExperience.ExperienceState.Exploration && vp.HoldProgress<1);
            yield return Capture("R2_F04_Viewpoint_Game");
            yield return Capture("R2_F05_AlignedReal_Game");
            while(f.State==GardenExperience.ExperienceState.Exploration)yield return null;
            Check("Automatic trigger after dwell without E",f.State==GardenExperience.ExperienceState.Transition && vp.Ready && !p.InputEnabled);
            var lockedPosition=p.transform.position;var lockedLook=p.viewCamera.transform.rotation;
            Check("Uniform Sphere Points selected for real splat renderer",f.gaussianTransition.Running && f.gaussianTransition.rendererSource.effectType==Gsplat.GsplatEffectType.UniformSpherePoints);
            Check("Guide hidden during transformation",!GameObject.Find("GardenGuideCanvas").GetComponent<Canvas>().enabled);Check("Marker hidden during transformation",!marker.MarkerVisible);
            double transitionStart=Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(.8f);yield return Capture("R2_05_SphereDissolution_Game");
            yield return new WaitForSecondsRealtime(1.9f);yield return Capture("R2_06_Dispersion_Game");
            yield return new WaitForSecondsRealtime(1.5f);yield return Capture("R2_07_Reconstruction_Game");
            Check("Actual GPU pigment billboards active",f.reassembly.VisibleParticleCount>=500,$"particles={f.reassembly.VisibleParticleCount}");
            Check("Dissolve render pass executes",f.EffectRendered && f.Progress>.35f && f.Progress<.8f,$"progress={f.Progress:F3}");
            Check("Transition camera and root fixed",Vector3.Distance(p.transform.position,lockedPosition)<.0001f && Quaternion.Angle(lockedLook,p.viewCamera.transform.rotation)<.001f);
            yield return Capture("R2_F05_DissolveReassembly_Game");
            while(f.State==GardenExperience.ExperienceState.Transition)yield return null;
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            Check("Seven-second sequence duration",Time.realtimeSinceStartupAsDouble-transitionStart>=6.8 && Time.realtimeSinceStartupAsDouble-transitionStart<8.5);
            Check("Original fully formed and clock started",f.State==GardenExperience.ExperienceState.Painting && f.Progress==1 && f.PaintingStartedAt>=0);
            Check("All control disabled during original",!p.InputEnabled);
            var frame=ScreenCapture.CaptureScreenshotAsTexture();var image=f.SelectedArtwork.originalPainting;
            float iw=Mathf.Min(frame.width,frame.height*(float)image.width/image.height),ih=iw*image.height/image.width;
            float left=(frame.width-iw)*.5f,bottom=(frame.height-ih)*.5f,colourError=0;
            // Compare patch means: a single resampled pixel cannot represent textured oil paint.
            foreach(float u in new[]{.25f,.5f,.75f})foreach(float v in new[]{.25f,.5f,.75f})
            {
                Color expected=Color.clear,observed=Color.clear;
                int cx=Mathf.RoundToInt(left+iw*u),cy=Mathf.RoundToInt(bottom+ih*v);
                for(int y=-12;y<12;y++)for(int x=-12;x<12;x++)
                {
                    int sx=cx+x,sy=cy+y;observed+=frame.GetPixel(sx,sy);
                    expected+=image.GetPixelBilinear((sx+.5f-left)/iw,(sy+.5f-bottom)/ih);
                }
                expected/=576;observed/=576;colourError+=Mathf.Abs(expected.r-observed.r)+Mathf.Abs(expected.g-observed.g)+Mathf.Abs(expected.b-observed.b);
            }
            colourError/=27;Check("Final original colour across nine image patches",colourError<.025f,$"mean RGB channel error={colourError:F5}");Destroy(frame);
            yield return Capture("R2_F05_FinalMonet_Game");
            while(Time.realtimeSinceStartupAsDouble-f.PaintingStartedAt<9.8)yield return null;
            Check("Original still visible before 10 seconds",f.State==GardenExperience.ExperienceState.Painting);
            Check("No camera drift throughout hold",Vector3.Distance(p.transform.position,lockedPosition)<.0001f && Quaternion.Angle(lockedLook,p.viewCamera.transform.rotation)<.001f);
            while(f.State==GardenExperience.ExperienceState.Painting)yield return null;
            Check("10 seconds measured separately from effect",f.LastPaintingHold>=10 && f.LastPaintingHold<10.5,$"actual={f.LastPaintingHold:F4}s");
            Check("Automatic gentle return",f.State==GardenExperience.ExperienceState.Returning);
            yield return new WaitForSecondsRealtime(f.returnSeconds+.3f);
            Check("Clean return to interactive globe",f.State==GardenExperience.ExperienceState.Selection && f.Progress==0 && !f.HasExplored && !vp.Ready && !g.Selected);
            Check("Particles reset",f.reassembly.VisibleParticleCount==0);Check("Splat parameters restored",!f.gaussianTransition.Running && f.gaussianTransition.rendererSource.effectType!=Gsplat.GsplatEffectType.UniformSpherePoints);Check("Guide quest resets",!f.guide.QuestConfirmed && f.guide.Page==0);
            // A second complete cycle exercises re-entry, capture refresh, timer reset and automatic return.
            g.SelectLandmark(g.ActiveMarker);yield return new WaitForSecondsRealtime(g.focusSeconds+.1f);f.EnterGarden();yield return new WaitForSecondsRealtime(f.entrySeconds+.2f);
            f.guide.NextPage();f.guide.NextPage();f.guide.NextPage();
            p.GetComponent<CharacterController>().Move(Vector3.left*.6f);yield return null;Align(f);
            while(f.State==GardenExperience.ExperienceState.Exploration)yield return null;
            Check("Second cycle auto-trigger",f.State==GardenExperience.ExperienceState.Transition);
            while(f.State!=GardenExperience.ExperienceState.Selection)yield return null;
            Check("Second cycle fully completes",f.CompletedLoops==2 && f.LastPaintingHold>=10 && f.Progress==0 && !p.InputEnabled);
            report+=$"RESULT: {checks} checks; {failures} failures. Full human keyboard/route regression and PICO are separate.\n";File.WriteAllText("Documentation/Validation/round2_play_validation.txt",report);
            Debug.Log("MONET_FINAL_VALIDATION_COMPLETE: "+failures+" failures.");Destroy(gameObject);
        }
    }
}
#endif
