using UnityEngine;
using System.Runtime.InteropServices;

namespace DropStack.UI
{
    public static class ArcadeFeedback
    {
        private static float lastPulse=-1;
        public static bool MotionEnabled => PlayerPrefs.GetInt("DropStack_Motion",1)!=0;
        public static bool HapticsEnabled => PlayerPrefs.GetInt("DropStack_Haptics",1)!=0;
        public static bool HapticsSupported {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return DS_HapticsAvailable()!=0;
#elif UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        public static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!PlayerPrefs.HasKey("DropStack_Motion") && DS_ReducedMotion()!=0) PlayerPrefs.SetInt("DropStack_Motion",0);
#endif
        }
        public static void Pulse(int milliseconds=15)
        {
            if(!HapticsEnabled || Time.unscaledTime-lastPulse<.09f) return;
            lastPulse=Time.unscaledTime;
#if UNITY_WEBGL && !UNITY_EDITOR
            DS_Vibrate(Mathf.Clamp(milliseconds,1,90));
#elif UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
        public static void RequestFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DS_RequestFullscreen();
#endif
        }
        public static void Cancel()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DS_Vibrate(0);
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int DS_HapticsAvailable();
        [DllImport("__Internal")] private static extern int DS_ReducedMotion();
        [DllImport("__Internal")] private static extern void DS_Vibrate(int duration);
        [DllImport("__Internal")] private static extern void DS_RequestFullscreen();
#endif
    }
}
