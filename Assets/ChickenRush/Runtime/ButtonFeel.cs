using UnityEngine;
using UnityEngine.EventSystems;

namespace ChickenRush
{
    public sealed class ButtonFeel : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        float target=1,current=1;
        public void OnPointerDown(PointerEventData data){target=.975f;}
        public void OnPointerUp(PointerEventData data){target=1;}
        public void OnPointerExit(PointerEventData data){target=1;}
        void Update(){if(Mathf.Abs(current-target)<.0001f)return;current=Mathf.Lerp(current,target,1-Mathf.Exp(-Time.unscaledDeltaTime*24));transform.localScale=Vector3.one*current;}
        void OnDisable(){target=current=1;transform.localScale=Vector3.one;}
    }
}
