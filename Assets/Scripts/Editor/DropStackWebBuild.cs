using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DropStack.Editor
{
    public static class DropStackWebBuild
    {
        public static void Configure()
        {
            PlayerSettings.productName="Drop Stack";
            PlayerSettings.bundleVersion="1.1.0";
            PlayerSettings.runInBackground=false;
            PlayerSettings.WebGL.template="PROJECT:DropStack";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.nameFilesAsHashes=true;
            PlayerSettings.WebGL.initialMemorySize=128;
            PlayerSettings.WebGL.maximumMemorySize=512;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode=true;
            var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if(pipeline!=null) { pipeline.supportsHDR=false; pipeline.msaaSampleCount=1; pipeline.supportsCameraDepthTexture=false; pipeline.supportsCameraOpaqueTexture=false; UnityEditor.EditorUtility.SetDirty(pipeline); }
        }
        [MenuItem("Drop Stack/Build WebGL")]
        public static void Build()
        {
            Configure();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
                throw new BuildFailedException("Install WebGL Build Support for Unity "+Application.unityVersion+" in Unity Hub, then run Drop Stack/Build WebGL.");
            string staging=System.IO.Path.GetFullPath("Builds/WebGL-Staging");
            string release=System.IO.Path.GetFullPath("Builds/WebGL");
            string previous=System.IO.Path.GetFullPath("Builds/WebGL-Previous");
            ValidateBuildPath(staging); ValidateBuildPath(release); ValidateBuildPath(previous);
            if(!Directory.Exists(release)&&Directory.Exists(previous)) Directory.Move(previous,release);
            DeleteTemporaryBuild(staging);
            Directory.CreateDirectory(staging);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/SampleScene.unity"}, locationPathName=staging, target=BuildTarget.WebGL, options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("WebGL build failed: "+report.summary.result);
            DeleteTemporaryBuild(previous);
            if(Directory.Exists(release)) Directory.Move(release,previous);
            try { Directory.Move(staging,release); }
            catch {
                if(!Directory.Exists(release)&&Directory.Exists(previous)) Directory.Move(previous,release);
                throw;
            }
            DeleteTemporaryBuild(previous);
            Debug.Log("[DropStack] WebGL ready at Builds/WebGL/index.html. Upload the full folder to a static host or zip its contents for itch.io.");
        }
        private static void ValidateBuildPath(string directory)
        {
            string buildsRoot=System.IO.Path.GetFullPath("Builds")+System.IO.Path.DirectorySeparatorChar;
            if(!System.IO.Path.GetFullPath(directory).StartsWith(buildsRoot,System.StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Build output must remain inside this project's Builds directory.");
        }
        private static void DeleteTemporaryBuild(string directory)
        {
            ValidateBuildPath(directory);
            string target=System.IO.Path.GetFullPath(directory);
            if(target!=System.IO.Path.GetFullPath("Builds/WebGL-Staging")&&target!=System.IO.Path.GetFullPath("Builds/WebGL-Previous"))
                throw new BuildFailedException("Only generated staging and previous-build directories can be removed.");
            if(Directory.Exists(target)) Directory.Delete(target,true);
        }
        public static void BuildSmokePlayer()
        {
            Directory.CreateDirectory("Builds/Smoke");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/SampleScene.unity"}, locationPathName="Builds/Smoke/DropStack.exe", target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("Smoke player build failed: "+report.summary.result);
        }
        public static void VerifyAndBuildSmoke()
        {
            DropStackSceneSetup.SetupScene();
            DropStackGameTests.RunAllTests();
            BuildSmokePlayer();
        }
    }
}
