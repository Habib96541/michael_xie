#if UNITY_EDITOR
using System;
using NeonDash.Core;
using UnityEditor;
using UnityEngine;

namespace NeonDash.EditorTools
{
    /// <summary>
    /// In-editor sanity check of the game rules. The same rules are covered in depth by the
    /// headless test suite in Tests/LogicTests; this menu item is for a quick check inside Unity.
    /// </summary>
    public static class GameChecks
    {
        [MenuItem("Geometry Dash/Validate Game Rules")]
        public static void Run()
        {
            int passed = 0;

            var ledger = new MultiplierLedger();
            Check(Mathf.Approximately(ledger.Value, 1f), "a run starts at 1.00x", ref passed);
            ledger.Collect(PickupKind.PinkOrb, 1f);
            Check(Mathf.Approximately(ledger.Value, 1.18f), "a pink orb is worth 0.18x", ref passed);
            ledger.Collect(PickupKind.YellowOrb, 2f);
            Check(ledger.Value > 1.5f, "a chained pickup is worth more than its base value", ref passed);
            Check(ledger.Orbs == 2, "orb pickups are counted", ref passed);

            var capped = new MultiplierLedger();
            for (int i = 0; i < 40; i++) capped.Collect(PickupKind.BlueOrb, i * 0.5f);
            Check(Mathf.Approximately(capped.Value, MultiplierLedger.Cap), "the multiplier stops at 10x", ref passed);

            Check(ScoreRules.Payout(100, 2f) == 20000, "score scales with distance and multiplier", ref passed);

            Level a = LevelBuilder.Build("9831");
            Level b = LevelBuilder.Build("9831");
            Level c = LevelBuilder.Build("9832");
            Check(a.Tiles.Count == b.Tiles.Count, "same seed rebuilds the same level", ref passed);
            Check(a.Tiles.Count > 20, "level has obstacles", ref passed);
            Check(a.Tiles.Count != c.Tiles.Count || a.Tiles[0].X != c.Tiles[0].X, "a new seed changes the level", ref passed);
            Check(Mathf.Approximately(a.Length, GameConstants.CourseLength), "course is one 30 second run", ref passed);

            var player = new PlayerSim();
            player.Reset();
            var empty = new Level();
            empty.Length = 100f;
            empty.Ceiling = GameConstants.CeilingY;
            player.Step(GameConstants.FixedStep, empty, false, true);
            Check(!player.Grounded, "the cube jumps", ref passed);
            Check(player.VY > 0f, "jump velocity is positive", ref passed);

            Debug.Log("Geometry Dash rule checks passed: " + passed);
        }

        static void Check(bool condition, string name, ref int passed)
        {
            if (condition)
            {
                passed++;
                return;
            }
            throw new Exception("Geometry Dash rule check failed: " + name);
        }
    }
}
#endif
