using System.Collections;
using HiLo.Core;
using UnityEngine;

namespace HiLo
{
    public sealed class CardAnimationController : MonoBehaviour
    {
        public IEnumerator Reveal(GameView view,Card card,Outcome outcome,float seconds,bool reduced)
        {
            var rect=view.NextCard;var original=rect.anchoredPosition;
            if(!reduced)
            {
                for(float t=0;t<seconds;t+=Time.unscaledDeltaTime)
                {
                    float p=Mathf.Clamp01(t/seconds);float x=Mathf.Abs(Mathf.Cos(p*Mathf.PI));rect.localScale=new Vector3(Mathf.Max(.015f,x),1,1);
                    rect.anchoredPosition=original+new Vector2(0,Mathf.Sin(p*Mathf.PI)*10);
                    if(p>=.5f)view.NextImage.sprite=Resources.Load<Sprite>("Art/"+card.AssetName);
                    yield return null;
                }
            }
            view.NextImage.sprite=Resources.Load<Sprite>("Art/"+card.AssetName);rect.localScale=Vector3.one;rect.anchoredPosition=original;
            if(!reduced)
            {
                float wait=outcome==Outcome.Wrong?.3f:.16f;
                for(float t=0;t<wait;t+=Time.unscaledDeltaTime){if(outcome==Outcome.Wrong)rect.anchoredPosition=original+new Vector2(Mathf.Sin(t*90)*7*(1-t/wait),0);yield return null;}
            }
            rect.anchoredPosition=original;
        }
        public IEnumerator Pulse(RectTransform rect,bool reduced)
        {
            if(reduced)yield break;
            Vector3 original=rect.localScale;for(float t=0;t<.24f;t+=Time.unscaledDeltaTime){rect.localScale=original*(1+Mathf.Sin(t/.24f*Mathf.PI)*.10f);yield return null;}rect.localScale=original;
        }
        public IEnumerator Transfer(GameView view,Card card,bool reduced)
        {
            Vector2 original=view.NextCard.anchoredPosition,target=view.CurrentCard.anchoredPosition;
            if(!reduced)
            {
                for(float t=0;t<.13f;t+=Time.unscaledDeltaTime){float p=Mathf.SmoothStep(0,1,t/.13f);view.NextCard.anchoredPosition=Vector2.Lerp(original,target,p);view.CurrentImage.color=new Color(1,1,1,1-p*.5f);yield return null;}
            }
            view.SetCard(card);view.CurrentImage.color=Color.white;view.NextImage.sprite=Resources.Load<Sprite>("Art/card_back");view.NextCard.anchoredPosition=original;
        }
    }
}
