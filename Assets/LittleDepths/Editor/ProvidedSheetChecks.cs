using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    public static class ProvidedSheetChecks
    {
        static void Require(bool test,string message){if(!test)throw new Exception("SUPPLIED ART CHECK FAILED: "+message);}
        public static void Line(FishingGame game)
        {
            game.SendMessage("LateUpdate");
            var top=game.fishingLine.GetPosition(0);var eye=game.fishingLine.GetPosition(1);
            Require(Mathf.Abs(top.x-eye.x)<.001f,"line falls vertically");
            Require(Vector3.Distance(top,game.world.angler.rodTip.position)<.001f,"line starts at painted rod tip");
            Require(Vector3.Distance(eye,game.HookEye)<.001f,"line ends at hook eye");
        }
        public static void Validate(FishingGame game)
        {
            Line(game);
            foreach(string name in new[]{"ref_hud","ref_dock","ref_character","ref_upgrades","ref_prizes","ref_results","ref_pause","ref_quests","ref_mountains","ref_play","ref_button_blue","ref_button_green","ref_button_red","ref_icon_shop","ref_icon_medal","ref_icon_trophy","ref_icon_hook","ref_icon_boat"})
            {
                var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Assets/LittleDepths/Art/Provided/"+name+".png"));
                // A complete outline must have transparent room on all sides.
                for(int x=0;x<image.width;x++)Require(image.GetPixel(x,0).a<.01f&&image.GetPixel(x,image.height-1).a<.01f,name+" top/bottom padding");
                for(int y=0;y<image.height;y++)Require(image.GetPixel(0,y).a<.01f&&image.GetPixel(image.width-1,y).a<.01f,name+" left/right padding");
                if(new[]{"ref_hud","ref_dock","ref_character","ref_upgrades","ref_prizes","ref_results","ref_pause","ref_quests"}.Contains(name))
                    foreach(float x in new[]{.25f,.5f,.75f})foreach(float y in new[]{.25f,.5f,.75f})
                        Require(image.GetPixel((int)(image.width*x),(int)(image.height*y)).a>.99f,name+" interior is opaque, with no scenery bleeding through");
                UnityEngine.Object.DestroyImmediate(image);
            }
            Require(game.hud.characterButton.image.preserveAspect&&game.hud.journalButton.image.preserveAspect&&game.hud.hooksButton.image.preserveAspect&&game.hud.shopButton.image.preserveAspect&&game.hud.missionsButton.image.preserveAspect,"navigation keeps original button proportions");
            Debug.Log("UI_OUTLINES_OK: Complete panel and navigation outlines have transparent padding and keep their proportions.");
            foreach(var entry in new[]{("ref_character",1500),("ref_hud",2100),("ref_dock",1900),("ref_mountains",2100),("ref_icon_boat",350)})
            {
                string path="Assets/LittleDepths/Art/Provided/"+entry.Item1+".png";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Require(texture.width>=entry.Item2,entry.Item1+" retains native high resolution");
                Require(importer.maxTextureSize>=4096&&importer.textureCompression==TextureImporterCompression.Uncompressed,entry.Item1+" is not reduced or compressed on import");
                Require(entry.Item1.StartsWith("ref_icon_")?importer.mipmapEnabled&&importer.filterMode==FilterMode.Trilinear:!importer.mipmapEnabled,entry.Item1+" uses filtering appropriate to its display size");
            }
            Require(game.hud.hooksButton.image.sprite.name=="ref_icon_hook","Hooks navigation shows a hook rather than an accessory belt");
            Debug.Log("REMASTERED_ART_OK: Large native sprites, uncompressed imports, boat and hook icons are active.");
            var sprites=game.hud.GetComponentsInChildren<Image>(true).Where(i=>i.sprite).Select(i=>i.sprite).ToArray();
            Require(sprites.Any(s=>s.name=="ref_hud")&&sprites.Any(s=>s.name=="ref_dock"),"HUD and dock use supplied sheet crops");
            Require(!sprites.Any(s=>AssetDatabase.GetAssetPath(s).Contains("UIV2")),"no recreated UI texture is rendered");
            Require(game.world.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.sprite&&s.sprite.name=="ref_mountains"),"supplied mountains are in scene");
            Require(game.world.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.sprite&&s.sprite.name.StartsWith("ref_")&&s.name.Contains("tree"))>=9,"supplied trees are in scene");
            Debug.Log("SUPPLIED_SHEETS_OK: Source UI and scenery are active; rod-to-eye line is vertical.");
        }
        public static void Edges(string name)
        {
            var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("Logs/Previews/"+name+".png"));
            Color bank=UiKit.Hex("695d78");
            foreach(int x in new[]{0,2,image.width-3,image.width-1})foreach(float y in new[]{.20f,.35f,.45f})
            {
                Color pixel=image.GetPixel(x,(int)(image.height*y));
                Require(Vector3.Distance(new Vector3(pixel.r,pixel.g,pixel.b),new Vector3(bank.r,bank.g,bank.b))<.06f,"bank covers camera edge in "+name);
            }
            UnityEngine.Object.DestroyImmediate(image);Debug.Log("VIEWPORT_EDGES_OK: "+name);
        }
    }
}
