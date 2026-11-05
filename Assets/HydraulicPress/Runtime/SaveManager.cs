using System;
using UnityEngine;

namespace HydraulicPress
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public long bank;
        public long totalCrushed;
        public long bestRound;
        public double bestMultiplier = 1;
        public float sound = .8f;
        public float music = .3f;
        public bool shake = true;
        public bool lowGraphics;
    }

    public static class SaveManager
    {
        private const string Key = "hydraulic-press.save.v1";
        public static SaveData Load()
        {
            if (TryParse(PlayerPrefs.GetString(Key, ""), out var data)) return data;
            if (TryParse(PlayerPrefs.GetString(Key + ".backup", ""), out data)) return data;
            return new SaveData();
        }

        public static bool TryParse(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{")) return false;
            try
            {
                var value = JsonUtility.FromJson<SaveData>(json);
                if (value == null || value.version != 1 || value.bank < 0 || value.totalCrushed < 0 || value.bestRound < 0 ||
                    double.IsNaN(value.bestMultiplier) || double.IsInfinity(value.bestMultiplier) || value.bestMultiplier < 1 ||
                    float.IsNaN(value.sound) || float.IsInfinity(value.sound) || float.IsNaN(value.music) || float.IsInfinity(value.music)) return false;
                value.sound = Mathf.Clamp01(value.sound);
                value.music = Mathf.Clamp01(value.music);
                data = value;
                return true;
            }
            catch (Exception) { return false; }
        }

        public static void Write(SaveData data)
        {
            try
            {
                string old = PlayerPrefs.GetString(Key, "");
                if (TryParse(old, out _)) PlayerPrefs.SetString(Key + ".backup", old);
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (Exception) { /* Browser storage can be unavailable in private browsing. The round remains playable. */ }
        }

        public static long AddSaturated(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;
    }
}
