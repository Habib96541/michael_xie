using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineGrid
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 3;
        public int coins;
        public int totalPoints;
        public int rounds;
        public int selectedMines = 3;
        public bool muted;
        public bool reducedEffects;
        public long nextSpin;
        public int pendingCoins;
        public string lastRoundId;
        public List<int> claimed = new List<int>();
        public List<string> ownedCosmetics = new List<string>();
        public List<EquippedCosmetic> equippedCosmetics = new List<EquippedCosmetic>();
    }

    public sealed class SaveService
    {
        public const string Key = "MineGrid.Save.v3";
        private const string PreviousKey = "MineGrid.Save.v2";
        private const string LegacyKey = "MineGrid.Save.v1";
        private readonly Func<long> _utcNow;
        public SaveData Data { get; private set; }

        public SaveService(Func<long> clock = null)
        {
            _utcNow = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            bool migrating = !PlayerPrefs.HasKey(Key) && !PlayerPrefs.HasKey(PreviousKey) && PlayerPrefs.HasKey(LegacyKey);
            string sourceKey = PlayerPrefs.HasKey(Key) ? Key : PlayerPrefs.HasKey(PreviousKey) ? PreviousKey : LegacyKey;
            try
            {
                Data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(sourceKey, ""));
            }
            catch
            {
            }

            if (Data == null || Data.version > 3)
            {
                Data = new SaveData();
            }

            // Shop migration retains earned balances and legacy claim records.
            if (migrating)
            {
                if (Data.selectedMines < 1 || Data.selectedMines > 24)
                {
                    Data.selectedMines = 3;
                }
            }

            Data.version = 3;
            Data.coins = Mathf.Max(0, Data.coins);
            Data.totalPoints = Mathf.Max(0, Data.totalPoints);
            Data.rounds = Mathf.Max(0, Data.rounds);
            Data.selectedMines = Mathf.Clamp(Data.selectedMines, 1, 24);
            Data.nextSpin = Math.Max(0, Data.nextSpin);
            Data.pendingCoins = Mathf.Clamp(Data.pendingCoins, 0, 1000000);
            if (Data.claimed == null)
            {
                Data.claimed = new List<int>();
            }

            Data.claimed = new List<int>(new HashSet<int>(Data.claimed.FindAll(i => i >= 0)));
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        private static int Add(int a, int b) => (int)Math.Min(int.MaxValue, (long)a + Math.Max(0, b));
        public bool Bank(int points, string roundId)
        {
            if (string.IsNullOrEmpty(roundId) || Data.lastRoundId == roundId)
            {
                return false;
            }

            points = Math.Max(0, points);
            Data.lastRoundId = roundId;
            Data.totalPoints = Add(Data.totalPoints, points);
            Data.coins = Add(Data.coins, points);
            Data.rounds = Add(Data.rounds, 1);
            Save();
            return true;
        }

        public long SpinSecondsLeft => Math.Max(0, Data.nextSpin - _utcNow());

        public bool Spin(GameConfig config, System.Random random, out int segment)
        {
            segment = -1;
            if (SpinSecondsLeft > 0 || Data.pendingCoins > 0 || config.wheelCoins.Length == 0)
            {
                return false;
            }

            segment = random.Next(config.wheelCoins.Length);
            Data.pendingCoins = Mathf.Clamp(config.wheelCoins[segment], 1, 1000000);
            Data.nextSpin = _utcNow() + 86400;
            Save();
            return true;
        }

        public bool ClaimPending()
        {
            if (Data.pendingCoins <= 0)
            {
                return false;
            }

            Data.coins = Add(Data.coins, Data.pendingCoins);
            Data.pendingCoins = 0;
            Save();
            return true;
        }

        public bool Claim(GameConfig config, int index)
        {
            if (index < 0 || index >= config.milestonePoints.Length || index >= config.milestoneCoins.Length || Data.totalPoints < config.milestonePoints[index] || Data.claimed.Contains(index))
            {
                return false;
            }

            Data.claimed.Add(index);
            Data.coins = Add(Data.coins, config.milestoneCoins[index]);
            Save();
            return true;
        }
    }
}
