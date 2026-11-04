using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BullseyeBlitz
{
    public static class BlitzBrowser
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BlitzTelemetry(string json);
        [DllImport("__Internal")] private static extern void BlitzVibrate();
        [DllImport("__Internal")] private static extern int BlitzHasVibration();
        [DllImport("__Internal")] private static extern int BlitzIsQa();
        [DllImport("__Internal")] private static extern int BlitzSave(string key, string value);
        [DllImport("__Internal")] private static extern string BlitzLoad(string key);
#endif
        public static bool IsQa
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return BlitzIsQa() == 1;
#else
                return false;
#endif
            }
        }

        public static string Load(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return BlitzLoad(key);
#else
            return PlayerPrefs.GetString(key, "");
#endif
        }
        public static void Save(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (BlitzSave(key, value) != 1) { throw new System.IO.IOException("Browser storage is unavailable or full."); }
#else
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
#endif
        }
        public static bool SupportsVibration
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return BlitzHasVibration() == 1;
#else
                return false;
#endif
            }
        }
        public static void Vibrate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BlitzVibrate();
#endif
        }
        public static void Publish(Snapshot snapshot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BlitzTelemetry(JsonUtility.ToJson(snapshot));
#endif
        }
        [Serializable]
        public sealed class Snapshot
        {
            public string State;
            public bool Paused;
            public double Time;
            public int Pending;
            public int Wallet;
            public int Completed;
            public int Best;
            public int Frames;
            public float Fps;
            public Slot[] Slots;
            public UiLabel[] Labels;
        }
        [Serializable]
        public sealed class UiLabel
        {
            public string Text;
            public int Vertices;
            public int Characters;
            public int FontSize;
            public float Left;
            public float Bottom;
            public float Right;
            public float Top;
        }
        [Serializable]
        public sealed class Slot
        {
            public string State;
            public bool Bomb;
            public int Reward;
            public int Activation;
            public float X;
            public float Y;
            public float Diameter;
            public bool Collider;
        }
    }
}
