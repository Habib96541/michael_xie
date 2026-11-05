using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    public static class FishingActionChecks
    {
        static float initialCursor;
        static void Require(bool test,string message){if(!test)throw new Exception("FISHING HUD CHECK FAILED: "+message);}
        public static void Artwork()
        {
            foreach(var entry in new[]{("ref_pines",1500),("ref_oak",850),("ref_gold_tree",850),("ref_shore_left",1400),("ref_shore_right",1900),("ref_cast_meter",2000),("ref_dive_panel",2000),("ref_depth_badge",900),("ref_reel_action",1300),("ref_cast_cursor",300),("ref_cast_target",800)})
            {
                string path="Assets/LittleDepths/Art/Provided/"+entry.Item1+".png";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Require(texture&&texture.width>=entry.Item2,entry.Item1+" has standalone native resolution");
                Require(importer.maxTextureSize>=4096&&importer.textureCompression==TextureImporterCompression.Uncompressed,entry.Item1+" is not compressed/downscaled");
                var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));
                for(int x=0;x<image.width;x++)Require(image.GetPixel(x,0).a<.01f&&image.GetPixel(x,image.height-1).a<.01f,entry.Item1+" has vertical padding");
                for(int y=0;y<image.height;y++)Require(image.GetPixel(0,y).a<.01f&&image.GetPixel(image.width-1,y).a<.01f,entry.Item1+" has horizontal padding");
                UnityEngine.Object.DestroyImmediate(image);
            }
            Debug.Log("SHORE_AND_ACTION_ART_OK: Native large foreground and action sprites retain complete transparent outlines.");
        }
        public static void BeginTiming(FishingGame game){initialCursor=game.hud.timingNeedle.anchoredPosition.x;}
        public static void Timing(FishingGame game)
        {
            game.hud.Refresh();var hud=game.hud;
            Require(hud.timingBar.activeInHierarchy&&!hud.activeRoot.activeInHierarchy,"timing and dive HUDs show in the right phases");
            Require(hud.timingBar.GetComponent<Image>().sprite.name=="ref_cast_meter"&&hud.timingNeedle.GetComponent<Image>().sprite.name=="ref_cast_cursor","timing uses generated frame and bobber sprites");
            float width=hud.timingLane.rect.width;var target=hud.castTarget.rectTransform;
            Require(Mathf.Abs(target.anchoredPosition.x-target.rect.width/2-(-width/2+width*FishingGame.PerfectCastThreshold))<.05f,"gold zone begins at the actual perfect-cast threshold");
            Require(Mathf.Abs(target.anchoredPosition.x+target.rect.width/2-width/2)<.05f,"gold zone ends at maximum power");
            Require(Mathf.Abs(hud.timingNeedle.anchoredPosition.x-width*(game.TimingPower-.5f))<.05f,"bobber position reflects actual cast power");
            Require(Mathf.Abs(initialCursor-hud.timingNeedle.anchoredPosition.x)>width*.01f,"bobber really animates in Play mode");
            Require(!hud.timingBar.GetComponentsInChildren<Image>().Any(i=>i.name=="Power track"||i.name=="Power marker"),"old plain meter graphics are absent");
            Debug.Log("CAST_METER_OK: Animated bobber and golden target match gameplay power.");
        }
        public static void Dive(FishingGame game,bool descending)
        {
            game.hud.Refresh();var hud=game.hud;
            Require(hud.activeRoot.activeInHierarchy&&!hud.castRoot.activeInHierarchy,"dive UI replaces cast controls");
            Require(hud.activeRoot.GetComponentsInChildren<Image>().Any(i=>i.sprite&&i.sprite.name=="ref_dive_side"),"dive uses the matching portrait haul frame");
            Require(hud.depthLabel.transform.parent.GetComponent<Image>().sprite.name=="ref_depth_badge","depth uses its own generated instrument");
            Require(hud.reelButton.gameObject.activeInHierarchy&&hud.reelButton.interactable==descending,"reel control starts retrieval during descent and disables while reeling");
            Require(hud.tripValue.text=="$"+game.HaulValue.ToString("N0")&&hud.diveCatchLabel.text.Contains(game.HaulCount+" / "+game.config.Capacity(game.Progress)),"haul and capacity values are live");
            Debug.Log("DIVE_HUD_OK: Generated depth, haul and reel graphics display real values; descending="+descending);
        }
    }
}
