using System;
using System.Collections.Generic;

namespace NeonDash.Core
{
    /// <summary>Best result recorded for one seed.</summary>
    [Serializable]
    public sealed class SeedRecord
    {
        public string Seed = "0000";
        public string Name = "PLAYER";
        public int BestDistance;
        public float BestProgress;
        public float BestMultiplier;
        public int BestPayout;
        public int Attempts;
        public int LastDistance;
        public float LastMultiplier;
        public int LastPayout;
    }

    /// <summary>
    /// Per-seed record book. Survives between sessions (the Unity layer serialises this with
    /// JsonUtility and keeps it in PlayerPrefs).
    /// </summary>
    [Serializable]
    public sealed class RecordTable
    {
        public const int MaxRecords = 60;
        public List<SeedRecord> Records = new List<SeedRecord>();

        public SeedRecord Find(string seed)
        {
            if (string.IsNullOrEmpty(seed)) return null;
            for (int i = 0; i < Records.Count; i++)
                if (Records[i] != null && Records[i].Seed == seed) return Records[i];
            return null;
        }

        /// <summary>
        /// Records one finished attempt. Returns true when it beat the stored best distance
        /// for that seed.
        /// </summary>
        public bool Submit(string seed, string playerName, int distance, float multiplier, int payout)
        {
            if (string.IsNullOrEmpty(seed)) seed = "0000";
            SeedRecord record = Find(seed);
            if (record == null)
            {
                record = new SeedRecord();
                record.Seed = seed;
                Records.Add(record);
            }

            record.Attempts++;
            record.LastDistance = distance;
            record.LastMultiplier = multiplier;
            record.LastPayout = payout;

            if (distance > record.BestDistance)
            {
                record.BestDistance = distance;
                record.BestProgress = ScoreRules.Progress(distance, GameConstants.CourseLength);
                record.BestMultiplier = multiplier;
                record.BestPayout = payout;
                record.Name = CleanName(playerName);
                Trim();
                return true;
            }

            if (distance == record.BestDistance && payout > record.BestPayout)
            {
                record.BestMultiplier = multiplier;
                record.BestPayout = payout;
                record.Name = CleanName(playerName);
            }

            Trim();
            return false;
        }

        /// <summary>Best runs first, used by the records screen.</summary>
        public List<SeedRecord> Best(int count)
        {
            var sorted = new List<SeedRecord>(Records);
            sorted.Sort(delegate (SeedRecord a, SeedRecord b)
            {
                if (a.BestDistance != b.BestDistance) return b.BestDistance.CompareTo(a.BestDistance);
                return b.BestPayout.CompareTo(a.BestPayout);
            });
            if (sorted.Count > count) sorted.RemoveRange(count, sorted.Count - count);
            return sorted;
        }

        /// <summary>Most recently played seeds first.</summary>
        public List<SeedRecord> RecentSeeds(int count)
        {
            var list = new List<SeedRecord>(Records);
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        public void Remove(string seed)
        {
            for (int i = Records.Count - 1; i >= 0; i--)
                if (Records[i] != null && Records[i].Seed == seed) Records.RemoveAt(i);
        }

        void Trim()
        {
            if (Records.Count <= MaxRecords) return;
            // Drop the seed with the shortest best distance first.
            int worst = 0;
            for (int i = 1; i < Records.Count; i++)
                if (Records[i].BestDistance < Records[worst].BestDistance) worst = i;
            Records.RemoveAt(worst);
        }

        static string CleanName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return "PLAYER";
            string trimmed = playerName.Trim();
            if (trimmed.Length == 0) return "PLAYER";
            if (trimmed.Length > 12) trimmed = trimmed.Substring(0, 12);
            return trimmed;
        }
    }
}
