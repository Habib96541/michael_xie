using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BullseyeBlitz.Editor
{
    public static class BlitzChecks
    {
        [MenuItem("Bullseye Blitz/Run deterministic checks")]
        public static void Run()
        {
            var passed = new List<string>();
            void Check(bool condition, string name)
            {
                if (!condition) { throw new Exception("FAILED: " + name); }
                passed.Add("PASS: " + name);
            }
            RoundRules rules = new RoundRules();
            rules.BeginCountdown();
            Check(!rules.Hit(false, 10), "Countdown rejects shots");
            rules.Start(); rules.Hit(false, 50);
            rules.Advance(19.999);
            Check(rules.State == RoundState.Playing && rules.Pending == 50, "Before deadline remains playing");
            rules.Advance(.001);
            Check(rules.State == RoundState.Success && rules.Time == 20, "Exact 20-second deadline");
            Check(!rules.Hit(true, 0) && rules.Pending == 50, "Deadline rejects bomb and preserves success");
            rules.BeginCountdown(); rules.Start(); rules.Hit(false, 100); rules.Advance(19.999);
            Check(rules.Hit(true, 0) && rules.Pending == 0 && rules.Lost == 100 && rules.State == RoundState.BombHit, "Bomb immediately before deadline loses pending");
            rules.Advance(1);
            Check(rules.State == RoundState.BombHit && !rules.Hit(false, 50), "Terminal outcome cannot change");
            rules.BeginCountdown(); rules.Start(); rules.Paused = true; rules.Advance(100);
            Check(rules.Time == 0 && !rules.Hit(false, 10), "Pause freezes clock and rejects hits");
            rules.Paused = false; rules.Hit(false, 10); rules.Abandon();
            Check(rules.Pending == 0 && rules.State == RoundState.Home, "Abandon discards pending");
            string oldId = rules.Id; rules.BeginCountdown();
            Check(rules.Id != oldId && rules.Pending == 0, "New round has unique ID and no carried earnings");
            BlitzConfig config = ScriptableObject.CreateInstance<BlitzConfig>();
            Check(config.IsValid(), "Default configuration accepted");
            config.Weights = new[] { 0, 0, 0, 0 };
            Check(!config.IsValid(), "Zero reward weights rejected");
            config.Weights = new[] { 45, 30, 18, 7 }; config.MaximumTargets = 10;
            Check(!config.IsValid(), "Invalid target capacity rejected");
            string key = "BullseyeBlitz.Test." + Guid.NewGuid().ToString("N");
            try
            {
                WalletStore wallet = new WalletStore(key);
                Check(wallet.Data.Wallet == 0, "Missing save starts with empty wallet");
                rules.BeginCountdown(); rules.Start(); rules.Hit(false, 100);
                Check(wallet.Data.Wallet == 0, "Pending earnings do not enter wallet");
                Check(wallet.Settle(rules.Id, 100), "Settlement save succeeds");
                Check(wallet.Settle(rules.Id, 100) && wallet.Data.Wallet == 100 && wallet.Data.Completed == 1, "Duplicate settlement does not pay twice");
                wallet = new WalletStore(key);
                Check(wallet.Data.Wallet == 100 && wallet.Data.Best == 100 && wallet.Data.Settlement == rules.Id, "Wallet and settlement reload together");
                Check(wallet.Settle(rules.Id, 100) && wallet.Data.Wallet == 100, "Persisted settlement rejects repeated payout");
                rules.BeginCountdown(); rules.Start(); rules.Hit(false, 50); rules.Hit(true, 0);
                Check(new WalletStore(key).Data.Wallet == 100, "Bomb preserves previously banked wallet");
                rules.BeginCountdown(); rules.Start(); rules.Hit(false, 50); rules.Abandon();
                Check(new WalletStore(key).Data.Wallet == 100, "Restart/reload cannot bank unfinished earnings");
                PlayerPrefs.SetString(key, "broken save");
                Check(new WalletStore(key).Data.Wallet == 0, "Corrupt save recovers safely");
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
            config.MaximumTargets = 5;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bullseye/Prefabs/BoothTarget.prefab");
            if (prefab == null) { throw new Exception("Target prefab missing"); }
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                BoothTarget target = instance.GetComponent<BoothTarget>();
                BlitzConfig real = AssetDatabase.LoadAssetAtPath<BlitzConfig>("Assets/Bullseye/BlitzConfig.asset");
                target.Spawn(real, false, 100, 0, 0);
                int activation = target.Activation;
                Check(!target.Consume(activation, .1), "Rising target rejects hit");
                target.Tick(.16);
                Check(target.Consume(activation, .16) && !target.Consume(activation, .16), "Duplicate target hit rejected immediately");
                Check(!instance.GetComponent<CircleCollider2D>().enabled, "Hit disables collider before retreat");
                target.Hide(); target.Spawn(real, true, 0, 1, 0); target.Tick(1.16);
                Check(!target.Consume(activation, 1.16) && target.Bomb && target.Reward == 0, "Pool reuse clears reward and rejects old activation");
                target.Hide();
                Check(!instance.activeSelf && !instance.GetComponent<CircleCollider2D>().enabled && target.Reward == 0, "Pooled target resets collider and visibility");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            SpriteMask[] masks = UnityEngine.Object.FindObjectsByType<SpriteMask>(FindObjectsSortMode.None);
            Check(masks.Length == 3, "Playable scene has three shelf reveal masks");
            foreach (SpriteMask mask in masks)
            {
                Check(mask.isCustomRangeActive && mask.backSortingLayerID == SortingLayer.NameToID("Targets") && mask.frontSortingLayerID == mask.backSortingLayerID && mask.frontSortingOrder - mask.backSortingOrder == 3, "Shelf mask is restricted to its own target row");
            }
            UnityEngine.Object.DestroyImmediate(config);
            Directory.CreateDirectory("SprintDoc/Tests");
            File.WriteAllLines("SprintDoc/Tests/Deterministic.txt", passed);
            Debug.Log("Bullseye deterministic checks passed: " + passed.Count);
        }
    }
}
