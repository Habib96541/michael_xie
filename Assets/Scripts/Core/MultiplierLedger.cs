using System;

namespace NeonDash.Core
{
    public enum PickupKind
    {
        PinkOrb,
        YellowOrb,
        BlueOrb,
        PinkPad,
        YellowPad,
        /// <summary>Gem in the jump arc over a hazard.</summary>
        ArcGem,
        /// <summary>Gem high above a clear stretch: needs a jump you did not have to make.</summary>
        HighGem
    }

    /// <summary>
    /// The multiplier is earned, never rolled.
    ///
    /// A run starts at 1.00x and every orb or pad the player actually touches adds to it. Collect
    /// again within <see cref="ChainWindow"/> seconds and the chain grows, making each further
    /// pickup worth more (up to +72%), so the high multipliers come from deliberately linking
    /// pickups instead of taking the safe line. Blue orbs flip gravity and pay the most; pads fire
    /// on contact and pay the least. Nothing here is random: the same inputs always earn the same
    /// multiplier, which is what makes two runs on one seed comparable.
    /// </summary>
    public sealed class MultiplierLedger
    {
        public const float Base = 1f;
        public const float ChainWindow = 2.5f;
        public const float ChainStep = 0.12f;
        public const int ChainCap = 6;
        public const float Cap = GameConstants.MaximumMultiplier;

        public float Value { get; private set; }
        public int Orbs { get; private set; }
        public int Pads { get; private set; }
        public int Gems { get; private set; }
        public int Chain { get; private set; }
        public int BestChain { get; private set; }
        public int Pickups { get { return Orbs + Pads + Gems; } }
        /// <summary>Increments on every pickup so the HUD can flash exactly once per gain.</summary>
        public int Serial { get; private set; }
        public float LastGain { get; private set; }
        public PickupKind LastKind { get; private set; }
        public PickupKind? FirstKind { get; private set; }
        public bool HasPickup { get; private set; }
        public float LastPickupTime { get; private set; }

        public MultiplierLedger()
        {
            Reset();
        }

        public void Reset()
        {
            Value = Base;
            Orbs = 0;
            Pads = 0;
            Gems = 0;
            Chain = 0;
            BestChain = 0;
            Serial = 0;
            LastGain = 0f;
            HasPickup = false;
            LastPickupTime = 0f;
            FirstKind = null;
        }

        /// <summary>What one pickup of this kind is worth before the chain bonus.</summary>
        public static float ValueOf(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BlueOrb: return 0.45f;
                case PickupKind.YellowOrb: return 0.28f;
                case PickupKind.PinkOrb: return 0.18f;
                case PickupKind.YellowPad: return 0.34f;
                case PickupKind.PinkPad: return 0.22f;
                case PickupKind.HighGem: return 0.30f;
                default: return 0.16f;
            }
        }

        public static string NameOf(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BlueOrb: return "GRAVITY ORB";
                case PickupKind.YellowOrb: return "YELLOW ORB";
                case PickupKind.PinkOrb: return "PINK ORB";
                case PickupKind.YellowPad: return "YELLOW PAD";
                case PickupKind.PinkPad: return "PINK PAD";
                case PickupKind.HighGem: return "HIGH GEM";
                default: return "GEM";
            }
        }

        /// <summary>
        /// Registers one collected pickup at run time <paramref name="time"/>. Returns how much the
        /// multiplier actually moved (0 once the cap is reached).
        /// </summary>
        public float Collect(PickupKind kind, float time)
        {
            bool chained = HasPickup && time - LastPickupTime <= ChainWindow;
            Chain = chained ? Chain + 1 : 0;
            if (Chain > BestChain) BestChain = Chain;

            HasPickup = true;
            LastPickupTime = time;
            if (FirstKind == null) FirstKind = kind;

            float bonus = 1f + ChainStep * Math.Min(Chain, ChainCap);
            float before = Value;
            Value = Math.Min(Cap, Value + ValueOf(kind) * bonus);
            float gain = Value - before;

            if (kind == PickupKind.PinkOrb || kind == PickupKind.YellowOrb || kind == PickupKind.BlueOrb)
                Orbs++;
            else if (kind == PickupKind.PinkPad || kind == PickupKind.YellowPad)
                Pads++;
            else
                Gems++;

            LastGain = gain;
            LastKind = kind;
            Serial++;
            return gain;
        }

        /// <summary>Value rounded to the two decimals shown in the UI and stored in records.</summary>
        public float RoundedValue
        {
            get
            {
                double rounded = Math.Round(Value * 100d) / 100d;
                if (rounded > Cap) rounded = Math.Floor(Cap * 100d) / 100d;
                if (rounded < Base) rounded = Base;
                return (float)rounded;
            }
        }
    }
}
