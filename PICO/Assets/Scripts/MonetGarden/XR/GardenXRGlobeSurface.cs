using UnityEngine;
using UnityEngine.EventSystems;
namespace MonetGarden {
public class GardenXRGlobeSurface:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
 public GardenGlobe globe;
 [Range(.005f,.08f)] public float dragThreshold=.025f;
 Vector2 down,last;bool dragging;
 Vector2 UV(PointerEventData e){
  var r=(RectTransform)transform;
  Vector3 p;
  if(e.pointerCurrentRaycast.isValid) p=r.InverseTransformPoint(e.pointerCurrentRaycast.worldPosition);
  else {RectTransformUtility.ScreenPointToLocalPointInRectangle(r,e.position,e.pressEventCamera,out var q);p=q;}
  return new Vector2((p.x-r.rect.x)/r.rect.width,(p.y-r.rect.y)/r.rect.height);
 }
 public void OnPointerDown(PointerEventData e){down=last=UV(e);dragging=false;Debug.Log("MONET_XR_GLOBE_DOWN "+down);}
 public void OnDrag(PointerEventData e){
  var v=UV(e);if(globe.Selected)return;
  if(!dragging && Vector2.Distance(v,down)<dragThreshold)return;
  var delta=v-last;globe.RotateBy(new Vector2(-delta.x*180,delta.y*100));last=v;dragging=true;
 }
 public void OnPointerUp(PointerEventData e){
  Debug.Log("MONET_XR_GLOBE_UP drag="+dragging);
  if(dragging)return;
  var uv=UV(e);
  if(Physics.Raycast(globe.GlobeCamera.ViewportPointToRay(uv),out var hit,10,1<<30)){
   var pin=hit.collider.GetComponentInParent<GardenLandmark>();
   if(pin&&pin.selectable){
    var normal=(pin.transform.position-globe.Sphere.position).normalized;
    if(Vector3.Dot(normal,(globe.GlobeCamera.transform.position-globe.Sphere.position).normalized)>.05f)globe.SelectLandmark(pin);
   }
  }
 }
}
}
