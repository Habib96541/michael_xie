using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BloomDoom
{
    public sealed class GardenPointer : MonoBehaviour,IPointerDownHandler,IPointerMoveHandler,IPointerExitHandler
    {
        public Action<Vector2,bool> Point;public Action Leave;
        public void OnPointerDown(PointerEventData data){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var local);Point?.Invoke(new Vector2(local.x,-local.y),true);}
        public void OnPointerMove(PointerEventData data){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.enterEventCamera,out var local);Point?.Invoke(new Vector2(local.x,-local.y),false);}
        public void OnPointerExit(PointerEventData data)=>Leave?.Invoke();
    }
}
