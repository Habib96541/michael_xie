using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;

namespace BrickRush
{
    public sealed partial class UIManager
    {
        bool mobilePresentation,compactLandscape,browserTouch;
        readonly List<PageElement> pageDefaults=new List<PageElement>();
        sealed class PageElement {public RectTransform Rect;public Vector2 Position,Size;}
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int BrickRushIsTouchDevice();
#endif
        bool IsMobilePresentation => browserTouch||UnityEngine.Device.Application.isMobilePlatform;
        void InitializeDeviceHint()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            browserTouch=BrickRushIsTouchDevice()!=0;
#endif
        }
        void CapturePageDefaults()
        {
            foreach(Transform page in overlayDesign)foreach(RectTransform child in page)
                pageDefaults.Add(new PageElement{Rect=child,Position=child.anchoredPosition,Size=child.sizeDelta});
        }
        void Position(Transform page,int index,float x,float y,float w=0,float h=0)
        {
            var rect=(RectTransform)page.GetChild(index);rect.anchoredPosition=new Vector2(x,y);
            if(w>0&&h>0)ResizeElement(rect,new Vector2(w,h));
        }
        static void ResizeElement(RectTransform rect,Vector2 size)
        {
            rect.sizeDelta=size;
            if(rect.TryGetComponent<UnityEngine.UI.Button>(out _))foreach(Transform child in rect)
                if(child.TryGetComponent<TextMeshProUGUI>(out var label))label.rectTransform.sizeDelta=size-new Vector2(12,0);
            if(rect.TryGetComponent<UnityEngine.UI.Slider>(out var slider))
            {
                slider.fillRect.sizeDelta=new Vector2(0,5-size.y);slider.handleRect.sizeDelta=new Vector2(22,28-size.y);
            }
        }
        void ConfigureResponsivePages(float bw,float bh,float scale,float w,float h)
        {
            foreach(var original in pageDefaults){original.Rect.anchoredPosition=original.Position;ResizeElement(original.Rect,original.Size);}
            if(!compactLandscape)
            {
                overlay.SetParent(board,false);Stretch(overlay);overlayDesign.localScale=Vector3.one*scale;
                ((RectTransform)overlay.GetChild(0)).sizeDelta=new Vector2(bw,bh);
                if(!desktop)
                {
                    // At the smallest supported portrait size these targets remain at least 44 points tall.
                    foreach(var original in pageDefaults)if(original.Rect.TryGetComponent<UnityEngine.UI.Button>(out _))ResizeElement(original.Rect,new Vector2(original.Size.x,92));
                    Position(menuPage.transform,5,0,-198);
                    Position(menuPage.transform,7,-97,-283);Position(menuPage.transform,8,97,-283);
                    Position(pausePage.transform,3,0,-162);Position(pausePage.transform,4,0,-262);
                    Position(resultsPage.transform,8,0,-278);
                    Position(settingsPage.transform,7,0,-180);Position(settingsPage.transform,8,0,-285);
                    foreach(var original in pageDefaults)if(original.Rect.TryGetComponent<UnityEngine.UI.Slider>(out _))ResizeElement(original.Rect,new Vector2(original.Size.x,92));
                }
                return;
            }
            // A phone rotated sideways gets a two-column menu in the safe area, rather than a tiny portrait menu.
            overlay.SetParent(root,false);Stretch(overlay);overlay.SetAsLastSibling();
            ((RectTransform)overlay.GetChild(0)).sizeDelta=new Vector2(w,h);
            overlayDesign.localScale=Vector3.one*Mathf.Min((w-48)/800,(h-120)/400);
            Transform p=menuPage.transform;
            Position(p,0,-210,150,360,30);Position(p,1,-210,35,360,218);Position(p,2,-210,-83);
            Position(p,3,-210,-137,350,65);Position(p,4,210,105,330,80);Position(p,5,210,-125,360,28);Position(p,6,210,-77,360,24);
            Position(p,7,122,0,160,80);Position(p,8,298,0,160,80);
            p=pausePage.transform;
            Position(p,0,-210,125,360,30);Position(p,1,-210,10,360,170);Position(p,2,210,105,330,80);Position(p,3,210,0,330,80);Position(p,4,210,-105,330,80);
            p=resultsPage.transform;
            Position(p,0,-210,150,380,40);Position(p,1,-210,107,380,26);Position(p,2,-210,40,380,105);Position(p,3,-210,-27,380,24);Position(p,4,-210,-57,330,1);
            Position(p,5,98,55,225,162);Position(p,6,282,55,143,162);Position(p,7,210,-105,330,80);Position(p,8,-210,-125,330,80);
            p=helpPage.transform;
            Position(p,0,-210,170,374,24);Position(p,1,-210,105,374,135);Position(p,2,-210,23,374,28);Position(p,3,-210,-30,374,75);
            Position(p,4,-210,-90,374,28);Position(p,5,-210,-143,374,76);Position(p,6,210,130,374,28);Position(p,7,210,50,374,102);Position(p,8,210,-105,330,80);
            p=settingsPage.transform;
            Position(p,0,-210,145,374,40);Position(p,1,-210,100,374,26);Position(p,2,-210,50,374,28);Position(p,3,-210,8,374,80);
            Position(p,4,-210,-65,374,28);Position(p,5,-210,-107,374,80);Position(p,6,210,105,330,80);Position(p,7,210,0,330,80);Position(p,8,210,-105,330,80);
        }
        void ConfigureTouchControls(float scale,float w,float h)
        {
            controlHint.gameObject.SetActive(!compactLandscape);
            var pauseRect=(RectTransform)pauseButton.transform;
            pauseRect.SetParent(compactLandscape?root:footer,false);
            pauseRect.sizeDelta=desktop?new Vector2(64,56):new Vector2(92,92);
            pauseRect.localScale=Vector3.one*(compactLandscape?1.8f:1);
            pauseRect.anchoredPosition=compactLandscape?new Vector2(w/2-86,-h/2+86):new Vector2(212,-5);
            foreach(Transform child in pauseRect)if(child.TryGetComponent<TextMeshProUGUI>(out var label))label.rectTransform.sizeDelta=pauseRect.sizeDelta;
            for(int i=0;i<powerTexts.Length;i++)
            {
                var r=powerTexts[i].rectTransform;r.SetParent(compactLandscape?root:footer,false);r.localScale=Vector3.one;
                r.anchoredPosition=compactLandscape?new Vector2(-w/2+120,h/2-210-i*64):new Vector2((i-3)*70,-47);
                r.sizeDelta=compactLandscape?new Vector2(220,50):new Vector2(70,28);powerTexts[i].fontSize=compactLandscape?32:desktop?10:14;
            }
            overlay.SetAsLastSibling();
        }
        [Serializable] public sealed class LayoutSnapshot
        {
            public string page;public bool mobile,landscape;public float width,height;public Rect safe,arena;public ElementSnapshot[] buttons;
        }
        [Serializable] public sealed class ElementSnapshot {public string text;public Rect rect;}
        public LayoutSnapshot InspectLayout()
        {
            var controls=new List<ElementSnapshot>();
            foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(false))
            {
                string text="";foreach(var label in button.GetComponentsInChildren<TextMeshProUGUI>())if(!string.IsNullOrWhiteSpace(label.text)){text=label.text;break;}
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                controls.Add(new ElementSnapshot{text=text,rect=UnityEngine.Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y)});
            }
            string active="Gameplay";if(overlay.gameObject.activeSelf)foreach(Transform page in overlayDesign)if(page.gameObject.activeSelf)active=page.name;
            return new LayoutSnapshot{page=active,mobile=mobilePresentation,landscape=compactLandscape,width=UnityEngine.Device.Screen.width,height=UnityEngine.Device.Screen.height,safe=UnityEngine.Device.Screen.safeArea,arena=game.Camera.pixelRect,buttons=controls.ToArray()};
        }
    }
}
