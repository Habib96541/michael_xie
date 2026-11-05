using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Run only inside the disposable deployment copy. Never regenerates the scene.
public static class BuildCloudflareWebGL
{
    public static void Run()
    {
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!project.Replace('\\','/').EndsWith("/Logs/SpriteValidation",StringComparison.OrdinalIgnoreCase))
            throw new Exception("Web export must run in the isolated deployment copy.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
            throw new Exception("Unity WebGL build support is not fully installed.");
        string outputName=Environment.GetEnvironmentVariable("LITTLE_DEPTHS_EXPORT_FOLDER") ?? "CloudflareWebGL";
        if(!outputName.StartsWith("CloudflareWebGL",StringComparison.Ordinal) || Path.GetFileName(outputName)!=outputName)
            throw new Exception("Use a CloudflareWebGL export folder under Logs.");
        string output=Path.GetFullPath(Path.Combine(project,"..",outputName));
        Directory.CreateDirectory(output);
        PlayerSettings.bundleVersion="1.1";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.threadsSupport=false;
        PlayerSettings.WebGL.initialMemorySize=128;
        PlayerSettings.WebGL.maximumMemorySize=2048;
        PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.nameFilesAsHashes=true;
        PlayerSettings.runInBackground=true;
        PlayerSettings.defaultWebScreenWidth=1600;
        PlayerSettings.defaultWebScreenHeight=900;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Low);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL,Il2CppCompilerConfiguration.Release);
        // These overrides affect this copy only. Preserve full native UI detail;
        // compress world sprites for the browser's texture memory budget.
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/LittleDepths/Art/")&&p.EndsWith(".png")&&!p.Contains("RemasteredSheets")))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer||importer.textureType!=TextureImporterType.Sprite)continue;
            var platform=importer.GetPlatformTextureSettings("WebGL");
            var format=path.Contains("/Provided/")?TextureImporterFormat.RGBA32:TextureImporterFormat.DXT5Crunched;
            if(platform.overridden && platform.maxTextureSize==2048 && platform.format==format &&
                platform.textureCompression==TextureImporterCompression.CompressedHQ && platform.compressionQuality==100)continue;
            platform.name="WebGL";platform.overridden=true;platform.maxTextureSize=2048;
            platform.format=format;
            platform.textureCompression=TextureImporterCompression.CompressedHQ;
            platform.compressionQuality=100;
            importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/LittleDepths.unity"},locationPathName=output,
            target=BuildTarget.WebGL,subtarget=(int)WebGLTextureSubtarget.DXT,options=BuildOptions.None});
        File.WriteAllText(Path.Combine(project,"../CloudflareBuild.status"),report.summary.result+" | "+report.summary.totalSize+" bytes | "+report.summary.totalTime);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        Debug.Log("CLOUDFLARE_WEBGL_BUILD_OK: "+output);
    }
}
