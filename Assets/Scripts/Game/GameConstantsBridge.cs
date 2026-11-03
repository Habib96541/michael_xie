namespace NeonDash
{
    /// <summary>
    /// The handful of simulation constants the view layer needs. Kept in one place so the view
    /// never hard-codes magic numbers that the simulation also owns.
    /// </summary>
    public static class GameConstantsBridge
    {
        public const float PortalWidth = Core.GameConstants.PortalWidth;
        public const float PortalHeight = Core.GameConstants.PortalHeight;
        public const float CeilingY = Core.GameConstants.CeilingY;
        public const float BlockSpeed = Core.GameConstants.BlockSpeed;
        public const float RoundSeconds = Core.GameConstants.RoundSeconds;
        public const float CourseLength = Core.GameConstants.CourseLength;
        public const float MultiplierRollSeconds = Core.GameConstants.MultiplierRollSeconds;
        public const float MinimumMultiplier = Core.GameConstants.MinimumMultiplier;
        public const float MaximumMultiplier = Core.GameConstants.MaximumMultiplier;
    }
}
