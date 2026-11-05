using System;

namespace ChickenRush
{
    // Counter-based rolls: waiting, frame rate and cosmetic traffic never consume RNG.
    // This is a reproducible virtual-coin game, not a certified gambling RNG.
    public static class SeededRisk
    {
        public static double Roll(int seed,int hop)
        {
            unchecked
            {
                uint value=(uint)seed+(uint)hop*0x9E3779B9u;
                value=(value^(value>>16))*0x85EBCA6Bu;
                value=(value^(value>>13))*0xC2B2AE35u;
                return (value^(value>>16))/4294967296.0;
            }
        }
        public static bool Survives(int seed,int hop,float probability)
        {
            if(float.IsNaN(probability) || probability<0 || probability>1)throw new ArgumentOutOfRangeException(nameof(probability));
            return Roll(seed,hop)<probability;
        }
    }
}
