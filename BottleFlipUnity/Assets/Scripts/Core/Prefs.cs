using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>
    /// Small int preference store. On WebGL it goes through localStorage instead of
    /// PlayerPrefs: PlayerPrefs rides on indexedDB, which a page opened from
    /// file:// is not allowed to use, so the best score would not survive a reload
    /// of the shared single-file build.
    /// </summary>
    public static class Prefs
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern IntPtr BF_PrefsGet(string key);

        [DllImport("__Internal")]
        static extern void BF_PrefsSet(string key, string value);

        [DllImport("__Internal")]
        static extern void BF_PrefsFree(IntPtr ptr);
#endif

        public static int GetInt(string key, int fallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                IntPtr ptr = BF_PrefsGet(key);
                if (ptr == IntPtr.Zero) return fallback;
                string raw = Marshal.PtrToStringAnsi(ptr);
                BF_PrefsFree(ptr);
                int value;
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
#else
            return PlayerPrefs.GetInt(key, fallback);
#endif
        }

        public static void SetInt(string key, int value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                BF_PrefsSet(key, value.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                // A blocked storage quota must never break the run.
            }
#else
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
#endif
        }
    }
}
