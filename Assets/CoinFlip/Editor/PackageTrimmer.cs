using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CoinFlipEditor
{
    public static class PackageTrimmer
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Trim()
        {
            request=Client.AddAndRemove(Array.Empty<string>(),new[]{
                "com.unity.2d.animation","com.unity.2d.aseprite","com.unity.2d.psdimporter","com.unity.2d.spriteshape",
                "com.unity.2d.tilemap","com.unity.2d.tilemap.extras","com.unity.2d.tooling","com.unity.collab-proxy",
                "com.unity.ide.rider","com.unity.ide.visualstudio","com.unity.multiplayer.center","com.unity.timeline","com.unity.visualscripting"});
            deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(!request.IsCompleted){if(EditorApplication.timeSinceStartup>deadline)EditorApplication.Exit(2);return;}
            EditorApplication.update-=Poll;
            if(request.Status!=StatusCode.Success){Debug.LogError(request.Error.message);EditorApplication.Exit(1);return;}
            Debug.Log("COINFLIP_PACKAGES_TRIMMED");EditorApplication.Exit(0);
        }
    }
}
