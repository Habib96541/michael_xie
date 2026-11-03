using System;
using System.Collections.Generic;

namespace NeonDash.Core
{
    public enum TileKind
    {
        /// <summary>Solid 1x1 block. Can be landed on from above.</summary>
        Block,
        /// <summary>Floor spike, apex pointing up. Fatal to touch.</summary>
        Spike,
        /// <summary>Ceiling spike, apex pointing down (used in flipped-gravity sections).</summary>
        CeilingSpike
    }

    public enum PlayerMode { Cube, Ship }

    public enum PortalKind
    {
        /// <summary>Restore normal gravity (downwards).</summary>
        GravityNormal,
        /// <summary>Flip gravity (upwards).</summary>
        GravityFlip,
        /// <summary>Enter ship mode.</summary>
        ShipPortal,
        /// <summary>Return to cube mode.</summary>
        CubePortal
    }

    public enum PadKind { Yellow, Pink }
    public enum OrbKind { Yellow, Pink, Blue }

    /// <summary>Multiplier gems. They never touch the physics: they are collected on contact.</summary>
    public enum GemKind
    {
        /// <summary>Sits in the jump arc over a hazard: you collect it by clearing the obstacle.</summary>
        Arc,
        /// <summary>Sits above a clear stretch: only reachable by jumping where you did not have to.</summary>
        High
    }

    [Serializable]
    public struct Gem
    {
        public GemKind Kind;
        public float X;
        public float Y;

        public Gem(GemKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public struct Tile
    {
        public TileKind Kind;
        public float X;
        public float Y;

        public Tile(TileKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }

        public float Top { get { return Y + 1f; } }
        public float Bottom { get { return Y; } }
        public float CenterY { get { return Y + 0.5f; } }
    }

    [Serializable]
    public struct Portal
    {
        public PortalKind Kind;
        public float X;
        /// <summary>Base of the portal; it occupies Y..Y+PortalHeight.</summary>
        public float Y;

        public Portal(PortalKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public struct Pad
    {
        public PadKind Kind;
        public float X;
        public float Y;

        public Pad(PadKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public struct Orb
    {
        public OrbKind Kind;
        public float X;
        /// <summary>Centre of the orb.</summary>
        public float Y;

        public Orb(OrbKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// A generated level. Produced by <see cref="LevelBuilder"/> from a seed string, so the same
    /// seed always yields the same layout (that is what makes seeded score chasing meaningful).
    /// </summary>
    public sealed class Level
    {
        public string Seed = "0000";
        public float Length = GameConstants.CourseLength;
        public float Ceiling = GameConstants.CeilingY;
        public readonly List<Tile> Tiles = new List<Tile>();
        public readonly List<Portal> Portals = new List<Portal>();
        public readonly List<Pad> Pads = new List<Pad>();
        public readonly List<Orb> Orbs = new List<Orb>();
        /// <summary>Multiplier gems scattered along the run (see <see cref="Gem"/>).</summary>
        public readonly List<Gem> Gems = new List<Gem>();
        /// <summary>Names of the chunks used, in order. Handy for debugging a seed.</summary>
        public readonly List<string> ChunkNames = new List<string>();
        /// <summary>World x where each chunk starts, parallel to <see cref="ChunkNames"/>.</summary>
        public readonly List<float> ChunkStarts = new List<float>();

        public float Progress(float distance)
        {
            return ScoreRules.Progress(distance, Length);
        }

        /// <summary>Highest point of any structure, used to frame the camera.</summary>
        public float HighestStructure()
        {
            float top = 0f;
            for (int i = 0; i < Tiles.Count; i++)
                if (Tiles[i].Top > top) top = Tiles[i].Top;
            return top;
        }
    }
}
