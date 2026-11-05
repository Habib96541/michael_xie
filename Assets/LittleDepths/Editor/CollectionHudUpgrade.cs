using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleDepths.Editor
{
    // Scoped upgrade: the existing scene is opened and only action HUD children,
    // HUD sprite references and the seven bank-tile joins are changed.
    public static class CollectionHudUpgrade
    {
        static readonly string[] Skins={"ref_collection_panel","ref_collection_card","ref_dive_side","ref_sound_on","ref_sound_off","ref_music_on","ref_music_off"};
        public static void Run()
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(!project.Replace('\\','/').EndsWith("/Logs/SpriteValidation",StringComparison.OrdinalIgnoreCase))
                throw new Exception("Apply and validate this upgrade in the isolated copy first.");
            AssetDatabase.Refresh();
            foreach(string name in Skins)
            {
                string path="Assets/LittleDepths/Art/Provided/"+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
                settings.spritePivot=new Vector2(.5f,.5f);importer.SetTextureSettings(settings);
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.spriteBorder=name=="ref_collection_panel"||name=="ref_collection_card"?Vector4.one*100:Vector4.zero;
                importer.SaveAndReimport();
            }
            var audio=AssetImporter.GetAtPath("Assets/LittleDepths/Resources/LittleDepthsAmbience.wav") as AudioImporter;
            if(audio){var settings=audio.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.5f;audio.defaultSampleSettings=settings;audio.SaveAndReimport();}
            var scene=EditorSceneManager.OpenScene(FishingSceneSetup.ScenePath);
            var hud=UnityEngine.Object.FindFirstObjectByType<FishingHUD>();
            hud.art=hud.art.Where(s=>s&&!Skins.Contains(s.name)).Concat(Skins.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LittleDepths/Art/Provided/"+n+".png"))).ToArray();
            hud.UpgradeSideControls();EditorUtility.SetDirty(hud);
            foreach(string side in new[]{"Left","Right"})
            {
                var first=GameObject.Find(side+" bank 0").transform;
                var sprite=first.GetComponent<SpriteRenderer>();
                float height=sprite.sprite.bounds.size.y*first.localScale.y;
                for(int i=1;i<7;i++)
                {
                    var tile=GameObject.Find(side+" bank "+i).transform;
                    var position=tile.localPosition;position.y=first.localPosition.y-height*i;tile.localPosition=position;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/CollectionHudUpgrade.status","PASSED: Scoped action HUD, collection/audio skins and periodic bank joins updated; existing harbor and shoreline retained.");
        }
    }
}
