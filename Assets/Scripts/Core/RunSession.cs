using System;

namespace NeonDash.Core
{
    /// <summary>
    /// One 30 second attempt on a seeded level. Holds the fixed-step accumulator so behaviour is
    /// frame-rate independent, and produces a <see cref="RunResult"/> when the run ends (death,
    /// timer, or finish line).
    /// </summary>
    public sealed class RunSession
    {
        public Level Level { get; private set; }
        public PlayerSim Player { get; private set; }
        public float Elapsed { get; private set; }
        public float Distance { get { return Player == null ? 0f : Player.X; } }
        public float TimeLeft { get { return Math.Max(0f, GameConstants.RoundSeconds - Elapsed); } }
        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        public RunResult Result { get; private set; }
        public int DistanceBlocks { get { return (int)Math.Floor(Distance); } }
        public float Progress { get { return Level == null ? 0f : ScoreRules.Progress(Distance, Level.Length); } }

        /// <summary>Earned multiplier for the run in progress.</summary>
        public MultiplierLedger Multiplier { get; private set; }

        /// <summary>
        /// Everything that happened across all fixed steps of the last <see cref="Advance"/> call.
        /// A frame can run several 120 Hz steps, so reading only <see cref="PlayerSim.Events"/> after
        /// the frame would drop the events of every step but the last.
        /// </summary>
        public SimEvents FrameEvents;

        /// <summary>Every gem index collected during the last <see cref="Advance"/> call.</summary>
        public readonly System.Collections.Generic.List<int> FrameGems = new System.Collections.Generic.List<int>();

        float accumulator;

        /// <summary>
        /// A tap that arrived during a frame that ran no fixed step. At high refresh rates a frame
        /// is shorter than one 120 Hz step, so without this the input would simply be dropped.
        /// </summary>
        bool bufferedPress;

        public RunSession()
        {
            Player = new PlayerSim();
            Multiplier = new MultiplierLedger();
        }

        public void Begin(Level level)
        {
            Level = level;
            Player.Reset();
            Multiplier.Reset();
            Elapsed = 0f;
            accumulator = 0f;
            bufferedPress = false;
            Running = true;
            Finished = false;
            Result = default(RunResult);
        }

        /// <summary>Advance by a frame delta; internally runs at the fixed 120 Hz step.</summary>
        public void Advance(float deltaSeconds, bool held, bool pressed)
        {
            // Cleared even when this call runs no steps, so the view never replays last frame's events.
            FrameEvents.Clear();
            FrameGems.Clear();

            if (!Running) return;
            if (deltaSeconds <= 0f) return;
            if (deltaSeconds > 0.25f) deltaSeconds = 0.25f;

            accumulator += deltaSeconds;
            bool pressPending = pressed || bufferedPress;
            bufferedPress = false;
            int guard = 0;
            while (accumulator >= GameConstants.FixedStep && Running && guard++ < 64)
            {
                Tick(GameConstants.FixedStep, held, pressPending);
                pressPending = false;
                accumulator -= GameConstants.FixedStep;
            }

            // Nothing ran this frame: keep the tap for the next one instead of losing it.
            if (Running && pressPending) bufferedPress = true;
        }

        void Tick(float dt, bool held, bool pressed)
        {
            Elapsed += dt;
            Player.Step(dt, Level, held, pressed);
            MergeEvents(Player.Events);
            CollectPickups();

            if (!Player.Alive)
            {
                Finish(true);
                return;
            }
            if (Player.X >= Level.Length)
            {
                Finish(false);
                return;
            }
            if (Elapsed >= GameConstants.RoundSeconds)
            {
                Finish(false);
                return;
            }
        }

        /// <summary>Folds one step's events into the frame summary the view reads.</summary>
        void MergeEvents(SimEvents step)
        {
            FrameEvents.Jumped |= step.Jumped;
            FrameEvents.Landed |= step.Landed;
            FrameEvents.Died |= step.Died;
            FrameEvents.ModeChanged |= step.ModeChanged;
            FrameEvents.GravityChanged |= step.GravityChanged;

            if (step.Orb)
            {
                FrameEvents.Orb = true;
                FrameEvents.OrbIndex = step.OrbIndex;
                FrameEvents.OrbKind = step.OrbKind;
            }
            if (step.Pad)
            {
                FrameEvents.Pad = true;
                FrameEvents.PadIndex = step.PadIndex;
                FrameEvents.PadKind = step.PadKind;
            }
            if (step.Portal)
            {
                FrameEvents.Portal = true;
                FrameEvents.PortalKind = step.PortalKind;
            }
            if (step.Gem)
            {
                FrameEvents.Gem = true;
                FrameEvents.GemIndex = step.GemIndex;
                FrameEvents.GemKind = step.GemKind;
            }

            for (int i = 0; i < Player.GemsThisStep.Count; i++) FrameGems.Add(Player.GemsThisStep[i]);
        }

        /// <summary>Turns the pickups of this step into earned multiplier.</summary>
        void CollectPickups()
        {
            SimEvents events = Player.Events;

            // Every gem of the step counts, not just the last index written to the summary.
            for (int i = 0; i < Player.GemsThisStep.Count; i++)
            {
                int index = Player.GemsThisStep[i];
                bool high = Level != null && index >= 0 && index < Level.Gems.Count
                    && Level.Gems[index].Kind == GemKind.High;
                Multiplier.Collect(high ? PickupKind.HighGem : PickupKind.ArcGem, Elapsed);
            }

            if (events.Orb)
                Multiplier.Collect(ToPickup(events.OrbKind), Elapsed);

            if (events.Pad)
                Multiplier.Collect(events.PadKind == PadKind.Yellow ? PickupKind.YellowPad : PickupKind.PinkPad, Elapsed);
        }

        static PickupKind ToPickup(OrbKind kind)
        {
            switch (kind)
            {
                case OrbKind.Yellow: return PickupKind.YellowOrb;
                case OrbKind.Blue: return PickupKind.BlueOrb;
                default: return PickupKind.PinkOrb;
            }
        }

        void Finish(bool died)
        {
            Running = false;
            Finished = true;

            int distance = DistanceBlocks;
            float progress = Level == null ? 0f : ScoreRules.Progress(distance, Level.Length);
            float multiplier = Multiplier.RoundedValue;

            var result = new RunResult();
            result.Seed = Level == null ? "0000" : Level.Seed;
            result.PlayerName = "PLAYER";
            result.Distance = distance;
            result.Progress = progress;
            result.Multiplier = multiplier;
            result.Orbs = Multiplier.Orbs;
            result.Pads = Multiplier.Pads;
            result.Gems = Multiplier.Gems;
            result.BestChain = Multiplier.BestChain;
            result.Payout = ScoreRules.Payout(distance, multiplier);
            result.Died = died;
            result.Completed = !died && distance >= (int)Level.Length;
            result.Duration = Elapsed;
            Result = result;
        }

        /// <summary>Progress at which the player died or finished, as a 0..1 fraction.</summary>
        public float ResultProgress { get { return Result.Progress; } }
    }
}
