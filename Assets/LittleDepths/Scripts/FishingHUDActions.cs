using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths
{
    public partial class FishingHUD
    {
        public RectTransform timingLane;
        public Image castTarget;
        public Text timingPowerReadout,diveCatchLabel,reelActionLabel;

        RectTransform PlayDock => (RectTransform)harborRoot.transform.Find("Supplied play and equipment panel");

        void ConfigureControlLayout()
        {
            // Expand keeps the whole HUD in view as the browser resizes.
            GetComponent<CanvasScaler>().referenceResolution=new Vector2(1360,780);

            var bar=(RectTransform)settingsButton.transform.parent;
            bar.localScale=Vector3.one*.70f;
            bar.anchoredPosition=new Vector2(0,-74);
            PadHitArea(settingsButton,7,12);
            PadHitArea(prizeButton,6,8);
            // Let the balance and the plus icon open the same upgrade panel.
            walletPlusButton.image.raycastPadding=new Vector4(-160,-30,-10,-30);

            var dock=PlayDock;
            dock.localScale=Vector3.one*.74f;
            dock.anchoredPosition=new Vector2(0,dock.rect.height*dock.localScale.y/2+12);
            foreach(var button in upgradeButtons)
            {
                var rect=(RectTransform)button.transform;
                // The lower part of each equipment card is a single target.
                rect.anchoredPosition+=Vector2.up*42;
                rect.sizeDelta+=Vector2.up*84;
                PadHitArea(button,6,4);
            }

            ResizeNav(characterButton,212);
            ResizeNav(journalButton,360);
            ResizeNav(hooksButton,508);
            ResizeNav(shopButton,212);
            ResizeNav(missionsButton,360);

            var haul=(RectTransform)reelButton.transform.parent;
            var motion=haul.GetComponent<UiMotion>();
            motion.SetRestScale(Vector3.one*1.17f);
            haul.anchoredPosition=new Vector2(-178,-36);
            PadHitArea(reelButton,18,17);
            reelActionLabel.fontSize=26;
        }

        void ResizeNav(Button button,float top)
        {
            var rect=(RectTransform)button.transform;
            rect.sizeDelta=Vector2.one*118;
            rect.anchoredPosition=new Vector2(rect.anchorMin.x==0?91:-91,-top);
            PadHitArea(button,9,9);
            var label=button.GetComponentInChildren<Text>();
            label.fontSize=17;
            label.rectTransform.anchoredPosition=new Vector2(0,-73);
        }

        static void PadHitArea(Button button,float x,float y)
        {
            button.image.raycastPadding=new Vector4(-x,-y,-x,-y);
        }

        public void UpgradeSideControls()
        {
            int sibling=activeRoot?activeRoot.transform.GetSiblingIndex():castRoot.transform.GetSiblingIndex()+1;
            bool showDive=activeRoot&&activeRoot.activeSelf,showTiming=timingBar&&timingBar.activeSelf;
            if(timingBar)DestroyImmediate(timingBar);
            if(activeRoot)DestroyImmediate(activeRoot);
            CreateFishingActions();
            activeRoot.transform.SetSiblingIndex(sibling);
            activeRoot.SetActive(showDive);timingBar.SetActive(showTiming);
        }

        void CreateFishingActions()
        {
            var left=new Vector2(0,.5f);var right=new Vector2(1,.5f);
            timingBar=UiKit.Image("Crafted cast timing meter",castRoot.transform,Art("ref_cast_meter"),Color.white,184,-126,320,320/3f,left).gameObject;
            timingBar.AddComponent<UiMotion>().entrance=true;
            timingPowerReadout=SourceText(timingBar.transform,"0%",32,Color.white,750,65,115,40,960,320);
            timingLane=Part("Bobber travel lane",timingBar.transform,252,156,612,54,960,320);
            float threshold=FishingGame.PerfectCastThreshold;
            float targetWidth=timingLane.rect.width*(1-threshold),targetHeight=timingLane.rect.height*.86f;
            castTarget=UiKit.Image("Gold perfect-cast target",timingLane,Art("ref_cast_target"),Color.white,timingLane.rect.width*threshold/2,0,targetWidth,targetHeight);
            castTarget.type=Image.Type.Sliced;
            castTarget.pixelsPerUnitMultiplier=castTarget.sprite.rect.height/targetHeight;
            var cursor=UiKit.Image("Animated bobber cursor",timingLane,Art("ref_cast_cursor"),Color.white,-timingLane.rect.width/2,6,17,32);
            cursor.preserveAspect=true;timingNeedle=cursor.rectTransform;
            timingBar.SetActive(false);

            activeRoot=UiKit.Rect("Dive HUD",transform,0,0,1,1).gameObject;UiKit.Stretch((RectTransform)activeRoot.transform);
            var depth=UiKit.Image("Nautical depth instrument",activeRoot.transform,Art("ref_depth_badge"),Color.white,108,65,140,175,left);
            depth.gameObject.AddComponent<UiMotion>().entrance=true;
            depthLabel=Label(depth.transform,"0.0 m",23,Color.white,0,-8,110,38,true);

            var trip=UiKit.Image("Crafted live haul panel",activeRoot.transform,Art("ref_dive_side"),Color.white,-158,-36,264,407,right);
            trip.gameObject.AddComponent<UiMotion>().entrance=true;
            phaseLabel=Label(trip.transform,"GOING DEEPER",20,UiKit.Coral,8,132,205,56,true);
            Label(trip.transform,"THIS HAUL",16,UiKit.Ink,0,65,190,28,true);
            tripValue=Label(trip.transform,"$0",43,UiKit.Ink,0,16,195,64,true);
            diveCatchLabel=Label(trip.transform,"CATCH 0 / 5",17,UiKit.Ink,0,-43,196,35,true);
            reelButton=UiKit.Button("Reel in",trip.transform,null,Color.clear,0,-126,206,62);
            var colors=reelButton.colors;colors.disabledColor=colors.highlightedColor=colors.selectedColor=Color.white;reelButton.colors=colors;
            reelActionLabel=Label(reelButton.transform,"REEL IN",24,Color.white,0,0,192,54,true);
            activeRoot.SetActive(false);
        }

        void PlaceCastControl(bool home)
        {
            var rect=(RectTransform)castButton.transform;
            rect.localScale=Vector3.one;
            var motion=castButton.GetComponent<UiMotion>();if(motion)motion.SetRestScale(Vector3.one);
            if(home)
            {
                // The original supplied dock already paints its PLAY disc. Align
                // an invisible hit area with it, respecting the user's dock scale.
                var dock=PlayDock;
                rect.anchorMin=rect.anchorMax=dock.anchorMin;
                float offset=(128-262.5f)*dock.rect.width/525*dock.localScale.x;
                rect.anchoredPosition=dock.anchoredPosition+Vector2.right*offset;
                float size=dock.rect.width*174/525*dock.localScale.x;
                rect.sizeDelta=Vector2.one*size;
                castButton.image.color=Color.clear;
            }
            else
            {
                rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);
                rect.anchoredPosition=new Vector2(184,26);rect.sizeDelta=Vector2.one*184;
                castButton.image.color=Color.white;
            }
        }

        void RefreshFishingActions()
        {
            reelButton.interactable=game.Phase==FishingPhase.Descending;
            reelActionLabel.text=game.Phase==FishingPhase.Ascending?"REELING IN…":"REEL IN";
            diveCatchLabel.text="CATCH  "+game.HaulCount+" / "+game.config.Capacity(game.Progress);
            if(game.Phase!=FishingPhase.Timing)return;
            float power=game.TimingPower;
            timingNeedle.anchoredPosition=new Vector2(Mathf.Lerp(-timingLane.rect.width/2,timingLane.rect.width/2,power),6);
            timingNeedle.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*5)*3);
            bool perfect=power>=FishingGame.PerfectCastThreshold;
            timingPowerReadout.text=Mathf.FloorToInt(power*100)+"%";
            timingPowerReadout.color=perfect?UiKit.Coral:Color.white;
            castTarget.color=perfect?Color.white:new Color(1,1,1,.9f+.05f*Mathf.Sin(Time.unscaledTime*4));
        }
    }
}
