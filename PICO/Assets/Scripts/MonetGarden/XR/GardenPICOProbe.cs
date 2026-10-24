using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
namespace MonetGarden {
public class GardenPICOProbe:MonoBehaviour {
 StringBuilder report=new StringBuilder("PICO editor simulation. No headset/controller hardware connected.\n");int pass,fail;
 void Check(string n,bool value){report.AppendLine((value?"PASS ":"FAIL ")+n);if(value)pass++;else fail++;}
 IEnumerator Capture(string n){yield return new WaitForEndOfFrame();Directory.CreateDirectory("Documentation/Portfolio_Screenshots");ScreenCapture.CaptureScreenshot("Documentation/Portfolio_Screenshots/"+n+".png");yield return null;}
 IEnumerator Start(){yield return new WaitForSeconds(1);var f=GardenExperience.Active;var b=GardenXRBridge.Active;
 Check("XR simulation active",b&&b.IsVR);Check("Mobile 3D Gaussian renderer allocated",b.mobileSplat.enabled&&b.mobileSplat.RenderedCount==65536);
 Check("Main camera reused",b.origin.Camera==f.player.viewCamera);Check("World-space globe",b.presentation.menu.renderMode==RenderMode.WorldSpace);
 yield return Capture("PICO_01_WorldSpaceGlobe_Game");
 for(int cycle=0;cycle<2;cycle++){
 Check("Map reset",f.State==GardenExperience.ExperienceState.Selection);
 f.globeInterface.SelectLandmark(f.globeInterface.ActiveMarker);yield return new WaitForSeconds(1.8f);yield return Capture("PICO_02_LandmarkCard_Game");f.EnterGarden();yield return new WaitForSeconds(f.entrySeconds+.3f);
 Check("Garden entered",f.State==GardenExperience.ExperienceState.Exploration);
 yield return Capture("PICO_03_WorldSpaceGuide_Game");Check("World guide",f.guide.CanvasView.renderMode==RenderMode.WorldSpace);Check("No quest before confirmation",!f.guide.QuestConfirmed);
 f.guide.NextPage();f.guide.NextPage();Check("Third page not enough",!f.guide.QuestConfirmed);f.guide.NextPage();yield return null;
 yield return Capture("PICO_04_ConfirmedMarker_Game");Check("Guide confirmed",f.guide.QuestConfirmed&&!f.guide.Expanded);
 var cc=f.player.GetComponent<CharacterController>();cc.enabled=false;f.player.transform.position+=Vector3.right*.5f;cc.enabled=true;yield return null;
 Check("Exploration based on eye movement",f.HasExplored);
 cc.enabled=false;var cam=f.player.viewCamera.transform;f.player.transform.position+=f.viewpoint.transform.position-cam.position;
 cam.rotation=Quaternion.LookRotation(-f.viewpoint.transform.forward);cc.enabled=true;yield return new WaitForSeconds(.4f);
 Check("Wrong direction rejected",f.State==GardenExperience.ExperienceState.Exploration&&f.viewpoint.HoldProgress==0);
 cam.rotation=f.viewpoint.transform.rotation;yield return Capture("PICO_05_HeadViewpoint_Game");yield return new WaitForSeconds(f.viewpoint.holdSeconds+.3f);
 Check("Position plus head direction triggers",f.State==GardenExperience.ExperienceState.Transition);
 Check("Locomotion locked",!b.move.enabled&&!b.turn.enabled);
 Check("Uniform Sphere Points choreography",f.gaussianTransition.Running);
 yield return new WaitForSeconds(1);yield return Capture("PICO_06_SphereDissolution_Game");yield return new WaitForSeconds(1.5f);yield return Capture("PICO_07_Dispersion_Game");yield return new WaitForSeconds(1.5f);yield return Capture("PICO_08_Reconstruction_Game");while(f.State==GardenExperience.ExperienceState.Transition)yield return null;
 yield return new WaitForEndOfFrame();yield return null;
 yield return Capture("PICO_09_OriginalPainting_Game");Check("Original fully formed",f.Progress==1&&f.State==GardenExperience.ExperienceState.Painting);
 Check("Original aspect",f.SelectedArtwork.originalPainting.width==2400&&f.SelectedArtwork.originalPainting.height==2293);
 double clock=Time.realtimeSinceStartupAsDouble;
 while(f.State==GardenExperience.ExperienceState.Painting)yield return null;
 report.AppendLine("HoldSeconds="+f.LastPaintingHold.ToString("F6"));Check("Independent ten seconds",f.LastPaintingHold>=10&&f.LastPaintingHold<10.2);
 while(f.State!=GardenExperience.ExperienceState.Selection)yield return null;
 yield return Capture("PICO_10_ReturnedGlobe_Game");Check("Ready for repeat",!f.guide.QuestConfirmed&&!f.gaussianTransition.Running);
 }
 report.AppendLine("Passed="+pass+" Failed="+fail);Directory.CreateDirectory("Documentation/Validation");File.WriteAllText("Documentation/Validation/PICO_Editor_Simulation.txt",report.ToString());Debug.Log("MONET_PICO_PROBE_DONE "+pass+" pass "+fail+" fail");
 }
}
}