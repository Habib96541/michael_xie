using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace BrickRush.Editor
{
    public static class BuildGame
    {
        const string ResourcePath="Assets/BrickRush/Resources/";
        [MenuItem("Brick Rush/1. Import typography resources")]
        public static void ImportTypography(){TMP_PackageResourceImporter.ImportResources(true,false,false);}
        [MenuItem("Brick Rush/2. Prepare project")]
        public static void Setup()
        {
            Directory.CreateDirectory(ResourcePath);Directory.CreateDirectory("Assets/BrickRush/Scenes");
            foreach(string path in Directory.GetFiles(ResourcePath+"Art","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=256;
                var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
                if(Path.GetFileNameWithoutExtension(path)=="panel"){ti.spriteBorder=new Vector4(16,16,16,16);ti.SaveAndReimport();}
            }
            foreach(string path in Directory.GetFiles(ResourcePath+"Audio","*.wav"))
            {
                var ai=(AudioImporter)AssetImporter.GetAtPath(path);var settings=ai.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;ai.defaultSampleSettings=settings;ai.forceToMono=true;ai.SaveAndReimport();
            }
            CreateFont("Rajdhani-Bold","DisplayFont");CreateFont("Barlow-Medium","BodyFont");
            TMP_Settings.defaultFontAsset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourcePath+"BodyFont.asset");EditorUtility.SetDirty(TMP_Settings.instance);
            CreateMaterial("ArenaMaterial","BrickRush/Arena");CreateMaterial("TrailMaterial","BrickRush/Trail");CreateMaterial("SpriteMaterial","Universal Render Pipeline/2D/Sprite-Unlit-Default");
            var config=AssetDatabase.LoadAssetAtPath<GameConfig>(ResourcePath+"GameConfig.asset");if(config==null){config=ScriptableObject.CreateInstance<GameConfig>();AssetDatabase.CreateAsset(config,ResourcePath+"GameConfig.asset");}
            Debug.Log("Quality tiers: "+string.Join(",",QualitySettings.names));QualitySettings.SetQualityLevel(0,true);QualitySettings.vSyncCount=0;
            var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(urp==null)throw new Exception("URP is required.");urp.supportsHDR=true;urp.msaaSampleCount=1;urp.renderScale=1;EditorUtility.SetDirty(urp);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ResourcePath+"NeonVolume.asset");
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ResourcePath+"NeonVolume.asset");
                var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.35f;bloom.threshold.value=.9f;bloom.scatter.value=.55f;bloom.highQualityFiltering.value=false;AssetDatabase.AddObjectToAsset(bloom,profile);
                var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.Neutral;AssetDatabase.AddObjectToAsset(tone,profile);
                var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.16f;vignette.smoothness.value=.5f;AssetDatabase.AddObjectToAsset(vignette,profile);
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Brick Rush");game.AddComponent<GameManager>();
            var cameraObject=new GameObject("Arena Camera");cameraObject.tag="MainCamera";cameraObject.transform.SetParent(game.transform);cameraObject.transform.position=new Vector3(0,0,-10);
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=7.3f;camera.backgroundColor=Art.Ink;camera.clearFlags=CameraClearFlags.SolidColor;camera.allowHDR=true;cameraObject.AddComponent<AudioListener>();
            var cameraData=cameraObject.AddComponent<UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            var volume=new GameObject("Neon grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            EditorSceneManager.SaveScene(scene,"Assets/BrickRush/Scenes/BrickRush.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/BrickRush/Scenes/BrickRush.unity",true)};
            PlayerSettings.companyName="Neon Current";PlayerSettings.productName="Brick Rush";PlayerSettings.bundleVersion="1.0.0";PlayerSettings.runInBackground=true;
            PlayerSettings.defaultWebScreenWidth=1440;PlayerSettings.defaultWebScreenHeight=960;PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.WebGL.template="PROJECT:BrickRush";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.initialMemorySize=64;PlayerSettings.WebGL.maximumMemorySize=512;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL,Il2CppCompilerConfiguration.Release);
            AssetDatabase.SaveAssets();Debug.Log("BRICK RUSH SETUP COMPLETE");
        }
        static void CreateFont(string source,string name)
        {
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ResourcePath+name+".asset")!=null)return;
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/BrickRush/Fonts/"+source+".ttf");
            var asset=TMP_FontAsset.CreateFontAsset(font,90,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);asset.name=name;
            AssetDatabase.CreateAsset(asset,ResourcePath+name+".asset");
            string chars=new string(Enumerable.Range(32,95).Select(c=>(char)c).ToArray());
            if(!asset.TryAddCharacters(chars,out string missing))throw new Exception("Missing font glyphs: "+missing);
            asset.atlasPopulationMode=AtlasPopulationMode.Static;
            foreach(var tex in asset.atlasTextures){tex.name=name+" Atlas";AssetDatabase.AddObjectToAsset(tex,asset);}
            asset.material.name=name+" Material";AssetDatabase.AddObjectToAsset(asset.material,asset);EditorUtility.SetDirty(asset);
        }
        static void CreateMaterial(string name,string shaderName)
        {
            if(AssetDatabase.LoadAssetAtPath<Material>(ResourcePath+name+".mat")!=null)return;
            var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Missing shader "+shaderName);AssetDatabase.CreateAsset(new Material(shader){name=name},ResourcePath+name+".mat");
        }
        [MenuItem("Brick Rush/3. Build WebGL")]
        public static void BuildWebGL()
        {
            string output="Builds/BrickRush_WebGL";Directory.CreateDirectory(output);Directory.CreateDirectory("TestResults");
            try
            {
                UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;AssetDatabase.SaveAssets();
                foreach(string shaderName in new[]{"BrickRush/Arena","BrickRush/Trail"})if(ShaderUtil.ShaderHasError(Shader.Find(shaderName)))throw new Exception("Shader failed preflight: "+shaderName);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
                var summary=report.summary;
                string json=JsonUtility.ToJson(new BuildReceipt{result=summary.result.ToString(),bytes=(long)summary.totalSize,seconds=summary.totalTime.TotalSeconds,errors=(int)summary.totalErrors,warnings=(int)summary.totalWarnings,unity=Application.unityVersion},true);
                File.WriteAllText("TestResults/build.json",json);Debug.Log("BRICK RUSH BUILD: "+json);
                if(summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed.");
                File.Copy("Assets/BrickRush/Fonts/rajdhani-OFL.txt",output+"/Rajdhani-LICENSE.txt",true);File.Copy("Assets/BrickRush/Fonts/barlow-OFL.txt",output+"/Barlow-LICENSE.txt",true);
                File.Copy("Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt",output+"/LiberationSans-LICENSE.txt",true);File.Copy("Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt",output+"/EmojiOne-Attribution.txt",true);
                File.WriteAllText(output+"/NOTICE.txt","Brick Rush 1.0.0\nBuilt with Unity.\nOriginal game code, sprites and synthesized audio created for this project.\nRajdhani Bold, Barlow Medium and bundled TMP Liberation Sans resources are distributed under the SIL Open Font License; see included license files.\nBundled TMP EmojiOne resources retain the included attribution. The game does not display EmojiOne artwork.\n");
            }
            catch(Exception error){File.WriteAllText("TestResults/build-failure.txt",error.ToString());Debug.LogException(error);throw;}
        }
        public static void QueueBuild(){EditorApplication.delayCall+=BuildWebGL;}
        [Serializable] sealed class BuildReceipt {public string result,unity;public long bytes;public double seconds;public int errors,warnings;}
    }
}
