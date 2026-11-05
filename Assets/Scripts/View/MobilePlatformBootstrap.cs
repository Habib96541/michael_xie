using UnityEngine;

namespace com.javierquevedo
{
    /// <summary>
    /// Runtime defaults for the WebGL / mobile build. Sprites are all camera-facing and flat,
    /// so multi-sample anti-aliasing only burns fill rate; keep the frame budget for animation
    /// instead. Runs before the first scene loads, so the (binary) scene file needs no changes.
    /// </summary>
    public static class MobilePlatformBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.antiAliasing = 0;
        }
    }
}
