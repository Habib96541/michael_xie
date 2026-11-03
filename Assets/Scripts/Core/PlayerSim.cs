using System;
using System.Collections.Generic;

namespace NeonDash.Core
{
    /// <summary>Events produced by one simulation step, consumed by the view and audio.</summary>
    public struct SimEvents
    {
        public bool Jumped;
        public bool Landed;
        public bool Died;
        public bool Orb;
        public int OrbIndex;
        public OrbKind OrbKind;
        public bool Pad;
        public int PadIndex;
        public PadKind PadKind;
        public bool Portal;
        public PortalKind PortalKind;
        public bool ModeChanged;
        public bool GravityChanged;
        /// <summary>A multiplier gem was touched this step (the first one, see <see cref="PlayerSim.GemsThisStep"/>).</summary>
        public bool Gem;
        public int GemIndex;
        public GemKind GemKind;

        public void Clear() { this = default(SimEvents); }
    }

    /// <summary>
    /// Kinematic player. No Rigidbody, no PhysX: Geometry Dash style movement is a fixed-step
    /// integration so identical inputs always reproduce identical results, and so the jump arc
    /// stays crisp instead of drifting with collision margins.
    ///
    /// Conventions: X = distance travelled, Y = bottom of the hitbox, VY positive = up.
    /// GravitySign +1 falls down (normal), -1 falls up (flipped, "standing" on the ceiling).
    /// </summary>
    public sealed class PlayerSim
    {
        public float X;
        public float Y;
        public float VY;
        public float PrevX;
        public float PrevY;
        public bool Grounded;
        public int GravitySign = 1;
        public PlayerMode Mode = PlayerMode.Cube;
        public bool Alive = true;
        public float Rotation;
        public float Airtime;
        public SimEvents Events;

        readonly HashSet<int> consumedPads = new HashSet<int>();
        readonly HashSet<int> consumedOrbs = new HashSet<int>();
        readonly HashSet<int> consumedPortals = new HashSet<int>();
        readonly HashSet<int> collectedGems = new HashSet<int>();
        float orbCooldown;

        /// <summary>
        /// Every gem index picked up during the current step. A single step can overlap more than
        /// one gem (they sit inside one jump arc), and all of them have to reach the view - not
        /// just whichever one happened to be written to <see cref="Events"/> last.
        /// </summary>
        public readonly List<int> GemsThisStep = new List<int>();

        public float HalfWidth
        {
            get { return Mode == PlayerMode.Cube ? GameConstants.CubeHalfWidth : GameConstants.ShipHalfWidth; }
        }

        public float HalfHeight
        {
            get { return Mode == PlayerMode.Cube ? GameConstants.CubeHalfHeight : GameConstants.ShipHalfHeight; }
        }

        public float Top { get { return Y + HalfHeight * 2f; } }
        public float CenterY { get { return Y + HalfHeight; } }
        public float CenterX { get { return X; } }

        public void Reset()
        {
            X = GameConstants.PlayerStartX;
            Y = 0f;
            VY = 0f;
            PrevX = X;
            PrevY = Y;
            Grounded = true;
            GravitySign = 1;
            Mode = PlayerMode.Cube;
            Alive = true;
            Rotation = 0f;
            Airtime = 0f;
            orbCooldown = 0f;
            consumedPads.Clear();
            consumedOrbs.Clear();
            consumedPortals.Clear();
            collectedGems.Clear();
            GemsThisStep.Clear();
            Events.Clear();
        }

        /// <summary>Advances one fixed step. Pressed is a fresh tap this step; Held is the button state.</summary>
        public void Step(float dt, Level level, bool held, bool pressed)
        {
            Events.Clear();
            GemsThisStep.Clear();
            if (!Alive || level == null) return;

            PrevX = X;
            PrevY = Y;
            X += GameConstants.BlockSpeed * dt;

            if (Mode == PlayerMode.Cube) StepCube(dt, level, held, pressed);
            else StepShip(dt, held);

            CollectGems(level);
            CheckPortals(level);
            CheckPads(level);
            CheckHazards(level);

            if (X > level.Length) X = level.Length;
        }

