using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static LittleDepths.UiKit;

namespace LittleDepths
{
    public partial class FishingHUD : MonoBehaviour
    {
        public Font bodyFont, displayFont;
        public Sprite rounded, circle;
        public Sprite[] art;
        public RectTransform canvasRoot, modalLayer, floatLayer;
        public GameObject harborRoot, castRoot, activeRoot;
        public Text wallet, best, sessionLabel, collectionLabel, depthLabel, haulLabel, phaseLabel, tripValue, castLabel, castHint, toastText;
        public Text[] upgradeValue, upgradePrice;
        public Button castButton, reelButton, journalButton, hooksButton, prizeButton, settingsButton, characterButton;
        public Button[] upgradeButtons;
        public RectTransform timingNeedle;
        public GameObject timingBar;
        public bool IsModalOpen => modalLayer && modalLayer.gameObject.activeSelf;
        FishingGame game;
        string modalType;
        float toastUntil;
        readonly List<Floating> floats = new List<Floating>();
        class Floating { public Text label; public Vector3 world; public float life=1.3f; }
        static readonly string[] hookNames = { "The original", "Sunbeam", "Sweetheart", "Sea glass", "Stardust", "Copper tide" };

        public Sprite Art(string name)
        {
            name=name switch {
                "ui_panel"=>"ref_panel","ui_inset"=>"ref_preview_fill","ui_wood"=>"ref_wood_patch",
                "ui_green"=>"ref_button_green","ui_blue"=>"ref_button_blue","ui_red"=>"ref_button_red",
                "ui_white"=>"ref_card_white","ui_gold"=>"ref_frame_gold","ui_silver_frame"=>"ref_frame_silver",
                "ui_bronze_frame"=>"ref_frame_gold","ui_gold_frame"=>"ref_frame_gold","ui_epic_frame"=>"ref_frame_epic",_=>name};
            foreach(var s in art) if(s && s.name == name) return s;
            return null;
        }
        public Text Label(Transform parent,string value,int size,Color color,float x,float y,float w,float h, bool heavy=false,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var label=UiKit.Text(value,parent,heavy?displayFont:bodyFont,value,size,color,x,y,w,h,align);
            if(heavy && size>=20 && (color==Color.white || color==Cream || color==Coral))
            {var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.035f,.065f,.16f,.94f);outline.effectDistance=new Vector2(size>=38?2.5f:1.5f,-(size>=38?2.5f:1.5f));}
            return label;
        }
        Image Card(string name,Transform parent,Color color,float x,float y,float w,float h) => UiKit.Image(name,parent,Art("ui_inset")??rounded,Color.white,x,y,w,h);
        Button ActionButton(Transform parent,string value,float x,float y,float w,float h,Action action,Color? color=null)
        {
            var b=UiKit.Button(value,parent,Art(color.HasValue?"ui_blue":"ui_green")??rounded,Color.white,x,y,w,h);
            Label(b.transform,value,21,Color.white,0,0,w-14,h,true); b.onClick.AddListener(()=>{game.Sound(game.clickSound);action();}); return b;
        }

        public void Bind(FishingGame controller)
        {
            game=controller;
            ConfigureControlLayout();
            castButton.onClick.AddListener(game.CastButton); reelButton.onClick.AddListener(game.ReelIn);
            reelButton.name="Reel in";
            journalButton.onClick.AddListener(ShowJournal); hooksButton.onClick.AddListener(ShowHooks);
            prizeButton.onClick.AddListener(ShowPrizes); settingsButton.onClick.AddListener(ShowSettings);
            characterButton.onClick.AddListener(ShowCharacterCreator);
            BindProvidedNavigation();
            for(int i=0;i<3;i++) { int index=i; upgradeButtons[i].onClick.AddListener(()=>game.BuyUpgrade(index)); }
            modalLayer.gameObject.SetActive(false); toastText.gameObject.SetActive(false); SetPhase(); Refresh();
        }

        public void SetPhase()
        {
            bool home=game.Phase==FishingPhase.Harbor;
            bool timing=game.Phase==FishingPhase.Timing;
            harborRoot.SetActive(home); castRoot.SetActive(home||timing); activeRoot.SetActive(game.IsFishing);
            timingBar.SetActive(timing); castLabel.text="";
            if(home||timing)PlaceCastControl(home);
            if(castHint)castHint.text=timing?"Tap again in the gold zone":"Tap PLAY to start fishing";
        }

