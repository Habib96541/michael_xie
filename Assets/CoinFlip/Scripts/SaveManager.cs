using System;
using UnityEngine;

namespace CoinFlip
{
    public static class SaveManager
    {
        public const string Key = "CoinFlip.Save.v1";
        public static SaveData Load()
        {
            try
            {
                if (!PlayerPrefs.HasKey(Key)) return new SaveData();
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                return data != null && data.IsValid() ? data : new SaveData();
            }
            catch (Exception) { return new SaveData(); }
        }
        public static void Save(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save(); // Unity's Web backend synchronizes PlayerPrefs to IndexedDB.
        }
    }
}
