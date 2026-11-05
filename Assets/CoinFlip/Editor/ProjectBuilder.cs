using System;
using System.IO;
using System.Linq;
using CoinFlip;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CoinFlipEditor
{
    public static class ProjectBuilder
    {
        const string ScenePath="Assets/CoinFlip/Scenes/CoinFlip.unity";
        public static void ImportEssentials()
        {
            AssetDatabase.importPackageCompleted += _ => EditorApplication.delayCall += ()=>EditorApplication.Exit(0);
            TMP_PackageResourceImporter.ImportResources(true,false,false);
        }
        [MenuItem("Coin Flip/Prepare Project")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if(!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                throw new InvalidOperationException("Run CoinFlipEditor.ProjectBuilder.ImportEssentials without -quit first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles("Assets/CoinFlip/Resources/Art","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit=100;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
                if(path.Contains("Rounded"))importer.spriteBorder=new Vector4(30,30,30,30);
                importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles("Assets/CoinFlip/Resources/Audio","*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;
                // Short mono source clips are inexpensive as PCM. Unity applies its Web
                // platform conversion; the generated data bundle supplies transport compression.
                AudioImporterSampleSettings sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;sample.quality=.6f;
                sample.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;sample.sampleRateOverride=22050;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
            }
            string fontPath="Assets/CoinFlip/Resources/Fonts/Outfit SDF.asset";
            TMP_FontAsset font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null)
            {
                Font source=AssetDatabase.LoadAssetAtPath<Font>("Assets/CoinFlip/Resources/Fonts/Outfit.ttf");
                font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
                font.name="Outfit SDF";AssetDatabase.CreateAsset(font,fontPath);
                font.TryAddCharacters(string.Concat(Enumerable.Range(32,95).Select(c=>(char)c))+"−×↗…·•",out string missing);
                if(!string.IsNullOrEmpty(missing))Debug.LogWarning("Missing glyphs: "+missing);
                foreach(Texture2D atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
                AssetDatabase.AddObjectToAsset(font.material,font);font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);
            }
            string settingsPath="Assets/CoinFlip/Resources/GameSettings.asset";
            GameSettings settings=AssetDatabase.LoadAssetAtPath<GameSettings>(settingsPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<GameSettings>();AssetDatabase.CreateAsset(settings,settingsPath);}
            Directory.CreateDirectory("Assets/CoinFlip/Scenes");
            AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            // NewScene unloads unused assets; reacquire native references afterwards.
            settings=AssetDatabase.LoadAssetAtPath<GameSettings>(settingsPath);font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(settings==null||font==null)throw new InvalidOperationException("Missing required game settings or font asset");
            var cameraObject=new GameObject("2D UI Camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.allowHDR=false;camera.allowMSAA=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Palette.Background;camera.transform.position=new Vector3(0,0,-10);cameraObject.tag="MainCamera";cameraObject.AddComponent<AudioListener>();
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            if(pipeline!=null){pipeline.supportsHDR=false;pipeline.msaaSampleCount=1;EditorUtility.SetDirty(pipeline);}
            var game=new GameObject("Coin Flip");var manager=game.AddComponent<GameManager>();var serialized=new SerializedObject(manager);
            serialized.FindProperty("settings").objectReferenceValue=settings;serialized.FindProperty("font").objectReferenceValue=font;serialized.ApplyModifiedPropertiesWithoutUndo();
            if(serialized.FindProperty("settings").objectReferenceValue==null||serialized.FindProperty("font").objectReferenceValue==null)throw new InvalidOperationException("Scene references failed to bind");
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="Little Orbit";PlayerSettings.productName="Coinflip";PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.defaultWebScreenWidth=1100;PlayerSettings.defaultWebScreenHeight=740;PlayerSettings.runInBackground=false;
            PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.WebGL.template="PROJECT:CoinFlip";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Medium);
            PlayerSettings.stripEngineCode=true;PlayerSettings.SplashScreen.show=false;
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;
            AssetDatabase.SaveAssets();Debug.Log("COINFLIP_PREPARE_OK");
        }
        [MenuItem("Coin Flip/Build WebGL")]
        public static void BuildWebGL()
        {
            Prepare();Directory.CreateDirectory("Builds/WebGL");
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            string summary=JsonUtility.ToJson(new BuildReceipt{result=report.summary.result.ToString(),bytes=(long)report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings,unity=Application.unityVersion},true);
            File.WriteAllText("Logs/build-receipt.json",summary);Debug.Log(summary);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed");
            Debug.Log("COINFLIP_WEBGL_BUILD_OK");
        }
        [Serializable] class BuildReceipt{public string result,unity;public long bytes;public double seconds;public int errors,warnings;}
    }
}