        // ------------------------------------------------------------------
        // Cube
        // ------------------------------------------------------------------
        void StepCube(float dt, Level level, bool held, bool pressed)
        {
            if (orbCooldown > 0f) orbCooldown -= dt;

            if (Grounded)
            {
                float support = SupportY(level);
                if (Math.Abs(Y - support) > 0.06f)
                {
                    Grounded = false;
                    Airtime = 0f;
                }
                else
                {
                    Y = support;
                    VY = 0f;
                }
            }

            // Geometry Dash jumps the instant you press, and keeps jumping while you hold.
            if (Grounded && (pressed || held)) Jump(GameConstants.CubeJumpVelocity);

            if (pressed && !Grounded) TryOrb(level, pressed);

            if (!Grounded)
            {
                float gravity = -GravitySign * GameConstants.CubeGravity;
                VY += gravity * dt;
                if (VY > GameConstants.CubeTerminalSpeed) VY = GameConstants.CubeTerminalSpeed;
                if (VY < -GameConstants.CubeTerminalSpeed) VY = -GameConstants.CubeTerminalSpeed;

                Y += VY * dt;
                Airtime += dt;
                // Negative Z is clockwise on screen, i.e. the cube rolls forward in the
                // direction it is travelling.
                Rotation -= GameConstants.CubeSpinSpeed * dt;
                ResolveLanding(level, dt);
            }
        }

        float SupportY(Level level)
        {
            if (GravitySign > 0)
            {
                float support = 0f;
                for (int i = 0; i < level.Tiles.Count; i++)
                {
                    Tile t = level.Tiles[i];
                    if (t.Kind != TileKind.Block) continue;
                    if (!OverlapsX(t)) continue;
                    float top = t.Top;
                    if (top <= Y + 0.08f && top > support) support = top;
                }
                return support;
            }

            // Flipped: stand on the ceiling, or on the underside of a block.
            float ceiling = level.Ceiling - HalfHeight * 2f;
            float best = ceiling;
            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile t = level.Tiles[i];
                if (t.Kind != TileKind.Block) continue;
                if (!OverlapsX(t)) continue;
                float bottom = t.Bottom - HalfHeight * 2f;
                if (t.Bottom >= Top - 0.08f && bottom < best) best = bottom;
            }
            return best;
        }

        void ResolveLanding(Level level, float dt)
        {
            if (GravitySign > 0)
            {
                if (VY > 0f) return;
                float landing = float.MinValue;
                if (Y <= 0f) landing = 0f;
                for (int i = 0; i < level.Tiles.Count; i++)
                {
                    Tile t = level.Tiles[i];
                    if (t.Kind != TileKind.Block) continue;
                    if (!OverlapsX(t)) continue;
                    float top = t.Top;
                    if (PrevY >= top - 0.001f && Y <= top && top > landing) landing = top;
                }
                if (landing > float.MinValue)
                {
                    Y = landing;
                    VY = 0f;
                    Grounded = true;
                    Airtime = 0f;
                    Rotation = SnapToQuarterTurn(Rotation);
                    Events.Landed = true;
                }
                else if (Y < 0f)
                {
                    Y = 0f;
                    VY = 0f;
                    Grounded = true;
                    Airtime = 0f;
                    Rotation = SnapToQuarterTurn(Rotation);
                    Events.Landed = true;
                }
            }
            else
            {
                if (VY < 0f) return;
                float ceiling = level.Ceiling - HalfHeight * 2f;
                float landing = float.MaxValue;
                if (Top >= level.Ceiling) landing = ceiling;
                for (int i = 0; i < level.Tiles.Count; i++)
                {
                    Tile t = level.Tiles[i];
                    if (t.Kind != TileKind.Block) continue;
                    if (!OverlapsX(t)) continue;
                    float newBottom = t.Bottom - HalfHeight * 2f;
                    if (PrevY + HalfHeight * 2f <= t.Bottom + 0.001f && Y + HalfHeight * 2f >= t.Bottom && newBottom < landing)
                        landing = newBottom;
                }
                if (landing < float.MaxValue)
                {
                    Y = landing;
                    VY = 0f;
                    Grounded = true;
                    Airtime = 0f;
                    Rotation = SnapToQuarterTurn(Rotation);
                    Events.Landed = true;
                }
            }
        }

