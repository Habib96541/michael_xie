using System;
using System.IO;
using MineGrid;
using UnityEditor;
using UnityEngine;

public static class MineGridVerification
{
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, msg) =>
        {
            if (!ok)
            {
                throw new Exception(msg);
            }

            checks++;
        };
        var c = ScriptableObject.CreateInstance<GameConfig>();
        string[] keys =
        {
            SaveService.Key,
            "MineGrid.Save.v2",
            "MineGrid.Save.v1"
        };
        var backups = new string[keys.Length];
        var exists = new bool[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            exists[i] = PlayerPrefs.HasKey(keys[i]);
            backups[i] = PlayerPrefs.GetString(keys[i], "");
            PlayerPrefs.DeleteKey(keys[i]);
        }

        try
        {
            for (int n = 1; n <= 24; n++)
            {
                for (int seed = 0; seed < 40; seed++)
                {
                    var b = new Board(n, new System.Random(seed));
                    int count = 0;
                    var initial = new bool[25];
                    for (int i = 0; i < 25; i++)
                    {
                        initial[i] = b.IsMine(i);
                        if (initial[i])
                        {
                            count++;
                        }
                    }

                    check(count == n, "unique mines");
                    for (int i = 0; i < 25; i++)
                    {
                        b.Reveal(i);
                    }

                    for (int i = 0; i < 25; i++)
                    {
                        check(b.IsMine(i) == initial[i], "fixed mines");
                    }

                    check(b.SafeRevealed == 25 - n, "safe count");
                    check(!b.Reveal(0), "duplicate board reveal");
                }
            }

            var r = new Round(c);
            r.Start(3, new System.Random(5));
            check(r.Finish() && r.Banked == 0, "zero finish");
            check(!r.Finish() && !r.Reveal(0), "ended input");
            r.Start(3, new System.Random(5));
            int gems = 0;
            for (int i = 0; i < 25 && gems < 4; i++)
            {
                if (!r.Board.IsMine(i))
                {
                    check(r.Reveal(i), "safe reveal");
                    check(!r.Reveal(i), "repeat reveal");
                    gems++;
                }
            }

            check(r.Score == 120, "four gems at three mines gives 120");
            r.Finish();
            check(r.Banked == 120, "finish reward");
            var save = new SaveService();
            check(save.Bank(r.Banked, r.Id), "bank once");
            check(!save.Bank(r.Banked, r.Id) && save.Data.coins == 120, "duplicate bank");
            r.Start(24, new System.Random(7));
            for (int i = 0; i < 25; i++)
            {
                if (!r.Board.IsMine(i))
                {
                    r.Reveal(i);
                }
            }

            check(r.State == RoundState.Won && r.Banked == 240, "auto win");
            check(!r.Finish(), "no finish after win");
            r.Start(3, new System.Random(1));
            for (int i = 0; i < 25; i++)
            {
                if (r.Board.IsMine(i))
                {
                    r.Reveal(i);
                    check(r.TriggeredMine == i, "triggered mine");
                    break;
                }
            }

            check(r.State == RoundState.Lost && r.Banked == 0 && r.Score == 0, "first click mine");
            r.Start(3, new System.Random(1));
            check(r.Forfeit() && r.Score == 0 && !r.Forfeit(), "forfeit only once");
            save.Bank(500, "milestone");
            check(save.Claim(c, 0) && !save.Claim(c, 0), "milestone once");
            check(!save.Claim(c, 1) && !save.Claim(c, -1), "locked milestone");
            int points = save.Data.totalPoints;
            long time = 200000;
            var timed = new SaveService(() => time);
            int balance = timed.Data.coins;
            check(timed.Spin(c, new System.Random(3), out int segment), "spin available");
            check(timed.Data.coins == balance && timed.Data.pendingCoins == c.wheelCoins[segment], "pending persisted before credit");
            var reload = new SaveService(() => time);
            check(!reload.Spin(c, new System.Random(1), out _), "pending blocks new spin");
            check(reload.ClaimPending() && !reload.ClaimPending(), "pending once after reload");
            check(reload.Data.coins == balance + c.wheelCoins[segment] && reload.Data.totalPoints == points, "rewards exclude progression");
            check(!reload.Spin(c, new System.Random(1), out _), "cooldown");
            time += 86400;
            check(reload.Spin(c, new System.Random(1), out _), "24h eligibility");
            reload.Data.selectedMines = 24;
            reload.Data.reducedEffects = true;
            reload.Data.muted = true;
            reload.Save();
            var persisted = new SaveService(() => time);
            check(persisted.Data.selectedMines == 24 && persisted.Data.reducedEffects && persisted.Data.muted, "settings selection persisted");
            PlayerPrefs.SetString(SaveService.Key, "{broken");
            check(new SaveService().Data.coins == 0, "invalid JSON recovery");
            PlayerPrefs.SetString(SaveService.Key, "{\"coins\":-4,\"totalPoints\":-10,\"selectedMines\":99,\"claimed\":null}");
            var sanitized = new SaveService();
            check(sanitized.Data.coins == 0 && sanitized.Data.totalPoints == 0 && sanitized.Data.selectedMines == 24 && sanitized.Data.claimed != null, "invalid fields sanitized");
            PlayerPrefs.DeleteKey(SaveService.Key);
            PlayerPrefs.SetString("MineGrid.Save.v1", "{\"coins\":75,\"totalPoints\":100,\"claimed\":[0]}");
            var migrated = new SaveService();
            check(migrated.Data.coins == 75 && migrated.Data.totalPoints == 100 && migrated.Data.claimed.Count == 1 && migrated.Data.version == 3, "v1 migration");
            File.WriteAllText("MineGrid-verification.txt", $"PASS: {checks} Sprint 2 checks. Fixed boards across 24 mine counts x 40 seeds; safe counts; duplicate input; direct-coin scoring; zero finish; idempotent banking; win/loss/forfeit; milestone eligibility; interrupted daily reward recovery; duplicate claims; UTC cooldown with injected clock; saved settings/selection; invalid JSON/fields; legacy migration.\n");
            Debug.Log($"MineGrid verification passed: {checks} checks");
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (exists[i])
                {
                    PlayerPrefs.SetString(keys[i], backups[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(keys[i]);
                }
            }

            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(c);
        }
    }
}
