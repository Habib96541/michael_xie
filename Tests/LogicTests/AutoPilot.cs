using System;
using NeonDash.Core;

namespace NeonDash.Tests
{
    /// <summary>
    /// A deliberately simple autopilot used to prove that generated levels are actually
    /// playable. It has no knowledge of the chunk library: it only looks at the level geometry
    /// and reacts the way a decent human player would (jump so the arc clears the whole obstacle,
    /// tap orbs when close, hold to keep the ship inside the corridor).
    /// </summary>
    public sealed class AutoPilot
    {
        public bool Held;
        public bool Pressed;
        float pressCooldown;

        public void Decide(PlayerSim player, Level level, float dt)
        {
            Pressed = false;
            Held = false;
            if (pressCooldown > 0f) pressCooldown -= dt;

            if (player.Mode == PlayerMode.Cube) DecideCube(player, level);
            else DecideShip(player, level);
        }

        void DecideCube(PlayerSim player, Level level)
        {
            if (!player.Grounded)
            {
                if (pressCooldown > 0f) return;
                for (int i = 0; i < level.Orbs.Count; i++)
                {
                    Orb orb = level.Orbs[i];
                    float dx = orb.X - player.X;
                    float dy = orb.Y - (player.CenterY + 0.25f);
                    if (dx * dx + dy * dy > 0.85f * 0.85f) continue;
                    Pressed = true;
                    pressCooldown = 0.14f;
                    return;
                }
                return;
            }

            float nearestStart = float.MaxValue;
            float farEnd = -1f;
            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile tile = level.Tiles[i];
                bool relevant = player.GravitySign > 0
                    ? tile.Kind != TileKind.CeilingSpike
                    : tile.Kind != TileKind.Spike;
                if (!relevant) continue;

                float start = tile.X;
                float end = tile.X + 1f;
                if (end < player.X + 0.15f) continue;
                if (start > player.X + 7f) continue;
                if (start < nearestStart) nearestStart = start;
                if (end > farEnd) farEnd = end;
            }

            if (farEnd < 0f) return;
            float toClear = farEnd - player.X;
            // A jump covers ~5.4 blocks and peaks at 2.6: take off just far enough back that the
            // landing point is past the obstacle instead of on top of it.
            if (toClear <= 4.85f && toClear >= 0.9f && nearestStart - player.X >= 0.35f)
                Pressed = true;
        }

        void DecideShip(PlayerSim player, Level level)
        {
            // Find the nearest wall group ahead and aim for the middle of its opening.
            float bestDx = float.MaxValue;
            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile tile = level.Tiles[i];
                if (tile.Kind != TileKind.Block) continue;
                float dx = tile.X - player.X;
                if (dx < -1.6f || dx > 14f) continue;
                if (dx < bestDx) bestDx = dx;
            }

            float target = 4.5f;
            if (bestDx < float.MaxValue)
            {
                bool floorWall = false;
                bool ceilingWall = false;
                float floorTop = 0f;
                float ceilingBottom = GameConstants.CeilingY;
                for (int i = 0; i < level.Tiles.Count; i++)
                {
                    Tile tile = level.Tiles[i];
                    if (tile.Kind != TileKind.Block) continue;
                    float dx = tile.X - player.X;
                    if (Math.Abs(dx - bestDx) > 1.6f) continue;
                    if (tile.Y < 4.5f) { floorWall = true; floorTop = Math.Max(floorTop, tile.Top); }
                    else { ceilingWall = true; ceilingBottom = Math.Min(ceilingBottom, tile.Bottom); }
                }
                if (floorWall && ceilingWall) target = (floorTop + ceilingBottom) * 0.5f;
                else if (floorWall) target = floorTop + 2.2f;
                else if (ceilingWall) target = ceilingBottom - 2.2f;
            }

            if (target < 1.2f) target = 1.2f;
            if (target > GameConstants.CeilingY - 1.2f) target = GameConstants.CeilingY - 1.2f;

            // Proportional control on vertical speed, like a player feathering the button:
            // ask for more climb the further below the opening the ship is, and bleed off the
            // climb before arriving so it does not overshoot into the opposite wall.
            float desiredVy = (target - player.CenterY) * 6f;
            if (desiredVy > GameConstants.ShipMaxSpeedY) desiredVy = GameConstants.ShipMaxSpeedY;
            if (desiredVy < -GameConstants.ShipMaxSpeedY) desiredVy = -GameConstants.ShipMaxSpeedY;
            Held = player.VY < desiredVy - 0.6f;
        }
    }
}