        /// <summary>
        /// Geometry Dash cubes always touch down flat. Snapping the accumulated spin to the
        /// nearest quarter turn (instead of resetting it to zero) stops the view from spinning
        /// the cube backwards to unwind on every landing.
        /// </summary>
        static float SnapToQuarterTurn(float rotation)
        {
            const float quarter = 90f;
            return (float)(Math.Round(rotation / quarter) * quarter);
        }

        void Jump(float velocity)
        {
            Grounded = false;
            Airtime = 0f;
            VY = GravitySign > 0 ? velocity : -velocity;
            Events.Jumped = true;
        }

        void TryOrb(Level level, bool pressed)
        {
            if (!pressed || orbCooldown > 0f) return;
            for (int i = 0; i < level.Orbs.Count; i++)
            {
                if (consumedOrbs.Contains(i)) continue;
                Orb orb = level.Orbs[i];
                float dx = orb.X - X;
                float dy = orb.Y - CenterY;
                if (dx * dx + dy * dy > GameConstants.OrbRadius * GameConstants.OrbRadius) continue;

                consumedOrbs.Add(i);
                orbCooldown = GameConstants.OrbCooldownSeconds;
                Events.Orb = true;
                Events.OrbIndex = i;
                Events.OrbKind = orb.Kind;
                Airtime = 0f;

                switch (orb.Kind)
                {
                    case OrbKind.Yellow:
                        VY = GravitySign > 0 ? GameConstants.CubeJumpVelocity * GameConstants.YellowOrbScale
                                            : -GameConstants.CubeJumpVelocity * GameConstants.YellowOrbScale;
                        break;
                    case OrbKind.Pink:
                        VY = GravitySign > 0 ? GameConstants.CubeJumpVelocity * GameConstants.PinkOrbScale
                                            : -GameConstants.CubeJumpVelocity * GameConstants.PinkOrbScale;
                        break;
                    case OrbKind.Blue:
                        GravitySign = -GravitySign;
                        VY = GravitySign > 0 ? GameConstants.CubeJumpVelocity * GameConstants.BlueOrbScale
                                            : -GameConstants.CubeJumpVelocity * GameConstants.BlueOrbScale;
                        Events.GravityChanged = true;
                        break;
                }
                return;
            }
        }

        /// <summary>True once the gem at this index has been picked up in this run.</summary>
        public bool IsGemCollected(int index)
        {
            return collectedGems.Contains(index);
        }

        /// <summary>
        /// Gems are the multiplier currency. They never move the player: touching one only awards
        /// multiplier, which is what makes a risky line voluntary instead of forced.
        /// </summary>
        void CollectGems(Level level)
        {
            bool first = true;
            for (int i = 0; i < level.Gems.Count; i++)
            {
                if (collectedGems.Contains(i)) continue;
                Gem gem = level.Gems[i];
                float dx = gem.X - X;
                float dy = gem.Y - CenterY;
                if (dx * dx + dy * dy > GameConstants.GemRadius * GameConstants.GemRadius) continue;

                collectedGems.Add(i);
                GemsThisStep.Add(i);
                Events.Gem = true;
                // Keep the single-gem fields pointing at the first pickup of the step for callers
                // that only look at the summary; the full list is in GemsThisStep.
                if (first)
                {
                    Events.GemIndex = i;
                    Events.GemKind = gem.Kind;
                    first = false;
                }
            }
        }

        // ------------------------------------------------------------------
        // Ship
        // ------------------------------------------------------------------
        void StepShip(float dt, bool held)
        {
            float accel = held
                ? GameConstants.ShipThrust - GameConstants.ShipGravity
                : -GameConstants.ShipGravity;
            VY += accel * dt;
            if (VY > GameConstants.ShipMaxSpeedY) VY = GameConstants.ShipMaxSpeedY;
            if (VY < -GameConstants.ShipMaxSpeedY) VY = -GameConstants.ShipMaxSpeedY;

            Y += VY * dt;
            float maxY = GameConstants.CeilingY - HalfHeight * 2f;
            if (Y < 0f) { Y = 0f; if (VY < 0f) VY = 0f; }
            if (Y > maxY) { Y = maxY; if (VY > 0f) VY = 0f; }
            Rotation = 0f;
            Grounded = false;
        }

