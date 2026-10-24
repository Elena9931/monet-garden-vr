using UnityEngine;
namespace MonetGarden {
 public class GardenViewpointLight : MonoBehaviour {
 public ParticleSystem lights;public float visibilityDistance=2.5f;
 [Range(.02f,.35f)]public float markerSize=.11f;
 public float groundClearance=.13f,floatAmplitude=.035f,floatPeriod=2f;
 Transform diamond;Material material;Vector3 basePosition;
 void Start(){basePosition=transform.position;var g=new GameObject("SmallGoldViewpointDiamond");g.transform.SetParent(transform,false);diamond=g.transform;var mesh=new Mesh();mesh.vertices=new[]{Vector3.up,Vector3.right,Vector3.forward,Vector3.left,Vector3.back,Vector3.down};mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));g.AddComponent<MeshRenderer>().sharedMaterial=material;}
 void Update(){var f=GardenExperience.Active;if(!f||!lights||!diamond)return;bool show=f.State==GardenExperience.ExperienceState.Exploration && (!f.guide||f.guide.QuestConfirmed);diamond.gameObject.SetActive(show);float near=1-Mathf.Clamp01(Vector3.Distance(f.player.viewCamera.transform.position,f.viewpoint.transform.position)/visibilityDistance);float phase=Time.time*Mathf.PI*2/Mathf.Max(.2f,floatPeriod);diamond.localPosition=Vector3.up*(groundClearance+Mathf.Sin(phase)*floatAmplitude);diamond.localScale=Vector3.one*markerSize*.5f;diamond.localRotation=Quaternion.Euler(0,Time.time*16,0);material.color=new Color(1,.79f+.08f*Mathf.Sin(phase),.33f);var emission=lights.emission;emission.rateOverTime=show?Mathf.Lerp(1,5,near):0;var main=lights.main;main.startColor=new Color(1,.8f,.4f,show?Mathf.Lerp(.06f,.24f,near):0);if(!show)lights.Clear();}
 public void HideForSequence(){if(diamond)diamond.gameObject.SetActive(false);if(lights)lights.Clear();}
 public bool MarkerVisible=>diamond && diamond.gameObject.activeSelf;
 void OnDestroy(){if(material)Destroy(material);}
 }}
