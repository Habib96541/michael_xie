using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleDepths
{
    public class UiMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool entrance, pulse;
        public float delay;
        Vector3 rest;
        float started, target=1;
        bool hovered;
        CanvasGroup group;
        void Awake() { rest=transform.localScale; if(rest==Vector3.zero)rest=Vector3.one; }
        public void SetRestScale(Vector3 scale){rest=scale;transform.localScale=scale;}
        void PrepareEntrance(){group=GetComponent<CanvasGroup>();if(!group)group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;transform.localScale=rest*.82f;}
        void Start(){if(entrance)PrepareEntrance();}
        void OnEnable() { started=Time.unscaledTime; if(entrance)PrepareEntrance(); }
        void Update()
        {
            float age=Time.unscaledTime-started-delay;
            if(entrance && age<.38f)
            {
                float t=Mathf.Clamp01(age/.38f);float back=1+2.3f*Mathf.Pow(t-1,3)+1.3f*Mathf.Pow(t-1,2);
                transform.localScale=rest*Mathf.LerpUnclamped(.82f,1,back);if(group)group.alpha=Mathf.Clamp01(t*2);return;
            }
            if(group)group.alpha=1;
            float scale=target+(pulse&&!hovered?Mathf.Sin(Time.unscaledTime*2.4f)*.025f:0);
            transform.localScale=Vector3.Lerp(transform.localScale,rest*scale,1-Mathf.Exp(-Time.unscaledDeltaTime*18));
        }
        public void OnPointerEnter(PointerEventData e){hovered=true;target=1.045f;}
        public void OnPointerExit(PointerEventData e){hovered=false;target=1;}
        public void OnPointerDown(PointerEventData e){target=.94f;}
        public void OnPointerUp(PointerEventData e){target=hovered?1.045f:1;}
    }

    public class SpriteUiLoop : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps=8;
        public bool bob;
        Image image;
        Vector2 origin;
        float phase;
        void Awake(){image=GetComponent<Image>();origin=((RectTransform)transform).anchoredPosition;phase=Random.value*6;}
        void Update()
        {
            if(image && frames!=null && frames.Length>1)image.sprite=frames[(int)(Time.unscaledTime*fps+phase)%frames.Length];
            if(bob)((RectTransform)transform).anchoredPosition=origin+Vector2.up*Mathf.Sin(Time.unscaledTime*2+phase)*5;
        }
    }
}
