using System.Collections.Generic;
using HiLo.Core;
using TMPro;
using UnityEngine;
using static HiLo.UIFactory;

namespace HiLo
{
    public sealed class GameView
    {
        public readonly RectTransform Root, CurrentCard, NextCard, MultiplierRect;
        public readonly UnityEngine.UI.Image CurrentImage, NextImage, TimerFill, Glow;
        public readonly UnityEngine.UI.Button Higher, Lower, Collect;
        public readonly TextMeshProUGUI Balance, Streak, Best, Multiplier, Payout, Stage, Timer, Feedback, OddsHigher, OddsLower, CollectLabel, DeckLabel;
        public readonly RectTransform Overlay, Modal;
        public readonly TextMeshProUGUI ModalTitle, ModalBody;
        public readonly UnityEngine.UI.Button ModalAction, ModalClose;
        public readonly TextMeshProUGUI ModalActionLabel;
        public readonly List<UnityEngine.UI.Button> Toggles = new List<UnityEngine.UI.Button>();
        public readonly List<TextMeshProUGUI> ToggleLabels = new List<TextMeshProUGUI>();
        readonly RectTransform brand, header, metricRow, table, stageRow, reward, controls, footer, tip, history;
        readonly TextMeshProUGUI tutorial, footerText;
        readonly UnityEngine.UI.Button settings, help, fullscreen;
        readonly RectTransform[] metric = new RectTransform[3];
        readonly UnityEngine.UI.Image[] mini = new UnityEngine.UI.Image[5];
        public bool Portrait { get; private set; }
        float cardW, cardH;
        public GameView(RectTransform root, GameManager manager)
        {
            Root=root;
            var bg=Image("Table felt",root,"rounded",Hex("0B2028")); Fill(bg.rectTransform);
            Glow=Image("Atmosphere",root,"glow",Color.white);Place(Glow.rectTransform,0,80,1100,1100);
            var pattern=Rect("Table linework",root);Fill(pattern);pattern.gameObject.AddComponent<TableLinework>();
            header=Rect("Header",root);brand=Rect("Brand",header);
            var logo=Image("Brand tile",brand,"rounded",Teal);Place(logo.rectTransform,0,0,38,38);
            var arrow=Image("Mark",logo.transform,"icon_up",Hex("0B2928"));Place(arrow.rectTransform,0,0,27,27);
            var word=Text("Hi Lo",brand,"HI / LO",25,Ink,TextAlignmentOptions.Left);Place(word.rectTransform,80,2,104,34);word.characterSpacing=3;
            metricRow=Rect("Statistics",header);
            var metricLayout=metricRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();metricLayout.spacing=12;metricLayout.childControlWidth=true;metricLayout.childControlHeight=true;metricLayout.childForceExpandWidth=true;metricLayout.childForceExpandHeight=true;metricLayout.childAlignment=TextAnchor.MiddleCenter;
            string[] icons={"coin","flame","trophy"},labels={"BALANCE","STREAK","BEST"};var values=new TextMeshProUGUI[3];
            for(int i=0;i<3;i++)
            {
                metric[i]=Rect(labels[i],metricRow);var icon=Image("Icon",metric[i],"icon_"+icons[i],i==0?Gold:i==1?Coral:Muted);Place(icon.rectTransform,-37,0,24,24);
                var label=Text("Label",metric[i],labels[i],10,Muted,TextAlignmentOptions.Left);label.characterSpacing=1.5f;Place(label.rectTransform,22,11,76,15);
                values[i]=Text("Value",metric[i],i==0?"200":"0",21,Ink,TextAlignmentOptions.Left);Place(values[i].rectTransform,26,-9,84,30);values[i].enableAutoSizing=true;values[i].fontSizeMin=11;values[i].fontSizeMax=21;
            }
            Balance=values[0];Streak=values[1];Best=values[2];
            settings=Button("Settings",header,Panel,manager.ShowSettings);var settingIcon=Image("Gear",settings.transform,"icon_settings",Muted);Place(settingIcon.rectTransform,0,0,22,22);
            stageRow=Rect("Difficulty",root);
            var dot=Image("Live dot",stageRow,"circle",Teal);Place(dot.rectTransform,-86,0,6,6);
            Stage=Text("Stage",stageRow,"01  /  BEGINNER",12,Teal);Stage.characterSpacing=1.4f;Place(Stage.rectTransform,5,0,166,25);
            table=Rect("Cards",root);CurrentCard=Rect("Current card",table);NextCard=Rect("Next card",table);
            foreach(var card in new[]{CurrentCard,NextCard}) { var shadow=Image("Shadow",card,"rounded",new Color(0,0,0,.26f));Place(shadow.rectTransform,5,-9,210,294); }
            CurrentImage=Image("Face",CurrentCard,"card_0_14",Color.white);Fill(CurrentImage.rectTransform);
            NextImage=Image("Face",NextCard,"card_back",Color.white);Fill(NextImage.rectTransform);
            var currentLabel=Text("Current label",table,"CURRENT",11,Muted);currentLabel.characterSpacing=2;
            var nextLabel=Text("Next label",table,"NEXT CARD",11,Muted);nextLabel.characterSpacing=2;
            var between=Text("To",table,"→",25,Hex("466560"));Place(between.rectTransform,0,0,40,40);
            Multiplier=Text("Multiplier",root,"1.00×",52,Ink);Multiplier.fontStyle=FontStyles.Bold;Multiplier.enableAutoSizing=true;Multiplier.fontSizeMin=24;Multiplier.fontSizeMax=55;MultiplierRect=Multiplier.rectTransform;
            reward=Rect("Potential reward",root);var coin=Image("Coin",reward,"icon_coin",Gold);Place(coin.rectTransform,-94,0,21,21);
            Payout=Text("Reward",reward,"10",20,Gold,TextAlignmentOptions.Left);Place(Payout.rectTransform,-24,0,110,32);Payout.enableAutoSizing=true;Payout.fontSizeMin=11;Payout.fontSizeMax=20;
            var potential=Text("Potential",reward,"POTENTIAL",10,Muted,TextAlignmentOptions.Left);potential.characterSpacing=1;Place(potential.rectTransform,80,0,80,22);
            tip=Rect("Feedback",root);Feedback=Text("Feedback",tip,"Your next move.",15,Muted);Fill(Feedback.rectTransform);
            controls=Rect("Controls",root);
            Higher=PredictionButton("Higher",controls,"up",Teal,()=>manager.Predict(true),out OddsHigher);
            Lower=PredictionButton("Lower",controls,"down",Coral,()=>manager.Predict(false),out OddsLower);
            Collect=Button("Cash out",controls,Gold,manager.CashOut);var collectIcon=Image("Collect",Collect.transform,"icon_collect",Hex("372D18"));Place(collectIcon.rectTransform,-52,5,29,29);
            CollectLabel=Text("Cash out label",Collect.transform,"Cash out",19,Hex("372D18"));Place(CollectLabel.rectTransform,20,5,108,30);
            var shortcut=Text("Shortcut",Collect.transform,"SPACE",9,Hex("756442"));shortcut.characterSpacing=1.4f;Place(shortcut.rectTransform,0,-23,100,14);
            tutorial=Text("Learn controls",root,"Predict the next card. Equal is a push. Ace is high.",12,Muted);
            footer=Rect("Footer",root);footerText=Text("Footer note",footer,"VIRTUAL COINS. REAL INSTINCT.",10,Hex("648783"),TextAlignmentOptions.Left);footerText.characterSpacing=1.5f;
            DeckLabel=Text("Deck",footer,"51 / 52",11,Muted);Place(DeckLabel.rectTransform,0,0,100,24);
            help=Button("How to play",footer,Color.clear,manager.ShowHelp);var helpIcon=Image("Help",help.transform,"icon_help",Muted);Place(helpIcon.rectTransform,0,0,20,20);
            fullscreen=Button("Fullscreen",footer,Color.clear,manager.Fullscreen);var fullIcon=Image("Expand",fullscreen.transform,"icon_fullscreen",Muted);Place(fullIcon.rectTransform,0,0,20,20);
            var timerRoot=Rect("Decision clock",root); TimerFill=Image("Countdown",timerRoot,"circle",Teal);Place(TimerFill.rectTransform,0,0,42,42);TimerFill.type=UnityEngine.UI.Image.Type.Filled;TimerFill.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;TimerFill.fillOrigin=2;TimerFill.fillClockwise=false;
            var timerHole=Image("Clock inner",timerRoot,"circle",Hex("102B31"));Place(timerHole.rectTransform,0,0,36,36);
            Timer=Text("Seconds",timerRoot,"∞",15,Teal);Place(Timer.rectTransform,0,0,40,40);
            history=Rect("Recent cards",root);for(int i=0;i<mini.Length;i++){mini[i]=Image("Recent "+i,history,"card_back",Color.white);Place(mini[i].rectTransform,(i-2)*32,0,26,36);mini[i].gameObject.SetActive(false);}
            Overlay=Rect("Modal overlay",root);Fill(Overlay);var shade=Image("Dim",Overlay,"rounded",new Color(.015f,.055f,.065f,.94f));Fill(shade.rectTransform);shade.raycastTarget=true;
            Modal=Rect("Panel",Overlay);var panel=Image("Panel background",Modal,"rounded",Hex("142E36"));Fill(panel.rectTransform);
            ModalTitle=Text("Title",Modal,"",30,Ink);Place(ModalTitle.rectTransform,0,163,300,44);
            ModalBody=Text("Description",Modal,"",16,Muted);ModalBody.textWrappingMode=TextWrappingModes.Normal;Place(ModalBody.rectTransform,0,16,294,220);
            ModalClose=Button("Close",Modal,Hex("203D43"),manager.CloseModal);var close=Image("X",ModalClose.transform,"icon_close",Muted);Place(close.rectTransform,0,0,17,17);Place((RectTransform)ModalClose.transform,144,163,34,34);
            ModalAction=Button("Play",Modal,Teal,manager.ModalPrimary);Place((RectTransform)ModalAction.transform,0,-154,286,59);
            ModalActionLabel=Text("Play label",ModalAction.transform,"Let’s play  ·  10",19,Hex("0C2927"));Fill(ModalActionLabel.rectTransform);
            string[] toggleNames={"Sound effects","Ambient music","Reduced motion","Streak timer"};
            for(int i=0;i<4;i++) { int index=i;var b=Button(toggleNames[i],Modal,Hex("203D43"),()=>manager.ToggleSetting(index));Place((RectTransform)b.transform,0,80-i*57,286,46);var l=Text("State",b.transform,toggleNames[i],16,Ink);Fill(l.rectTransform);Toggles.Add(b);ToggleLabels.Add(l);b.gameObject.SetActive(false); }
            Overlay.gameObject.SetActive(false);
        }
        UnityEngine.UI.Button PredictionButton(string name,Transform parent,string icon,Color color,UnityEngine.Events.UnityAction action,out TextMeshProUGUI odds)
        {
            var b=Button(name,parent,color,action);var arrow=Image("Arrow",b.transform,"icon_"+icon,Hex("14322D"));Place(arrow.rectTransform,-60,3,33,33);
            var l=Text("Label",b.transform,name,21,Hex("14322D"));Place(l.rectTransform,15,7,112,32);l.fontStyle=FontStyles.Bold;
            odds=Text("Odds",b.transform,"—",11,Hex("36564D"));Place(odds.rectTransform,15,-20,144,19);return b;
        }
        public void Layout(float width,float height,bool portrait,bool compact)
        {
            Portrait=portrait;float w=portrait?Mathf.Min(width-36,420):Mathf.Min(width-90,1060);float top=height/2;
            Place(header,0,top-44,w,65);Place(brand,-w/2+19,0,140,46);
            Place((RectTransform)settings.transform,w/2-20,0,42,42);
            Place(metricRow,portrait?0:w/2-270,portrait?-76:0,portrait?w:390,52);
            float cy, multY, controlY, feedbackY;
            if(portrait)
            {
                cardW=Mathf.Min(153,(w-44)/2);cardH=Mathf.Min(cardW*1.4f,height-602);cardW=cardH/1.4f;
                cy=top-203-cardH/2;multY=cy-cardH/2-54;controlY=-top+157;feedbackY=controlY+104;
                Place(stageRow,0,top-164,210,30);
                float historyGap=multY-59-(feedbackY+15);history.gameObject.SetActive(historyGap>54);Place(history,0,(multY-59+feedbackY+15)/2,200,40);
                Place(MultiplierRect,0,multY,340,76);Multiplier.fontSizeMax=48;
                Place(reward,0,multY-43,230,32);
                Place(controls,0,controlY,w,180);
                Place((RectTransform)Higher.transform,-(w+10)/4,16,(w-10)/2,74);Place((RectTransform)Lower.transform,(w+10)/4,16,(w-10)/2,74);
                Place((RectTransform)Collect.transform,0,-69,w,60);
                Place(tutorial.rectTransform,0,-top+39,w,26);tutorial.fontSize=10.5f;
                Place(footer,0,-top+11,w,25);footerText.gameObject.SetActive(false);DeckLabel.gameObject.SetActive(false);
                Place((RectTransform)help.transform,-w/2+12,0,40,34);Place((RectTransform)fullscreen.transform,w/2-12,0,40,34);
            }
            else if(compact)
            {
                Place(header,0,top-31,w,50);Place(brand,-w/2+19,0,140,46);cardW=110;cardH=154;cy=22;multY=35;controlY=-top+65;feedbackY=controlY+65;
                Place(stageRow,220,top-94,210,30);Place(history,-160,-80,200,38);history.gameObject.SetActive(true);
                Place(MultiplierRect,220,55,260,74);Multiplier.fontSizeMax=43;Place(reward,220,10,230,30);
                Place(controls,0,controlY,650,70);Place((RectTransform)Higher.transform,-220,0,206,62);Place((RectTransform)Lower.transform,0,0,206,62);Place((RectTransform)Collect.transform,220,0,206,62);
                Place(tutorial.rectTransform,0,-top+15,650,20);Place(footer,0,-top+14,w,28);footerText.gameObject.SetActive(false);DeckLabel.gameObject.SetActive(false);
                Place((RectTransform)help.transform,-w/2+12,0,40,34);Place((RectTransform)fullscreen.transform,w/2-12,0,40,34);
            }
            else
            {
                cardW=200;cardH=280;cy=36;multY=-150;controlY=-top+110;feedbackY=-top+175;
                Place(stageRow,0,top-144,210,30);Place(history,-345,5,200,40);history.gameObject.SetActive(true);
                Place(MultiplierRect,0,multY,500,80);Multiplier.fontSizeMax=55;Place(reward,0,multY-46,230,30);
                Place(controls,0,controlY,740,90);Place((RectTransform)Higher.transform,-248,0,232,82);Place((RectTransform)Lower.transform,0,0,232,82);Place((RectTransform)Collect.transform,248,0,232,82);
                Place(tutorial.rectTransform,0,-top+57,700,25);Place(footer,0,-top+28,w,30);footerText.gameObject.SetActive(true);DeckLabel.gameObject.SetActive(true);Place(footerText.rectTransform,-w/2+160,0,320,25);
                Place((RectTransform)help.transform,w/2-64,0,40,40);Place((RectTransform)fullscreen.transform,w/2-15,0,40,40);
            }
            float tableX=compact?-160:0;Place(table,tableX,cy,500,cardH);
            float gap=portrait?20:40;float offset=(cardW+gap)/2;
            Place(CurrentCard,-offset,0,cardW,cardH);Place(NextCard,offset,0,cardW,cardH);
            foreach(var card in new[]{CurrentCard,NextCard})Place((RectTransform)card.GetChild(0),4,-7,cardW,cardH);
            Place((RectTransform)table.Find("Current label"),-offset,cardH/2+22,cardW,22);Place((RectTransform)table.Find("Next label"),offset,cardH/2+22,cardW,22);
            Place(tip,compact?220:0,compact?-45:feedbackY,portrait?w:500,30);
            Place((RectTransform)Timer.transform.parent,portrait?w/2-22:compact?350:345,portrait?top-166:compact?10:80,44,44);
            Place(Modal,0,0,Mathf.Min(width-28,350),Mathf.Min(height-24,428));
            if(compact) { Modal.localScale=Vector3.one*.85f; } else Modal.localScale=Vector3.one;
        }
        public void SetCard(Card card) => CurrentImage.sprite=Resources.Load<Sprite>("Art/"+card.AssetName);
        public void LayoutReward()
        {
            float valueWidth=Mathf.Clamp(Payout.GetPreferredValues().x+4,22,116);float total=21+7+valueWidth+10+80;
            Place((RectTransform)reward.Find("Coin"),-total/2+10.5f,0,21,21);
            Place(Payout.rectTransform,-total/2+28+valueWidth/2,0,valueWidth,32);
            Place((RectTransform)reward.Find("Potential"),total/2-40,0,80,22);
        }
        public void AddHistory(Card card)
        {
            for(int i=mini.Length-1;i>0;i--){mini[i].sprite=mini[i-1].sprite;mini[i].gameObject.SetActive(mini[i-1].gameObject.activeSelf);}
            mini[0].sprite=Resources.Load<Sprite>("Art/"+card.AssetName);mini[0].gameObject.SetActive(true);
        }
        public void ResetHistory(){foreach(var item in mini)item.gameObject.SetActive(false);}
        public void ModalMode(bool settingsMode,bool close)
        {Overlay.gameObject.SetActive(true);ModalClose.gameObject.SetActive(close);ModalBody.gameObject.SetActive(!settingsMode);foreach(var t in Toggles)t.gameObject.SetActive(settingsMode);ModalAction.gameObject.SetActive(!settingsMode);}
    }
    public sealed class TableLinework : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Color32 c=new Color(.2f,.5f,.43f,.07f);
            for(int i=0;i<9;i++) {float radius=180+i*56;int steps=96;for(int j=0;j<steps;j++) {float a=j*Mathf.PI*2/steps,b=(j+1)*Mathf.PI*2/steps;Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,q=new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;int n=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);vh.AddVert(q,c,Vector2.zero);vh.AddVert(q+q.normalized*.7f,c,Vector2.zero);vh.AddVert(p+p.normalized*.7f,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}}
        }
        protected override void Awake(){base.Awake();raycastTarget=false;}
    }
}
