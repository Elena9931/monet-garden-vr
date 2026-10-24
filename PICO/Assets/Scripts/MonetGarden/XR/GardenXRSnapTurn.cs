using UnityEngine;using UnityEngine.XR.Interaction.Toolkit;
namespace MonetGarden {public class GardenXRSnapTurn:ActionBasedSnapTurnProvider {protected override Vector2 ReadInput(){var a=rightHandSnapTurnAction.action;if(a!=null&&a.activeControl!=null)return a.ReadValue<Vector2>();if(Application.platform==RuntimePlatform.Android && UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out var v))return v.magnitude>.2f?v:Vector2.zero;return Vector2.zero;}}}

