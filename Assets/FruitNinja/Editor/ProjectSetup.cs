using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackyardDojo.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Backyard Dojo/Set up game scene")]
        public static void Configure()
        {
            ConfigureTexture("Assets/FruitNinja/Resources/Art/Orchard.png");
            ConfigureTexture("Assets/FruitNinja/Resources/Art/FruitAtlas.png");
            ConfigureTexture("Assets/FruitNinja/Resources/Art/JuiceDojoLogo.png");
            PlayerSettings.companyName="Backyard Dojo";
            PlayerSettings.productName="Juice Dojo";
            PlayerSettings.defaultScreenWidth=1953;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.defaultWebScreenWidth=1953;PlayerSettings.defaultWebScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";cameraObject.transform.position=new Vector3(0,0,-10);
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.09f,.065f);
            new GameObject("Juice Dojo",typeof(FruitNinjaGame));
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/BackyardDojo.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/BackyardDojo.unity",true)};
            AssetDatabase.SaveAssets();
            Debug.Log("BACKYARD_DOJO_SETUP_OK");
        }
        static void ConfigureTexture(string path)
        {
            if(!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        [MenuItem("Backyard Dojo/Build Windows player")]
        public static void Build()
        {
            Configure();
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/BackyardDojo.unity"},
                locationPathName="Builds/Windows/Fruit Ninja.exe",
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            File.WriteAllText("Artifacts/build-result.txt",report.summary.result+" | "+report.summary.totalErrors+" errors | "+report.summary.totalSize+" bytes");
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Player build failed");
        }
    }

    // A file-based local bridge lets the project be verified without changing the user's editor layout.
    [InitializeOnLoad]
    static class EditorVerification
    {
        static double next;
        static bool smokePending=SessionState.GetBool("Dojo.SmokePending",false);
        static double captureAt;
        static string capturePath;
        static EditorVerification() { EditorApplication.update+=Tick; }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next || EditorApplication.isCompiling) return;
            next=EditorApplication.timeSinceStartup+.3;
            string commandPath=Path.Combine(Directory.GetCurrentDirectory(),"Artifacts/editor-command.txt");
            if(File.Exists(commandPath))
            {
                string cmd=File.ReadAllText(commandPath).Trim();File.Delete(commandPath);
                if(cmd=="setup") ProjectSetup.Configure();
                if(cmd=="refresh") AssetDatabase.Refresh();
                if(cmd=="quit") {EditorSceneManager.SaveOpenScenes();EditorApplication.Exit(0);}
                if(cmd=="play") { smokePending=false;SessionState.SetBool("Dojo.SmokePending",false);EditorApplication.isPlaying=true; }
                if(cmd=="smoke") { smokePending=true;SessionState.SetBool("Dojo.SmokePending",true);EditorApplication.isPlaying=true; }
                if(cmd=="stop") EditorApplication.isPlaying=false;
                if(cmd=="capture" && Application.isPlaying) ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts/menu.png"));
                if(cmd=="ui-smoke" && FruitNinjaGame.Instance!=null) FruitNinjaGame.Instance.RunUiSmokeTest(result=>File.WriteAllText("Artifacts/ui-result.txt",result));
                if(cmd=="polish-smoke" && FruitNinjaGame.Instance!=null && FruitNinjaGame.Instance.IsReady)
                    FruitNinjaGame.Instance.RunPolishSmokeTest(result=>File.WriteAllText("Artifacts/polish-result.txt",result));
                if(cmd=="gameplay-capture" && FruitNinjaGame.Instance!=null)
                {
                    FruitNinjaGame.Instance.StartRound(FruitNinjaGame.GameMode.Zen);
                    captureAt=EditorApplication.timeSinceStartup+3.6;capturePath=Path.GetFullPath("Artifacts/gameplay.png");
                }
                if(cmd=="arcade" && FruitNinjaGame.Instance!=null) FruitNinjaGame.Instance.StartRound(FruitNinjaGame.GameMode.Arcade);
                if(cmd=="zen" && FruitNinjaGame.Instance!=null) FruitNinjaGame.Instance.StartRound(FruitNinjaGame.GameMode.Zen);
                if(cmd=="menu" && FruitNinjaGame.Instance!=null) FruitNinjaGame.Instance.ShowMenu();
                if(cmd=="build" && !Application.isPlaying) ProjectSetup.Build();
            }
            if(captureAt>0 && EditorApplication.timeSinceStartup>=captureAt && Application.isPlaying)
            { captureAt=0;ScreenCapture.CaptureScreenshot(capturePath); }
            if(smokePending && Application.isPlaying && FruitNinjaGame.Instance!=null && FruitNinjaGame.Instance.IsReady)
            {
                smokePending=false;
                SessionState.SetBool("Dojo.SmokePending",false);
                FruitNinjaGame.Instance.RunSmokeTest(result=>{
                    File.WriteAllText("Artifacts/smoke-result.txt",result);
                    Debug.Log("BACKYARD_DOJO_SMOKE: "+result);
                });
            }
        }
    }
}
