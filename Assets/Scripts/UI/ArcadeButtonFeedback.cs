using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DropStack.UI
{
    public class ArcadeButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        private float target=1, glow;
        private Button button;
        private ArcadeSurface surface;
        private void Awake() { button=GetComponent<Button>(); surface=GetComponent<ArcadeSurface>(); }
        private void Update()
        {
            float goal=button!=null&&button.interactable?target:1;
            transform.localScale=Vector3.one*(ArcadeFeedback.MotionEnabled?Mathf.Lerp(transform.localScale.x,goal,1-Mathf.Exp(-26*Time.unscaledDeltaTime)):1);
            if(surface!=null) surface.SetGlow(button.interactable?glow:0);
        }
        public void OnPointerEnter(PointerEventData data) { if(button.interactable) { target=1.025f; glow=.75f; } }
        public void OnPointerExit(PointerEventData data) { target=1; glow=0; }
        public void OnPointerDown(PointerEventData data) { if(button.interactable) { target=.965f; glow=1; } }
        public void OnPointerUp(PointerEventData data) { target=1; glow=0; }
        private void OnDisable() { transform.localScale=Vector3.one; target=1; glow=0; }
    }
}
