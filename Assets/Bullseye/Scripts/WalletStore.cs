using System;
using UnityEngine;

namespace BullseyeBlitz
{
    [Serializable]
    public sealed class WalletData
    {
        public int Version = 1;
        public int Wallet;
        public int Best;
        public int Completed;
        public string Settlement = "";
        public float Music = 0.35f;
        public float Sound = 0.7f;
        public bool ReducedEffects;
        public bool Vibration;
    }

    public sealed class WalletStore
    {
        private readonly string _key;
        public WalletData Data { get; private set; }
        public string Error { get; private set; }
        public WalletStore(string key = "BullseyeBlitz.Save.v1")
        {
            _key = key;
            try
            {
                Data = JsonUtility.FromJson<WalletData>(BlitzBrowser.Load(_key));
                if (Data == null || Data.Version != 1 || Data.Wallet < 0 || Data.Best < 0 || Data.Completed < 0) { Data = new WalletData(); }
                Data.Music = float.IsNaN(Data.Music) ? .35f : Mathf.Clamp01(Data.Music);
                Data.Sound = float.IsNaN(Data.Sound) ? .7f : Mathf.Clamp01(Data.Sound);
            }
            catch { Data = new WalletData(); }
        }
        public bool Save()
        {
            try { BlitzBrowser.Save(_key, JsonUtility.ToJson(Data)); Error = null; return true; }
            catch (Exception e) { Error = e.Message; return false; }
        }
        public bool Settle(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount < 0) { return false; }
            if (Data.Settlement == id) { return Save(); }
            WalletData previous = JsonUtility.FromJson<WalletData>(JsonUtility.ToJson(Data));
            try
            {
                Data.Wallet = checked(Data.Wallet + amount);
                Data.Best = Math.Max(Data.Best, amount);
                Data.Completed++;
                Data.Settlement = id;
                if (Save()) { return true; }
            }
            catch (Exception e) { Error = e.Message; }
            Data = previous;
            return false;
        }
    }
}
