using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace LittleDepths.Editor
{
    public static class FishingSceneSetup
    {
        public const string Root = "Assets/LittleDepths";
        public const string ScenePath = "Assets/Scenes/LittleDepths.unity";

        [MenuItem("Little Depths/Set up fishing scene")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            var current=EditorSceneManager.GetActiveScene();
            if(current.IsValid()&&current.isDirty&&!Application.isBatchMode)
            {Directory.CreateDirectory("Assets/Scenes/Backups");EditorSceneManager.SaveScene(current,"Assets/Scenes/Backups/BeforeSpriteUpgrade_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);}
            foreach(string path in Directory.GetFiles(Root+"/Art","*.png",SearchOption.AllDirectories).Where(p=>!p.Contains("SpriteAtlases")&&!p.Contains("IntegratedAnglers")&&!p.Contains("RemasteredSheets")))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=path.Contains("Provided")?4096:2048;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.filterMode=FilterMode.Bilinear;
                // These high resolution cutouts are displayed much smaller than
                // their native canvases. Mips average their outlines instead of
                // sampling single texels and producing jagged circles/foliage.
                if(path.Contains("Provided")&&(path.Contains("ref_icon_")||path.Contains("ref_prize_")||path.Contains("ref_pine")||path.Contains("ref_oak")||path.Contains("ref_gold_tree")||path.Contains("ref_cloud")||path.Contains("ref_shore")))
                {importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;}
                if(path.Contains("Provided")&&(path.Contains("ref_cast_")||path.EndsWith("ref_dive_panel.png")||path.EndsWith("ref_depth_badge.png")||path.EndsWith("ref_reel_action.png")))
                {importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;}
                if(path.EndsWith("rounded.png")) importer.spriteBorder=new Vector4(25,25,25,25);
                if(path.Contains("UIV2")&&!path.EndsWith("ui_play_disc.png")&&!path.EndsWith("ui_seaweed.png")&&!path.EndsWith("ui_fish_shadow.png")&&!path.EndsWith("ui_pixel.png"))importer.spriteBorder=new Vector4(36,36,36,36);
                if(path.Contains("Provided"))
                {
                    var fullRect=new TextureImporterSettings();importer.ReadTextureSettings(fullRect);fullRect.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(fullRect);
                    importer.spriteBorder=Vector4.zero;
                    if(path.Contains("ref_button"))
                    {
                        var skin=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        float horizontal=Mathf.Min(skin.width*.45f,skin.height*.55f),vertical=skin.height*.48f;
                        importer.spriteBorder=new Vector4(horizontal,vertical,horizontal,vertical);
                    }
                    if(path.EndsWith("ref_panel.png"))importer.spriteBorder=new Vector4(32,32,32,32);
                    if(path.Contains("ref_frame_"))importer.spriteBorder=new Vector4(6,6,6,6);
                    if(path.EndsWith("ref_cast_target.png"))
                    {
                        var band=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        importer.spriteBorder=new Vector4(band.height*.48f,band.height*.43f,band.height*.48f,band.height*.43f);
                    }
                }
                importer.SaveAndReimport();
            }
            Directory.CreateDirectory(Root+"/Data");Directory.CreateDirectory(Root+"/Prefabs");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var config=AssetDatabase.LoadAssetAtPath<FishingConfig>(Root+"/Data/FishingBalance.asset");
            if(!config) { config=ScriptableObject.CreateInstance<FishingConfig>();AssetDatabase.CreateAsset(config,Root+"/Data/FishingBalance.asset"); }
            config.species.Clear();
            AddSpecies(config,"minnow","Mint minnow","Common",10,0,18,5,.68f,.45f,"71d5ce");
            AddSpecies(config,"perch","Meadow perch","Common",18,1,28,3,.74f,.38f,"c1d86a");
            AddSpecies(config,"clown","Clementine","Uncommon",28,3,34,2.4f,.77f,.47f,"f3a163");
            AddSpecies(config,"bluefin","Blueberry fin","Uncommon",40,5,45,1.9f,.8f,.55f,"83b8ef");
            AddSpecies(config,"sunfish","Sunset stripe","Rare",65,10,60,2.4f,.87f,.6f,"efb04e");
            AddSpecies(config,"jelly","Lavender jelly","Rare",90,12,75,1.9f,.68f,.22f,"b5a1ed");
            AddSpecies(config,"moonfish","Moon drifter","Epic",140,18,90,1.8f,.96f,.45f,"ac9ae0");
            AddSpecies(config,"ruby","Ruby wanderer","Epic",200,25,95,1.3f,.98f,.7f,"ee8aa3");
            AddSpecies(config,"golden","Golden daydream","Legendary",250,2,99,.13f,.8f,.42f,"e7af3e");
            AddSpecies(config,"abyss","Aurora spirit","Legendary",400,40,99,1.7f,1.13f,.68f,"5cd3bc");
            config.hookSprite=Sprite("hook");config.trashSprite=Sprite("icon_trash");
            ReferenceSpriteImporter.Import(config);
            EditorUtility.SetDirty(config);

            var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGO.tag="MainCamera";
            var camera=cameraGO.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=UiKit.Hex("3479af");
            camera.nearClipPlane=.1f;camera.farClipPlane=150;camera.allowHDR=false;camera.allowMSAA=true;
            cameraGO.AddComponent<UniversalAdditionalCameraData>();
            var worldGO=new GameObject("Lake • scenery and fish");var world=worldGO.AddComponent<FishingWorld>();
            WorldSprite("Warm morning sky",world.transform,"sky",new Vector2(0,5.15f),new Vector2(40,8.1f),-30);
            WorldSprite("Water • depth gradient",world.transform,"water",new Vector2(0,-38.9f),new Vector2(40,80),-29);
            ProvidedScenery(world);
            WorldSprite("Morning light • left",world.transform,"sunray",new Vector2(-1.6f,-6.5f),new Vector2(2.9f,16),-20);
            WorldSprite("Morning light • right",world.transform,"sunray",new Vector2(1,-6.5f),new Vector2(2.3f,16),-20);
            for(int i=0;i<7;i++)
            {
                float y=1.05f-7.98f-i*16;
                WorldSprite("Left bank "+i,world.transform,"bank_left",new Vector2(-5.93f,y),new Vector2(5.7f,16),-12);
                WorldSprite("Right bank "+i,world.transform,"bank_right",new Vector2(5.93f,y),new Vector2(5.7f,16),-12);
            }
            var leftFill=WorldSprite("Left bank • opaque coverage beyond viewport",world.transform,"UIV2/ui_pixel",new Vector2(-54.3f,-53.95f),new Vector2(92,110),-13);leftFill.color=UiKit.Hex("695d78");
            var rightFill=WorldSprite("Right bank • opaque coverage beyond viewport",world.transform,"UIV2/ui_pixel",new Vector2(54.3f,-53.95f),new Vector2(92,110),-13);rightFill.color=UiKit.Hex("695d78");
            for(int i=0;i<37;i++)
            {
                float y=-.8f-i*1.9f;float sign=i%2==0?-1:1;
                var coral=WorldSprite("River flora "+i,world.transform,i>12?"deep_coral":i%3==0?"kelp":"coral",new Vector2(sign*3.04f,y),new Vector2(.8f,1.05f),-5);
                coral.transform.rotation=Quaternion.Euler(0,0,sign>0?23:-24);coral.color=new Color(1,1,1,.55f);
            }
            world.boat=CreateAngler(world,config);
            WorldSprite("Rippling lake surface",world.transform,"surface",new Vector2(0,1.04f),new Vector2(40,.34f),9);
            world.fishRoot=new GameObject("Swimming fish • populated on Play").transform;world.fishRoot.SetParent(world.transform);
            world.effectsRoot=new GameObject("Bubbles and catch sparkles").transform;world.effectsRoot.SetParent(world.transform);
            world.bubbleSprite=Sprite("bubble");world.sparkleSprite=Sprite("sparkle");
            for(int i=0;i<22;i++)
            {
                var b=WorldSprite("Ambient bubble "+i,world.transform,"bubble",new Vector2(Mathf.Sin(i*5.6f)*2.8f,-.5f-i*1.5f),Vector2.one*(.045f+(i%3)*.025f),-4);
                b.color=new Color(1,1,1,.4f);
            }

            var gameGO=new GameObject("Fishing Game • progression and session");var game=gameGO.AddComponent<FishingGame>();
            game.config=config;game.gameCamera=camera;game.world=world;
            var hook=WorldSprite("Player hook • steer left and right",game.transform,"hook",new Vector2(0,config.surfaceY-.55f),new Vector2(.57f,.57f),30);
            hook.color=config.hookColors[0];
            game.hook=hook.transform;
            var line=new GameObject("Fishing line",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(game.transform);
            hook.transform.position+=Vector3.right*(world.angler.rodTip.position.x-game.HookEye.x);
            line.positionCount=2;line.useWorldSpace=true;line.SetPosition(0,world.angler.rodTip.position);line.SetPosition(1,game.HookEye);
            line.startWidth=.018f;line.endWidth=.012f;line.startColor=line.endColor=UiKit.Cream;line.sortingOrder=20;
            var lineMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Data/FishingLine.mat");
            if(!lineMat) { lineMat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(lineMat,Root+"/Data/FishingLine.mat"); }
            line.sharedMaterial=lineMat;game.fishingLine=line;
            game.audioSource=gameGO.AddComponent<AudioSource>();game.audioSource.playOnAwake=false;
            game.catchSound=Clip("catch");game.clickSound=Clip("click");game.sellSound=Clip("sell");game.legendarySound=Clip("legendary");game.splashSound=Clip("splash");

            var canvasGO=new GameObject("UI • Little Depths",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;canvas.sortingOrder=100;
            var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var hud=canvasGO.AddComponent<FishingHUD>();hud.bodyFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Nunito-SemiBold.ttf");hud.displayFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Nunito-Black.ttf");
            hud.rounded=Sprite("rounded");hud.circle=Sprite("circle");
            hud.art=Directory.GetFiles(Root+"/Art","*.png",SearchOption.AllDirectories).Where(p=>!p.Contains("RemasteredSheets")).SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p.Replace('\\','/')).OfType<Sprite>()).ToArray();
            hud.CreateLayout();game.hud=hud;
            var eventSystem=new GameObject("EventSystem • mouse, keyboard and touch",typeof(EventSystem),typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            // A reusable fish prefab exposes the visual and movement component for designers.
            var fishTemplate=new GameObject("FishActor",typeof(FishActor));
            PrefabUtility.SaveAsPrefabAsset(fishTemplate,Root+"/Prefabs/FishActor.prefab");UnityEngine.Object.DestroyImmediate(fishTemplate);
            PlayerSettings.productName="Little Depths";PlayerSettings.companyName="Quiet Lake Studio";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.ImportAsset(ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true) { guid = new GUID(AssetDatabase.AssetPathToGUID(ScenePath)) }};
            Selection.activeGameObject=gameGO;
            Debug.Log("LITTLE_DEPTHS_SETUP_OK: Scene, balance data, original art and gameplay ready. No player build was created.");
        }

        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+name+".png");
        static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+name+".wav");
        static void ProvidedScenery(FishingWorld world)
        {
            var mountains=Sprite("Provided/ref_mountains");float mountainHeight=19.2f*mountains.rect.height/mountains.rect.width;
            WorldSprite("Supplied layered mountains",world.transform,"Provided/ref_mountains",new Vector2(0,1.05f+mountainHeight/2),new Vector2(19.2f,mountainHeight),-25);
            for(int i=0;i<9;i++)
            {
                string name="Provided/"+(i%3==0?"ref_oak":i%3==1?"ref_pines":"ref_gold_tree");var sprite=Sprite(name);float height=i%3==1?1.75f:1.9f;
                var tree=WorldSprite("Supplied shoreline tree "+i,world.transform,name,new Vector2(-10.5f+i*2.65f,1.04f+height/2),new Vector2(height*sprite.rect.width/sprite.rect.height,height),-16+i%2);
                if(i%2==1)tree.flipX=true;
            }
            WorldSprite("Supplied near shore • left",world.transform,"Provided/ref_shore_left",new Vector2(-5.8f,1.32f),new Vector2(2.6f,1.5f),-10);
            WorldSprite("Supplied near shore • right",world.transform,"Provided/ref_shore_right",new Vector2(5.9f,1.27f),new Vector2(3.5f,1.01f),-10);
            WorldSprite("Supplied cloud • left",world.transform,"Provided/ref_cloud",new Vector2(-5.8f,4),new Vector2(1.6f,.7f),-26);
            WorldSprite("Supplied cloud • right",world.transform,"Provided/ref_cloud_long",new Vector2(5.9f,4.2f),new Vector2(2.2f,.7f),-26);
        }
        static void AddSpecies(FishingConfig c,string id,string name,string rarity,int value,float min,float max,float weight,float size,float speed,string color)
        { c.species.Add(new FishSpecies{id=id,displayName=name,rarity=rarity,value=value,minDepth=min,maxDepth=max,weight=weight,size=size,speed=speed,color=UiKit.Hex(color),sprite=Sprite("fish_"+id)}); }
        static SpriteRenderer WorldSprite(string name,Transform parent,string sprite,Vector2 position,Vector2 size,int order)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent);go.transform.position=position;
            var r=go.GetComponent<SpriteRenderer>();r.sprite=Sprite(sprite);r.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);return r;
        }

        static Transform CreateAngler(FishingWorld world,FishingConfig config)
        {
            var root=new GameObject("Angler and boat • complete animated sprite").transform;root.SetParent(world.transform);root.position=new Vector3(1.6f,2.1f,0);
            var rig=root.gameObject.AddComponent<AnglerRig>();world.angler=rig;
            var angler=new GameObject("Angler").transform;angler.SetParent(root,false);
            rig.visual=RigSprite("Complete girl, worn hat and occupied boat",angler,config.CompleteSprite(0,0,0,0),Vector2.zero,Vector2.one,10);
            rig.rodTip=new GameObject("Line anchor • painted rod tip").transform;rig.rodTip.SetParent(angler,false);
            rig.animator=root.gameObject.AddComponent<Animator>();rig.animator.runtimeAnimatorController=config.anglerController;
            rig.Configure(config,new GameProgress());
            return root;
        }
        static SpriteRenderer RigSprite(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,int order)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return renderer;
        }
    }
}
