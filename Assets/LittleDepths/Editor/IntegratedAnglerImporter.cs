using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LittleDepths.Editor
{
    public static class IntegratedAnglerImporter
    {
        public static void Import(FishingConfig config)
        {
            string[] outfits={"Sailor","Raincoat","Ranger"},hairstyles={"Braids","Bob","Ponytail"};
            config.completeAnglers=new Sprite[108];config.completeRodTips=new Vector2[108];config.completeHullWidths=new float[108];
            for(int outfit=0;outfit<3;outfit++)for(int hair=0;hair<3;hair++)
            {
                string path=FishingSceneSetup.Root+"/Art/IntegratedAnglers/"+outfits[outfit]+"_"+hairstyles[hair]+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(!importer)throw new Exception("Missing complete character atlas: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=100;importer.isReadable=true;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.Tight;importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
                // Generated rows need not lie on exact fractional boundaries. Follow the
                // twelve complete painted subjects so no hull or rod is cut by a grid.
                var found=ReferenceSpriteImporter.FindRegions(texture).OrderByDescending(r=>r.width*r.height).Take(12).OrderByDescending(r=>r.center.y).ToArray();
                if(found.Length!=12)throw new Exception("Expected twelve separate whole character/boat drawings: "+path);
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var previous=provider.GetSpriteRects().ToDictionary(s=>s.name,s=>s.spriteID);
                var rects=new List<SpriteRect>();var regions=new List<SpriteRegion>();
                for(int boat=0;boat<3;boat++)
                {
                    var row=found.Skip(boat*4).Take(4).OrderBy(r=>r.center.x).ToArray();
                    for(int hat=0;hat<4;hat++)
                    {
                        var raw=row[hat];int left=Math.Max(0,raw.x-3),bottom=Math.Max(0,raw.y-3);
                        var rect=new RectInt(left,bottom,Math.Min(texture.width,raw.xMax+3)-left,Math.Min(texture.height,raw.yMax+3)-bottom);
                        var region=new SpriteRegion(pixels,texture.width,rect);BoatLandmarks(region,out var waterline,out float width);
                        string name=$"complete_{outfit}_{hair}_{hat}_{boat}";
                        rects.Add(new SpriteRect{name=name,rect=new Rect(rect.x,rect.y,rect.width,rect.height),pivot=waterline,alignment=SpriteAlignment.Custom,spriteID=previous.TryGetValue(name,out var id)?id:GUID.Generate()});regions.Add(region);
                        int index=FishingConfig.CompleteIndex(outfit,hair,hat,boat);
                        config.completeRodTips[index]=region.RodTip();config.completeHullWidths[index]=width/100f;
                    }
                }
                provider.SetSpriteRects(rects.ToArray());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
                var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                for(int i=0;i<rects.Count;i++){outlines.SetOutlines(rects[i].spriteID,new List<Vector2[]>{regions[i].Outline()});outlines.SetTessellationDetail(rects[i].spriteID,1);}
                provider.Apply();importer.SaveAndReimport();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
                for(int hat=0;hat<4;hat++)for(int boat=0;boat<3;boat++)config.completeAnglers[FishingConfig.CompleteIndex(outfit,hair,hat,boat)]=sprites[$"complete_{outfit}_{hair}_{hat}_{boat}"];
                Debug.Log("COMPLETE_ANGLER_ATLAS_OK: "+outfits[outfit]+" / "+hairstyles[hair]+" / 12 whole sprites; hats and boats are painted in.");
            }
            EditorUtility.SetDirty(config);
        }

        static void BoatLandmarks(SpriteRegion region,out Vector2 waterline,out float width)
        {
            int w=region.Rect.width,h=region.Rect.height,left=w,right=0;var bottomPixels=new List<int>();
            for(int y=0;y<h*.45f;y++)for(int x=0;x<w;x++)if(region.Contains(x,y)){left=Math.Min(left,x);right=Math.Max(right,x);}
            for(int x=(int)(w*.30f);x<w*.70f;x++)for(int y=0;y<h*.30f;y++)if(region.Contains(x,y)){bottomPixels.Add(y);break;}
            if(left>=right||bottomPixels.Count==0)throw new Exception("Cannot locate complete boat hull in "+region.Rect);
            bottomPixels.Sort();width=right-left+1;
            waterline=new Vector2((left+right+1)*.5f/w,(bottomPixels[bottomPixels.Count/2]+h*.035f)/h);
        }
    }
}
