using UnityEngine;
using UnityEngine.EventSystems;
namespace MonetGarden {
public class GardenXRButtonTrace:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,IPointerClickHandler {
 public static string Path(Transform t){if(!t)return "none";string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
 void Trace(string kind,PointerEventData e){Debug.Log("MONET_XR_POINTER "+kind+" path="+Path(transform)+" pointer="+e.pointerId+" hit="+Path(e.pointerCurrentRaycast.gameObject?e.pointerCurrentRaycast.gameObject.transform:null)+" pressed="+Path(e.pointerPress?e.pointerPress.transform:null)+" eligible="+e.eligibleForClick+" dragging="+e.dragging);}
 public void OnPointerEnter(PointerEventData e){Trace("Enter",e);}
 public void OnPointerExit(PointerEventData e){Trace("Exit",e);}
 public void OnPointerDown(PointerEventData e){Trace("Down",e);}
 public void OnPointerUp(PointerEventData e){Trace("Up",e);}
 public void OnPointerClick(PointerEventData e){Trace("Click",e);}
}
}