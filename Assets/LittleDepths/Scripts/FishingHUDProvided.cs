using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static LittleDepths.UiKit;

namespace LittleDepths
{
    public partial class FishingHUD
    {
        public void CreateLayout()=>CreateProvidedLayout();
        public void ShowCharacterCreator()=>ShowProvidedCreator();
        public void ShowPrizes()=>ShowProvidedPrizes();
        public void ShowResults()=>ShowProvidedResults();
        public void ShowSettings()=>ShowProvidedPause();
        public Button shopButton,missionsButton,walletPlusButton;
        void BindProvidedNavigation()
        {
            shopButton.onClick.AddListener(ShowProvidedUpgrades);
            missionsButton.onClick.AddListener(ShowProvidedMissions);
            walletPlusButton.onClick.AddListener(ShowProvidedUpgrades);
        }
        // Reference-guided high resolution sprites retain the supplied designs.
        // Variable values are live Text objects over deliberately blank fields.
        RectTransform Part(string name,Transform parent,float x,float y,float w,float h,float sw,float sh)
        {
            var area=(RectTransform)parent;float sx=area.rect.width/sw,sy=area.rect.height/sh;
            return UiKit.Rect(name,parent,(x+w/2-sw/2)*sx,(sh/2-y-h/2)*sy,w*sx,h*sy);
        }
        Image Source(string name,Transform parent,float x,float y,float w,float h,float sw,float sh)
        {
            var r=Part(name,parent,x,y,w,h,sw,sh);var im=r.gameObject.AddComponent<Image>();im.sprite=Art(name);im.raycastTarget=false;return im;
        }
        Text SourceText(Transform parent,string text,int size,Color color,float x,float y,float w,float h,float sw,float sh,bool heavy=true)
        {
            var r=Part("Live "+text,parent,x,y,w,h,sw,sh);
            return Label(r,text,Mathf.RoundToInt(size*((RectTransform)parent).rect.height/sh),color,0,0,r.rect.width,r.rect.height,heavy);
        }
        Button Hotspot(string name,Transform parent,float x,float y,float w,float h,float sw,float sh,Action action=null)
        {
            var r=Part(name,parent,x,y,w,h,sw,sh);var im=r.gameObject.AddComponent<Image>();im.color=Color.clear;im.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};
            if(action!=null)b.onClick.AddListener(()=>{game.Sound(game.clickSound);action();});return b;
        }
        Text Value(Transform parent,string patch,int font,Color color,float x,float y,float w,float h,float sw,float sh)
        {
            Source(patch,parent,x,y,w,h,sw,sh);return SourceText(parent,"0",font,color,x,y,w,h,sw,sh);
        }
        void CompleteFrame(RectTransform coordinates,string sprite,Rect previous,Rect complete)
        {
            float sx=coordinates.rect.width/previous.width,sy=coordinates.rect.height/previous.height;
            var offset=complete.center-previous.center;
            UiKit.Image("Complete original border",coordinates,Art(sprite),Color.white,offset.x*sx,-offset.y*sy,complete.width*sx,complete.height*sy);
        }
        RectTransform SuppliedPanel(string type,string sprite,float width,float height)
        {
            toastUntil=0;toastText.gameObject.SetActive(false);
            ClearWindowChildren();
            modalType=type;modalLayer.gameObject.SetActive(true);
            var shade=UiKit.Image("Backdrop",modalLayer,null,new Color(.02f,.06f,.14f,.7f),0,0,1,1);Stretch(shade.rectTransform);shade.raycastTarget=true;
            var panel=UiKit.Image("Supplied "+type+" artwork",modalLayer,Art(sprite),Color.white,0,0,width,height).rectTransform;
            panel.gameObject.AddComponent<UiMotion>().entrance=true;
            AddWindowClose(panel);
            return panel;
        }

