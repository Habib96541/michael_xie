namespace NeonDash.Core
{
    /// <summary>
    /// Deterministic xorshift RNG seeded from a text seed. System.Random is avoided so the
    /// layout is identical across runtimes (editor, WebGL, tests).
    /// </summary>
    public sealed class DetRandom
    {
        uint state;
        ulong wideState;
        readonly bool useWideState;

        public DetRandom(string seed)
        {
            string text = (seed ?? string.Empty).Trim();
            useWideState = SeedCode.TryGetState(text, out wideState);
            state = Hash(text);
            if (state == 0u) state = 0x9E3779B9u;
        }

        public DetRandom(uint seed)
        {
            state = seed == 0u ? 0x9E3779B9u : seed;
        }

        /// <summary>FNV-1a hash; stable across platforms and .NET versions.</summary>
        public static uint Hash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        public uint NextUInt()
        {
            unchecked
            {
                if (useWideState)
                {
                    // SplitMix64 uses all 64 bits of a share code. Legacy seeds keep xorshift32.
                    ulong z = wideState += 0x9E3779B97F4A7C15ul;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ul;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBul;
                    return (uint)((z ^ (z >> 31)) >> 32);
                }
                uint x = state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                state = x;
                return x;
            }
        }

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) / 16777216f;
        }

        /// <summary>Uniform float in [min,max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Uniform integer in [min,maxExclusive).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextUInt() % (uint)(maxExclusive - min));
        }

        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>Picks an index using per-entry weights. Returns -1 for an empty set.</summary>
        public int WeightedPick(float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;
            float roll = NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                roll -= weights[i];
                if (roll <= 0f) return i;
            }
            return weights.Length - 1;
        }
    }
}
