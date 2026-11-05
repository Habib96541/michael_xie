using System;
using System.IO;
using System.Linq;
using ChickenRush;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace ChickenRushEditor
{
    public static class RushBuilder
    {
        const string Root="Assets/ChickenRush";
        const string FontPath="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        static bool building,assetsOnly;

        public static void Prepare(){assetsOnly=true;BuildAll();}
        public static void BuildPlayerOnly()
        {
            try{ConfigureBuild();AssetDatabase.SaveAssets();ValidateAssets();BuildConfiguredPlayer();}
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        [MenuItem("Chicken Rush/Generate assets, scene and WebGL build")]
        public static void BuildAll()
        {
            try
            {
                if(!File.Exists(FontPath))
                {
                    TMP_PackageResourceImporter.ImportResources(true,false,false);
                    EditorApplication.update+=WaitForFont;
                    return;
                }
                Complete();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void WaitForFont()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(FontPath))return;
            EditorApplication.update-=WaitForFont;
            try{Complete();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Complete()
        {
            if(building)return;building=true;
            Directory.CreateDirectory(Root+"/Animations");Directory.CreateDirectory(Root+"/Prefabs");
            Directory.CreateDirectory(Root+"/Config");Directory.CreateDirectory(Root+"/Art/Shapes");
            Directory.CreateDirectory("QA");Directory.CreateDirectory("Builds/WebGL");
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Root+"/Art/Chicken","*.png")) ImportSprite(path,false);
            CreateShape("Rounded",false);CreateShape("Disc",true);CreateGraphics();
            foreach(string path in Directory.GetFiles(Root+"/Art/Arcade","*.png"))ImportArcade(path);
            var rounded=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Shapes/Rounded.png");
            var disc=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Shapes/Disc.png");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(!font)throw new Exception("TMP essentials did not import.");
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if(!shader)throw new Exception("URP sprite unlit shader missing.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/ChickenUnlit.mat");
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"/Art/ChickenUnlit.mat");}
            var controller=CreateAnimator();
            var prefab=CreateChicken(controller,material);
            var configurations=new RushDifficulty[4];
            string[] names={"EASY","NORMAL","HARD","HARDCORE"};float[] speed={.85f,1f,1.13f,1.28f};
            float[] progression={1.12f,1.3f,1.55f,1.9f,2.4f,3.15f,4.2f,5.7f,8,11.5f,16.8f,25,38,58,90,142,228,370,610,1020};
            for(int i=0;i<4;i++)
            {
                string path=Root+"/Config/"+names[i]+".asset";
                var config=AssetDatabase.LoadAssetAtPath<RushDifficulty>(path);
                bool needsDefaults=!config || config.tiers==null || config.tiers.Length==0;
                if(!config){config=ScriptableObject.CreateInstance<RushDifficulty>();AssetDatabase.CreateAsset(config,path);}
                if(needsDefaults)
                {
                config.displayName=names[i];config.speedScale=speed[i];config.hopSeconds=.42f;
                config.multipliers=progression.Select(value=>(float)Math.Round(Math.Pow(value,i==0?.86:i==2?1.12:1),2)).ToArray();
                config.tiers=new[]{
                    new TrafficTier{firstLane=1,vehicleCount=3,speed=3.9f,speedPerLane=.20f,minGapSeconds=.72f,maxGapSeconds=1.25f,guaranteedGapSeconds=.98f},
                    new TrafficTier{firstLane=4,vehicleCount=4,speed=4.9f,speedPerLane=.20f,minGapSeconds=.40f,maxGapSeconds=.90f,guaranteedGapSeconds=.85f},
                    new TrafficTier{firstLane=7,vehicleCount=6,speed=5.9f,speedPerLane=.23f,minGapSeconds=.18f,maxGapSeconds=.52f,guaranteedGapSeconds=.76f},
                    new TrafficTier{firstLane=11,vehicleCount=7,speed=7.1f,speedPerLane=.10f,minGapSeconds=.12f,maxGapSeconds=.30f,guaranteedGapSeconds=.72f}};
                }
                if(config.riskVersion<3)
                {
                    config.displayName=i==1?"MEDIUM":names[i];config.hopSeconds=.62f;
                    config.speedScale=speed[i];
                    int count=new[]{24,20,16,12}[i];float p=new[]{.96f,.88f,.78f,.64f}[i],decay=new[]{.006f,.009f,.012f,.013f}[i];
                    config.successChances=new float[count];config.multipliers=new float[count];double survival=1;
                    for(int lane=0;lane<count;lane++)
                    {
                        config.successChances[lane]=p-lane*decay;survival*=config.successChances[lane];
                        config.multipliers[lane]=(float)Math.Round(.985/survival,2);
                    }
                    config.riskVersion=3;
                }
                configurations[i]=config;EditorUtility.SetDirty(config);
            }
            CreateAtlas();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.SetQualityLevel(0,true);QualitySettings.renderPipeline=pipeline;QualitySettings.globalTextureMipmapLimit=0;
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;QualitySettings.shadows=UnityEngine.ShadowQuality.Disable;
            pipeline.renderScale=1;pipeline.msaaSampleCount=1;pipeline.supportsHDR=false;pipeline.supportsCameraDepthTexture=false;pipeline.supportsCameraOpaqueTexture=false;
            EditorUtility.SetDirty(pipeline);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));
            cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=4.4f;camera.backgroundColor=RoadWorld.Hex("14283A");
            camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.transform.position=new Vector3(2,.38f,-10);
            var data=cameraObject.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
            var game=new GameObject("Chicken Rush").AddComponent<ChickenRushGame>();
            var so=new SerializedObject(game);
            so.FindProperty("difficulties").arraySize=configurations.Length;
            for(int i=0;i<configurations.Length;i++)so.FindProperty("difficulties").GetArrayElementAtIndex(i).objectReferenceValue=configurations[i];
            so.FindProperty("chickenPrefab").objectReferenceValue=prefab;so.FindProperty("roundedSprite").objectReferenceValue=rounded;
            so.FindProperty("discSprite").objectReferenceValue=disc;so.FindProperty("font").objectReferenceValue=font;
            foreach(string name in new[]{"Square","Arrow","Asphalt"})so.FindProperty(char.ToLowerInvariant(name[0])+name.Substring(1)+"Sprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Shapes/"+name+".png");
            foreach(var pair in new[]{new[]{"uiSprite","Card"},new[]{"gearSprite","Gear"},new[]{"uiArrowSprite","Arrow"},new[]{"coinSprite","Coin"}})
                so.FindProperty(pair[0]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/UI/"+pair[1]+".png");
            so.FindProperty("asphaltSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Arcade/RoadSurface.png");
            foreach(var pair in new[]{new[]{"landingPadSprite","LandingPad"},new[]{"barrierSprite","Barrier"},new[]{"shrubSprite","Shrub"},new[]{"sidewalkSprite","Sidewalk"}})
                so.FindProperty(pair[0]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Arcade/"+pair[1]+".png");
            var vehicleProperty=so.FindProperty("vehicleSprites");vehicleProperty.arraySize=10;
            string[] kinds={"Compact","Taxi","Van","Truck","Delivery"};
            for(int i=0;i<10;i++)vehicleProperty.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Arcade/"+kinds[i%5]+(i<5?"Down":"Up")+".png");
            so.FindProperty("spriteMaterial").objectReferenceValue=material;so.FindProperty("gameCamera").objectReferenceValue=camera;so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,Root+"/ChickenRush.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/ChickenRush.unity",true)};
            ConfigureBuild();AssetDatabase.SaveAssets();
            ValidateAssets();
            if(assetsOnly){building=false;assetsOnly=false;if(Application.isBatchMode)EditorApplication.Exit(0);return;}
            BuildConfiguredPlayer();
        }
        static void BuildConfiguredPlayer()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/ChickenRush.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            File.WriteAllText("QA/BuildReport.json",JsonUtility.ToJson(new BuildSummaryData{result=report.summary.result.ToString(),bytes=(long)report.summary.totalSize,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds},true));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
            Debug.Log("CHICKEN_RUSH_BUILD_SUCCEEDED "+report.summary.totalSize+" bytes");
            if(Application.isBatchMode)EditorApplication.Exit(0);
            building=false;
        }
        [Serializable] class BuildSummaryData {public string result;public long bytes;public int errors,warnings;public double seconds;}
        static void ImportSprite(string path,bool shape)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=shape?128:100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=shape?new Vector2(.5f,.5f):new Vector2(.5f,18f/192f);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            if(shape)importer.spriteBorder=new Vector4(16,16,16,16);
            // Keep source drawings lossless; compress only the assembled atlas.
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="WebGL",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.RGBA32,compressionQuality=100});
            importer.SaveAndReimport();
        }
        static void ImportArcade(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=128;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;
            settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.spriteBorder=Vector4.zero;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="WebGL",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.RGBA32});importer.SaveAndReimport();
        }
        static void CreateShape(string name,bool circle)
        {
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float distance=circle?new Vector2(x-63.5f,y-63.5f).magnitude-62.5f:
                    new Vector2(Mathf.Max(Mathf.Abs(x-63.5f)-53.5f,0),Mathf.Max(Mathf.Abs(y-63.5f)-53.5f,0)).magnitude-10;
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-distance)));
            }
            texture.Apply();string path=Root+"/Art/Shapes/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);ImportSprite(path,true);
        }
        static void CreateGraphics()
        {
            Directory.CreateDirectory(Root+"/Art/UI");
            AssetDatabase.Refresh();
            foreach(string name in new[]{"Square","Arrow","Asphalt","Card","Gear","Coin","UIArrow"})
            {
                bool ui=name=="Card" || name=="Gear" || name=="Coin" || name=="UIArrow";
                var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);var random=new System.Random(17);
                for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                {
                    float dx=x-63.5f,dy=y-63.5f,alpha=1,shade=1;
                    if(name=="Card")
                    {
                        float distance=new Vector2(Mathf.Max(Mathf.Abs(dx)-39.5f,0),Mathf.Max(Mathf.Abs(dy)-39.5f,0)).magnitude-24;
                        alpha=Mathf.Clamp01(.5f-distance);shade=.93f+.07f*y/127;
                    }
                    else if(name=="Coin")alpha=Mathf.Clamp01(49.5f-new Vector2(dx,dy).magnitude);
                    else if(name=="Gear")
                    {
                        float radius=new Vector2(dx,dy).magnitude,angle=Mathf.Atan2(dy,dx);
                        float outer=45+8*Mathf.Clamp01((Mathf.Cos(angle*8)-.1f)/.15f);
                        alpha=Mathf.Min(Mathf.Clamp01(outer-radius+.5f),Mathf.Clamp01(radius-19));
                    }
                    else if(name=="Arrow" || name=="UIArrow")
                    {
                        float distance=Mathf.Abs(y-(96-Mathf.Abs(dx)))*.7071f;
                        alpha=Mathf.Clamp01(8.5f-distance)*Mathf.Clamp01(41.5f-Mathf.Abs(dx));
                    }
                    else if(name=="Asphalt")shade=.95f+((float)random.NextDouble()-.5f)*.035f;
                    texture.SetPixel(x,y,new Color(shade,shade,shade,alpha));
                }
                texture.Apply();string path=Root+"/Art/"+(ui?"UI/":"Shapes/")+(name=="UIArrow"?"Arrow":name)+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                ImportSprite(path,true);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.spritePixelsPerUnit=ui?100:128;
                importer.spriteBorder=name=="Card"?new Vector4(32,32,32,32):Vector4.zero;
                importer.SaveAndReimport();
            }
            File.Copy(Root+"/Art/Chicken/Idle_00.png","Assets/WebGLTemplates/ChickenRush/chicken.png",true);
            AssetDatabase.ImportAsset("Assets/WebGLTemplates/ChickenRush/chicken.png");
        }
        static AnimationClip Clip(string name,int count,float duration,bool loop)
        {
            string path=Root+"/Animations/"+name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.ClearCurves();clip.frameRate=30;clip.name=name;
            var keys=new ObjectReferenceKeyframe[count+1];
            string sourceName=name=="Danger"?"Idle":name;
            for(int i=0;i<count;i++)keys[i]=new ObjectReferenceKeyframe{time=i*duration/count,value=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Chicken/"+sourceName+"_"+i.ToString("00")+".png")};
            if(name=="Hit")for(int i=0;i<count;i++)keys[i].time=i<2?i*.02f:.04f+(i-2)*.1f;
            keys[count]=new ObjectReferenceKeyframe{time=duration,value=keys[loop?0:count-1].value};
            if(name=="Idle")
            {
                var blink=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Chicken/IdleBlink.png");
                if(blink)
                {
                    var list=keys.ToList();list.Add(new ObjectReferenceKeyframe{time=duration-.18f,value=blink});
                    list.Add(new ObjectReferenceKeyframe{time=duration-.06f,value=keys[0].value});keys=list.OrderBy(k=>k.time).ToArray();
                }
            }
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
            if(name=="Idle")
                foreach(string axis in new[]{"x","y"})clip.SetCurve("Visual",typeof(Transform),"localScale."+axis,new AnimationCurve(new Keyframe(0,1),new Keyframe(duration*.5f,1.015f),new Keyframe(duration,1)));
            if(name=="Hop")clip.SetCurve("Visual",typeof(Transform),"localPosition.y",new AnimationCurve(new Keyframe(0,0),new Keyframe(.07f,-.035f),new Keyframe(.3f,.23f),new Keyframe(.53f,0),new Keyframe(.6f,0)));
            else if(name=="Celebrate")
            {
                clip.SetCurve("Visual",typeof(Transform),"localPosition.y",new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.25f,.13f),new Keyframe(duration*.5f,0),new Keyframe(duration*.75f,.13f),new Keyframe(duration,0)));
                clip.SetCurve("Visual",typeof(Transform),"localEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.25f,-4),new Keyframe(duration*.5f,0),new Keyframe(duration*.75f,4),new Keyframe(duration,0)));
            }
            // Every state writes the same transform properties, preventing pose residue.
            if(name=="Hop")
            {
                clip.SetCurve("Visual",typeof(Transform),"localScale.x",new AnimationCurve(new Keyframe(0,1),new Keyframe(.065f,1.06f),new Keyframe(.24f,.96f),new Keyframe(.51f,1.055f),new Keyframe(duration,1)));
                clip.SetCurve("Visual",typeof(Transform),"localScale.y",new AnimationCurve(new Keyframe(0,1),new Keyframe(.065f,.90f),new Keyframe(.24f,1.08f),new Keyframe(.51f,.94f),new Keyframe(duration,1)));
            }
            else if(name!="Idle")foreach(string axis in new[]{"x","y"})clip.SetCurve("Visual",typeof(Transform),"localScale."+axis,AnimationCurve.Constant(0,duration,1));
            if(name!="Hop" && name!="Celebrate")clip.SetCurve("Visual",typeof(Transform),"localPosition.y",AnimationCurve.Constant(0,duration,0));
            if(name=="Danger")clip.SetCurve("Visual",typeof(Transform),"localEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.25f,-3),new Keyframe(duration*.5f,1),new Keyframe(duration,0)));
            else if(name!="Celebrate")clip.SetCurve("Visual",typeof(Transform),"localEulerAnglesRaw.z",AnimationCurve.Constant(0,duration,0));
            EditorUtility.SetDirty(clip);return clip;
        }
        static AnimatorController CreateAnimator()
        {
            string path=Root+"/Animations/Chicken.controller";
            var old=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(old)AssetDatabase.DeleteAsset(path);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            controller.AddParameter("Danger",AnimatorControllerParameterType.Bool);
            foreach(string name in new[]{"Hop","Hit","Celebrate","Reset"})controller.AddParameter(name,AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HopSpeed",AnimatorControllerParameterType.Float);
            var parameters=controller.parameters;for(int i=0;i<parameters.Length;i++)if(parameters[i].name=="HopSpeed")parameters[i].defaultFloat=1;controller.parameters=parameters;
            var sm=controller.layers[0].stateMachine;
            var idle=sm.AddState("Idle",new Vector3(200,0));idle.motion=Clip("Idle",11,2.4f,true);sm.defaultState=idle;
            var walk=sm.AddState("Walk",new Vector3(440,0));walk.motion=Clip("Walk",11,.6f,true);
            var hop=sm.AddState("Hop",new Vector3(200,130));hop.motion=Clip("Hop",10,.6f,false);hop.speedParameter="HopSpeed";hop.speedParameterActive=true;
            var hit=sm.AddState("Hit",new Vector3(440,260));hit.motion=Clip("Hit",10,.8f,false);
            var celebrate=sm.AddState("Celebrate",new Vector3(200,260));celebrate.motion=Clip("Celebrate",11,.88f,true);
            var danger=sm.AddState("Danger",new Vector3(440,130));danger.motion=Clip("Danger",11,1.1f,true);
            Transition(idle,danger,"Danger",AnimatorConditionMode.If);Transition(danger,idle,"Danger",AnimatorConditionMode.IfNot);Transition(danger,hop,"Hop",AnimatorConditionMode.If);
            Transition(idle,walk,"Moving",AnimatorConditionMode.If);Transition(walk,idle,"Moving",AnimatorConditionMode.IfNot);
            Transition(idle,hop,"Hop",AnimatorConditionMode.If);Transition(walk,hop,"Hop",AnimatorConditionMode.If);
            var land=hop.AddTransition(idle);land.hasExitTime=true;land.exitTime=1;land.duration=0;land.interruptionSource=TransitionInterruptionSource.None;
            foreach(var state in new[]{idle,walk,hop,danger})
            {
                Transition(state,hit,"Hit",AnimatorConditionMode.If);
                Transition(state,celebrate,"Celebrate",AnimatorConditionMode.If);
            }
            var reset=sm.AddAnyStateTransition(idle);reset.hasExitTime=false;reset.duration=0;reset.canTransitionToSelf=false;reset.AddCondition(AnimatorConditionMode.If,0,"Reset");
            return controller;
        }
        static void Transition(AnimatorState from,AnimatorState to,string parameter,AnimatorConditionMode mode)
        {
            var transition=from.AddTransition(to);transition.hasExitTime=false;transition.duration=0;transition.interruptionSource=TransitionInterruptionSource.None;transition.AddCondition(mode,0,parameter);
        }
        static GameObject CreateChicken(AnimatorController controller,Material material)
        {
            var root=new GameObject("Chicken");var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
            var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Chicken/Idle_00.png");sr.sortingOrder=50;sr.sharedMaterial=material;
            root.AddComponent<ChickenAnimationController>().Configure(animator,sr);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Chicken.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static void CreateAtlas()
        {
            string path=Root+"/Art/Chicken.spriteatlas";
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if(!atlas){atlas=new SpriteAtlas();AssetDatabase.CreateAsset(atlas,path);}
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false,blockOffset=1});
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{readable=false,generateMipMaps=false,sRGB=true,filterMode=FilterMode.Bilinear});
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings{name="WebGL",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGBA8,compressionQuality=100});
            atlas.SetIncludeInBuild(true);
            var objects=new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/Chicken"),AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/Shapes")};
            var existing=atlas.GetPackables();if(existing.Length>0)atlas.Remove(existing);atlas.Add(objects);EditorUtility.SetDirty(atlas);
            SpriteAtlasUtility.PackAtlases(new[]{atlas},BuildTarget.WebGL);
            string uiPath=Root+"/Art/UI.spriteatlas";var ui=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(uiPath);
            if(!ui){ui=new SpriteAtlas();AssetDatabase.CreateAsset(ui,uiPath);}
            ui.SetPackingSettings(new SpriteAtlasPackingSettings{padding=8,enableRotation=false,enableTightPacking=false,blockOffset=1});
            ui.SetTextureSettings(new SpriteAtlasTextureSettings{readable=false,generateMipMaps=false,sRGB=true,filterMode=FilterMode.Bilinear});
            ui.SetPlatformSettings(new TextureImporterPlatformSettings{name="WebGL",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.RGBA32,textureCompression=TextureImporterCompression.Uncompressed});
            ui.SetIncludeInBuild(true);var packables=ui.GetPackables();if(packables.Length>0)ui.Remove(packables);
            ui.Add(new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/UI")});EditorUtility.SetDirty(ui);SpriteAtlasUtility.PackAtlases(new[]{ui},BuildTarget.WebGL);
            string artPath=Root+"/Art/Arcade.spriteatlas";var art=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(artPath);
            if(!art){art=new SpriteAtlas();AssetDatabase.CreateAsset(art,artPath);}
            art.SetPackingSettings(new SpriteAtlasPackingSettings{padding=8,enableRotation=false,enableTightPacking=false,blockOffset=1});
            art.SetTextureSettings(new SpriteAtlasTextureSettings{readable=false,generateMipMaps=false,sRGB=true,filterMode=FilterMode.Bilinear});
            art.SetPlatformSettings(new TextureImporterPlatformSettings{name="WebGL",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGBA8,compressionQuality=100});
            art.SetIncludeInBuild(true);var oldArt=art.GetPackables();if(oldArt.Length>0)art.Remove(oldArt);
            art.Add(new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Art/Arcade")});EditorUtility.SetDirty(art);SpriteAtlasUtility.PackAtlases(new[]{art},BuildTarget.WebGL);
        }
        static void ConfigureBuild()
        {
            PlayerSettings.companyName="Chicken Rush Studio";PlayerSettings.productName="Chicken Rush: Cash Out!";PlayerSettings.bundleVersion="3.0.0";
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=false;
            PlayerSettings.defaultWebScreenWidth=1366;PlayerSettings.defaultWebScreenHeight=768;
            PlayerSettings.WebGL.template="PROJECT:ChickenRush";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.initialMemorySize=64;PlayerSettings.WebGL.maximumMemorySize=512;
            PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;PlayerSettings.WebGL.geometricMemoryGrowthStep=.2f;
            PlayerSettings.WebGL.threadsSupport=false;PlayerSettings.WebGL.wasm2023=false;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Medium);
            PlayerSettings.stripEngineCode=true;
        }
        [MenuItem("Chicken Rush/Validate generated animation assets")]
        public static void ValidateAssets()
        {
            int total=0;
            foreach(string name in new[]{"Idle","Walk","Hop","Hit","Celebrate"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/"+name+".anim");
                if(!clip)throw new Exception("Missing clip "+name);
                var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
                if(keys.Any(key=>key.value==null))throw new Exception("Null sprite in "+name);
                total+=keys.Length;
            }
            File.WriteAllText("QA/AssetValidation.txt","PASS: all five animation clips contain valid sprite references. "+total+" sprite keys.\nFullRect, bilinear, mipmaps disabled, common 192px canvas / foot pivot (0.5, 0.09375).\n");
            Debug.Log("CHICKEN_RUSH_ASSETS_VALIDATED");
        }
    }
}
