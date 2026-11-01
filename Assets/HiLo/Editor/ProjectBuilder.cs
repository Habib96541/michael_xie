using System;
using System.IO;
using HiLo;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

public static class ProjectBuilder
{
    public static void PrepareAndBuild() { Prepare(); Build(); }
    [MenuItem("HiLo/Prepare project")]
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/HiLo/Resources");Directory.CreateDirectory("Validation");
        foreach(string path in Directory.GetFiles("Assets/HiLo/Resources/Art","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            if(path.EndsWith("rounded.png"))importer.spriteBorder=new Vector4(16,16,16,16);
            importer.SaveAndReimport();
        }
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/HiLo/Resources/Fonts/Outfit SDF.asset");
        if(font!=null) { AssetDatabase.DeleteAsset("Assets/HiLo/Resources/Fonts/Outfit SDF.asset"); font=null; }
        if(font==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/HiLo/Resources/Fonts/Outfit.ttf");
            font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="Outfit SDF";font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~×↑↓→∞·’♠♥♣♦–");
            AssetDatabase.CreateAsset(font,"Assets/HiLo/Resources/Fonts/Outfit SDF.asset");
            foreach(var texture in font.atlasTextures) { texture.name="Outfit atlas"; AssetDatabase.AddObjectToAsset(texture,font); }
            font.material.name="Outfit SDF Material";AssetDatabase.AddObjectToAsset(font.material,font);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            if(TMP_Settings.defaultFontAsset!=null) font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{TMP_Settings.defaultFontAsset};
            EditorUtility.SetDirty(font);
        }
        var config=AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/HiLo/Resources/GameConfig.asset");
        if(config==null){config=ScriptableObject.CreateInstance<GameConfig>();AssetDatabase.CreateAsset(config,"Assets/HiLo/Resources/GameConfig.asset");}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));cameraGO.tag="MainCamera";
        var camera=cameraGO.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=UIFactory.Hex("0B2028");camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
        // Screen-space UI is deliberately unlit; post processing never blurs card type or icons.
        cameraGO.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        var game=new GameObject("Game",typeof(GameManager));game.GetComponent<GameManager>().config=config;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/HiLo.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/HiLo.unity",true)};
        PlayerSettings.companyName="Little Instinct Studio";PlayerSettings.productName="Hi Lo — Endless";PlayerSettings.bundleVersion="1.0.0";
        PlayerSettings.SplashScreen.show=false;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Medium);
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=512;
        PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.template="PROJECT:HiLo";
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.stripEngineCode=true;
        QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;QualitySettings.shadows=UnityEngine.ShadowQuality.Disable;QualitySettings.SetQualityLevel(0,true);
        var urp=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if(urp!=null){urp.supportsHDR=false;urp.msaaSampleCount=1;urp.renderScale=1;urp.supportsCameraDepthTexture=false;urp.supportsCameraOpaqueTexture=false;EditorUtility.SetDirty(urp);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        File.WriteAllText("Validation/project-setup.txt","Unity "+Application.unityVersion+"\nURP "+(GraphicsSettings.currentRenderPipeline!=null?GraphicsSettings.currentRenderPipeline.name:"NONE")+"\nScene: Assets/Scenes/HiLo.unity\nPrepared "+DateTime.UtcNow.ToString("O"));
        Debug.Log("HILO PREPARE PASSED");
    }
    [MenuItem("HiLo/Build WebGL")]
    public static void Build()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/HiLo.unity"},locationPathName="WebGL_Build",target=BuildTarget.WebGL,options=BuildOptions.None});
        string summary=$"Result: {report.summary.result}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nUnity: {Application.unityVersion}\n";
        File.WriteAllText("Validation/build-report.txt",summary);Debug.Log(summary);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed");
    }
}
