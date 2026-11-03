namespace NeonDash.Core
{
    /// <summary>
    /// All gameplay tuning lives here so the simulation layer stays pure C# and can be
    /// compiled and tested outside Unity. Units: 1 world unit = 1 grid block (Geometry Dash
    /// uses 30px blocks; 1x speed there is 10.39 blocks/second, which is what BlockSpeed
    /// reproduces).
    /// </summary>
    public static class GameConstants
    {
        // ---- Run pacing ----------------------------------------------------
        /// <summary>Forward scroll speed in blocks per second (Geometry Dash 1x speed).</summary>
        public const float BlockSpeed = 10.4f;
        /// <summary>A run is 30 seconds long.</summary>
        public const float RoundSeconds = 30f;
        /// <summary>312 blocks: exactly 30 seconds of travel at BlockSpeed.</summary>
        public const float CourseLength = BlockSpeed * RoundSeconds;
        /// <summary>Fixed simulation rate. Deterministic: identical inputs give identical runs.</summary>
        public const float FixedStep = 1f / 120f;

        // ---- Playfield -----------------------------------------------------
        public const float CeilingY = 9f;
        /// <summary>Empty space before the first hazard, so runs start smoothly.</summary>
        public const float WarmupDistance = 22f;
        /// <summary>The run starts at x=0 so one course length is exactly 30 seconds of travel.</summary>
        public const float PlayerStartX = 0f;

        // ---- Cube ----------------------------------------------------------
        /// <summary>Jump impulse (blocks/sec). With gravity 76.9 this peaks at 2.6 blocks.</summary>
        public const float CubeJumpVelocity = 20f;
        public const float CubeGravity = 76.9f;
        public const float CubeTerminalSpeed = 34f;
        /// <summary>Hitbox is deliberately smaller than the sprite, like Geometry Dash.</summary>
        public const float CubeHalfWidth = 0.30f;
        public const float CubeHalfHeight = 0.30f;
        /// <summary>Cosmetic spin while airborne; the cube snaps flat on landing.</summary>
        public const float CubeSpinSpeed = 720f;
        /// <summary>Time the view takes to settle the cube onto a 90 degree multiple.</summary>
        public const float CubeLandSnapSeconds = 0.07f;

        // ---- Ship ----------------------------------------------------------
        public const float ShipGravity = 52f;
        public const float ShipThrust = 96f;
        public const float ShipMaxSpeedY = 13f;
        public const float ShipHalfWidth = 0.32f;
        public const float ShipHalfHeight = 0.22f;

        // ---- Pads and orbs -------------------------------------------------
        public const float YellowPadScale = 1.42f;
        public const float PinkPadScale = 1.18f;
        public const float YellowOrbScale = 1.0f;
        public const float PinkOrbScale = 0.72f;
        public const float BlueOrbScale = 0.65f;
        public const float OrbRadius = 1.15f;
        /// <summary>How close the player has to pass to a multiplier gem for it to count.</summary>
        public const float GemRadius = 0.85f;
        public const float PadHalfWidth = 0.55f;
        public const float PadTriggerDepth = 0.55f;
        public const float OrbCooldownSeconds = 0.12f;

        // ---- Portals -------------------------------------------------------
        public const float PortalWidth = 1.2f;
        /// <summary>
        /// Portals are full-height gates: a mode or gravity switch must never be skippable by
        /// flying over or under it, so the gate and its trigger both span the whole playfield.
        /// </summary>
        public const float PortalHeight = CeilingY;

        // ---- Scoring -------------------------------------------------------
        public const float MinimumMultiplier = 0.1f;
        public const float MaximumMultiplier = 10f;
        /// <summary>Points per block before the multiplier is applied.</summary>
        public const int PointsPerBlock = 100;
        /// <summary>How long the multiplier reel spins before it settles.</summary>
        public const float MultiplierRollSeconds = 1.7f;
    }
}
