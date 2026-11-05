using UnityEngine;
using UnityEngine.UI;
using DropStack.Rendering;

namespace DropStack.UI
{
    [ExecuteAlways]
    public class ResponsiveGameLayout : MonoBehaviour
    {
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private SpriteRenderer backdrop;
        [SerializeField] private RectTransform boardFrame,header,holdPanel,statsPanel,nextPanel,timerPanel,goalPanel,dangerPanel,controls,footer;
        private Vector2 lastSize;
        private Rect lastSafeArea;
        public bool IsPortrait => Screen.width<Screen.height;
        private void LateUpdate() { if(lastSize!=new Vector2(Screen.width,Screen.height)||lastSafeArea!=Screen.safeArea) ApplyLayout(); }
        public void ApplyLayout()
        {
            if(scaler==null||gameCamera==null||boardFrame==null) return;
            lastSize=new Vector2(Screen.width,Screen.height); lastSafeArea=Screen.safeArea;
            bool p=IsPortrait; Vector2 reference=p?new Vector2(660,1280):new Vector2(1200,900);
            Rect safe=Screen.safeArea; if(safe.width<=0||safe.height<=0) safe=new Rect(0,0,Screen.width,Screen.height);
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor=Mathf.Max(.01f,Mathf.Min(safe.width/reference.x,safe.height/reference.y));
            Vector2 shift=(safe.center-new Vector2(Screen.width,Screen.height)*.5f)/scaler.scaleFactor;
            Place(header,new Vector2(0,p?590:397)+shift,new Vector2(p?620:1100,68));
            Child(header,"Brand",new Vector2(p?-158:-385,6),new Vector2(p?245:270,50)); Child(header,"Edition",new Vector2(p?-158:-385,-31),new Vector2(300,24));
            Vector2 headerButtonSize = p ? new Vector2(84, 84) : new Vector2(50, 50);
            Child(header,"Restart",new Vector2(p?90:410,0),headerButtonSize); Child(header,"Pause",new Vector2(p?184:470,0),headerButtonSize); Child(header,"Help",new Vector2(p?278:530,0),headerButtonSize);
            header.Find("Mode").gameObject.SetActive(!p); header.Find("Status").gameObject.SetActive(!p);
            Place(boardFrame,new Vector2(0,p?4:-6)+shift,new Vector2(p?384:336,p?752:656));
            Place(holdPanel,new Vector2(p?-274:-304,p?284:234)+shift,new Vector2(p?112:208,p?154:174));
            Place(statsPanel,new Vector2(p?0:-304,p?510:-62)+shift,new Vector2(p?608:208,p?88:340));
            Place(nextPanel,new Vector2(p?274:304,p?245:171)+shift,new Vector2(p?112:208,p?326:334));
            Place(timerPanel,new Vector2(p?274:304,p?-187:-112)+shift,new Vector2(p?112:208,p?184:184));
            Place(goalPanel,new Vector2(p?-274:-304,p?48:-296)+shift,new Vector2(p?112:208,p?150:108));
            Place(dangerPanel,new Vector2(p?-274:304,p?-196:-296)+shift,new Vector2(p?112:208,p?150:108));
            float touchWidth = Mathf.Min(900, safe.width / scaler.scaleFactor - 24);
            Place(controls,new Vector2(0,p?-500:-389)+shift,new Vector2(p?touchWidth:760,p?224:72)); controls.localScale=Vector3.one;
            Place(footer,new Vector2(0,p?-627:-437)+shift,new Vector2(p?640:1100,p?20:28)); footer.GetComponent<Text>().text=p?"HOLD LEFT / RIGHT / DOWN TO REPEAT":"ARROWS / WASD  MOVE     Z / X  ROTATE     SPACE  DROP     C  HOLD     ESC  PAUSE";
            if(p) {
                TouchRow(touchWidth, 59, "Left", "Right", "SoftDrop", "Hold");
                TouchRow(touchWidth, -59, "RotateLeft", "RotateRight", "HardDrop");
            } else {
                Control("Left",new Vector2(-333,0),new Vector2(64,60)); Control("Right",new Vector2(-259,0),new Vector2(64,60)); Control("SoftDrop",new Vector2(-185,0),new Vector2(64,60)); Control("Hold",new Vector2(-89,0),new Vector2(108,60));
                Control("RotateLeft",new Vector2(31,0),new Vector2(112,60)); Control("RotateRight",new Vector2(153,0),new Vector2(112,60)); Control("HardDrop",new Vector2(295,0),new Vector2(150,60));
            }
            string[] statNames={"Score","Best","Lines","Level","Combo"}; float[] mobileX={-220,-80,53,140,226};
            Vector2[] labelPos={new Vector2(0,139),new Vector2(0,49),new Vector2(-45,-43),new Vector2(45,-43),new Vector2(-44,-134)};
            Vector2[] numberPos={new Vector2(0,96),new Vector2(0,19),new Vector2(-45,-74),new Vector2(45,-74),new Vector2(44,-134)};
            int[] sizes={44,25,28,28,24};
            for(int i=0;i<statNames.Length;i++) {
                Child(statsPanel,statNames[i]+"Label",p?new Vector2(mobileX[i],23):labelPos[i],new Vector2(p?(i<2?126:80):160,28));
                var value=statsPanel.Find(statNames[i]).GetComponent<Text>(); value.fontSize=p?(i<2?28:26):sizes[i]; Child(statsPanel,statNames[i],p?new Vector2(mobileX[i],-15):numberPos[i],new Vector2(p?(i<2?126:80):180,p?42:58));
            }
            foreach(Transform child in statsPanel) if(child.name=="Divider") child.gameObject.SetActive(!p);
            foreach(Transform child in nextPanel) { var preview=child.GetComponent<PiecePreviewRenderer>(); if(preview!=null) preview.SetBlockSize(p?18:24); if(child.name=="Divider") child.GetComponent<RectTransform>().sizeDelta=new Vector2(p?76:150,1); }
            holdPanel.GetComponentInChildren<PiecePreviewRenderer>().SetBlockSize(p?19:24);
            timerPanel.Find("Time").GetComponent<Text>().fontSize=p?28:40;
            foreach(string name in new[]{"TimerTrack","TimerFill","SpeedTrack","SpeedFill"}) { var rt=timerPanel.Find(name).GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(p?82:144,rt.sizeDelta.y); }
            foreach(string name in new[]{"GoalTrack","GoalFill"}) { var rt=goalPanel.Find(name).GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(p?80:144,rt.sizeDelta.y); }
            dangerPanel.Find("Segments").localScale=Vector3.one*(p?.67f:1);
            var toast=transform.Find("ActionToast").GetComponent<RectTransform>(); Place(toast,new Vector2(0,p?438:356)+shift,new Vector2(400,p?56:68));
            Child(toast,"Title",new Vector2(0,p?10:11),new Vector2(380,p?28:36)); toast.Find("Title").GetComponent<Text>().fontSize=p?21:24;
            Child(toast,"Detail",new Vector2(0,p?-15:-18),new Vector2(380,p?22:26)); toast.Find("Detail").GetComponent<Text>().fontSize=p?11:13;
            GetComponent<DropStackUIManager>()?.ApplyMenuLayout(p);
            Canvas.ForceUpdateCanvases(); float heightPx=(boardFrame.sizeDelta.y-16)*scaler.scaleFactor;
            gameCamera.orthographicSize=16*Screen.height/Mathf.Max(1,heightPx)*.5f; Vector2 screenCenter=RectTransformUtility.WorldToScreenPoint(null,boardFrame.position);
            float units=gameCamera.orthographicSize*2/Mathf.Max(1,Screen.height); Vector3 cameraPosition=new Vector3((Screen.width*.5f-screenCenter.x)*units,(Screen.height*.5f-screenCenter.y)*units,-10);
            gameCamera.transform.position=cameraPosition; if(Application.isPlaying) JuiceEffects.Instance?.SetCameraAnchor(cameraPosition);
            if(backdrop!=null&&backdrop.sprite!=null) { backdrop.transform.position=new Vector3(cameraPosition.x,cameraPosition.y,5); float scale=Mathf.Max(gameCamera.orthographicSize*2*gameCamera.aspect/backdrop.sprite.bounds.size.x,gameCamera.orthographicSize*2/backdrop.sprite.bounds.size.y); backdrop.transform.localScale=new Vector3(scale,scale,1); }
        }
        private void TouchRow(float width, float y, params string[] buttons)
        {
            const float gap = 12;
            float buttonWidth = (width - gap * (buttons.Length - 1)) / buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                float x = -width / 2 + buttonWidth / 2 + i * (buttonWidth + gap);
                Control(buttons[i], new Vector2(x, y), new Vector2(buttonWidth, 106));
            }
        }
        private void Control(string name,Vector2 pos,Vector2 size) { var rt=controls.Find(name).GetComponent<RectTransform>(); Rect(rt,pos,size); rt.Find("Label").GetComponent<RectTransform>().sizeDelta=size-new Vector2(12,4); }
        private static void Child(Transform parent,string name,Vector2 pos,Vector2? size=null) { var child=parent.Find(name); if(child==null) return; var rt=child.GetComponent<RectTransform>(); rt.anchoredPosition=pos; if(size.HasValue) rt.sizeDelta=size.Value; }
        private static void Rect(RectTransform rt,Vector2 pos,Vector2 size) { rt.anchoredPosition=pos; rt.sizeDelta=size; }
        private static void Place(RectTransform rt,Vector2 pos,Vector2 size) { rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); Rect(rt,pos,size); }
    }
}
