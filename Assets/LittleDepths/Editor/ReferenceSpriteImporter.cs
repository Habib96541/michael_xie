using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LittleDepths.Editor
{
    public static class ReferenceSpriteImporter
    {
        const string Root=FishingSceneSetup.Root;
        static readonly string[] FishNames={"minnow","perch","clown","bluefin","sunfish","jelly","moonfish","ruby","golden","abyss"};
        public static void Import(FishingConfig config)
        {
            string fishPath=Root+"/Art/SpriteAtlases/FishSwimAtlas.png", kitPath=Root+"/Art/SpriteAtlases/AnglerKitAtlas.png";
            Slice(fishPath,4,10,true);Slice(kitPath,4,4,false);
            var fish=AssetDatabase.LoadAllAssetsAtPath(fishPath).OfType<Sprite>().ToDictionary(s=>s.name);
            var kit=AssetDatabase.LoadAllAssetsAtPath(kitPath).OfType<Sprite>().ToDictionary(s=>s.name);
            System.IO.Directory.CreateDirectory(Root+"/Animations");AssetDatabase.Refresh();
            foreach(var species in config.species)
            {
                species.swimFrames=Enumerable.Range(0,4).Select(i=>fish["fish_"+species.id+"_"+i]).ToArray();
                species.sprite=species.swimFrames[0];
                species.mouthPoints=species.swimFrames.Select(s=>FishMouth(s,species.id=="jelly")).ToArray();
                var clip=new AnimationClip {name=species.id+" • swimming",frameRate=8};
                var keys=new ObjectReferenceKeyframe[5];
                for(int i=0;i<5;i++)keys[i]=new ObjectReferenceKeyframe{time=i/8f,value=species.swimFrames[i%4]};
                AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
                Loop(clip);species.swimController=Controller(species.id,SaveClip(clip,"Fish_"+species.id),"Swim");
            }
            config.outfits=Enumerable.Range(0,3).Select(i=>kit["outfit_"+i]).ToArray();
            config.boats=Enumerable.Range(0,3).Select(i=>kit["boat_"+i]).ToArray();
            config.hats=Enumerable.Range(0,3).Select(i=>kit["hat_"+i]).ToArray();
            config.hairstyles=Enumerable.Range(0,3).Select(i=>kit["hair_"+i]).ToArray();
            config.lockSprite=kit["lock"];
            IntegratedAnglerImporter.Import(config);
            config.anglerController=CreateAnglerController();EditorUtility.SetDirty(config);
        }

        static void Slice(string path,int columns,int rows,bool swimming)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=100;importer.isReadable=true;importer.alphaIsTransparency=true;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var regions=FindRegions(texture);
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects().ToDictionary(s=>s.name,s=>s.spriteID);
            var output=new List<SpriteRect>();var used=new HashSet<int>();
            float[] fishY={.052f,.139f,.228f,.323f,.422f,.532f,.637f,.735f,.834f,.938f};
            float[] kitY={.177f,.472f,.670f,.884f};
            for(int row=0;row<rows;row++)
            {
                var rowRegions=new List<RectInt>();
                for(int col=0;col<columns;col++)
                {
                    float targetX=(swimming?(new[]{.163f,.387f,.612f,.842f})[col]:(col+.5f)/columns)*texture.width;
                    float targetY=(1-(swimming?fishY[row]:kitY[row]))*texture.height;
                    int best=-1;float distance=float.MaxValue;
                    for(int i=0;i<regions.Count;i++)
                    {
                        if(used.Contains(i))continue;
                        var rect=regions[i];float d=(new Vector2(rect.center.x-targetX,rect.center.y-targetY)).sqrMagnitude;
                        if(d<distance){distance=d;best=i;}
                    }
                    if(best<0)throw new Exception("Not enough separate sprites in "+path);
                    used.Add(best);rowRegions.Add(regions[best]);
                }
                int width=rowRegions.Max(r=>r.width)+8,height=rowRegions.Max(r=>r.height)+8;
                for(int col=0;col<columns;col++)
                {
                    string name=swimming?"fish_"+FishNames[row]+"_"+col:KitName(row,col);
                    var r=rowRegions[col];
                    var rect=swimming?new Rect(Mathf.Clamp(r.center.x-width/2f,0,texture.width-width),Mathf.Clamp(r.center.y-height/2f,0,texture.height-height),width,height):new Rect(r.x-2,r.y-2,r.width+4,r.height+4);
                    output.Add(new SpriteRect{name=name,rect=rect,alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=old.TryGetValue(name,out var id)?id:GUID.Generate()});
                }
            }
            provider.SetSpriteRects(output.ToArray());
            var meshSettings=new TextureImporterSettings();importer.ReadTextureSettings(meshSettings);meshSettings.spriteMeshType=SpriteMeshType.Tight;importer.SetTextureSettings(meshSettings);
            var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();var allPixels=texture.GetPixels32();
            foreach(var spriteRect in output)
            {
                var r=spriteRect.rect;var region=new SpriteRegion(allPixels,texture.width,new RectInt(Mathf.RoundToInt(r.x),Mathf.RoundToInt(r.y),Mathf.RoundToInt(r.width),Mathf.RoundToInt(r.height)));
                outlines.SetOutlines(spriteRect.spriteID,new List<Vector2[]>{region.Outline()});outlines.SetTessellationDetail(spriteRect.spriteID,1);
            }
            var nameProvider=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(output.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
            provider.Apply();importer.SaveAndReimport();
            Debug.Log("SPRITE_ATLAS_OK: "+path+" / "+output.Count+" alpha-trimmed sprites");
        }
        static string KitName(int row,int col)=>row==0?(col<3?"outfit_"+col:"rod"):row==1?(col<3?"boat_"+col:"arm"):row==2?(col<3?"hat_"+col:"kit_hook"):(col<3?"hair_"+col:"lock");
        static Vector2 FishMouth(Sprite sprite,bool jelly)
        {
            if(jelly)return new Vector2(.50f,.51f);
            var texture=sprite.texture;var pixels=texture.GetPixels32();var r=sprite.rect;
            int left=Mathf.RoundToInt(r.x),bottom=Mathf.RoundToInt(r.y),w=Mathf.RoundToInt(r.width),h=Mathf.RoundToInt(r.height);
            for(int x=0;x<w/3;x++)
            {
                float sum=0;int count=0;
                for(int y=(int)(h*.32f);y<h*.66f;y++)if(pixels[(bottom+y)*texture.width+left+x].a>140){sum+=y;count++;}
                if(count>0)return new Vector2((x+1.5f)/w,(sum/count+.5f)/h);
            }
            throw new Exception("Cannot find fish mouth: "+sprite.name);
        }
        // Alpha-region slicing avoids clipping characters when generated rows are not perfectly uniform.
        internal static List<RectInt> FindRegions(Texture2D texture)
        {
            int w=texture.width,h=texture.height;var pixels=texture.GetPixels32();var seen=new bool[pixels.Length];var output=new List<RectInt>();var queue=new Queue<int>();
            for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start]||pixels[start].a<45)continue;
                queue.Enqueue(start);seen[start]=true;int left=w,right=0,bottom=h,top=0,count=0;
                while(queue.Count>0)
                {
                    int n=queue.Dequeue(),x=n%w,y=n/w;left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);count++;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int k=ny*w+nx;
                        if(!seen[k]&&pixels[k].a>=45){seen[k]=true;queue.Enqueue(k);}
                    }
                }
                if(count>550&&right-left>25&&top-bottom>25)output.Add(new RectInt(left,bottom,right-left+1,top-bottom+1));
            }
            return output;
        }

        static AnimationClip SaveClip(AnimationClip clip,string name)
        {
            string path=Root+"/Animations/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static void Loop(AnimationClip clip){var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);}
        static AnimatorController Controller(string name,AnimationClip clip,string stateName)
        {
            string path=Root+"/Animations/"+name+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.FirstOrDefault(s=>s.state.name==stateName).state??machine.AddState(stateName);
            state.motion=clip;machine.defaultState=state;EditorUtility.SetDirty(controller);return controller;
        }
        static RuntimeAnimatorController CreateAnglerController()
        {
            var idle=new AnimationClip{name="Idle",frameRate=30};
            Curve(idle,"Angler","localEulerAnglesRaw.z",new[]{0f,.8f,1.6f},new[]{0f,.35f,0f});Loop(idle);
            var controller=Controller("Angler",SaveClip(idle,"Angler_Idle"),"Idle");
            string[] names={"Cast","Reel","Celebrate"};
            for(int i=0;i<3;i++)
            {
                var clip=new AnimationClip{name=names[i],frameRate=30};
                if(i==0)Curve(clip,"Angler","localEulerAnglesRaw.z",new[]{0f,.2f,.42f,.7f},new[]{0f,-2.2f,2f,0f});
                if(i==1){Curve(clip,"Angler","localEulerAnglesRaw.z",new[]{0f,.3f,.6f},new[]{-.8f,-2f,-.8f});Loop(clip);}
                if(i==2)Curve(clip,"Angler","localEulerAnglesRaw.z",new[]{0f,.18f,.36f,.54f,.75f},new[]{0f,-2f,2f,-1f,0f});
                var saved=SaveClip(clip,"Angler_"+names[i]);var machine=controller.layers[0].stateMachine;
                var state=machine.states.FirstOrDefault(s=>s.state.name==names[i]).state??machine.AddState(names[i]);state.motion=saved;
                if(i!=1 && state.transitions.Length==0){var back=state.AddTransition(machine.defaultState);back.hasExitTime=true;back.exitTime=1;back.duration=.12f;}
            }
            EditorUtility.SetDirty(controller);return controller;
        }
        static void Curve(AnimationClip clip,string path,string property,float[] times,float[] values)
        { var keys=times.Select((t,i)=>new Keyframe(t,values[i])).ToArray();clip.SetCurve(path,typeof(Transform),property,new AnimationCurve(keys)); }
    }
}
