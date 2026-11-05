using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BloomDoom
{
    /// <summary>One wordmark for the editor intro and the browser's actual download screen.</summary>
    public sealed class GardenLoadingScreen : MonoBehaviour
    {
        Image fill,pea;Text percent;CanvasGroup group;RectTransform content;bool gentle;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]static extern void BloomDoomLoadingReady();
#endif
        void Awake()
        {
            gentle=PlayerPrefs.GetInt("BloomDoom.Gentle",0)==1;
#if !UNITY_WEBGL || UNITY_EDITOR
            BuildNativeIntro();
#endif
        }
        IEnumerator Start()
        {
            float began=Time.realtimeSinceStartup;yield return null;
            while(!SplashScreen.isFinished)yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            yield return new WaitForEndOfFrame();BloomDoomLoadingReady();
#else
            began=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-began<1.65f){float t=Mathf.Clamp01((Time.realtimeSinceStartup-began)/1.65f);float value=Mathf.SmoothStep(0,1,t);fill.rectTransform.sizeDelta=new Vector2(380*value,12);pea.rectTransform.anchoredPosition=new Vector2(410+value*366,-522);percent.text=Mathf.FloorToInt(value*100)+"%";yield return null;}
            percent.text="100%";float fading=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-fading<.3f){group.alpha=1-(Time.realtimeSinceStartup-fading)/.3f;yield return null;}
#endif
            Destroy(gameObject);
        }
        void Update()
        {
            if(content==null)return;var parent=(RectTransform)content.parent;float scale=Mathf.Min(parent.rect.width/1200,parent.rect.height/720)*.96f;content.localScale=Vector3.one*scale;content.anchoredPosition=new Vector2(0,gentle?0:Mathf.Sin(Time.unscaledTime*1.2f)*3);
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;}
        static Image Image(Transform parent,string name,float x,float y,float width,float height,Color color,Sprite sprite=null)
        {var rect=Rect(parent,name,x,y,width,height);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;image.sprite=sprite;return image;}
        static Text Label(Transform parent,string text,float x,float y,float width,float height,int size,Font font,Color color)
        {var rect=Rect(parent,text,x,y,width,height);var label=rect.gameObject.AddComponent<Text>();label.text=text;label.font=font;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.color=color;label.raycastTarget=false;return label;}
        void BuildNativeIntro()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;gameObject.AddComponent<GraphicRaycaster>();var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=.5f;
            group=gameObject.AddComponent<CanvasGroup>();var bg=Image(transform,"Loading paper",0,0,1600,1000,GardenAssets.Color("#f6f1df"));bg.raycastTarget=true;bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.offsetMin=bg.rectTransform.offsetMax=Vector2.zero;
            var meadow=Image(transform,"Soft meadow border",0,0,1600,70,GardenAssets.Color("#dfe6c9"));meadow.rectTransform.anchorMin=new Vector2(0,0);meadow.rectTransform.anchorMax=new Vector2(1,0);meadow.rectTransform.pivot=new Vector2(.5f,0);meadow.rectTransform.anchoredPosition=Vector2.zero;meadow.rectTransform.sizeDelta=new Vector2(0,70);
            content=Rect(transform,"Loading composition",0,0,1200,720);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
            Font title=Resources.Load<Font>("BloomDoom/Fonts/LilitaOne-Regular"),body=Resources.Load<Font>("BloomDoom/Fonts/Nunito");if(title==null)title=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(body==null)body=title;
            Label(content,"HOMEGROWN HEROES.  UNINVITED GUESTS.",100,6,1000,28,14,body,GardenAssets.Color("#879268")).fontStyle=FontStyle.Bold;
            var logo=Image(content,"Plants and zombies title",60,42,1080,480,Color.white,GardenAssets.Get("title_logo"));logo.preserveAspect=true;
            Label(content,"Putting down roots...",100,485,1000,39,29,title,GardenAssets.Color("#637a44"));
            var track=Image(content,"Growing progress",410,522,380,12,GardenAssets.Color("#dfe6c9"),GardenAssets.Rounded);track.type=UnityEngine.UI.Image.Type.Sliced;
            fill=Image(content,"Progress leaves",410,522,0,12,GardenAssets.Color("#96af61"),GardenAssets.Rounded);fill.type=UnityEngine.UI.Image.Type.Sliced;
            pea=Image(content,"Progress pea",410,522,18,18,GardenAssets.Color("#c4dc87"),GardenAssets.Circle);
            percent=Label(content,"0%",810,510,70,33,19,title,GardenAssets.Color("#819157"));
            Label(content,"Sunbuds first. A little sunshine goes a long way.",100,574,1000,35,17,body,GardenAssets.Color("#7c8761")).fontStyle=FontStyle.Bold;
            Label(content,"GROW A LITTLE COURAGE.",100,654,1000,30,13,body,GardenAssets.Color("#879268"));
        }
    }
}
