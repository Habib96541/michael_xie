using System;
using System.Collections.Generic;

namespace NeonDash.Core
{
    /// <summary>
    /// Turns a seed string into a level by shuffling the authored chunk library.
    ///
    /// Section plan, in order: a cube warm-up, then repeating sections that are mostly cube
    /// runs with occasional ship and flipped-gravity detours. Because every chunk is authored
    /// to be clearable from a flat approach, any shuffle of them stays playable, while the order
    /// still meaningfully changes the level - which is the point of seed chasing.
    /// </summary>
    public static class LevelBuilder
    {
        const float SectionGap = 2f;

        public static Level Build(string seed)
        {
            string stableSeed = NormaliseSeed(seed);
            var rng = new DetRandom(stableSeed);
            var level = new Level();
            level.Seed = stableSeed;
            level.Length = GameConstants.CourseLength;
            level.Ceiling = GameConstants.CeilingY;

            float x = 0f;
            Place(level, ChunkLibrary.Intro, ref x);
            x = GameConstants.WarmupDistance;
            level.ChunkNames.Add("warmup");

            int section = 0;
            int guard = 0;
            while (x < level.Length - 34f && guard++ < 80)
            {
                float roll = rng.NextFloat();
                bool lateEnoughForShip = x > level.Length * 0.22f;
                bool lateEnoughForGravity = x > level.Length * 0.16f;

                if (lateEnoughForShip && roll < 0.24f)
                {
                    x = BuildShipSection(level, rng, x);
                }
                else if (lateEnoughForGravity && roll < 0.46f)
                {
                    x = BuildGravitySection(level, rng, x);
                }
                else
                {
                    x = BuildCubeSection(level, rng, x);
                }

                x += SectionGap;
                section++;
            }

            // Guarantee a calm run-in to the finish line.
            level.ChunkNames.Add("outro");

            AddGemTrails(level, rng);
            return level;
        }

        /// <summary>
        /// Scatters multiplier gems along the level. Every gem sits above the run line, so it is
        /// only ever collected by jumping: over a hazard the gem sits in the arc you already fly
        /// through, and on open ground it floats high enough that grabbing it means taking a jump
        /// you did not have to take. That is the whole earning curve - safe play earns little, a
        /// greedy line earns a lot. Gems follow the local gravity, so flipped sections get theirs
        /// on the ceiling side.
        /// </summary>
        static void AddGemTrails(Level level, DetRandom rng)
        {
            float x = GameConstants.WarmupDistance + 5f;
            float end = level.Length - 12f;
            int guard = 0;

            while (x < end && guard++ < 160)
            {
                x += rng.Range(9f, 15f);
                if (x >= end) break;
                if (NearPortal(level, x)) continue;

                bool flipped = IsFlippedAt(level, x);
                float surface = flipped ? level.Ceiling : 0f;
                float dir = flipped ? -1f : 1f;
                bool overHazard = HasHazardNear(level, x, 2.4f);

                GemKind kind = overHazard ? GemKind.Arc : GemKind.High;
                float y = surface + dir * (overHazard ? 2.3f : 3f);

                if (!GemCellClear(level, x, y)) continue;
                level.Gems.Add(new Gem(kind, x, y));
                x += 1.5f;
            }
        }

        /// <summary>True when the level has gravity flipped at world x, judging by the portals.</summary>
        static bool IsFlippedAt(Level level, float x)
        {
            bool flipped = false;
            for (int i = 0; i < level.Portals.Count; i++)
            {
                Portal portal = level.Portals[i];
                if (portal.X > x) continue;
                if (portal.Kind == PortalKind.GravityFlip) flipped = true;
                else if (portal.Kind == PortalKind.GravityNormal) flipped = false;
            }
            return flipped;
        }

        /// <summary>Keeps generated gems out of the full-height portal gates.</summary>
        static bool NearPortal(Level level, float x)
        {
            for (int i = 0; i < level.Portals.Count; i++)
                if (Math.Abs(level.Portals[i].X + 0.5f - x) < 2f) return true;
            return false;
        }

        static bool HasHazardNear(Level level, float x, float radius)
        {
            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile tile = level.Tiles[i];
                if (Math.Abs(tile.X + 0.5f - x) > radius) continue;
                if (tile.Kind == TileKind.Spike) return true;
                if (tile.Kind == TileKind.CeilingSpike) return true;
            }
            return false;
        }