        void CreateProvidedLayout()
        {
            canvasRoot=GetComponent<RectTransform>();var top=new Vector2(.5f,1);var left=new Vector2(0,1);var right=new Vector2(1,1);var bottom=new Vector2(.5f,0);
            var bar=UiKit.Image("Supplied HUD and counters",transform,Art("ref_hud"),Color.white,0,-104,1130,185,top).rectTransform;
            wallet=SourceText(bar,"$0",20,Color.white,75,23,91,36,525,86);
            haulLabel=SourceText(bar,"0 / 5",22,Color.white,291,23,63,36,525,86);
            prizeButton=Hotspot("Supplied event star",bar,373,6,62,72,525,86);
            settingsButton=Hotspot("Supplied settings gear",bar,443,14,66,63,525,86);
            walletPlusButton=Hotspot("Wallet plus • upgrades",bar,171,22,35,43,525,86);
            bestCaption=UiKit.Text("Best caption",transform,bodyFont,"BEST",12,Color.white,-121,-48,205,25,TextAnchor.MiddleCenter,right);
            best=UiKit.Text("Best haul",transform,displayFont,"$0",23,Color.white,-121,-77,205,39,TextAnchor.MiddleCenter,right);

            harborRoot=UiKit.Rect("Harbor navigation",transform,0,0,1,1).gameObject;Stretch((RectTransform)harborRoot.transform);
            characterButton=ProvidedNav("Character","ref_icon_outfit",83,-275,left);
            journalButton=ProvidedNav("Fish gallery","ref_icon_medal",83,-405,left);
            hooksButton=ProvidedNav("Hooks","ref_icon_hook",83,-535,left);
            shopButton=ProvidedNav("Upgrades","ref_icon_shop",-83,-275,right);
            missionsButton=ProvidedNav("Missions","ref_icon_trophy",-83,-405,right);
            sessionLabel=UiKit.Text("Casts remaining",harborRoot.transform,displayFont,"CASTS 5 / 5",18,Color.white,-135,-515,245,40,TextAnchor.MiddleCenter,right);
            collectionLabel=UiKit.Text("Fish discovered",harborRoot.transform,bodyFont,"0 / 10 species",15,Color.white,-135,-550,245,45,TextAnchor.MiddleCenter,right);

            const float dockW=997,dockH=391,dockY=204;
            var dock=UiKit.Image("Supplied play and equipment panel",harborRoot.transform,Art("ref_dock"),Color.white,0,dockY,dockW,dockH,bottom).rectTransform;
            upgradeButtons=new Button[3];upgradeValue=new Text[3];upgradePrice=new Text[3];
            for(int i=0;i<3;i++)
            {
                upgradeValue[i]=SourceText(dock,"5",8,Ink,249+i*91,i==0?113:124,63,12,525,206);
                upgradePrice[i]=SourceText(dock,"0",15,Color.white,250+i*91,145,62,21,525,206);
                upgradeButtons[i]=Hotspot("Supplied equipment upgrade "+i,dock,242+i*91,138,77,35,525,206);
            }
            castRoot=UiKit.Rect("Cast controls",transform,0,0,1,1).gameObject;Stretch((RectTransform)castRoot.transform);
            float factor=dockW/525;
            castButton=UiKit.Button("Supplied PLAY disc",castRoot.transform,Art("ref_play"),Color.white,(128-262.5f)*factor,dockY,174*factor,174*factor,bottom);
            castLabel=Label(castButton.transform,"",26,Color.white,0,0,220,40,true);
            castHint=UiKit.Text("Cast instruction",castRoot.transform,bodyFont,"Tap PLAY to cast",14,Color.white,-250,13,440,26,TextAnchor.MiddleCenter,bottom);
            CreateFishingActions();
            floatLayer=UiKit.Rect("Catch feedback",transform,0,0,1,1);Stretch(floatLayer);
            modalLayer=UiKit.Rect("Menus",transform,0,0,1,1);Stretch(modalLayer);modalLayer.gameObject.SetActive(false);
            toastText=UiKit.Text("Rewards and discoveries",transform,displayFont,"",23,Coral,0,-201,1120,45,TextAnchor.MiddleCenter,top);
            var outline=toastText.gameObject.AddComponent<Outline>();outline.effectColor=Ink;outline.effectDistance=new Vector2(2,-2);
        }
        Button ProvidedNav(string title,string sprite,float x,float y,Vector2 anchor)
        {
            var b=UiKit.Button(title,harborRoot.transform,Art(sprite),Color.white,x,y,88,88,anchor);
            b.image.preserveAspect=true;
            Label(b.transform,title.ToUpperInvariant(),13,Color.white,0,-58,145,27,true);return b;
        }

