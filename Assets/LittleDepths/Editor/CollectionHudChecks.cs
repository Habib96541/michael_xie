using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    public static class CollectionHudChecks
    {
        static void Require(bool test,string message){if(!test)throw new Exception("COLLECTION HUD CHECK FAILED: "+message);}
        public static void AnimatedReel(FishingGame game)
        {
            int wallet=game.Progress.coins,animations=game.world.angler.ManualReelAnimations;
            bool manual=game.Phase==FishingPhase.Descending;var position=game.hook.position;var camera=game.gameCamera.transform.position;
            game.hud.reelButton.onClick.Invoke();
            Require(game.Phase==FishingPhase.Ascending&&game.hook.position==position,"reel starts a visible return without teleporting");
            Require(game.gameCamera.transform.position==camera&&game.Progress.coins==wallet,"reel does not snap the camera or pay early");
            Require(game.world.angler.ManualReelAnimations==animations+(manual?1:0),"only the first manual reel starts a manual rod-pull animation");
            var tick=typeof(FishingGame).GetMethod("TickFishing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;game.IsFishing&&i<3000;i++)tick.Invoke(game,new object[]{1f/60});
            Require(game.Phase==FishingPhase.Results&&Vector3.Distance(game.HookEye,game.world.angler.rodTip.position)<.001f,"the hook reaches the player before results");
            Require(game.Progress.coins==wallet+game.HaulValue+game.LastCollectionBonus,"animated retrieval pays the completed haul exactly");
            wallet=game.Progress.coins;game.ReelIn();game.Land();
            Require(game.Progress.coins==wallet,"double click or a second landing cannot pay twice");
            Debug.Log("ANIMATED_REEL_OK: Continuous return to the player, rod animation and exactly-once sale.");
        }
        public static void Collection(FishingGame game,bool hooks)
        {
            var images=game.hud.modalLayer.GetComponentsInChildren<Image>();
            Require(images.Count(i=>i.sprite&&i.sprite.name=="ref_collection_panel")==1,"complete matching collection background is active");
            Require(images.Count(i=>i.sprite&&i.sprite.name=="ref_collection_card")== (hooks?6:10),"every item uses a complete equipment-style card");
            Require(!images.Any(i=>i.sprite&&i.sprite.name.StartsWith("ref_frame_")),"thin cropped legacy frames are absent");
            WindowAndDailyChecks.Cross(game);
        }
        public static void Audio(FishingGame game)
        {
            var sound=game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="Toggle sound");
            var music=game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="Toggle music");
            bool initialSound=game.Progress.sound,initialMusic=game.Progress.music;
            sound.onClick.Invoke();music.onClick.Invoke();
            Require(game.Progress.sound!=initialSound&&game.Progress.music!=initialMusic,"both audio channels toggle independently");
            Require(sound.image.sprite.name=="ref_sound_"+(game.Progress.sound?"on":"off"),"speaker icon reflects mute");
            Require(music.image.sprite.name=="ref_music_"+(game.Progress.music?"on":"off"),"music icon reflects mute");
            Require(game.musicSource&&!game.musicSource.isPlaying,"music actually pauses");
            var saved=GameProgress.Load(game.SaveSlot);
            Require(saved.sound==game.Progress.sound&&saved.music==game.Progress.music,"audio preferences persist");
            Require(!game.hud.toastText.gameObject.activeSelf,"audio toggles do not show ON/OFF toast text");
            FishingSmokeTests.Capture("33-PauseMuted",1920,1080);
            sound.onClick.Invoke();music.onClick.Invoke();
            Require(game.Progress.sound==initialSound&&game.Progress.music==initialMusic&&game.musicSource.isPlaying,"both toggles restore playback and original preferences");
            Debug.Log("AUDIO_ICONS_OK: Independent speaker/music icons, real playback and saved preferences, no text toast.");
        }
        public static void Sides(FishingGame game)
        {
            var trip=game.hud.tripValue.transform.parent as RectTransform;
            Require(trip.anchorMin==new Vector2(1,.5f)&&trip.rect.width<300,"haul and reel panel lives on the right bank");
            var meter=(RectTransform)game.hud.timingBar.transform;
            Require(meter.anchorMin==new Vector2(0,.5f)&&meter.rect.width<=320,"cast controls live on the left bank");
            Require(game.hud.depthLabel.transform.parent.GetComponent<RectTransform>().anchorMin==new Vector2(0,.5f),"depth instrument lives on the left bank");
        }
        public static void DeepSeams(FishingGame game)
        {
            foreach(string side in new[]{"Left","Right"})for(int i=0;i<6;i++)
            {
                var upper=GameObject.Find(side+" bank "+i).GetComponent<SpriteRenderer>();
                var lower=GameObject.Find(side+" bank "+(i+1)).GetComponent<SpriteRenderer>();
                Require(Mathf.Abs(upper.bounds.min.y-lower.bounds.max.y)<.001f,"bank tile bounds meet exactly");
            }
            foreach(string name in new[]{"bank_left","bank_right"})
            {
                var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Assets/LittleDepths/Art/"+name+".png"));
                for(int x=0;x<image.width;x++)Require(image.GetPixel(x,0)==image.GetPixel(x,image.height-1),"all bank layers repeat at their seam");
                UnityEngine.Object.DestroyImmediate(image);
            }
            var camera=game.gameCamera;var position=camera.transform.position;
            camera.transform.position=new Vector3(0,-14.93f,-10);FishingSmokeTests.Capture("31-FirstBankSeam",1920,1080);
            camera.transform.position=new Vector3(0,-30.93f,-10);FishingSmokeTests.Capture("32-DeepBankSeam",1920,1080);
            camera.transform.position=position;
            Debug.Log("UNDERWATER_SEAMS_OK: Pixel-periodic bank layers and contiguous world-space tiles at depth.");
        }
    }
}
