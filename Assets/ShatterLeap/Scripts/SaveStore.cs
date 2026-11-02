using System;
using UnityEngine;

namespace ShatterLeap
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public int Coins;
        public int Difficulty;
        public float Music = 0.35f;
        public float Sound = 0.7f;
        public bool Vibration = true;
        public bool ReducedEffects;
        public string LastPaidRun = "";
    }

    public sealed class SaveStore
    {
        public const string Key = "ShatterLeap.Save.v1";
        public SaveData Data { get; private set; }
        private readonly Action<string> _write;

        public SaveStore(string json, Action<string> write)
        {
            _write = write;
            Data = Decode(json);
        }
        public static SaveData Decode(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) { return new SaveData(); }
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.Version != 1 || data.Coins < 0 || data.Difficulty < 0 || data.Difficulty > 2 ||
                    float.IsNaN(data.Music) || float.IsInfinity(data.Music) || float.IsNaN(data.Sound) || float.IsInfinity(data.Sound))
                {
                    return new SaveData();
                }
                data.Music = Mathf.Clamp01(data.Music);
                data.Sound = Mathf.Clamp01(data.Sound);
                data.LastPaidRun ??= "";
                return data;
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }
        public void Save() => _write(JsonUtility.ToJson(Data));
        public bool Bank(RunRules run)
        {
            if (run.Outcome != RunState.Won || run.Payout <= 0 || Data.LastPaidRun == run.Id) { return false; }
            // The active run is never serialized. Only this guarded terminal transaction pays it.
            Data.Coins = checked(Data.Coins + run.Payout);
            Data.LastPaidRun = run.Id;
            Save();
            return true;
        }
    }
}
