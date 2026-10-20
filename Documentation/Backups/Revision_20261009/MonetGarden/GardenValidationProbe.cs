#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MonetGarden
{
    /// <summary>Runs in the actual Editor Play world; never included in a player build.</summary>
    public class GardenValidationProbe : MonoBehaviour
    {
        string report;
        int failures;
        void Check(string label,bool pass,string detail="")
        {
            report+=$"{(pass ? "PASS" : "FAIL")} {label} {detail}\n";
            if(!pass)failures++;
            File.WriteAllText("Documentation/Validation/play_validation.txt",report);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Documentation/Validation");
            report=$"Actual Unity Editor Play validation {DateTime.Now:O}\nUnity {Application.unityVersion} / GPU {SystemInfo.graphicsDeviceName}\n";
            yield return null;yield return null;
            var flow=GardenExperience.Active;
            Check("Experience reference",flow!=null);
            if(!flow)yield break;
            var player=flow.player;var vp=flow.viewpoint;var capsule=player.GetComponent<CharacterController>();
            Check("Map entry state",flow.State==GardenExperience.ExperienceState.Selection && !player.InputEnabled);
            flow.EnterGarden();player.SetInput(false);
            yield return new WaitForSeconds(.7f);
            Check("Gravity lands on proxy",capsule.isGrounded,$"feet={player.transform.position:F3}");
            Check("Starting composition preserved",Vector3.Distance(player.viewCamera.transform.position,vp.transform.position)<.04f &&
                Quaternion.Angle(player.viewCamera.transform.rotation,vp.transform.rotation)<.1f);
            Check("No transition before exploration",!flow.BeginTransition());
            Vector3 before=player.transform.position;
            for(int i=0;i<40;i++){capsule.Move(Vector3.forward*.04f);yield return null;}
            Check("North water boundary blocks",player.transform.position.z<.62f,$"z={player.transform.position.z:F3}");
            player.ResetToSpawn();
            for(int i=0;i<40;i++){capsule.Move(Vector3.back*.04f);yield return null;}
            Check("South pond boundary blocks",player.transform.position.z>-.3f,$"z={player.transform.position.z:F3}");
            player.ResetToSpawn();
            for(int i=0;i<90;i++){capsule.Move(Vector3.left*.04f);yield return null;}
            Check("West garden boundary blocks",player.transform.position.x>-1.3f,$"x={player.transform.position.x:F3}");
            Check("Exploration recorded from actual movement",flow.HasExplored);
            player.ResetToSpawn();player.SetInput(false);
            yield return new WaitForSeconds(.2f);
            Check("Viewpoint rejects wrong orientation",!GardenViewpoint.IsAligned(vp.transform.position,-vp.transform.forward,vp.transform.position,vp.transform.forward,vp.radius,.2f,10,8,true));
            Check("Viewpoint rejects wrong distance",!GardenViewpoint.IsAligned(vp.transform.position+Vector3.right, vp.transform.forward,vp.transform.position,vp.transform.forward,vp.radius,.2f,10,8,true));
            Check("Viewpoint accepts calibrated pose",GardenViewpoint.IsAligned(vp.transform.position,vp.transform.forward,vp.transform.position,vp.transform.forward,vp.radius,.2f,10,8,true));
            vp.Tick(player.viewCamera,1f,true);vp.Tick(player.viewCamera,.1f,false);
            Check("Interrupted hold resets",vp.HoldProgress==0);
            yield return new WaitForSeconds(vp.holdSeconds+.3f);
            Check("Actual viewpoint hold ready",vp.Ready,$"hold={vp.HoldProgress}");
            Check("Explicit transition starts",flow.BeginTransition());
            Check("Movement locked during transition",!player.InputEnabled);
            yield return new WaitForSeconds(flow.transitionSeconds*.5f);
            Check("Intermediate painterly pass executes",flow.EffectRendered && flow.Progress>.3f && flow.Progress<.8f,$"progress={flow.Progress}");
            ScreenCapture.CaptureScreenshot("Documentation/ProcessShots/S05_Transition_Game.png");
            yield return new WaitForSeconds(flow.transitionSeconds*.55f);
            Check("Painting state reached",flow.State==GardenExperience.ExperienceState.Painting && flow.Progress==1);
            ScreenCapture.CaptureScreenshot("Documentation/ProcessShots/S05_PainterlyStudy_Game.png");
            yield return new WaitForSeconds(.2f); // CaptureScreenshot writes a later frame; finish it before changing artwork.
            Check("Missing original truthfully gated",!flow.SelectedArtwork.HasDocumentedOriginal);
            // Synthetic magenta test texture validates the original-image blend. It is never saved as artwork/evidence.
            var art=flow.SelectedArtwork;
            var oldTexture=art.originalPainting;var oldTitle=art.title;var oldYear=art.year;var oldSource=art.sourceAndRights;
            var pattern=new Texture2D(16,8,TextureFormat.RGBA32,false);
            var colours=new Color[128];for(int i=0;i<colours.Length;i++)colours[i]=Color.magenta;pattern.SetPixels(colours);pattern.Apply();
            art.originalPainting=pattern;art.title="VALIDATION TEST PATTERN";art.year="TEST";art.sourceAndRights="Locally generated shader diagnostic";
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            Color centre=frame.GetPixel(frame.width/2,frame.height/2);
            Check("Original texture reaches final frame",centre.r>.8f && centre.g<.25f && centre.b>.8f,$"sample={centre}");
            Destroy(frame);
            art.originalPainting=oldTexture;art.title=oldTitle;art.year=oldYear;art.sourceAndRights=oldSource;Destroy(pattern);
            flow.ResetExperience(false);
            yield return new WaitForSeconds(.4f);
            Check("Reset restores exploration",flow.State==GardenExperience.ExperienceState.Exploration && flow.Progress==0 && !flow.HasExplored && !vp.Ready && player.InputEnabled);
            Check("Reset returns camera",Vector3.Distance(player.viewCamera.transform.position,vp.transform.position)<.04f);
            ScreenCapture.CaptureScreenshot("Documentation/ProcessShots/S05_RealWorld_Game.png");
            var actor=flow.dynamicEnvironment.GetComponentInChildren<GardenActorPath>();
            Check("Actor and Animator present",actor && actor.animator && actor.animator.runtimeAnimatorController);
            Vector3 actorBefore=actor ? actor.transform.position : Vector3.zero;
            yield return new WaitForSeconds(4f);
            Check("Actor travels path",actor && Vector3.Distance(actorBefore,actor.transform.position)>.04f);
            flow.ResetExperience(true);yield return null;
            Check("Return to map",flow.State==GardenExperience.ExperienceState.Selection && !player.InputEnabled);
            report+=$"RESULT: {failures} failures. Keyboard/mouse user regression, visual bank alignment, mobile XR performance require separate checks.\n";
            File.WriteAllText("Documentation/Validation/play_validation.txt",report);
            Debug.Log("MONET_PLAY_VALIDATION_COMPLETE: "+failures+" failures.");
            Destroy(gameObject);
        }
    }
}
#endif