        // ------------------------------------------------------------------
        // Interactions
        // ------------------------------------------------------------------
        void CheckPortals(Level level)
        {
            for (int i = 0; i < level.Portals.Count; i++)
            {
                if (consumedPortals.Contains(i)) continue;
                Portal portal = level.Portals[i];
                float dx = Math.Abs(portal.X + 0.5f - X);
                if (dx > GameConstants.PortalWidth * 0.5f + HalfWidth) continue;
                if (Top < portal.Y || Y > portal.Y + GameConstants.PortalHeight) continue;

                consumedPortals.Add(i);
                Events.Portal = true;
                Events.PortalKind = portal.Kind;

                switch (portal.Kind)
                {
                    case PortalKind.GravityFlip:
                        if (GravitySign > 0) { GravitySign = -1; Grounded = false; Airtime = 0f; Events.GravityChanged = true; }
                        break;
                    case PortalKind.GravityNormal:
                        if (GravitySign < 0) { GravitySign = 1; Grounded = false; Airtime = 0f; Events.GravityChanged = true; }
                        break;
                    case PortalKind.ShipPortal:
                        if (Mode != PlayerMode.Ship)
                        {
                            Mode = PlayerMode.Ship;
                            GravitySign = 1;
                            Grounded = false;
                            VY = 0f;
                            Events.ModeChanged = true;
                        }
                        break;
                    case PortalKind.CubePortal:
                        if (Mode != PlayerMode.Cube)
                        {
                            Mode = PlayerMode.Cube;
                            GravitySign = 1;
                            Grounded = false;
                            VY = 0f;
                            Events.ModeChanged = true;
                        }
                        break;
                }
            }
        }

        void CheckPads(Level level)
        {
            for (int i = 0; i < level.Pads.Count; i++)
            {
                if (consumedPads.Contains(i)) continue;
                Pad pad = level.Pads[i];
                if (Math.Abs(pad.X + 0.5f - X) > GameConstants.PadHalfWidth + HalfWidth) continue;
                if (Y > pad.Y + GameConstants.PadTriggerDepth || Y < pad.Y - 0.05f) continue;

                consumedPads.Add(i);
                float scale = pad.Kind == PadKind.Yellow ? GameConstants.YellowPadScale : GameConstants.PinkPadScale;
                Grounded = false;
                Airtime = 0f;
                VY = GravitySign > 0
                    ? GameConstants.CubeJumpVelocity * scale
                    : -GameConstants.CubeJumpVelocity * scale;
                Events.Pad = true;
                Events.PadIndex = i;
                Events.PadKind = pad.Kind;
            }
        }

        void CheckHazards(Level level)
        {
            float left = X - HalfWidth;
            float right = X + HalfWidth;
            float bottom = Y;
            float top = Top;

            for (int i = 0; i < level.Tiles.Count; i++)
            {
                Tile t = level.Tiles[i];
                if (t.Kind == TileKind.Block)
                {
                    // Standing on top is fine; any real overlap is fatal.
                    if (right <= t.X + 0.02f || left >= t.X + 0.98f) continue;
                    if (bottom >= t.Top - 0.02f) continue;
                    if (top <= t.Bottom + 0.02f) continue;
                    Kill();
                    return;
                }

                if (t.Kind == TileKind.Spike)
                {
                    // Forgiving inner hitbox: the spike tip does not reach the full block.
                    float sx0 = t.X + 0.30f;
                    float sx1 = t.X + 0.70f;
                    float sy0 = t.Y;
                    float sy1 = t.Y + 0.58f;
                    if (right > sx0 && left < sx1 && top > sy0 && bottom < sy1) { Kill(); return; }
                }
                else
                {
                    float sx0 = t.X + 0.30f;
                    float sx1 = t.X + 0.70f;
                    float sy0 = t.Y + 0.42f;
                    float sy1 = t.Y + 1f;
                    if (right > sx0 && left < sx1 && top > sy0 && bottom < sy1) { Kill(); return; }
                }
            }
        }

        void Kill()
        {
            Alive = false;
            Events.Died = true;
        }

        bool OverlapsX(Tile t)
        {
            return X + HalfWidth > t.X + 0.02f && X - HalfWidth < t.X + 0.98f;
        }
    }
}