        public void Refresh()
        {
            var p=game.Progress; var c=game.config;
            wallet.text="$ " + p.coins.ToString("N0");
            // Custom scene layouts can omit these supplementary labels.
            if(best)best.text="$ " + p.bestHaul.ToString("N0");
            if(sessionLabel)sessionLabel.text=$"CASTS  {Mathf.Max(0,c.castsPerSession-p.sessionCasts)} / {c.castsPerSession}";
            if(collectionLabel)collectionLabel.text=$"{p.discovered.Count:00} / {c.species.Count:00} species discovered";
            depthLabel.text=game.Depth.ToString("0.0")+" m";
            haulLabel.text=$"{game.HaulCount} / {c.Capacity(p)}";
            phaseLabel.text=game.Phase==FishingPhase.Descending?"GOING DEEPER":"REELING IT IN";
            tripValue.text="$"+game.HaulValue.ToString("N0");
            RefreshFishingActions();
            upgradeValue[0].text=c.Capacity(p)+" fish"; upgradeValue[1].text=c.MaxDepth(p).ToString("0")+" meters";
            upgradeValue[2].text="$"+c.IncomePerMinute(p)+" / min";
            for(int i=0;i<3;i++)
            {
                bool max=c.UpgradeLevel(p,i)>=c.UpgradeLimit(i); int price=c.UpgradeCost(p,i);
                upgradePrice[i].text=max?"MAX":"$"+price.ToString("N0");
                upgradeButtons[i].interactable=!max && p.coins>=price;
            }
            toastText.gameObject.SetActive(Time.unscaledTime<toastUntil);
            toastText.rectTransform.anchoredPosition=new Vector2(0,IsModalOpen?-canvasRoot.rect.height+42:-183);
            if(IsModalOpen)refreshWindow?.Invoke();
        }

        void LateUpdate()
        {
            if(!game) return;
            for(int i=floats.Count-1;i>=0;i--)
            {
                var f=floats[i]; f.life-=Time.unscaledDeltaTime;
                if(f.life<=0) { Destroy(f.label.gameObject); floats.RemoveAt(i); continue; }
                Vector2 screen=game.gameCamera.WorldToScreenPoint(f.world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer,screen,game.gameCamera,out var local);
                f.label.rectTransform.anchoredPosition=local+Vector2.up*(1.3f-f.life)*65;
                var color=f.label.color; color.a=Mathf.Clamp01(f.life*2.5f); f.label.color=color;
            }
        }
        public void Toast(string text,Color color)
        {
            toastText.text=text; toastText.color=color; toastUntil=Time.unscaledTime+2.3f; toastText.gameObject.SetActive(true);
        }
        public void FloatText(string text,Vector3 world,Color color)
        {
            var label=Label(floatLayer,text,29,color,0,0,360,80,true);
            var shadow=label.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.05f,.17f,.28f,.3f); shadow.effectDistance=new Vector2(0,-2);
            floats.Add(new Floating {label=label,world=world});
        }

        public void CloseModal(bool force=false)
        {
            if(!force&&modalType=="results"){game.ReturnToHarbor();return;}
            if(prizeReveal!=null){StopCoroutine(prizeReveal);prizeReveal=null;}
            refreshWindow=null;
            modalLayer.gameObject.SetActive(false); modalType=null;
        }
        RectTransform Open(string type,string eyebrow,string title,string subtitle,float height=620,float width=1000)
        {
            toastUntil=0;toastText.gameObject.SetActive(false);
            ClearWindowChildren();
            modalLayer.gameObject.SetActive(true); modalType=type;
            var shade=UiKit.Image("Quiet backdrop",modalLayer,null,new Color(.05f,.16f,.23f,.68f),0,0,1600,1600); Stretch(shade.rectTransform); shade.raycastTarget=true;
            bool aquatic=type=="hooks"||type=="journal"||type=="welcome";
            var panel=UiKit.Image(title,modalLayer,Art(aquatic?"ref_collection_panel":"ui_panel"),Color.white,0,0,width,height).rectTransform;
            if(aquatic)panel.GetComponent<Image>().pixelsPerUnitMultiplier=2;
            panel.gameObject.AddComponent<UiMotion>().entrance=true;
            Decorate(panel,width,height);
            Label(panel,eyebrow.ToUpperInvariant(),13,Muted,0,height/2-49,width-140,22,true);
            Label(panel,title,43,Coral,0,height/2-82,width-140,68,true);
            Label(panel,subtitle,17,Muted,0,height/2-133,width-120,42);
            AddWindowClose(panel);
            return panel;
        }


        public void ShowSession()
        {
            var p=game.Progress; var panel=Open("session","FIVE CASTS. ONE GOOD DAY.","SESSION COMPLETE!","Your fishing session is complete. The lake will be waiting.",600);
            Label(panel,"$ "+p.sessionCoins.ToString("N0"),76,Teal,0,18,680,100,true);
            Label(panel,$"{p.sessionFish} fish caught    •    {p.discovered.Count} species in your journal",21,Muted,0,-57,680,40);
            ActionButton(panel,"FISH AGAIN",0,-192,370,70,game.NewSession);
        }
        public void ShowWelcome(int coins)
        {
            var panel=Open("welcome","YOUR AQUARIUM KEPT BUSY","WELCOME BACK","Your discovered fish earned a little something while you were away.",560,900);
            var earnings=UiKit.Image("Offline earnings equipment card",panel,Art("ref_collection_card"),Color.white,0,-14,520,170);
            earnings.type=UnityEngine.UI.Image.Type.Simple;
            Label(earnings.transform,"OFFLINE EARNINGS",20,Ink,0,47,460,36,true);
            Label(earnings.transform,"+ $ "+coins.ToString("N0"),67,Hex("27852a"),0,-20,460,90,true);
            ActionButton(panel,"LOVELY, THANK YOU",0,-190,420,70,()=>{CloseModal();if(game.Progress.sessionCasts>=game.config.castsPerSession)ShowSession();});
        }





        // Called by the scene setup tool. Everything here is saved into the scene.

    }
}