        void ShowProvidedCreator()
        {
            var p=game.Progress;draftOutfit=p.outfit;draftHair=p.hairstyle;draftHat=p.hat;draftBoat=p.boatStyle;refreshChoices.Clear();
            var panel=SuppliedPanel("character","ref_character",1014,616);
            var stage=Part("Complete sprite preview stage",panel,68,66,181,166,507,308);
            previewAngler=AppearanceImage("Complete character with hat and boat",stage);
            var hair=SourceText(panel,"HAIR: BRAIDS\nUSE THE ARROWS",10,Color.white,391,163,94,60,507,308);
            refreshChoices.Add(()=>hair.text="HAIR: "+new[]{"BRAIDS","BOB","PONYTAIL"}[draftHair]+"\nUSE THE ARROWS");
            Hotspot("Previous hairstyle",panel,29,130,31,41,507,308,()=>{draftHair=(draftHair+2)%3;RefreshCharacterPreview();});
            Hotspot("Next hairstyle",panel,261,130,33,41,507,308,()=>{draftHair=(draftHair+1)%3;RefreshCharacterPreview();});
            Hotspot("Outfit",panel,317,58,75,83,507,308,()=>{draftOutfit=(draftOutfit+1)%3;RefreshCharacterPreview();});
            Hotspot("Worn hat",panel,394,58,76,83,507,308,()=>{draftHat=(draftHat+1)%4;RefreshCharacterPreview();});
            Hotspot("Boat • cycle complete seated sprite",panel,317,144,75,90,507,308,()=>{draftBoat=(draftBoat+1)%3;RefreshCharacterPreview();});
            Hotspot("SELECT • save complete look",panel,115,245,130,43,507,308,SaveCharacter);
            Hotspot("BACK • cancel complete look",panel,252,245,130,43,507,308,()=>CloseModal());
            RefreshCharacterPreview();
        }
        void ShowProvidedPrizes()
        {
            PrizeRevealed=false;var panel=SuppliedPanel("prizes","ref_prizes",1018,677);
            bool ready=GameProgress.Now>=game.Progress.prizeReadyAt;
            var instruction=SourceText(panel,"",8,Color.white,55,292,220,12,463,308);
            for(int i=0;i<9;i++)
            {
                int cardIndex=i;
                float x=55+i%3*72.5f,y=77+i/3*72;
                var face=Source("ref_prize_back",panel,x,y,68,68,463,308);face.preserveAspect=true;
                if(!ready&&game.Progress.prizeLastReward>0&&game.Progress.prizeLastCard==i)
                {SetPrizeFace(face,game.Progress.prizeLastReward);PrizeRevealed=true;}
                var b=Hotspot("Prize card "+i,panel,x,y,68,68,463,308);b.interactable=ready;
                b.onClick.AddListener(()=>
                {
                    int reward=game.ClaimPrize(cardIndex);if(reward==0)return;
                    ready=false;
                    foreach(var button in panel.GetComponentsInChildren<Button>())if(button.name.StartsWith("Prize card"))button.interactable=false;
                    face.transform.SetAsLastSibling();face.enabled=true;
                    prizeReveal=StartCoroutine(FlipProvidedPrize(face,reward));
                });
            }
            refreshWindow=()=>
            {
                long remaining=Math.Max(0,game.Progress.prizeReadyAt-GameProgress.Now);
                if(!ready&&remaining==0&&prizeReveal==null){ShowProvidedPrizes();return;}
                instruction.text=ready?"PICK ONE CARD":"NEXT FREE PRIZE IN "+Remaining(remaining);
            };
            refreshWindow();
        }
        IEnumerator FlipProvidedPrize(Image face,int reward)
        {
            for(float t=0;t<.14f;t+=Time.unscaledDeltaTime){face.transform.localScale=new Vector3(1-t/.14f,1,1);yield return null;}
            face.transform.localScale=new Vector3(0,1,1);
            SetPrizeFace(face,reward);
            for(float t=0;t<.17f;t+=Time.unscaledDeltaTime){face.transform.localScale=new Vector3(t/.17f,1,1);yield return null;}
            face.transform.localScale=Vector3.one;PrizeRevealed=true;prizeReveal=null;Toast("PRIZE COLLECTED +$"+reward,Coral);
        }
        void SetPrizeFace(Image face,int reward)
        {
            face.sprite=Art("ref_prize_front");
            SourceText(face.transform,reward.ToString(),14,Ink,10,51,48,14,68,68);
        }
        void ShowProvidedResults()
        {
            var panel=SuppliedPanel("results","ref_results",1093,442);
            SourceText(panel,game.HaulCount.ToString(),17,Ink,267,62,65,24,497,201);
            SourceText(panel,"+$"+game.HaulValue,16,Hex("27852a"),266,87,67,24,497,201);
            SourceText(panel,game.Progress.deepest.ToString("0")+"m",15,Ink,267,111,65,23,497,201);
            // The reference's video ad control has no ad integration in this project.
            // Leave its artwork intact and cover it with the supplied lock indicator.
            var lockImage=UiKit.Image("Reward video unavailable",panel,game.config.lockSprite,Color.white,220,0,28,33);
            lockImage.raycastTarget=false;
            Hotspot("RETRY",panel,133,140,117,42,497,201,game.NextCast);
            Hotspot("HOME",panel,258,140,120,42,497,201,()=>{game.ReturnToHarbor();if(game.Progress.sessionCasts>=game.config.castsPerSession)ShowSession();});
        }
        void ShowProvidedPause()
        {
            var panel=SuppliedPanel("settings","ref_pause",464,679);
            Hotspot("Resume",panel,14,99,110,27,136,199,()=>CloseModal());
            Hotspot("Restart trip",panel,14,127,110,27,136,199,game.NewSession);
            Hotspot("Quit to harbor",panel,14,155,110,27,136,199,game.ReturnToHarbor);
            AudioToggle(panel,false,29.5f,63);
            AudioToggle(panel,true,71.5f,63);
        }