        /// <summary>A gem needs clear air: no block to sit inside and no other pickup nearby.</summary>
        static bool GemCellClear(Level level, float x, float y)
        {
            if (y < 1.2f || y > level.Ceiling - 0.9f) return false;

            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile tile = level.Tiles[i];
                if (Math.Abs(tile.X + 0.5f - x) < 0.9f && Math.Abs(tile.CenterY - y) < 0.9f) return false;
            }
            for (int i = 0; i < level.Orbs.Count; i++)
            {
                Orb orb = level.Orbs[i];
                float dx = orb.X - x;
                float dy = orb.Y - y;
                if (dx * dx + dy * dy < 1.1f * 1.1f) return false;
            }
            for (int i = 0; i < level.Pads.Count; i++)
            {
                Pad pad = level.Pads[i];
                float dx = pad.X + 0.5f - x;
                float dy = pad.Y - y;
                if (dx * dx + dy * dy < 1.2f * 1.2f) return false;
            }
            for (int i = 0; i < level.Gems.Count; i++)
            {
                Gem gem = level.Gems[i];
                float dx = gem.X - x;
                float dy = gem.Y - y;
                if (dx * dx + dy * dy < 1.1f * 1.1f) return false;
            }
            return true;
        }

        static string NormaliseSeed(string seed)
        {
            if (string.IsNullOrEmpty(seed)) return "0000";
            string trimmed = seed.Trim();
            if (trimmed.Length == 0) return "0000";
            if (trimmed.Length > 10) trimmed = trimmed.Substring(0, 10);
            return trimmed;
        }

        static float BuildCubeSection(Level level, DetRandom rng, float x)
        {
            int count = rng.Range(2, 4);
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                int pick = PickDifferent(rng, ChunkLibrary.CubeChunks.Count, last);
                last = pick;
                Chunk chunk = ChunkLibrary.CubeChunks[pick];
                if (x + chunk.Width > level.Length - 8f) break;
                Place(level, chunk, ref x);
                x += rng.Range(1f, 3f);
            }
            return x;
        }

        static float BuildGravitySection(Level level, DetRandom rng, float x)
        {
            if (x + ChunkLibrary.GravityEnter.Width + 40f > level.Length - 10f) return x;
            Place(level, ChunkLibrary.GravityEnter, ref x);
            x += 3f;

            int count = rng.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                int pick = rng.Range(0, ChunkLibrary.FlippedChunks.Count);
                Chunk chunk = ChunkLibrary.FlippedChunks[pick];
                Place(level, chunk, ref x);
                x += rng.Range(1f, 2.5f);
            }

            Place(level, ChunkLibrary.GravityExit, ref x);
            return x;
        }

        static float BuildShipSection(Level level, DetRandom rng, float x)
        {
            if (x + ChunkLibrary.ShipEnter.Width + 44f > level.Length - 10f) return x;
            Place(level, ChunkLibrary.ShipEnter, ref x);
            // Give the ship room to settle on its line before the first wall.
            x += 8f;

            int count = rng.Range(2, 4);
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                int pick = PickDifferent(rng, ChunkLibrary.ShipChunks.Count, last);
                last = pick;
                Place(level, ChunkLibrary.ShipChunks[pick], ref x);
                x += rng.Range(1f, 2.5f);
            }

            Place(level, ChunkLibrary.ShipExit, ref x);
            return x;
        }

        static int PickDifferent(DetRandom rng, int count, int last)
        {
            if (count <= 1) return 0;
            int pick = rng.Range(0, count);
            if (pick == last) pick = (pick + 1 + rng.Range(0, count - 1)) % count;
            return pick;
        }

        static void Place(Level level, Chunk chunk, ref float x)
        {
            level.ChunkStarts.Add(x);
            for (int row = 0; row < chunk.Rows.Length; row++)
            {
                string line = chunk.Rows[row];
                for (int col = 0; col < line.Length; col++)
                {
                    char c = line[col];
                    if (c == '.') continue;
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
                        // Portals are full-height gates, so they always start at the floor.
                        case 'F': level.Portals.Add(new Portal(PortalKind.GravityFlip, px, 0f)); break;
                        case 'N': level.Portals.Add(new Portal(PortalKind.GravityNormal, px, 0f)); break;
                        case 'S': level.Portals.Add(new Portal(PortalKind.ShipPortal, px, 0f)); break;
                        case 'C': level.Portals.Add(new Portal(PortalKind.CubePortal, px, 0f)); break;
                        default:
                            throw new ArgumentException("Unknown chunk glyph '" + c + "' in chunk " + chunk.Name);
                    }
                }
            }
            level.ChunkNames.Add(chunk.Name);
            x += chunk.Width;
        }
    }
}
