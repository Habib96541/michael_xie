using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static LittleDepths.UiKit;

namespace LittleDepths
{
    public partial class FishingHUD
    {
        public Text bestCaption;
        Image previewAngler;
        public Sprite CharacterPreviewSprite => previewAngler?previewAngler.sprite:null;
        int draftOutfit,draftHair,draftHat,draftBoat;
        readonly List<Action> refreshChoices=new List<Action>();
        Coroutine prizeReveal;
        public bool PrizeRevealed { get; private set; }
        public bool HasCharacterDraft => modalType=="character";

        void Decorate(Transform parent,float width,float height)
        {}
        Image FishPicture(Transform parent,FishSpecies species,float x,float y,float w,float h,bool silhouette=false)
        {
            var im=UiKit.Image(species.displayName,parent,species.sprite,silhouette?new Color(.035f,.12f,.27f,1):Color.white,x,y,w,h);
            im.preserveAspect=true;
            if(!silhouette){var loop=im.gameObject.AddComponent<SpriteUiLoop>();loop.frames=species.swimFrames;loop.bob=true;}
            return im;
        }



        public void ShowJournal()
        {
            var p=game.Progress;var config=game.config;
            var panel=Open("journal","DISCOVER THE WORLD BELOW","FISH GALLERY",$"{p.discovered.Count} / {config.species.Count} discovered  •  Collection bonus: $150 for every 5 species",835,1210);
            for(int i=0;i<config.species.Count;i++)
            {
                var s=config.species[i];bool known=p.discovered.Contains(s.id);
                float x=(i%5-2)*210,y=109-i/5*230;
                var card=UiKit.Image(s.displayName,panel,Art("ref_collection_card"),Color.white,x,y,194,216);
                card.pixelsPerUnitMultiplier=card.sprite.rect.width/194;
                card.gameObject.AddComponent<UiMotion>().entrance=true;
                FishPicture(card.transform,s,0,28,150,103,!known);
                if(!known){Label(card.transform,"?",40,Hex("91a8c6"),0,27,100,65,true);UiKit.Image("Locked",card.transform,game.config.lockSprite,Color.white,65,71,23,28);}
                Label(card.transform,known?s.displayName:Mathf.CeilToInt(s.minDepth)+"m DEPTH",15,Ink,0,-43,170,31,true);
                var rarity=Label(card.transform,known?s.rarity.ToUpperInvariant():"UNDISCOVERED",12,known?Hex("27757f"):Hex("62738e"),0,-72,170,23,true);
                if(known&&s.rarity=="Legendary")rarity.color=Hex("a76700");
                if(known&&s.rarity=="Legendary")for(int j=0;j<3;j++){var star=UiKit.Image("Legendary sparkle",card.transform,Art("sparkle"),Coral,-69+j*66,67-j%2*12,15,15);star.gameObject.AddComponent<UiMotion>().pulse=true;}
            }
            Label(panel,"AQUARIUM  ·  $"+config.IncomePerMinute(p)+" / MIN",22,Coral,0,-290,900,40,true);
            Label(panel,"Discovered fish earn $1/min each. Income upgrades add $5/min.\nEarnings collect automatically, including up to 8 hours away.",16,Muted,0,-346,1000,58);
        }



        public void ShowHooks()
        {
            var p=game.Progress;var c=game.config;
            var panel=Open("hooks","YOUR TACKLE BOX","HOOK COLLECTION","Choose your favorite. All hooks have the same fishing performance.",780,990);
            for(int i=0;i<c.hookColors.Length;i++)
            {
                int index=i;bool unlocked=p.unlockedHooks.Contains(i),selected=p.selectedHook==i;
                float x=(i%3-1)*245,y=97-i/3*213;
                if(selected)UiKit.Image("Equipped card highlight",panel,rounded,Hex("8ce623"),x,y,218,205);
                var b=UiKit.Button(hookNames[i],panel,Art("ref_collection_card"),Color.white,x,y,210,197);
                b.image.pixelsPerUnitMultiplier=b.image.sprite.rect.width/210;
                UiKit.Image("Hook sprite",b.transform,c.hookSprite,unlocked?c.hookColors[i]:Hex("8594ab"),0,28,90,90);
                if(!unlocked)UiKit.Image("Hook locked",b.transform,c.lockSprite,Color.white,67,61,25,29);
                Label(b.transform,unlocked?hookNames[i].ToUpperInvariant():"LOCKED",15,Ink,0,-35,186,29,true);
                var status=UiKit.Image("Hook status",b.transform,Art("ref_button_green"),selected?Color.white:new Color(.45f,.62f,.81f,1),0,-68,158,29);
                status.pixelsPerUnitMultiplier=status.sprite.rect.width/158;
                Label(status.transform,selected?"EQUIPPED":unlocked?"SELECT":"UNDISCOVERED",11,Color.white,0,0,146,26,true);
                b.onClick.AddListener(()=>{if(game.SelectHook(index))ShowHooks();});
            }
            var buy=ActionButton(panel,p.unlockedHooks.Count==c.hookColors.Length?"ALL HOOKS UNLOCKED":"UNLOCK RANDOM  ·  $250",0,-304,590,70,()=>{if(game.UnlockHook())ShowHooks();});
            buy.interactable=p.coins>=250&&p.unlockedHooks.Count<c.hookColors.Length;
        }






        void RefreshCharacterPreview()
        {
            previewAngler.sprite=game.config.CompleteSprite(draftOutfit,draftHair,draftHat,draftBoat);
            var size=AppearanceFit.Size(previewAngler.sprite);var area=((RectTransform)previewAngler.transform.parent).rect.size;float scale=Mathf.Min((area.x-10)/size.x,(area.y-10)/size.y);
            previewAngler.rectTransform.anchoredPosition=Vector2.zero;
            previewAngler.rectTransform.sizeDelta=size*scale;
            foreach(var update in refreshChoices)update();
        }
        Image AppearanceImage(string name,Transform parent)
        {
            var rect=UiKit.Rect(name,parent,0,0,1,1);var image=rect.gameObject.AddComponent<SpritePartImage>();image.raycastTarget=false;return image;
        }
        public void SetCharacterDraft(int outfit,int hair,int hat,int boat)
        {
            draftOutfit=Mathf.Clamp(outfit,0,2);draftHair=Mathf.Clamp(hair,0,2);draftHat=Mathf.Clamp(hat,0,3);draftBoat=Mathf.Clamp(boat,0,2);
            if(HasCharacterDraft)RefreshCharacterPreview();
        }
        public void SaveCharacter()
        {
            var p=game.Progress;p.outfit=draftOutfit;p.hairstyle=draftHair;p.hat=draftHat;p.boatStyle=draftBoat;
            game.world.angler.Apply(p);game.Save();CloseModal();Toast("LOOKING GOOD!",Coral);
        }
    }
}
