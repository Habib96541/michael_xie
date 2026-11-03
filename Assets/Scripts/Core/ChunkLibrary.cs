using System;
using System.Collections.Generic;

namespace NeonDash.Core
{
    public enum ChunkKind
    {
        /// <summary>Playable in cube form with normal gravity.</summary>
        Cube,
        /// <summary>Needs flipped gravity; the section starts with a gravity portal.</summary>
        Flipped,
        /// <summary>Needs ship mode; only placed inside a ship section.</summary>
        Ship,
        /// <summary>Opens or closes a section (contains the mode/gravity portal).</summary>
        Transition
    }

    /// <summary>
    /// One hand-authored pattern. Rows are listed bottom-up: <c>Rows[0]</c> is the y=0 line and
    /// every character is one block wide. This mirrors how Geometry Dash levels are authored:
    /// a designer builds provably playable fragments, and the seed only reshuffles their order.
    /// </summary>
    public sealed class Chunk
    {
        public readonly string Name;
        public readonly string[] Rows;
        public readonly ChunkKind Kind;
        public readonly int Width;

        public Chunk(string name, ChunkKind kind, string[] rowsBottomUp)
        {
            Name = name;
            Kind = kind;
            Rows = rowsBottomUp;
            Width = rowsBottomUp.Length == 0 ? 0 : rowsBottomUp[0].Length;
            for (int i = 0; i < rowsBottomUp.Length; i++)
            {
                if (rowsBottomUp[i].Length != Width)
                    throw new ArgumentException("Chunk '" + name + "' has ragged rows (" + rowsBottomUp[i].Length + " vs " + Width + ").");
            }
        }
    }

    /// <summary>
    /// The authored vocabulary. Legend:
    ///   .  empty        B  block          ^  floor spike    v  ceiling spike
    ///   Y  yellow orb   p  pink orb       g  blue (gravity) orb
    ///   P  yellow pad   q  pink pad
    ///   F  gravity flip portal             N  gravity normal portal
    ///   S  ship portal                     C  cube portal
    /// Portals occupy 3 rows starting at the row they are placed on.
    /// </summary>
    public static class ChunkLibrary
    {
        public static readonly List<Chunk> CubeChunks = new List<Chunk>();
        public static readonly List<Chunk> FlippedChunks = new List<Chunk>();
        public static readonly List<Chunk> ShipChunks = new List<Chunk>();

        public static readonly Chunk GravityEnter;
        public static readonly Chunk GravityExit;
        public static readonly Chunk ShipEnter;
        public static readonly Chunk ShipExit;
        public static readonly Chunk Intro;
        public static readonly Chunk Flat;

