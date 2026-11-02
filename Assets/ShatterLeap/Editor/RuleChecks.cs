using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShatterLeap.Editor
{
    public static class RuleChecks
    {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition, string label)
        {
            if (!condition) { throw new Exception("FAILED: " + label); }
            Results.Add("PASS " + label);
        }
        private static RunRules Ready(Difficulty difficulty, int seed = 42)
        {
            RunRules run = new RunRules(difficulty, 10, seed);
            run.BeginTransition();
            run.BeginPlaying();
            return run;
        }
        private static void Complete(RunRules run)
        {
            while (!run.AtFinish)
            {
                run.Choose(run.SafeSide(run.Steps));
                run.Land(true);
            }
            run.BeginFinish();
            run.Land(true);
        }
        [MenuItem("ShatterLeap/Run Deterministic Rule Checks")]
        public static void Run()
        {
            Results.Clear();
            GameConfig actual = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ShatterLeap/Config/Sprint1.asset");
            actual.Validate();
            // Test the shipped defaults independently so later configuration edits remain buildable.
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            int[] rows = { 6, 10, 14 };
            float[] previews = { 5, 4, 3 };
            float[] timers = { 30, 45, 60 };
            int[] bonuses = { 50, 100, 200 };
            int[] payouts = { 260, 650, 1250 };
            for (int d = 0; d < 3; d++)
            {
                Difficulty difficulty = config.Difficulties[d];
                Check(difficulty.Rows == rows[d] && difficulty.PreviewSeconds == previews[d] && difficulty.PlaySeconds == timers[d] && difficulty.Bonus == bonuses[d], difficulty.Name + " defaults");
                for (int seed = 0; seed < 100; seed++)
                {
                    RunRules run = Ready(difficulty, seed);
                    int[] layout = new int[difficulty.Rows];
                    for (int row = 0; row < layout.Length; row++)
                    {
                        layout[row] = run.SafeSide(row);
                        if (layout[row] != 0 && layout[row] != 1) { throw new Exception("Nonbinary safety"); }
                    }
                    Complete(run);
                    if (run.Payout != payouts[d]) { throw new Exception("Incorrect payout"); }
                    for (int row = 0; row < layout.Length; row++)
                    {
                        if (run.SafeSide(row) != layout[row]) { throw new Exception("Layout changed"); }
                    }
                }
                Results.Add("PASS " + difficulty.Name + " 100 seeds: one safe panel per row, stable layout, full payout");
            }
            Difficulty easy = config.Difficulties[0];
            RunRules active = new RunRules(easy, 10, 9);
            Check(!active.Choose(0) && !active.BeginPlaying(), "Preview blocks choices and invalid transitions");
            Check(active.BeginTransition() && !active.BeginTransition() && !active.Choose(0) && active.BeginPlaying(), "Camera transition gates playing");
            Check(!active.Choose(-1) && !active.Choose(2), "Invalid side rejected");
            for (int step = 1; step <= easy.Rows; step++)
            {
                Check(active.Choose(active.SafeSide(active.Steps)) && !active.Choose(0), "Step " + step + " rejects repeated inputs");
                Check(active.Land(true) && !active.Land(true), "Step " + step + " resolves once");
                Check(active.Earnings == 10 * step * (step + 1) / 2, "Step " + step + " earnings");
            }
            Check(active.BeginFinish() && active.Land(true) && !active.End(RunState.Falling), "Only one terminal outcome");
            Check(active.Payout == 260, "Finish payout includes bonus");
            string stored = "";
            SaveStore wallet = new SaveStore("", json => stored = json);
            Check(wallet.Bank(active) && !wallet.Bank(active) && wallet.Data.Coins == 260, "Exactly-once wallet payout");
            SaveStore reload = new SaveStore(stored, json => stored = json);
            Check(reload.Data.Coins == 260 && !reload.Bank(active), "Reload retains wallet and payout guard");
            RunRules fail = Ready(easy);
            fail.Choose(fail.SafeSide(0));
            fail.Land(true);
            fail.Choose(1 - fail.SafeSide(1));
            Check(!fail.Land(true) && fail.Outcome == RunState.Falling && fail.Payout == 0 && !reload.Bank(fail) && reload.Data.Coins == 260, "Death discards round earnings, preserves banked wallet");
            RunRules unsupported = Ready(easy);
            unsupported.Choose(unsupported.SafeSide(0));
            Check(!unsupported.Land(false) && unsupported.Outcome == RunState.Falling, "Missing support fails landing");
            RunRules timeout = Ready(easy);
            timeout.Choose(timeout.SafeSide(0));
            Check(timeout.Tick(30) && !timeout.Land(true) && timeout.Payout == 0, "Timeout during jump prevents landing and payout");
            RunRules finalTie = Ready(easy);
            for (int i = 0; i < easy.Rows; i++) { finalTie.Choose(finalTie.SafeSide(i)); finalTie.Land(true); }
            finalTie.BeginFinish();
            Check(finalTie.Tick(30) && !finalTie.Land(true) && finalTie.Outcome == RunState.TimedOut, "Exact final landing/time boundary: timeout takes priority");
            RunRules finalBefore = Ready(easy);
            for (int i = 0; i < easy.Rows; i++) { finalBefore.Choose(finalBefore.SafeSide(i)); finalBefore.Land(true); }
            finalBefore.BeginFinish();
            finalBefore.Tick(29.99f);
            Check(finalBefore.Land(true) && finalBefore.Payout == 260 && !finalBefore.Tick(1), "Finish before timeout wins once");
            RunRules abandoned = Ready(easy);
            abandoned.Choose(abandoned.SafeSide(0));
            abandoned.Land(true);
            Check(!reload.Bank(abandoned) && new SaveStore(stored, _ => { }).Data.Coins == 260, "Abandonment never pays or serializes an active run");
            SaveData roundtrip = new SaveData { Coins = 345, Difficulty = 2, Music = 0.2f, Sound = 0.8f, Vibration = false, ReducedEffects = true };
            SaveData decoded = SaveStore.Decode(JsonUtility.ToJson(roundtrip));
            Check(decoded.Coins == 345 && decoded.Difficulty == 2 && decoded.Music == 0.2f && decoded.Sound == 0.8f && !decoded.Vibration && decoded.ReducedEffects, "Settings roundtrip");
            foreach (string corrupt in new[] { "", "broken", "{", "null", "{\"Version\":99}", "{\"Version\":1,\"Coins\":-99}", "{\"Version\":1,\"Difficulty\":8}" })
            {
                Check(SaveStore.Decode(corrupt).Coins == 0, "Corrupt/missing save recovery: " + corrupt);
            }
            SaveData clamped = SaveStore.Decode("{\"Version\":1,\"Music\":20,\"Sound\":-5}");
            Check(clamped.Music == 1 && clamped.Sound == 0, "Loaded volumes clamped");
            foreach (Difficulty difficulty in actual.Difficulties)
            {
                RunRules tuned = new RunRules(difficulty, actual.StepValue, 7);
                tuned.BeginTransition();
                tuned.BeginPlaying();
                Complete(tuned);
                int expected = actual.StepValue * difficulty.Rows * (difficulty.Rows + 1) / 2 + difficulty.Bonus;
                Check(tuned.Payout == expected, "Serialized configuration plays correctly: " + difficulty.Name);
            }
            UnityEngine.Object.DestroyImmediate(config);
            Directory.CreateDirectory("SprintDoc");
            File.WriteAllLines("SprintDoc/RuleChecks.txt", Results);
            Debug.Log("ShatterLeap: " + Results.Count + " rule checks passed (including 300 seeded complete runs).");
        }
    }
}
