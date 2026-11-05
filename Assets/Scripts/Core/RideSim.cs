using System;
using UnityEngine;

namespace BullRide.Core
{
    public enum RidePhase { Menu, Chute, Ride, Done, Fall, Over }

    public struct BuckPose
    {
        public float Turn, Speed, Pitch, Roll, Kick, Neck, Hop;
        public Vector2 Force;

        public static BuckPose Blend(BuckPose a, BuckPose b, float t) => new BuckPose
        {
            Turn = Mathf.Lerp(a.Turn, b.Turn, t), Speed = Mathf.Lerp(a.Speed, b.Speed, t),
            Pitch = Mathf.Lerp(a.Pitch, b.Pitch, t), Roll = Mathf.Lerp(a.Roll, b.Roll, t),
            Kick = Mathf.Lerp(a.Kick, b.Kick, t), Neck = Mathf.Lerp(a.Neck, b.Neck, t),
            Hop = Mathf.Lerp(a.Hop, b.Hop, t), Force = Vector2.Lerp(a.Force, b.Force, t)
        };
    }

    internal readonly struct BuckKey
    {
        public readonly float Time;
        public readonly BuckPose Pose;

        public BuckKey(float time, float turn, float speed, float pitch, float roll, float kick,
            float neck, float hop, float x, float y)
        {
            Time = time;
            Pose = new BuckPose { Turn = turn, Speed = speed, Pitch = pitch, Roll = roll,
                Kick = kick, Neck = neck, Hop = hop, Force = new Vector2(x, y) };
        }
    }

    internal sealed class BuckMove
    {
        public readonly string Name;
        readonly BuckKey[] keys;
        public float Duration => keys[keys.Length - 1].Time;
        public BuckMove(string name, BuckKey[] keys) { Name = name; this.keys = keys; }

