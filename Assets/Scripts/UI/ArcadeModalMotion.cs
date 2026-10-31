using UnityEngine;

namespace DropStack.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ArcadeModalMotion : MonoBehaviour
    {
        public RectTransform card;
        private CanvasGroup group;
        private float age;
        private void OnEnable() { group=GetComponent<CanvasGroup>(); age=0; group.alpha=0; }
        private void LateUpdate()
        {
            if(card==null) return;
            age+=Time.unscaledDeltaTime;
            float t=ArcadeFeedback.MotionEnabled?Mathf.Clamp01(age/.22f):1;
            float ease=1-Mathf.Pow(1-t,3);
            group.alpha=ease;
            float pixelScale=GetComponentInParent<Canvas>().scaleFactor;
            float fit=Mathf.Max(.05f,Mathf.Min(1,(Screen.safeArea.width-30)/(card.sizeDelta.x*pixelScale),(Screen.safeArea.height-30)/(card.sizeDelta.y*pixelScale)));
            card.localScale=Vector3.one*fit*Mathf.Lerp(.94f,1,ease);
            card.anchoredPosition=(Screen.safeArea.center-new Vector2(Screen.width,Screen.height)*.5f)/pixelScale+new Vector2(0,Mathf.Lerp(-14,0,ease));
        }
    }
}
