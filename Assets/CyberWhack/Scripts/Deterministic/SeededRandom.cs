using System;
using UnityEngine;

namespace CyberWhack.Deterministic
{
    /// <summary>
    /// High-performance, zero-allocation pseudo-random number generator (PCG32).
    /// Guarantees identical deterministic sequence output across all platforms (WebGL, iOS, Android, Desktop)
    /// given the same numerical match seed.
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;
        private ulong _inc;

        public int CurrentSeed { get; private set; }

        public SeededRandom(int seed = 1337)
        {
            Init(seed);
        }

        public void Init(int seed)
        {
            CurrentSeed = seed;
            _state = 0UL;
            _inc = ((ulong)seed << 1) | 1UL;
            NextUInt();
            _state += (ulong)seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = oldState * 6364136223846793005UL + _inc;
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>
        /// Returns a float in [0.0f, 1.0f). Zero allocations.
        /// </summary>
        public float NextFloat()
        {
            return (float)(NextUInt() / (double)4294967296.0);
        }

        /// <summary>
        /// Returns a float in [min, max). Zero allocations.
        /// </summary>
        public float NextFloat(float min, float max)
        {
            if (max <= min) return min;
            return min + NextFloat() * (max - min);
        }

        /// <summary>
        /// Returns an integer in [minInclusive, maxExclusive). Zero allocations.
        /// </summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return (int)(minInclusive + (NextUInt() % range));
        }
    }
}
