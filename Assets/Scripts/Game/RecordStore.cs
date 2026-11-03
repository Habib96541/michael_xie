using NeonDash.Core;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// Local persistence for the per-seed record book, the player name and the last seed used.
    /// WebGL PlayerPrefs are backed by browser storage, so records survive a reload.
    /// </summary>
    public sealed class RecordStore
    {
        // v2: multipliers are earned now, so records saved under the old rolled-multiplier rules
        // are not comparable and start fresh. Name, seed and mute settings are kept.
        const string RecordsKey = "NeonDash.Records.v2";
        const string NameKey = "NeonDash.PlayerName";
        const string SeedKey = "NeonDash.LastSeed";
        const string MuteKey = "NeonDash.Muted";

        public RecordTable Table { get; private set; }
        public string PlayerName { get; private set; }
        public string LastSeed { get; private set; }
        public bool Muted { get; private set; }

        public RecordStore()
        {
            Table = new RecordTable();
            PlayerName = "PLAYER";
            LastSeed = "2026";
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(RecordsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    RecordTable loaded = JsonUtility.FromJson<RecordTable>(json);
                    if (loaded != null && loaded.Records != null) Table = loaded;
                }
                catch
                {
                    Table = new RecordTable();
                }
            }

            PlayerName = Sanitise(PlayerPrefs.GetString(NameKey, "PLAYER"), 12, "PLAYER");
            LastSeed = Sanitise(PlayerPrefs.GetString(SeedKey, "2026"), 10, "2026");
            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        }

        public void Save()
        {
            PlayerPrefs.SetString(RecordsKey, JsonUtility.ToJson(Table));
            PlayerPrefs.SetString(NameKey, PlayerName);
            PlayerPrefs.SetString(SeedKey, LastSeed);
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetPlayerName(string value)
        {
            PlayerName = Sanitise(value, 12, "PLAYER");
        }

        public void SetSeed(string value)
        {
            LastSeed = Sanitise(value, 10, "2026");
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
        }

        public static string Sanitise(string value, int maxLength, string fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            string trimmed = value.Trim();
            if (trimmed.Length == 0) return fallback;
            if (trimmed.Length > maxLength) trimmed = trimmed.Substring(0, maxLength);
            return trimmed;
        }

        /// <summary>Seeds are digits only so a seed is easy to type and share.</summary>
        public static string SanitiseSeed(string value)
        {
            if (string.IsNullOrEmpty(value)) return "2026";
            string digits = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c >= '0' && c <= '9' && digits.Length < 10) digits += c;
            }
            return digits.Length == 0 ? "2026" : digits;
        }
    }
}
