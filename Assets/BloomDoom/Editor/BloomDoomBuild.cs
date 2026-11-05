#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BloomDoom.Editor
{
    public static class BloomDoomBuild
    {
        [MenuItem("Bloom & Doom/Configure Native Game Scene")]
        public static void ConfigureScene()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles("Assets/Resources/BloomDoom/Art","*.png")){
                var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(importer==null)continue;
                bool changed=importer.mipmapEnabled||importer.maxTextureSize!=4096||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.textureType!=TextureImporterType.Default;
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;if(changed)importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera");camera.tag="MainCamera";var cam=camera.AddComponent<Camera>();cam.orthographic=true;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=GardenAssets.Color("#f6f1df");cam.clearFlags=CameraClearFlags.SolidColor;camera.AddComponent<AudioListener>();
            new GameObject("Bloom & Doom - Native Game").AddComponent<BloomDoomGame>();
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;int previous=QualitySettings.GetQualityLevel();for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=null;}QualitySettings.SetQualityLevel(previous,false);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/SampleScene.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",true)};
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            PlayerSettings.productName="Bloom & Doom";PlayerSettings.bundleVersion="2.1.2";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=1010;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
            AssetDatabase.SaveAssets();Debug.Log("[BloomDoom] Native Unity scene saved. Editor Play and WebGL both run BloomDoomGame.");
        }
        [MenuItem("Bloom & Doom/Run Gameplay Checks")]
        public static void Verify()=>GardenChecks.Run();
        public static void ConfigureAndVerify(){ConfigureScene();Verify();}
        [MenuItem("Bloom & Doom/Build WebGL")]
        public static void BuildWeb()
        {
            ConfigureAndVerify();if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new BuildFailedException("Open this project with the WebGL-capable Unity editor. Tools/Build-Web.ps1 locates it automatically.");
            PlayerSettings.WebGL.template="PROJECT:BloomDoom";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.nameFilesAsHashes=true;PlayerSettings.WebGL.initialMemorySize=256;PlayerSettings.WebGL.maximumMemorySize=512;PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;PlayerSettings.stripEngineCode=true;
            Directory.CreateDirectory("Builds");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Unity WebGL build failed: "+report.summary.result);Debug.Log("[BloomDoom] Genuine Unity WebGL build ready at Builds/WebGL.");
            string loadingAssets=Path.Combine("Builds/WebGL","TemplateData");Directory.CreateDirectory(loadingAssets);
            File.Copy("Assets/Resources/BloomDoom/Art/title_logo.png",Path.Combine(loadingAssets,"bloom-doom-logo.png"),true);
            File.Copy("Assets/Resources/BloomDoom/Art/icon_sunbud.png",Path.Combine(loadingAssets,"home-screen-icon.png"),true);
            File.Copy("Assets/Resources/BloomDoom/Fonts/LilitaOne-Regular.ttf",Path.Combine(loadingAssets,"garden-title.ttf"),true);
            File.Copy("Assets/Resources/BloomDoom/Fonts/FONT-LICENSE.txt",Path.Combine(loadingAssets,"garden-title-license.txt"),true);
        }
        public static void BuildSmoke()
        {
            ConfigureAndVerify();Directory.CreateDirectory("Builds/Smoke");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},locationPathName="Builds/Smoke/BloomDoom.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Native smoke build failed.");
        }
    }
}
#endif
