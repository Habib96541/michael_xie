using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace PegRush
{
    [Serializable]
    public sealed class DropTransaction
    {
        public string Id;
        public long Amount;
        public int Rows;
        public int Profile;
        public int[] Table;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public long Coins = 100000;
        public bool Muted;
        public bool Reduced;
        public List<DropTransaction> Pending = new List<DropTransaction>();
    }

    public sealed class Wallet
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string PegRushRead();
        [DllImport("__Internal")]
        private static extern int PegRushWrite(string json);
#endif
        public bool StorageAvailable { get; private set; } = true;
        private const string Key = "PegRush.Save.v1";
        private readonly Action<string> _write;
        public SaveData Data { get; private set; }

        public Wallet(string json, Action<string> write)
        {
            _write = write;
            try
            {
                Data = JsonUtility.FromJson<SaveData>(json ?? "");
                if (string.IsNullOrEmpty(json) || !json.Contains("\"Coins\":") || !json.Contains("\"Pending\":") || Data == null || Data.Version != 1 || Data.Coins < 0 || Data.Coins > 100000000000L || Data.Pending == null) throw new Exception("Invalid save");
                HashSet<string> ids = new HashSet<string>();
                foreach (DropTransaction pending in Data.Pending)
                {
                    if (pending == null || string.IsNullOrEmpty(pending.Id) || !ids.Add(pending.Id) || pending.Amount < 100 || pending.Amount > 5000) throw new Exception("Invalid pending");
                }
            }
            catch
            {
                Data = new SaveData();
            }
            foreach (DropTransaction pending in Data.Pending) Data.Coins += pending.Amount;
            Data.Pending.Clear();
            Save();
        }

        public static Wallet Open()
        {
    #if UNITY_WEBGL && !UNITY_EDITOR
            return new Wallet(PegRushRead(), json =>
            {
                if (PegRushWrite(json) != 1) throw new InvalidOperationException("Browser storage unavailable");
            });
#else
            return new Wallet(PlayerPrefs.GetString(Key, ""), json =>
            {
                PlayerPrefs.SetString(Key, json);
                PlayerPrefs.Save();
            });
#endif
        }

        public void Save()
        {
            try
            {
                _write(JsonUtility.ToJson(Data));
                StorageAvailable = true;
            }
            catch
            {
                StorageAvailable = false;
            }
        }

        public DropTransaction Reserve(int amount, int rows, int profile, int[] table)
        {
            long cost = amount * 100L;
            if (!StorageAvailable || Data.Coins < cost) return null;
            DropTransaction transaction = new DropTransaction
            {
                Id = Guid.NewGuid().ToString("N"),
                Amount = cost,
                Rows = rows,
                Profile = profile,
                Table = (int[])table.Clone()
            };
            Data.Coins -= cost;
            Data.Pending.Add(transaction);
            Save();
            if (!StorageAvailable)
            {
                Data.Pending.Remove(transaction);
                Data.Coins += cost;
                return null;
            }
            return transaction;
        }

        public long Resolve(string id, int pocket)
        {
            DropTransaction transaction = Data.Pending.Find(t => t.Id == id);
            if (transaction == null) return -1;
            long credit = pocket < 0 ? transaction.Amount : Reward(transaction.Amount, transaction.Table[pocket]);
            Data.Pending.Remove(transaction);
            Data.Coins += credit;
            Save();
            if (!StorageAvailable)
            {
                Data.Coins -= credit;
                Data.Pending.Add(transaction);
                return -1;
            }
            return credit;
        }

        public static long Reward(long amount, int multiplier)
        {
            return (amount * multiplier + 50) / 100;
        }

        public bool Refill()
        {
            if (Data.Pending.Count != 0 || Data.Coins >= 100) return false;
            long previous = Data.Coins;
            Data.Coins = 100000;
            Save();
            if (StorageAvailable) return true;
            Data.Coins = previous;
            return false;
        }
    }
}



