using System;
using System.Numerics;
using HiLo.Core;
using UnityEngine;

namespace HiLo
{
    [Serializable] public sealed class PlayerSave
    {
        public int version = 1;
        public string balance = "2|2", bestStreak = "0", bestMultiplier = "1|0", predictions = "0", rounds = "0", cashouts = "0";
        public bool sound = true, music = true, reducedMotion, pressure = true, learned;
    }
    public sealed class SaveManager
    {
        const string Key = "hilo.endless.v1";
        public PlayerSave Data { get; private set; }
        public bool PersistenceAvailable { get; private set; } = true;
        public SaveManager(double initialBalance = 200)
        {
            PlayerSave Fresh() => new PlayerSave { balance = new ScientificNumber(initialBalance).Encode() };
            try { Data = JsonUtility.FromJson<PlayerSave>(PlayerPrefs.GetString(Key, "")) ?? Fresh(); }
            catch { Data = Fresh(); PersistenceAvailable = false; }
            if (Data.version != 1) Data = Fresh();
        }
        public BigInteger Number(string value) => BigInteger.TryParse(value, out var number) && number >= 0 ? number : BigInteger.Zero;
        public void Record(GameSession game)
        {
            Data.balance = game.Balance.Encode();
            if (game.Streak > Number(Data.bestStreak)) Data.bestStreak = game.Streak.ToString();
            if (game.Multiplier.CompareTo(ScientificNumber.Decode(Data.bestMultiplier, 1)) > 0) Data.bestMultiplier = game.Multiplier.Encode();
            Flush();
        }
        public void Flush() { try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); } catch { PersistenceAvailable = false; } }
    }
}
