using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleDepths
{
    [Serializable]
    public class GameProgress
    {
        public const string SaveKey = "LittleDepths.Progress.v1";
        public int version = 1;
        public int coins, bestHaul, capacityLevel, depthLevel, incomeLevel, speedLevel;
        public int totalFish, totalCasts, sessionCasts, sessionCoins, sessionFish;
        public int selectedHook, collectionMilestones;
        public int outfit, hat, hairstyle, boatStyle;
        public bool sound = true;
        public bool music = true;
        public float deepest;
        public long lastSeen;
        public double passiveRemainder;
        public List<string> discovered = new List<string>();
        public List<int> unlockedHooks = new List<int> { 0 };
        public long prizeReadyAt;
        public int prizeLastCard,prizeLastReward;
        public DailyQuestProgress dailyQuests=new DailyQuestProgress();

        public static GameProgress Load(string key = SaveKey)
        {
            GameProgress p;
            try { p = JsonUtility.FromJson<GameProgress>(PlayerPrefs.GetString(key, "")) ?? new GameProgress(); }
            catch { p = new GameProgress(); }
            p.discovered ??= new List<string>();
            p.unlockedHooks ??= new List<int> { 0 };
            if (!p.unlockedHooks.Contains(0)) p.unlockedHooks.Add(0);
            p.coins = Math.Max(0, p.coins);
            p.capacityLevel = Mathf.Clamp(p.capacityLevel, 0, 20);
            p.depthLevel = Mathf.Clamp(p.depthLevel, 0, 20);
            p.incomeLevel = Mathf.Clamp(p.incomeLevel, 0, 15);
            p.speedLevel = Mathf.Clamp(p.speedLevel, 0, 10);
            p.sessionCasts = Mathf.Clamp(p.sessionCasts, 0, 5);
            p.selectedHook = p.unlockedHooks.Contains(p.selectedHook) ? p.selectedHook : 0;
            p.outfit=Mathf.Clamp(p.outfit,0,2);p.hat=Mathf.Clamp(p.hat,0,3);p.hairstyle=Mathf.Clamp(p.hairstyle,0,2);
            p.boatStyle=Mathf.Clamp(p.boatStyle,0,2);
            p.dailyQuests??=new DailyQuestProgress();p.dailyQuests.EnsureDay(Now);
            return p;
        }

        public int AccruePassive(FishingConfig config, long now)
        {
            if (lastSeen == 0) { lastSeen = now; return 0; }
            long elapsed = Math.Clamp(now - lastSeen, 0L, config.offlineCapHours * 3600L);
            double earned = elapsed / 60.0 * config.IncomePerMinute(this) + passiveRemainder;
            int whole = (int)Math.Min(earned, int.MaxValue / 4);
            passiveRemainder = earned - whole;
            coins = (int)Math.Min((long)coins + whole, int.MaxValue / 2);
            lastSeen = now;
            return whole;
        }

        public void Save(string key = SaveKey)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
