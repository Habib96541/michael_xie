using System;
using System.Collections.Generic;
using System.Linq;
using NeonDash.Core;

namespace NeonDash.Tests
{
    /// <summary>
    /// Headless verification of the game's rules. Everything here runs against the same core
    /// code Unity compiles, so a green run means the simulation logic is sound; it cannot prove
    /// how the game looks or feels (that needs the Unity player).
    /// </summary>
    public static class Program
    {
        static int passed;
        static int failed;
        static readonly List<string> failures = new List<string>();

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "trace") { TraceShipChunk(args.Length > 1 ? args[1] : "ship-floor-wall"); return 0; }

            Console.WriteLine("Neon Dash - headless logic verification");
            Console.WriteLine("=======================================");

            SeedDeterminism();
            ShareCodeDeterminism();
            SeedVariety();
            LayoutSanity();
            JumpArc();
            SpikeKills();
            BlockTopLanding();
            OrbBoost();
            PadBoost();
            GravityAndShipPortals();
            MultiplierRules();
            GemReporting();
            InputResponsiveness();
            PayoutMath();
            RecordBook();
            ThirtySecondTiming();
            ChunkPlayability();
            FullLevelPlayability(false);
            FullLevelPlayability(true);
            FrameRateIndependence();

            Console.WriteLine();
            Console.WriteLine("Passed: " + passed + "   Failed: " + failed);
            if (failed > 0)
            {
                Console.WriteLine();
                foreach (string f in failures) Console.WriteLine("FAIL: " + f);
                return 1;
            }
            return 0;
        }

        // ------------------------------------------------------------------
        // Tests
        // ------------------------------------------------------------------
        static void SeedDeterminism()
        {
            Level a = LevelBuilder.Build("9831");
            Level b = LevelBuilder.Build("9831");
            Check(a.Tiles.Count == b.Tiles.Count, "same seed gives same tile count");
            Check(a.Portals.Count == b.Portals.Count, "same seed gives same portal count");
            Check(a.Orbs.Count == b.Orbs.Count, "same seed gives same orb count");
            Check(a.ChunkNames.Count == b.ChunkNames.Count, "same seed gives the same chunk order");

            bool identical = true;
            for (int i = 0; i < a.Tiles.Count && identical; i++)
                identical = a.Tiles[i].Kind == b.Tiles[i].Kind
                            && Math.Abs(a.Tiles[i].X - b.Tiles[i].X) < 0.0001f
                            && Math.Abs(a.Tiles[i].Y - b.Tiles[i].Y) < 0.0001f;
            Check(identical, "same seed gives identical layout");

            Level c = LevelBuilder.Build("9832");
            bool different = c.Tiles.Count != a.Tiles.Count;
            for (int i = 0; i < Math.Min(a.Tiles.Count, c.Tiles.Count) && !different; i++)
                different = a.Tiles[i].Kind != c.Tiles[i].Kind || Math.Abs(a.Tiles[i].X - c.Tiles[i].X) > 0.0001f;
            Check(different, "a different seed changes the layout");

            Check(a.Length > 300f && a.Length < 320f, "course length is one 30 second run (" + a.Length + ")");
            Check(a.Tiles.Count > 20, "level contains real obstacles (" + a.Tiles.Count + " tiles)");
        }

        static void SeedVariety()
        {
            var signatures = new HashSet<string>();
            int minTiles = int.MaxValue;
            int maxTiles = 0;
            float minLastTile = float.MaxValue;

            for (int i = 0; i < 120; i++)
            {
                Level level = LevelBuilder.Build(i.ToString());
                signatures.Add(level.Tiles.Count + ":" + level.Portals.Count + ":" + level.Orbs.Count + ":" +
                               (level.Tiles.Count > 0 ? level.Tiles[0].X + "," + level.Tiles[level.Tiles.Count - 1].X : "0"));
                minTiles = Math.Min(minTiles, level.Tiles.Count);
                maxTiles = Math.Max(maxTiles, level.Tiles.Count);
                if (level.Tiles.Count > 0) minLastTile = Math.Min(minLastTile, level.Tiles[level.Tiles.Count - 1].X);
            }

            Check(signatures.Count > 60, "120 seeds produce many distinct layouts (" + signatures.Count + ")");
            Check(minTiles > 15, "every seed has enough obstacles (min " + minTiles + ")");
            Check(maxTiles < 400, "levels stay within a sane object budget (max " + maxTiles + ")");
            Check(minLastTile > 250f, "obstacles cover most of the course (last tile at " + minLastTile.ToString("0.0") + ")");
        }

        static void ShareCodeDeterminism()
        {
            string code = SeedCode.Generate();
            Level original = LevelBuilder.Build(code);
            Level replay = LevelBuilder.Build(" " + code.ToLowerInvariant() + " ");
            Check(replay.Seed == code && original.Tiles.SequenceEqual(replay.Tiles)
                  && original.Gems.SequenceEqual(replay.Gems) && original.Pads.SequenceEqual(replay.Pads)
                  && original.Portals.SequenceEqual(replay.Portals),
                "a pasted share code reproduces the course and pickups exactly");

            Check(!SeedCode.TryNormalize(code + "1", out _) && !SeedCode.TryNormalize("seed 2026", out _)
                  && SeedCode.TryNormalize("0002026", out string legacy) && legacy == "0002026",
                "invalid codes are rejected and legacy numeric seeds retain leading zeroes");

            var generated = new HashSet<string>();
            for (int i = 0; i < 1000; i++) generated.Add(SeedCode.Generate());
            Check(generated.Count == 1000 && generated.All(s => SeedCode.TryNormalize(s, out _)),
                "1000 generated share codes are valid and distinct");
            Check(!LevelBuilder.Build("ND1-0123456700000000").Tiles.SequenceEqual(
                    LevelBuilder.Build("ND1-8123456700000000").Tiles),
                "changing the high bits of a share code changes the layout");
        }

        /// <summary>
        /// Structural checks that physics tests cannot catch: overlapping tiles, pickups buried
        /// inside solid blocks, and sections that open a mode without closing it.
        /// </summary>
        static void LayoutSanity()
        {
            int duplicates = 0;
            int buriedPickups = 0;
            int unbalancedSections = 0;
            string firstProblem = string.Empty;

            for (int s = 0; s < 120; s++)
            {
                Level level = LevelBuilder.Build((7000 + s).ToString());
                var cells = new HashSet<string>();

                for (int i = 0; i < level.Tiles.Count; i++)
                {
                    Tile tile = level.Tiles[i];
                    string key = Mathf_Round(tile.X) + ":" + Mathf_Round(tile.Y);
                    if (!cells.Add(key))
                    {
                        duplicates++;
                        if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " has two tiles at " + key;
                    }
                }

                for (int i = 0; i < level.Pads.Count; i++)
                {
                    string key = Mathf_Round(level.Pads[i].X + 0.5f) + ":" + Mathf_Round(level.Pads[i].Y);
                    if (cells.Contains(key))
                    {
                        buriedPickups++;
                        if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " has a pad inside a block";
                    }
                }

                for (int i = 0; i < level.Orbs.Count; i++)
                {
                    string key = Mathf_Round(level.Orbs[i].X) + ":" + Mathf_Round(level.Orbs[i].Y);
                    if (cells.Contains(key))
                    {
                        buriedPickups++;
                        if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " has an orb inside a block";
                    }
                }

                for (int i = 0; i < level.Gems.Count; i++)
                {
                    float gemX = level.Gems[i].X;
                    float gemY = level.Gems[i].Y;
                    if (gemY < 1.2f || gemY > level.Ceiling - 0.9f)
                    {
                        buriedPickups++;
                        if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " has a gem outside the playfield";
                    }
                    for (int j = 0; j < level.Tiles.Count; j++)
                    {
                        Tile tile = level.Tiles[j];
                        if (tile.Kind != TileKind.Block) continue;
                        if (Math.Abs(tile.X + 0.5f - gemX) < 0.9f && Math.Abs(tile.CenterY - gemY) < 0.9f)
                        {
                            buriedPickups++;
                            if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " has a gem inside a block";
                        }
                    }
                }

                int shipPortals = 0, cubePortals = 0, flips = 0, normals = 0;
                for (int i = 0; i < level.Portals.Count; i++)
                {
                    switch (level.Portals[i].Kind)
                    {
                        case PortalKind.ShipPortal: shipPortals++; break;
                        case PortalKind.CubePortal: cubePortals++; break;
                        case PortalKind.GravityFlip: flips++; break;
                        case PortalKind.GravityNormal: normals++; break;
                    }
                }
                if (shipPortals != cubePortals) unbalancedSections++;
                if (flips != normals)
                {
                    unbalancedSections++;
                    if (firstProblem.Length == 0) firstProblem = "seed " + level.Seed + " flips gravity without restoring it";
                }
            }

            Check(duplicates == 0, "no two tiles share a grid cell" + (duplicates > 0 ? " (" + firstProblem + ")" : ""));
            Check(buriedPickups == 0, "pads, orbs and gems never sit inside solid blocks");
            Check(unbalancedSections == 0, "every ship and gravity section is closed again" + (firstProblem.Length > 0 ? " (" + firstProblem + ")" : ""));
        }

        static int Mathf_Round(float value)
        {
            return (int)Math.Floor(value + 0.5f);
        }

        static void JumpArc()
        {
            Level level = EmptyLevel(40f);
            var player = new PlayerSim();
            player.Reset();
            float startX = player.X;

            player.Step(GameConstants.FixedStep, level, false, true); // take off
            float peak = 0f;
            int frames = 0;
            while (!player.Grounded && frames < 400)
            {
                player.Step(GameConstants.FixedStep, level, false, false);
                peak = Math.Max(peak, player.Y);
                frames++;
            }

            float expectedPeak = GameConstants.CubeJumpVelocity * GameConstants.CubeJumpVelocity / (2f * GameConstants.CubeGravity);
            float airtime = frames * GameConstants.FixedStep;
            float travelled = player.X - startX;

            Check(Math.Abs(peak - expectedPeak) < 0.15f, "jump peaks at the configured height (" + peak.ToString("0.00") + " vs " + expectedPeak.ToString("0.00") + ")");
            Check(peak > 2.5f, "cube clears a two block wall (" + peak.ToString("0.00") + " blocks)");
            Check(airtime > 0.47f && airtime < 0.60f, "airtime is about half a second (" + airtime.ToString("0.000") + "s)");
            Check(Math.Abs(player.Y) < 0.001f && player.Grounded, "cube lands back on the floor");
            Check(Math.Abs(player.Rotation + 180f) < 0.001f, "a full floor jump lands flat after a half-turn");
            Check(travelled > 5.0f && travelled < 5.8f, "one jump covers about five and a half blocks (" + travelled.ToString("0.00") + ")");
        }

        static void SpikeKills()
        {
            Level level = EmptyLevel(40f);
            level.Tiles.Add(new Tile(TileKind.Spike, 8f, 0f));

            var player = new PlayerSim();
            player.Reset();
            bool died = false;
            for (int i = 0; i < 600 && !died; i++)
            {
                player.Step(GameConstants.FixedStep, level, false, false);
                died = !player.Alive;
            }
            Check(died, "running into a spike ends the run");
            Check(player.X > 7f && player.X < 9f, "death registers at the spike (" + player.X.ToString("0.00") + ")");
        }

        static void BlockTopLanding()
        {
            Level level = EmptyLevel(40f);
            level.Tiles.Add(new Tile(TileKind.Block, 8f, 0f));

            // Drop onto the top face of a long platform (the cube keeps moving forward, so a
            // single block would be behind it by the time it lands).
            Level platform = EmptyLevel(40f);
            for (int i = 0; i < 10; i++) platform.Tiles.Add(new Tile(TileKind.Block, 6f + i, 0f));

            var player = new PlayerSim();
            player.Reset();
            player.X = 6.5f;
            player.Y = 3f;
            player.Grounded = false;
            player.VY = 0f;
            for (int i = 0; i < 40 && player.Alive && !player.Grounded; i++)
                player.Step(GameConstants.FixedStep, platform, false, false);
            Check(player.Alive, "cube survives landing on a block");
            Check(player.Grounded && Math.Abs(player.Y - 1f) < 0.02f, "cube stands on the block top (y=" + player.Y.ToString("0.00") + ")");

            // Walking into the side of a block is fatal.
            var side = new PlayerSim();
            side.Reset();
            bool died = false;
            for (int i = 0; i < 600 && !died; i++)
            {
                side.Step(GameConstants.FixedStep, level, false, false);
                died = !side.Alive;
            }
            Check(died, "hitting the side of a block ends the run");
        }

        static void OrbBoost()
        {
            Level level = EmptyLevel(40f);
            level.Orbs.Add(new Orb(OrbKind.Yellow, 4f, 2.4f));

            var player = new PlayerSim();
            player.Reset();
            player.Step(GameConstants.FixedStep, level, false, true);

            bool used = false;
            float extraPeak = 0f;
            for (int i = 0; i < 400; i++)
            {
                bool press = !player.Grounded && !used && Math.Abs(player.X - 4f) < 0.45f;
                player.Step(GameConstants.FixedStep, level, false, press);
                if (player.Events.Orb) used = true;
                if (used) extraPeak = Math.Max(extraPeak, player.Y);
            }
            Check(used, "tapping a yellow orb while airborne triggers it");
            Check(extraPeak > 2.6f, "yellow orb jumps higher than a normal jump (" + extraPeak.ToString("0.00") + ")");
        }

        static void PadBoost()
        {
            Level level = EmptyLevel(40f);
            level.Pads.Add(new Pad(PadKind.Yellow, 6f, 0f));

            var player = new PlayerSim();
            player.Reset();
            float startX = player.X;
            float peak = 0f;
            bool triggered = false;
            for (int i = 0; i < 400; i++)
            {
                player.Step(GameConstants.FixedStep, level, false, false);
                if (player.Events.Pad) triggered = true;
                if (triggered) peak = Math.Max(peak, player.Y);
            }
            Check(triggered, "a pad fires on contact without any input");
            Check(peak > 3.2f, "a yellow pad launches higher than a normal jump (" + peak.ToString("0.00") + ")");
            Check(player.X - startX > 7f, "a yellow pad carries the cube further (" + (player.X - startX).ToString("0.0") + " blocks)");
        }

        static void GravityAndShipPortals()
        {
            Level level = EmptyLevel(60f);
            level.Portals.Add(new Portal(PortalKind.GravityFlip, 6f, 0f));

            var player = new PlayerSim();
            player.Reset();
            for (int i = 0; i < 200; i++) player.Step(GameConstants.FixedStep, level, false, false);
            Check(player.GravitySign == -1, "gravity portal flips gravity");
            Check(player.Y + 0.6f > GameConstants.CeilingY - 0.05f, "flipped cube settles against the ceiling (y=" + player.Y.ToString("0.00") + ")");

            Level shipLevel = EmptyLevel(60f);
            shipLevel.Portals.Add(new Portal(PortalKind.ShipPortal, 6f, 0f));
            shipLevel.Portals.Add(new Portal(PortalKind.CubePortal, 20f, 0f));

            var ship = new PlayerSim();
            ship.Reset();
            bool wasShip = false;
            for (int i = 0; i < 400; i++)
            {
                ship.Step(GameConstants.FixedStep, shipLevel, i % 40 < 20, false);
                if (ship.Mode == PlayerMode.Ship) wasShip = true;
            }
            Check(wasShip, "ship portal switches to ship mode");
            Check(ship.Alive, "ship flies without leaving the playfield");
            Check(ship.Mode == PlayerMode.Cube, "cube portal returns to cube mode");
        }

        static void MultiplierRules()
        {
            var ledger = new MultiplierLedger();
            Check(Math.Abs(ledger.Value - 1f) < 0.0001f, "a run starts at 1.00x");

            ledger.Collect(PickupKind.PinkOrb, 1f);
            Check(Math.Abs(ledger.Value - 1.18f) < 0.0001f, "a first pink orb adds its base 0.18x");
            Check(ledger.Orbs == 1 && ledger.Pads == 0, "orb pickups are counted");

            ledger.Collect(PickupKind.YellowOrb, 2f);
            float chained = ledger.Value - 1.18f;
            Check(chained > MultiplierLedger.ValueOf(PickupKind.YellowOrb) + 0.0001f,
                  "a pickup inside the chain window beats its base value (" + chained.ToString("0.000") + ")");

            var spread = new MultiplierLedger();
            spread.Collect(PickupKind.PinkOrb, 1f);
            spread.Collect(PickupKind.YellowOrb, 2f + MultiplierLedger.ChainWindow + 1f);
            Check(spread.Chain == 0, "a slow pickup breaks the chain");

            var pads = new MultiplierLedger();
            pads.Collect(PickupKind.YellowPad, 1f);
            Check(pads.Pads == 1 && pads.Orbs == 0, "pad pickups are counted separately");

            var capped = new MultiplierLedger();
            for (int i = 0; i < 60; i++) capped.Collect(PickupKind.BlueOrb, i * 0.4f);
            Check(Math.Abs(capped.Value - MultiplierLedger.Cap) < 0.0001f, "the multiplier stops at 10x");
            Check(Math.Abs(capped.Collect(PickupKind.BlueOrb, 100f)) < 0.0001f, "pickups past the cap add nothing");

            var a = new MultiplierLedger();
            var b = new MultiplierLedger();
            for (int i = 0; i < 5; i++) { a.Collect(PickupKind.YellowOrb, i * 1.1f); b.Collect(PickupKind.YellowOrb, i * 1.1f); }
            Check(Math.Abs(a.Value - b.Value) < 0.0001f, "identical pickups earn an identical multiplier");

            Check(MultiplierLedger.ValueOf(PickupKind.BlueOrb) > MultiplierLedger.ValueOf(PickupKind.YellowOrb)
                  && MultiplierLedger.ValueOf(PickupKind.YellowOrb) > MultiplierLedger.ValueOf(PickupKind.PinkOrb),
                  "the gravity orb is worth the most, the pink orb the least");
        }

        /// <summary>
        /// Gems must reach the view. Two ways that used to break: two gems touched inside one step
        /// (only the last index was reported), and a gem collected in an early sub-step of a frame
        /// (only the last step's events were read). Both left the gem on screen while its
        /// multiplier was already banked.
        /// </summary>
        static void GemReporting()
        {
            // 1. Two gems inside the pickup radius during the same 120 Hz step.
            Level level = EmptyLevel(60f);
            level.Gems.Add(new Gem(GemKind.Arc, 6f, 0.5f));
            level.Gems.Add(new Gem(GemKind.High, 5.95f, 0.5f));

            var player = new PlayerSim();
            player.Reset();
            player.X = 5.9f;              // both gems are within reach right now
            player.Step(GameConstants.FixedStep, level, false, false);

            Check(player.GemsThisStep.Count == 2, "both gems touched inside one step are reported ("
                + player.GemsThisStep.Count + " of 2)");
            Check(player.Events.Gem && player.Events.GemIndex == 0, "the summary still points at the first gem of the step");

            // 2. A gem collected in the first of several sub-steps must survive to the frame summary.
            //    Advance(1/30) runs four 120 Hz steps, and the cube starts 0.8 blocks before the gem,
            //    so it is the first step that touches it.
            Level framed = EmptyLevel(60f);
            framed.Gems.Add(new Gem(GemKind.Arc, 6f, 0.5f));

            var session = new RunSession();
            session.Begin(framed);
            session.Player.X = 5.2f;

            session.Advance(1f / 30f, false, false);
            Check(session.FrameGems.Count == 1, "a gem from an early sub-step reaches the frame summary (" + session.FrameGems.Count + ")");
            Check(session.FrameEvents.Gem, "the frame summary reports a gem event");
            Check(session.Multiplier.Gems == 1, "the gem also counted toward the multiplier (" + session.Multiplier.Gems + ")");

            session.Advance(1f / 30f, false, false);
            Check(session.FrameGems.Count == 0, "the next frame does not replay the same gem");
        }

        /// <summary>
        /// A 144 Hz frame is shorter than one 120 Hz simulation step, so some frames run no step at
        /// all. A tap that lands in one of those frames used to be dropped; it has to be kept until
        /// a step can act on it, and a used orb has to report which one it was.
        /// </summary>
        static void InputResponsiveness()
        {
            var session = new RunSession();
            session.Begin(EmptyLevel(60f));

            session.Advance(1f / 144f, false, true);
            Check(!session.FrameEvents.Jumped, "a 144 Hz frame runs no simulation step");

            session.Advance(1f / 144f, false, false);
            Check(session.FrameEvents.Jumped, "a tap is not lost when its frame ran no step");
            Check(!session.Player.Grounded && session.Player.VY > 0f, "the buffered tap actually jumped");

            // Orb index reporting, so the view can pop the exact orb that fired.
            Level level = EmptyLevel(60f);
            level.Orbs.Add(new Orb(OrbKind.Pink, 0f, 0.5f));
            level.Orbs.Add(new Orb(OrbKind.Yellow, 0f, 3f));

            var orbSession = new RunSession();
            orbSession.Begin(level);
            orbSession.Advance(1f / 60f, false, false);   // get off the ground
            orbSession.Advance(1f / 60f, false, true);
            Check(orbSession.FrameEvents.Orb, "tapping near an orb fires it");
            Check(orbSession.FrameEvents.OrbIndex == 0, "the frame reports which orb fired ("
                + orbSession.FrameEvents.OrbIndex + ")");
        }

        static void PayoutMath()
        {
            Check(ScoreRules.Payout(100, 1f) == 10000, "100 blocks at 1x pays 10,000");
            Check(ScoreRules.Payout(100, 2.5f) == 25000, "100 blocks at 2.5x pays 25,000");
            Check(ScoreRules.Payout(0, 10f) == 0, "no distance pays nothing");
            Check(ScoreRules.Payout(-5, 3f) == 0, "negative distance is clamped to zero");
            Check(ScoreRules.Progress(156f, 312f) > 0.499f && ScoreRules.Progress(156f, 312f) < 0.501f, "half the course is 50%");
        }

        static void RecordBook()
        {
            var table = new RecordTable();
            bool first = table.Submit("1234", "Ann", 100, 1.2f, 12000);
            Check(first, "the first run on a seed is a record");

            bool worse = table.Submit("1234", "Bob", 80, 0.9f, 7200);
            Check(!worse, "a shorter run is not a record");
            SeedRecord record = table.Find("1234");
            Check(record.BestDistance == 100 && record.Name == "Ann", "the best distance and name are kept");
            Check(record.Attempts == 2, "attempts are counted");

            bool better = table.Submit("1234", "Cy", 200, 3.1f, 62000);
            Check(better, "a longer run becomes the new record");
            Check(table.Find("1234").Name == "Cy", "the record holder updates");

            table.Submit("9999", "Dee", 150, 2f, 30000);
            Check(table.Best(2).Count == 2 && table.Best(2)[0].Seed == "1234", "records sort by best distance");
            Check(table.Find("9999") != null, "each seed keeps its own record");

            var many = new RecordTable();
            for (int i = 0; i < RecordTable.MaxRecords + 15; i++) many.Submit("s" + i, "P", i + 1, 1f, 100);
            Check(many.Records.Count <= RecordTable.MaxRecords, "the record book stays bounded (" + many.Records.Count + ")");

            many.Submit("long-seed-name-here", "AReallyLongPlayerName", 50, 1f, 5000);
            Check(many.Find("long-seed-name-here").Name.Length <= 12, "player names are shortened to fit");
        }

        static void ThirtySecondTiming()
        {
            var session = new RunSession();
            Level level = EmptyLevel(GameConstants.CourseLength);
            session.Begin(level);

            int frames = 0;
            while (session.Running && frames < 60 * 40)
            {
                session.Advance(1f / 60f, false, false);
                frames++;
            }

            Check(session.Finished, "the run ends on its own");
            Check(Math.Abs(session.Elapsed - 30f) < 0.1f, "a flat course finishes after 30 seconds (" + session.Elapsed.ToString("0.00") + "s)");
            Check(session.Result.Completed, "finishing the course counts as complete");
            Check(Math.Abs(session.Result.Progress - 1f) < 0.02f, "completion is 100%");
            Check(Math.Abs(session.Result.Multiplier - 1f) < 0.0001f, "a run with no pickups ends at exactly 1.00x (" + session.Result.Multiplier + ")");
        }

        static void ChunkPlayability()
        {
            int cubeFails = 0;
            foreach (Chunk chunk in ChunkLibrary.CubeChunks)
            {
                Level level = ChunkLevel(chunk, false);
                float reached = Play(level, 10f);
                bool survived = reached >= 22f + chunk.Width + 1.5f;
                if (!survived)
                {
                    cubeFails++;
                    Check(false, "cube chunk '" + chunk.Name + "' is clearable (reached " + reached.ToString("0.0") + ")");
                }
            }
            if (cubeFails == 0) Check(true, "all " + ChunkLibrary.CubeChunks.Count + " cube chunks are clearable by the autopilot");

            int shipFails = 0;
            foreach (Chunk chunk in ChunkLibrary.ShipChunks)
            {
                Level level = ChunkLevel(chunk, true);
                float reached = Play(level, 6f);
                bool survived = reached >= 22f + chunk.Width + 1.5f;
                if (!survived)
                {
                    shipFails++;
                    Check(false, "ship chunk '" + chunk.Name + "' is clearable (reached " + reached.ToString("0.0") + ")");
                }
            }
            if (shipFails == 0) Check(true, "all " + ChunkLibrary.ShipChunks.Count + " ship chunks are clearable by the autopilot");

            int flipFails = 0;
            foreach (Chunk chunk in ChunkLibrary.FlippedChunks)
            {
                Level level = EmptyLevel(90f);
                level.Seed = "flip";
                level.Portals.Add(new Portal(PortalKind.GravityFlip, 16f, 0f));
                InjectChunk(level, chunk, 22f);
                level.Portals.Add(new Portal(PortalKind.GravityNormal, 22f + chunk.Width + 4f, GameConstants.CeilingY - 1f));
                float reached = Play(level, 10f);
                if (reached < 22f + chunk.Width) flipFails++;
            }
            Check(flipFails == 0, "all flipped-gravity chunks teach a survivable pattern (" + flipFails + " failed)");
        }

        static void FullLevelPlayability(bool shareCodes)
        {
            int cleared = 0;
            int total = 300;
            float totalProgress = 0f;
            float totalMultiplier = 0f;
            int totalPickups = 0;
            float worst = 1f;
            string worstSeed = "";
            string worstChunk = "";
            var failureCounts = new Dictionary<string, int>();

            for (int i = 0; i < total; i++)
            {
                string seed = shareCodes ? "ND1-" + ((ulong)i * 0x45D9F3B163A94BUL).ToString("X16")
                    : (1000 + i).ToString();
                Level level = LevelBuilder.Build(seed);
                var session = new RunSession();
                session.Begin(level);

                var bot = new AutoPilot();
                float progress = 0f;
                int guard = 0;
                while (session.Running && guard++ < 60 * 40)
                {
                    bot.Decide(session.Player, level, 1f / 60f);
                    session.Advance(1f / 60f, bot.Held, bot.Pressed);
                    progress = Math.Max(progress, session.Progress);
                }

                totalProgress += progress;
                totalMultiplier += session.Result.Multiplier;
                totalPickups += session.Result.Orbs + session.Result.Pads + session.Result.Gems;
                if (progress > 0.995f) cleared++;
                else
                {
                    string chunk = ChunkAt(level, session.Player.X);
                    if (!failureCounts.ContainsKey(chunk)) failureCounts[chunk] = 0;
                    failureCounts[chunk]++;
                    if (progress < worst)
                    {
                        worst = progress;
                        worstSeed = seed;
                        worstChunk = chunk;
                    }
                }
            }

            var ranked = new List<KeyValuePair<string, int>>(failureCounts);
            ranked.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b) { return b.Value.CompareTo(a.Value); });
            for (int i = 0; i < ranked.Count && i < 8; i++)
                Console.WriteLine("  deaths at '" + ranked[i].Key + "': " + ranked[i].Value);

            float average = totalProgress / total * 100f;
            Console.WriteLine("  autopilot (" + (shareCodes ? "share codes" : "numeric seeds") + "): "
                              + cleared + "/" + total + " seeds cleared ("
                              + (cleared * 100f / total).ToString("0.0") + "%), average progress "
                              + average.ToString("0.0") + "%, worst " + worstSeed + " at "
                              + (worst * 100f).ToString("0.0") + "% (" + worstChunk + ")");

            float averageMultiplier = totalMultiplier / total;
            Console.WriteLine("  autopilot earns x" + averageMultiplier.ToString("0.00") + " from "
                              + (totalPickups / (float)total).ToString("0.0") + " pickups per run");

            Check(cleared >= total * 0.75f, "the autopilot clears most seeds (" + cleared + "/" + total + ")");
            Check(average > 85f, "average progress across seeds is high (" + average.ToString("0.0") + "%)");
            Check(averageMultiplier > 1.2f, "pickups actually raise the multiplier (x" + averageMultiplier.ToString("0.00") + ")");
            Check(LevelBuilder.Build("1000").Gems.Count >= 8, "levels are seeded with collectable gems (" + LevelBuilder.Build("1000").Gems.Count + ")");
            Check(averageMultiplier < MultiplierLedger.Cap, "the cap is not reached by ordinary play");
        }

        static void FrameRateIndependence()
        {
            Level level = LevelBuilder.Build("4242");
            int AtRate(int stepsPerSecond)
            {
                var session = new RunSession();
                session.Begin(level);
                var bot = new AutoPilot();
                int guard = 0;
                while (session.Running && guard++ < 20000)
                {
                    bot.Decide(session.Player, level, 1f / stepsPerSecond);
                    session.Advance(1f / stepsPerSecond, bot.Held, bot.Pressed);
                }
                return session.Result.Distance;
            }

            int d60 = AtRate(60);
            int d144 = AtRate(144);
            Check(Math.Abs(d60 - d144) <= 2, "60 Hz and 144 Hz produce the same result (" + d60 + " vs " + d144 + " blocks)");
        }

        static void TraceShipChunk(string chunkName)
        {
            Chunk chunk = null;
            foreach (Chunk c in ChunkLibrary.ShipChunks) if (c.Name == chunkName) chunk = c;
            if (chunk == null) { Console.WriteLine("no such ship chunk: " + chunkName); return; }

            Level level = ChunkLevel(chunk, true);
            Console.WriteLine("portals: " + level.Portals.Count + "  tiles: " + level.Tiles.Count + "  width: " + chunk.Width);
            var session = new RunSession();
            session.Begin(level);
            session.Player.X = 6f;
            var bot = new AutoPilot();
            int frame = 0;
            while (session.Running && frame < 60 * 20)
            {
                bot.Decide(session.Player, level, 1f / 60f);
                session.Advance(1f / 60f, bot.Held, bot.Pressed);
                if (frame % 6 == 0 || !session.Player.Alive)
                    Console.WriteLine("x=" + session.Player.X.ToString("0.00") + " y=" + session.Player.Y.ToString("0.00")
                                      + " vy=" + session.Player.VY.ToString("0.0") + " mode=" + session.Player.Mode
                                      + " held=" + bot.Held + " alive=" + session.Player.Alive);
                if (!session.Player.Alive) break;
                frame++;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        static Level EmptyLevel(float length)
        {
            var level = new Level();
            level.Seed = "test";
            level.Length = length;
            level.Ceiling = GameConstants.CeilingY;
            return level;
        }

        static Level ChunkLevel(Chunk chunk, bool shipSection)
        {
            var level = EmptyLevel(90f);
            level.Seed = "chunk";
            if (shipSection) level.Portals.Add(new Portal(PortalKind.ShipPortal, 14f, 0f));
            InjectChunk(level, chunk, 22f);
            if (shipSection) level.Portals.Add(new Portal(PortalKind.CubePortal, 22f + chunk.Width + 6f, 0f));
            return level;
        }

        static void InjectChunk(Level level, Chunk chunk, float x)
        {
            for (int row = 0; row < chunk.Rows.Length; row++)
            {
                string line = chunk.Rows[row];
                for (int col = 0; col < line.Length; col++)
                {
                    char c = line[col];
                    float px = x + col;
                    float py = row;
                    switch (c)
                    {
                        case 'B': level.Tiles.Add(new Tile(TileKind.Block, px, py)); break;
                        case '^': level.Tiles.Add(new Tile(TileKind.Spike, px, py)); break;
                        case 'v': level.Tiles.Add(new Tile(TileKind.CeilingSpike, px, py)); break;
                        case 'Y': level.Orbs.Add(new Orb(OrbKind.Yellow, px + 0.5f, py)); break;
                        case 'p': level.Orbs.Add(new Orb(OrbKind.Pink, px + 0.5f, py)); break;
                        case 'g': level.Orbs.Add(new Orb(OrbKind.Blue, px + 0.5f, py)); break;
                        case 'P': level.Pads.Add(new Pad(PadKind.Yellow, px, py)); break;
                        case 'q': level.Pads.Add(new Pad(PadKind.Pink, px, py)); break;
                        case 'F': level.Portals.Add(new Portal(PortalKind.GravityFlip, px, py)); break;
                        case 'N': level.Portals.Add(new Portal(PortalKind.GravityNormal, px, py)); break;
                        case 'S': level.Portals.Add(new Portal(PortalKind.ShipPortal, px, py)); break;
                        case 'C': level.Portals.Add(new Portal(PortalKind.CubePortal, px, py)); break;
                    }
                }
            }
        }

        static float Play(Level level, float startX)
        {
            var session = new RunSession();
            session.Begin(level);
            session.Player.X = startX;

            var bot = new AutoPilot();
            int guard = 0;
            while (session.Running && guard++ < 60 * 20)
            {
                bot.Decide(session.Player, level, 1f / 60f);
                session.Advance(1f / 60f, bot.Held, bot.Pressed);
            }
            return session.Player.X;
        }

        static string ChunkAt(Level level, float x)
        {
            string found = "unknown";
            for (int i = 0; i < level.ChunkStarts.Count && i < level.ChunkNames.Count; i++)
                if (x >= level.ChunkStarts[i]) found = level.ChunkNames[i];
            return found;
        }

        static void Check(bool condition, string name)
        {
            if (condition)
            {
                passed++;
                Console.WriteLine("  PASS  " + name);
            }
            else
            {
                failed++;
                failures.Add(name);
                Console.WriteLine("  FAIL  " + name);
            }
        }
    }
}