        void AudioToggle(RectTransform panel,bool music,float x,float y)
        {
            var socket=Part(music?"Music control":"Sound control",panel,x,y,35,36,136,199);
            var button=UiKit.Button(music?"Toggle music":"Toggle sound",socket,null,Color.white,0,6,socket.rect.width+17,socket.rect.height+17);
            button.image.preserveAspect=true;
            void RefreshIcon(){button.image.sprite=Art("ref_"+(music?"music":"sound")+((music?game.Progress.music:game.Progress.sound)?"_on":"_off"));}
            RefreshIcon();
            button.onClick.AddListener(()=>{
                if(music){game.Progress.music=!game.Progress.music;game.ApplyMusicPreference();}
                else game.Progress.sound=!game.Progress.sound;
                game.Save();RefreshIcon();
            });
        }
        void ShowProvidedUpgrades()
        {
            var panel=SuppliedPanel("upgrades","ref_upgrades",1010,678);
            int[] indices={0,1,3};
            for(int i=0;i<3;i++)
            {
                int upgrade=indices[i];float x=30+i*139;
                Source("ref_level_fill",panel,x+5,169,110,21,459,308);
                SourceText(panel,"LEVEL "+game.config.UpgradeLevel(game.Progress,upgrade),10,Color.white,x+5,169,110,21,459,308);
                if(i==1)
                {
                    Source("ref_description_fill",panel,x+4,195,113,29,459,308);
                    SourceText(panel,"+"+game.config.depthPerUpgrade.ToString("0")+"m Max Depth",12,Color.white,x+5,196,109,28,459,308);
                }
                var cost=SourceText(panel,"0",12,Color.white,x+45,252,50,25,459,308);
                cost.text="$"+game.config.UpgradeCost(game.Progress,upgrade);
                var b=Hotspot("Buy supplied upgrade "+upgrade,panel,x+8,231,112,48,459,308,()=>{if(game.BuyUpgrade(upgrade))ShowProvidedUpgrades();});
                b.interactable=game.Progress.coins>=game.config.UpgradeCost(game.Progress,upgrade)&&game.config.UpgradeLevel(game.Progress,upgrade)<game.config.UpgradeLimit(upgrade);
            }
        }
        void ShowProvidedMissions()
        {
            var panel=SuppliedPanel("missions","ref_quests",723,597);
            var quests=game.Progress.dailyQuests;if(quests.EnsureDay(GameProgress.Now))game.Save();
            var labels=new Text[DailyQuestProgress.Count];var bars=new Image[DailyQuestProgress.Count];
            var checks=new Image[DailyQuestProgress.Count];var claims=new Button[DailyQuestProgress.Count];var rewards=new Text[DailyQuestProgress.Count];
            var reset=SourceText(panel,"",6,Color.white,44,188,166,7,241,199);
            for(int i=0;i<DailyQuestProgress.Count;i++)
            {
                int index=i;
                checks[i]=Source("ref_quest_check",panel,19,48+i*38,22,22,241,199);
                labels[i]=SourceText(panel,"",8,Ink,45,46+i*38,112,15,241,199);
                var progress=Source("ref_quest_progress",panel,45,new[]{63f,100f,138f,173f}[i],112,7,241,199);
                progress.type=UnityEngine.UI.Image.Type.Filled;progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;progress.fillOrigin=0;
                bars[i]=progress;
                var area=Part("Daily quest reward "+i,panel,164,60+i*38,54,14,241,199);
                claims[i]=UiKit.Button("Claim daily quest "+i,area,Art("ref_button_green"),Color.white,0,0,area.rect.width,area.rect.height);
                var colors=claims[i].colors;colors.disabledColor=new Color(.65f,.72f,.68f,1);colors.highlightedColor=colors.selectedColor=Color.white;claims[i].colors=colors;
                rewards[i]=SourceText(area,"",7,Color.white,0,0,54,14,54,14);
                claims[i].onClick.AddListener(()=>game.ClaimDailyQuest(index));
            }
            refreshWindow=()=>
            {
                for(int i=0;i<DailyQuestProgress.Count;i++)
                {
                    int target=DailyQuestProgress.Targets[i],value=quests.Value(i);bool claimed=quests.claimed.Contains(i),complete=quests.Complete(i);
                    labels[i].text=DailyQuestProgress.Titles[i]+" ("+Mathf.Min(value,target)+" / "+target+")";
                    bars[i].fillAmount=Mathf.Clamp01((float)value/target);checks[i].enabled=complete;
                    claims[i].interactable=complete&&!claimed;
                    rewards[i].text=claimed?"CLAIMED":(complete?"CLAIM ":"")+"$"+DailyQuestProgress.Rewards[i];
                }
                reset.text="DAILY RESET IN "+Remaining(quests.SecondsToReset(GameProgress.Now))+" UTC";
            };
            refreshWindow();
        }
    }
}