        static ChunkLibrary()
        {
            Intro = new Chunk("intro", ChunkKind.Cube, new[]
            {
                "................"
            });

            Flat = new Chunk("flat", ChunkKind.Cube, new[]
            {
                ".........."
            });

            // ---------------- cube patterns ----------------
            CubeChunks.Add(new Chunk("spike", ChunkKind.Cube, new[]
            {
                "......^....."
            }));

            CubeChunks.Add(new Chunk("double-spike", ChunkKind.Cube, new[]
            {
                "......^^....."
            }));

            CubeChunks.Add(new Chunk("triple-spike", ChunkKind.Cube, new[]
            {
                "......^^^....."
            }));

            CubeChunks.Add(new Chunk("block-hop", ChunkKind.Cube, new[]
            {
                "......B....."
            }));

            CubeChunks.Add(new Chunk("block-pair", ChunkKind.Cube, new[]
            {
                "......BB....."
            }));

            CubeChunks.Add(new Chunk("block-spike", ChunkKind.Cube, new[]
            {
                "......B....^.."
            }));

            CubeChunks.Add(new Chunk("spike-then-block", ChunkKind.Cube, new[]
            {
                "......^...B.."
            }));

            CubeChunks.Add(new Chunk("stair-step", ChunkKind.Cube, new[]
            {
                "......B.........",
                ".......B........"
            }));

            CubeChunks.Add(new Chunk("pad-launch", ChunkKind.Cube, new[]
            {
                ".....P...^^^...."
            }));

            CubeChunks.Add(new Chunk("pink-pad-spike", ChunkKind.Cube, new[]
            {
                ".....q...^......"
            }));

            // These two used to carry an orb above the spikes. Orbs are gone from the game, so the
            // rows keep their shape (chunk heights stay stable for every seed) with nothing there.
            CubeChunks.Add(new Chunk("spike-run-long", ChunkKind.Cube, new[]
            {
                "......^^^^......",
                "................",
                "................",
                "................"
            }));

            CubeChunks.Add(new Chunk("spike-run-short", ChunkKind.Cube, new[]
            {
                "......^^^.......",
                "................",
                "................",
                "................"
            }));

            CubeChunks.Add(new Chunk("block-gap-block", ChunkKind.Cube, new[]
            {
                "....B.......B..."
            }));

            CubeChunks.Add(new Chunk("spike-block-spike", ChunkKind.Cube, new[]
            {
                "....^....B....^.."
            }));

            // ---------------- flipped gravity patterns ----------------
            GravityEnter = new Chunk("gravity-enter", ChunkKind.Transition, new[]
            {
                "......F....."
            });

            // The exit portal sits on the ceiling line, because in flipped gravity that is where
            // the cube is travelling. Placing it on the floor would leave the player inverted.
            GravityExit = new Chunk("gravity-exit", ChunkKind.Transition, new[]
            {
                "............",
                "............",
                "............",
                "............",
                "............",
                "............",
                "............",
                "............",
                "......N....."
            });

            FlippedChunks.Add(new Chunk("ceiling-spikes", ChunkKind.Flipped, new[]
            {
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "......vvv....."
            }));

            FlippedChunks.Add(new Chunk("ceiling-single", ChunkKind.Flipped, new[]
            {
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "......v......."
            }));

            FlippedChunks.Add(new Chunk("ceiling-pair", ChunkKind.Flipped, new[]
            {
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "......vv......"
            }));

            FlippedChunks.Add(new Chunk("ceiling-spikes-gap", ChunkKind.Flipped, new[]
            {
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "....vv...vv..."
            }));

            // ---------------- ship patterns ----------------
            // The ship flies between y=0 and the ceiling. Walls are 3 blocks tall, leaving a
            // generous 6 block corridor so the section reads as flowing rather than surgical.
            ShipEnter = new Chunk("ship-enter", ChunkKind.Transition, new[]
            {
                "......S......"
            });

            ShipExit = new Chunk("ship-exit", ChunkKind.Transition, new[]
            {
                "......C......"
            });

            ShipChunks.Add(new Chunk("ship-floor-wall", ChunkKind.Ship, new[]
            {
                "....BB.......",
                "....BB.......",
                "....BB......."
            }));

            ShipChunks.Add(new Chunk("ship-ceiling-wall", ChunkKind.Ship, new[]
            {
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "..............",
                "....BB........",
                "....BB........",
                "....BB........"
            }));

            // Two gentler corridor patterns: climb over a floor wall, then duck under a ceiling
            // wall, with plenty of travel between them.
            ShipChunks.Add(new Chunk("ship-gate", ChunkKind.Ship, new[]
            {
                "....BB.............",
                "....BB.............",
                "....BB.............",
                "...................",
                "...................",
                "...................",
                "..............BB...",
                "..............BB...",
                "..............BB..."
            }));

            ShipChunks.Add(new Chunk("ship-window", ChunkKind.Ship, new[]
            {
                "......BB........",
                "......BB........",
                "................",
                "................",
                "................",
                "................",
                "................",
                "......BB........",
                "......BB........"
            }));
        }

        public static Chunk FindCube(string name)
        {
            for (int i = 0; i < CubeChunks.Count; i++) if (CubeChunks[i].Name == name) return CubeChunks[i];
            return null;
        }
    }
}
