using System;
using System.Globalization;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>
    /// One-way reporter that mirrors the game state into window.__bottleFlipState so the
    /// WebGL build can be inspected automatically. No gameplay code depends on it.
    /// </summary>
    public static class WebBridge
    {
        public static bool Enabled = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void BF_Report(string json);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void BF_Log(string msg);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void BF_UploadPng(string name, byte[] data, int length);
#endif

        /// <summary>Console log line visible in the browser dev tools.</summary>
        public static void Log(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Enabled) return;
            try { BF_Log(message); } catch (Exception) { }
#endif
        }

        /// <summary>Sends a PNG to the local screenshot receiver (playtest tooling only).</summary>
        public static void UploadPng(string name, byte[] png)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (png == null || png.Length == 0) return;
            try { BF_UploadPng(name, png, png.Length); } catch (Exception) { }
#endif
        }

        public static void Report(string phase, int flips, int best, string lastEvent)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Enabled) return;
            try
            {
                string json = "{\"phase\":\"" + phase + "\",\"flips\":" + flips
                            + ",\"best\":" + best
                            + ",\"last\":\"" + lastEvent + "\""
                            + ",\"t\":" + Time.realtimeSinceStartup.ToString("0.000", CultureInfo.InvariantCulture)
                            + "}";
                BF_Report(json);
            }
            catch (Exception)
            {
                // Reporting must never break the game.
            }
#endif
        }
    }
}
