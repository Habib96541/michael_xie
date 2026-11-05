using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    public static class CleanAppearanceChecks
    {
        static void Require(bool value,string message){if(!value)throw new Exception("APPEARANCE CHECK FAILED: "+message);}
        public static void ValidateAll(FishingGame game)
        {
            var c=game.config;var rig=game.world.angler;int count=0;var unique=new HashSet<Sprite>();
            Require(c.completeAnglers.Length==108&&c.completeAnglers.All(s=>s),"108 complete sprite assets exist");
            Require(rig.GetComponentsInChildren<SpriteRenderer>().Length==1,"one whole-sprite renderer, no separate hats, bodies or boats");
            for(int outfit=0;outfit<3;outfit++)for(int hair=0;hair<3;hair++)for(int boat=0;boat<3;boat++)for(int hat=0;hat<4;hat++)
            {
                var p=new GameProgress{outfit=outfit,hairstyle=hair,boatStyle=boat,hat=hat};rig.Apply(p);
                Require(rig.visual.sprite==c.CompleteSprite(outfit,hair,hat,boat),"selection swaps the complete illustration");
                Require(unique.Add(rig.visual.sprite),"each combination has its own distinct sprite");
                Require(rig.visual.color==Color.white,"original painted colors");
                Require(Mathf.Abs(rig.visual.transform.localScale.x-rig.visual.transform.localScale.y)<.0001f,"complete artwork keeps its proportions");
                Require(rig.visual.bounds.max.y<c.surfaceY+2.4f,"complete rod and headwear stay below the top HUD");
                Vector3 tip=rig.visual.transform.TransformPoint(AppearanceFit.Point(rig.visual.sprite,c.completeRodTips[rig.LookIndex]));
                Require(Vector3.Distance(tip,rig.rodTip.position)<.001f,"fishing line follows the painted rod");
                count++;
            }
            rig.Apply(game.Progress);
            Debug.Log("COMPLETE_LOOKS_OK: "+count+" distinct whole girl / worn hat / occupied boat sprites, rendered as one piece.");
        }

        public static void CaptureSheet(FishingGame game)
        {
            for(int boat=0;boat<3;boat++)CaptureBoatSheet(game,boat);
        }
        static void CaptureBoatSheet(FishingGame game,int boat)
        {
            var root=new GameObject("Temporary appearance contact sheet");
            string[] outfits={"SAILOR","RAINCOAT","RANGER"},hairNames={"BRAIDS","BOB","PONYTAIL"},hats={"NONE","VISOR","BUCKET","BANDANA"};
            for(int row=0;row<9;row++)for(int col=0;col<4;col++)
            {
                int outfit=row/3,hair=row%3;
                var go=UnityEngine.Object.Instantiate(game.world.angler.gameObject,root.transform);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                var rig=go.GetComponent<AnglerRig>();rig.animator.enabled=false;
                rig.Configure(game.config,new GameProgress{outfit=outfit,hairstyle=hair,hat=col,boatStyle=boat});
                rig.visual.transform.localScale=Vector3.one*(AppearanceFit.BoatWidth/game.config.completeHullWidths[rig.LookIndex]);
                rig.visual.transform.parent.localScale=Vector3.one;rig.visual.transform.parent.localRotation=Quaternion.identity;
                go.transform.position=new Vector3(-8.25f+col*5.5f,16-row*4.25f,0);go.transform.rotation=Quaternion.identity;
                var label=new GameObject("Appearance label",typeof(TextMesh));label.transform.SetParent(root.transform);label.layer=30;
                label.transform.position=new Vector3(go.transform.position.x,go.transform.position.y-1.65f,-.1f);
                var text=label.GetComponent<TextMesh>();text.text=$"{outfits[outfit]} / {hairNames[hair]}\n{hats[col]} / BOAT {boat+1}";
                text.font=game.hud.bodyFont;text.fontSize=36;text.characterSize=.04f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;
                label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            }
            var camera=game.gameCamera;var position=camera.transform.position;float size=camera.orthographicSize;
            int mask=camera.cullingMask;Color background=camera.backgroundColor;var oldTarget=camera.targetTexture;
            camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=20.6f;camera.cullingMask=1<<30;camera.backgroundColor=UiKit.Hex("153267");
            var target=new RenderTexture(2400,4000,24);camera.targetTexture=target;var active=RenderTexture.active;
            camera.Render();RenderTexture.active=target;var result=new Texture2D(2400,4000,TextureFormat.RGB24,false);
            result.ReadPixels(new Rect(0,0,2400,4000),0,0);result.Apply();Directory.CreateDirectory("Logs/Previews");
            File.WriteAllBytes($"Logs/Previews/{18+boat}-CompleteBoat{boat+1}.png",result.EncodeToPNG());
            camera.targetTexture=oldTarget;camera.transform.position=position;camera.orthographicSize=size;camera.cullingMask=mask;camera.backgroundColor=background;
            RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(root);
        }

        public static void ValidatePreview(FishingGame game)
        {
            Require(game.hud.modalLayer.GetComponentsInChildren<SpritePartImage>().Length==1,"creator has one complete illustration image");
            for(int outfit=0;outfit<3;outfit++)for(int hair=0;hair<3;hair++)for(int hat=0;hat<4;hat++)for(int boat=0;boat<3;boat++)
            {
                game.hud.SetCharacterDraft(outfit,hair,hat,boat);
                Require(game.hud.CharacterPreviewSprite==game.config.CompleteSprite(outfit,hair,hat,boat),"creator swaps the whole sprite for every option");
            }
            var p=game.Progress;game.hud.SetCharacterDraft(p.outfit,p.hairstyle,p.hat,p.boatStyle);
            Debug.Log("COMPLETE_PREVIEWS_OK: All 108 creator choices select their matching whole sprites.");
        }

        public static void CheckMouths(FishingGame game)
        {
            foreach(var fish in game.Caught)
            {
                if(fish.isTrash)continue;
                Require(Vector3.Distance(fish.MouthWorld,fish.HookPointWorld)<.002f,"fish mouth remains on hook after swim animation frame changes");
                Require(Vector3.Distance(fish.visual.transform.position,fish.HookPointWorld)>.06f,"fish body hangs below the hook instead of clipping through it");
            }
            Debug.Log("FISH_MOUTH_ANCHOR_OK: All caught fish attach at their mouths.");
        }
        public static void CaptureMouths(FishingGame game)
        {
            var camera=game.gameCamera;var position=camera.transform.position;float size=camera.orthographicSize;
            var canvas=game.hud.GetComponent<Canvas>();bool enabled=canvas.enabled;canvas.enabled=false;
            camera.orthographicSize=1.35f;camera.transform.position=game.hook.position+new Vector3(0,-.25f,-10);
            FishingSmokeTests.Capture("15-MouthHookAttachment");camera.transform.position=position;camera.orthographicSize=size;canvas.enabled=enabled;
        }

        public static void CheckAllMouthFrames(FishingGame game)
        {
            var hook=new GameObject("Temporary mouth attachment check",typeof(SpriteRenderer));hook.layer=30;
            var renderer=hook.GetComponent<SpriteRenderer>();renderer.sprite=game.config.hookSprite;renderer.sortingOrder=30;
            renderer.color=game.hook.GetComponent<SpriteRenderer>().color;hook.transform.localScale=game.hook.lossyScale;
            int frames=0;
            foreach(var species in game.config.species)
            {
                var go=new GameObject("Temporary "+species.id);var fish=go.AddComponent<FishActor>();
                fish.Initialize(species,false,species.sprite,Vector2.zero,-1);if(fish.animator)fish.animator.enabled=false;
                foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                fish.Attach(hook.transform,0,game.config.hookMouthPoint);
                foreach(var sprite in species.swimFrames)
                {
                    fish.visual.sprite=sprite;fish.SendMessage("LateUpdate");
                    Require(Vector3.Distance(fish.MouthWorld,fish.HookPointWorld)<.002f,sprite.name+" mouth follows hook");
                    frames++;
                }
                if(species.id=="clown")
                {
                    var camera=game.gameCamera;var position=camera.transform.position;float size=camera.orthographicSize;
                    int mask=camera.cullingMask;Color background=camera.backgroundColor;
                    var canvas=game.hud.GetComponent<Canvas>();bool enabled=canvas.enabled;canvas.enabled=false;
                    camera.transform.position=new Vector3(0,-.2f,-10);camera.orthographicSize=.9f;
                    camera.cullingMask=1<<30;camera.backgroundColor=UiKit.Hex("3697B8");
                    FishingSmokeTests.Capture("17-SingleFishMouthAttachment");
                    camera.transform.position=position;camera.orthographicSize=size;camera.cullingMask=mask;camera.backgroundColor=background;canvas.enabled=enabled;
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            UnityEngine.Object.DestroyImmediate(hook);
            Debug.Log("ALL_FISH_MOUTH_FRAMES_OK: "+frames+" frames across all species stay attached at the mouth.");
        }
    }
}
