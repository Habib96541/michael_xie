using UnityEngine;
using System.Runtime.InteropServices;

namespace HiLo
{
    public sealed class ResponsiveUIManager : MonoBehaviour
    {
        [DllImport("__Internal")] static extern double HiLoViewportHeight();
        UnityEngine.UI.CanvasScaler scaler;
        RectTransform safeRoot;
        GameView view;
        int width, height;
        Rect safe;
        public void Initialize(UnityEngine.UI.CanvasScaler canvasScaler, RectTransform root, GameView gameView)
        { scaler=canvasScaler;safeRoot=root;view=gameView;Apply(); }
        void Update() { if (Screen.width!=width || Screen.height!=height || Screen.safeArea!=safe) Apply(); }
        void Apply()
        {
            width=Screen.width;height=Screen.height;safe=Screen.safeArea;
            bool portrait=(float)width/height < 1.05f;
            float viewportHeight=height;
            #if UNITY_WEBGL && !UNITY_EDITOR
            viewportHeight=(float)HiLoViewportHeight();
            #endif
            bool compact=!portrait && viewportHeight<520;
            float portraitWidth=Mathf.Max(420,800f*width/height);
            scaler.referenceResolution=portrait ? new Vector2(portraitWidth,900) : compact ? new Vector2(900,420) : new Vector2(1280,800);
            scaler.matchWidthOrHeight=portrait ? 0 : 1;
            safeRoot.anchorMin=new Vector2(safe.xMin/width,safe.yMin/height);safeRoot.anchorMax=new Vector2(safe.xMax/width,safe.yMax/height);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();
            float scale=portrait ? width/portraitWidth : height/(compact ? 420f : 800f);
            view.Layout(safe.width/scale,safe.height/scale,portrait,compact);
        }
    }
}
