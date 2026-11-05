using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BackyardDojo.Editor
{
    public static class CloudflareWebBuild
    {
        [MenuItem("Backyard Dojo/Build web player for Cloudflare")]
        public static void Build()
        {
            string output=Environment.GetEnvironmentVariable("DOJO_WEB_OUTPUT") ?? Path.GetFullPath("Builds/WebGL");
            string artifacts=Environment.GetEnvironmentVariable("DOJO_WEB_ARTIFACTS") ?? Path.GetFullPath("Artifacts");
            Directory.CreateDirectory(artifacts);
            Directory.CreateDirectory(output);
            PlayerSettings.productName="Juice Dojo";
            PlayerSettings.bundleVersion="1.3";
            PlayerSettings.defaultWebScreenWidth=1953;PlayerSettings.defaultWebScreenHeight=900;
            PlayerSettings.WebGL.template="PROJECT:BackyardDojo";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback=false;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.nameFilesAsHashes=true;
            PlayerSettings.WebGL.initialMemorySize=128;
            PlayerSettings.WebGL.maximumMemorySize=512;
            PlayerSettings.stripEngineCode=true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Medium);
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/BackyardDojo.unity"},
                locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None });
            string result=report.summary.result+" | "+report.summary.totalErrors+" errors | "+report.summary.totalSize+" bytes";
            File.WriteAllText(Path.Combine(artifacts,"web-build-result.txt"),result);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed: "+result);

            File.WriteAllText(Path.Combine(output,"_headers"),
                "/*\n  X-Content-Type-Options: nosniff\n  Referrer-Policy: strict-origin-when-cross-origin\n"+
                "/Build/*.wasm.br\n  Content-Type: application/wasm\n  Content-Encoding: br\n  Cache-Control: public, max-age=31536000, immutable\n"+
                "/Build/*.js.br\n  Content-Type: application/javascript\n  Content-Encoding: br\n  Cache-Control: public, max-age=31536000, immutable\n"+
                "/Build/*.data.br\n  Content-Type: application/octet-stream\n  Content-Encoding: br\n  Cache-Control: public, max-age=31536000, immutable\n"+
                "/index.html\n  Cache-Control: no-cache\n");
            File.WriteAllText(Path.Combine(output,"404.html"),"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>Back to the garden</title><body style=\"background:#10291f;color:#fff0be;font:24px sans-serif;text-align:center;padding:15vh 20px\"><h1>Lost in the orchard?</h1><a style=\"color:#b4d359\" href=\"/\">Back to Juice Dojo</a></body></html>");
            foreach(string path in Directory.GetFiles(output,"*",SearchOption.AllDirectories))
                if(new FileInfo(path).Length>25L*1024*1024)throw new Exception("Cloudflare Pages asset exceeds 25 MiB: "+path);
            UnityEngine.Debug.Log("BACKYARD_DOJO_CLOUDFLARE_WEB_BUILD_OK: "+result);
        }
    }
}