        public BuckPose Sample(float time)
        {
            int i = 1;
            while (i < keys.Length - 1 && time > keys[i].Time) i++;
            BuckKey a = keys[i - 1], b = keys[i];
            return BuckPose.Blend(a.Pose, b.Pose, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a.Time, b.Time, time)));
        }
    }

    public sealed class RideSim
    {
        public const int StepsPerSecond = 120;
        public const float StepSeconds = 1f / StepsPerSecond;
        const int HighBuckTicks = 90;
        public const float GateZ = -20f;
        public const float GateWait = 1.15f;
        public const float ShoulderOffset = .52f;
        public static readonly Vector2 StartPosition = new Vector2(-1.75f, -22.55f);

        readonly System.Random random;
        readonly int[] bag = new int[8];
        int bagLeft, moveIndex, rideTicks, phaseTicks;
        float moveTime, hold;
        BuckPose entry, lastSample;
        bool blendEntry, highBuckAvailable;
        int highBuckLeft, centerTicks;
        double balanceBonus;

        public RidePhase Phase { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 WorldVelocity { get; private set; }
        public Vector2 Lean { get; private set; }
        public Vector2 LeanVelocity { get; private set; }
        public float Yaw { get; private set; }
        public float Gate { get; private set; }
        public float Distance { get; private set; }
        public float IdleTime { get; private set; }
        public BuckPose Pose { get; private set; }
        public double RideTime => (double)rideTicks / StepsPerSecond;
        public double Multiplier => Phase == RidePhase.Chute || Phase == RidePhase.Menu ? 1 : Math.Min(RideRules.MaxMultiplier, RideRules.BaseMultiplierAt(RideTime) + balanceBonus);
        public bool HighBuck => Phase == RidePhase.Ride && highBuckLeft > 0;
        public float PlaybackRate => HighBuck ? .5f : 1;
        public bool InCenter => Lean.sqrMagnitude <= RideRules.CenterRadius * RideRules.CenterRadius;
        public string Move => Phase == RidePhase.Menu || (Phase == RidePhase.Chute && Gate == 0)
            ? "IN THE CHUTE" : BuckMoves.Sequence[moveIndex].Name;
        public bool Whistle => rideTicks == RideRules.Duration * StepsPerSecond;
        public bool CanBalance => Phase == RidePhase.Chute || Phase == RidePhase.Ride;

        public RideSim(int seed)
        {
            random = new System.Random(seed);
            Reset();
        }

        public void Reset()
        {
            Phase = RidePhase.Menu;
            Position = StartPosition;
            WorldVelocity = Lean = LeanVelocity = Vector2.zero;
            Yaw = Gate = Distance = IdleTime = moveTime = hold = 0;
            rideTicks = phaseTicks = moveIndex = bagLeft = 0;
            blendEntry = highBuckAvailable = false;
            highBuckLeft = centerTicks = 0;
            balanceBonus = 0;
            Pose = default;
        }

        public void Play()
        {
            if (Phase != RidePhase.Menu) return;
            highBuckAvailable = random.NextDouble() < .72;
            Phase = RidePhase.Chute;
            phaseTicks = 0;
        }

        public void Step(Vector2 stick)
        {
            phaseTicks++;
            IdleTime += StepSeconds;
            switch (Phase)
            {
                case RidePhase.Menu:
                    Fidget();
                    break;
                case RidePhase.Chute:
                    StepChute();
                    break;
                case RidePhase.Ride:
                    StepRide(Vector2.ClampMagnitude(stick, 1));
                    break;
                case RidePhase.Fall:
                    StepFall();
                    break;
                case RidePhase.Done:
                    if (phaseTicks >= 144) Phase = RidePhase.Over;
                    break;
            }
        }

        void Fidget()
        {
            Pose = new BuckPose { Neck = .059f * Mathf.Sin(IdleTime * 2.3f) + .025f * Mathf.Sin(IdleTime * 5.9f) };
            Lean = new Vector2(.037f * Mathf.Sin(IdleTime * 1.7f), .029f * Mathf.Sin(IdleTime * 2.4f));
        }

        void StepChute()
        {
            float elapsed = phaseTicks / (float)StepsPerSecond - GateWait;
            if (elapsed <= 0)
            {
                Fidget();
                return;
            }

            Gate = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .33f));
            if (elapsed < .18f) return;

            float speed = Mathf.SmoothStep(2.3f, 4.8f, Mathf.Clamp01((elapsed - .18f) / .23f));
            Position += new Vector2(0, speed * StepSeconds);
            WorldVelocity = new Vector2(0, speed);
            Distance += speed * StepSeconds;
            Pose = new BuckPose { Speed = speed, Pitch = .04f, Neck = .08f };
            if (Position.y + ShoulderOffset < GateZ) return;
            Gate = 1;
            Lean = Vector2.zero;
            phaseTicks = 0;
            Phase = RidePhase.Ride;
        }

        void StepRide(Vector2 stick)
        {
            rideTicks++;
            bool late = RideTime > RideRules.SurgeTime;
            float u = Mathf.Clamp01((float)((RideTime - RideRules.SurgeTime) / (RideRules.Duration - RideRules.SurgeTime)));
            moveTime += StepSeconds * (late ? Mathf.Lerp(1.68f, 1.93f, u) : 1.46f);
            BuckMove move = BuckMoves.Sequence[moveIndex];
            if (moveTime >= move.Duration)
            {
                moveTime -= move.Duration;
                entry = lastSample;
                blendEntry = true;
                moveIndex = NextMove();
                move = BuckMoves.Sequence[moveIndex];
            }

            BuckPose pose = move.Sample(moveTime);
            if (blendEntry && moveTime < .12f)
                pose = BuckPose.Blend(entry, pose, Mathf.SmoothStep(0, 1, moveTime / .12f));
            bool lifting = pose.Hop >= .20f && pose.Hop > lastSample.Hop;
            lastSample = pose;
            if (highBuckLeft > 0) highBuckLeft--;
            if (highBuckAvailable && RideTime >= 4.25 && RideTime <= 6.75 &&
                Multiplier >= 2 && lifting)
            {
                highBuckAvailable = false;
                highBuckLeft = HighBuckTicks;
            }
            float lift = HighBuck ? Mathf.Sin(Mathf.PI * (1 - (float)highBuckLeft / HighBuckTicks)) : 0;
            pose.Hop += .58f * lift;
            pose.Pitch += .10f * lift;
            // Keep peak buck force below full stick authority, even in the surge.
            float forceScale = late ? Mathf.Lerp(1.84f, 2.14f, u) : 1.62f;
            pose.Force = Vector2.ClampMagnitude(pose.Force * forceScale * (1 + .25f * lift) +
                new Vector2(.23f * lift * Mathf.Cos(Mathf.PI * 2 * highBuckLeft / HighBuckTicks), .20f * lift),
                HighBuck ? 1.32f : 1.18f);
            if (late)
            {
                pose.Turn *= Mathf.Lerp(1.04f, 1.14f, u);
                pose.Roll *= Mathf.Lerp(1.03f, 1.10f, u);
            }

            Pose = pose;
            Travel(pose.Speed, pose.Turn);
            LeanVelocity += (pose.Force * 4.35f + stick * 6.15f - LeanVelocity * 3.05f - Lean * .28f) * StepSeconds;
            Lean += LeanVelocity * StepSeconds;
            centerTicks = InCenter ? centerTicks + 1 : 0;
            if (centerTicks >= (HighBuck ? 27 : 42))
                balanceBonus += (HighBuck ? 1.8 : late ? .36 : .17) / StepsPerSecond;

            float magnitude = Lean.magnitude;
            if (magnitude >= 1) hold += StepSeconds;
            else if (magnitude <= (late ? Mathf.Lerp(.68f, .62f, u) : .72f))
                hold = Mathf.Max(0, hold - StepSeconds * (late ? Mathf.Lerp(2.15f, 1.85f, u) : 2.4f));

            if (Whistle || hold + 1e-6f >= (late ? Mathf.Lerp(.33f, .24f, u) : .38f))
            {
                phaseTicks = 0;
                Phase = Whistle ? RidePhase.Done : RidePhase.Fall;
            }
        }

        int NextMove()
        {
            if (moveIndex == 0) return 1;
            if (bagLeft == 0)
            {
                for (int i = 0; i < bag.Length; i++) bag[i] = i + 1;
                for (int i = bag.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }
                bagLeft = bag.Length;
            }
            return bag[--bagLeft];
        }

        void Travel(float speed, float turn)
        {
            Vector2 pos = Position;
            float distance = new Vector2(pos.x / 11.4f, pos.y / 16.5f).magnitude;
            float outward = pos.x * Mathf.Sin(Yaw) / 130 + pos.y * Mathf.Cos(Yaw) / 272;
            float yaw = Yaw + turn * StepSeconds;
            if (distance > .73f && outward > 0)
            {
                float inward = Mathf.Atan2(-pos.x, -pos.y);
                float delta = Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, inward * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                yaw += Mathf.Clamp(delta, -2.8f, 2.8f) * Mathf.Clamp((distance - .73f) * 4.5f, 0, 1.7f) * StepSeconds;
            }

            Yaw = yaw;
            Vector2 next = pos + new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)) * (speed * StepSeconds);
            Position = new Vector2(Mathf.Clamp(next.x, -11.4f, 11.4f), Mathf.Clamp(next.y, GateZ - .6f, 16.4f));
            WorldVelocity = (Position - pos) / StepSeconds;
            Distance += (Position - pos).magnitude;
        }

        void StepFall()
        {
            BuckPose pose = Pose;
            float easing = Mathf.Exp(-4 * StepSeconds);
            pose.Hop *= easing;
            pose.Kick *= easing;
            pose.Pitch *= easing;
            pose.Roll *= easing;
            pose.Speed = 2 * (1 - Mathf.Clamp01(phaseTicks / (float)StepsPerSecond));
            Pose = pose;
            Travel(pose.Speed, .33f);
            if (phaseTicks >= 168) Phase = RidePhase.Over;
        }
    }
}
