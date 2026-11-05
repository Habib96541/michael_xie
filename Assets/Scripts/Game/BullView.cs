using System.Collections.Generic;
using BullRide.Core;
using UnityEngine;

namespace BullRide
{
    public sealed class BullView : MonoBehaviour
    {
        static readonly Quaternion[] Tuck =
        {
            new Quaternion(-.46397f, .52904f, .53419f, .46849f),
            new Quaternion(.69650f, .58807f, .26433f, .31494f),
            new Quaternion(0, .52033f, .85397f, 0),
            new Quaternion(-.46397f, -.52904f, -.53419f, .46849f),
            new Quaternion(.69650f, -.58807f, -.26433f, .31494f),
            new Quaternion(0, -.52033f, -.85397f, 0),
            new Quaternion(-.73739f, -.46109f, -.20980f, .44681f),
            new Quaternion(.77454f, .59687f, .11229f, .17674f),
            new Quaternion(.80352f, .01650f, .11131f, .58454f),
            new Quaternion(.06633f, -.58393f, .80806f, .04082f),
            new Quaternion(-.73739f, .46109f, .20980f, .44681f),
            new Quaternion(.77454f, -.59687f, -.11229f, .17674f),
            new Quaternion(.80352f, -.01650f, -.11131f, .58454f),
            new Quaternion(.06633f, .58393f, -.80806f, .04082f)
        };

        Transform[] bones;
        Quaternion[] rest;
        Dictionary<string, Transform> rig;
        Transform seat, handle;
        LineRenderer rope;
        RiderRagdoll ragdoll;
        bool detached;

        public Transform Rider => rig["Rider"];

        public void Build()
        {
            RodeoArt art = RodeoArt.Shared;
            (bones, rest) = art.BuildBull(transform);
            seat = Socket("Rider seat");
            handle = Socket("Rope anchor");
            rig = art.BuildRider(seat);
            ragdoll = new RiderRagdoll(rig, seat, transform.parent);
            rope = MakeRope(Socket("Grip rope"), 2, true);
            LineRenderer cinch = MakeRope(handle, 19, false);
            for (int i = 0; i < cinch.positionCount; i++)
            {
                float angle = i * 2 * Mathf.PI / (cinch.positionCount - 1);
                cinch.SetPosition(i, new Vector3(Mathf.Sin(angle) * .365f, (Mathf.Cos(angle) - 1) * .415f, 0));
            }
        }

        Transform Socket(string label)
        {
            Transform node = new GameObject(label).transform;
            node.SetParent(transform, false);
            return node;
        }

        LineRenderer MakeRope(Transform node, int points, bool worldSpace)
        {
            var line = node.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = RodeoArt.Shared.RopeMaterial;
            line.positionCount = points;
            line.useWorldSpace = worldSpace;
            line.generateLightingData = true;
            line.startColor = line.endColor = new Color(.78f, .66f, .42f);
            line.startWidth = line.endWidth = .036f;
            return line;
        }

        public void ResetPose()
        {
            ragdoll.Reset();
            if (detached)
            {
                Rider.SetParent(seat, false);
                Rider.localPosition = Vector3.zero;
                Rider.localRotation = Quaternion.identity;
            }
            detached = false;
            rope.enabled = true;
        }

        public void Apply(RideSim sim, Vector2 stick, float dt = 0)
        {
            BuckPose pose = sim.Pose;
            float lift = pose.Hop + .17f * pose.Kick;
            float tilt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(lift / .24f));
            Quaternion rotation = RodeoArt.Rotation(new Vector3(.0084f + pose.Pitch * tilt, sim.Yaw,
                pose.Roll * tilt) * Mathf.Rad2Deg);
            Vector3 pivot = .84f * Vector3.up;
            Vector3 position = new Vector3(sim.Position.x, lift, sim.Position.y) + pivot - rotation * pivot;
            transform.SetPositionAndRotation(position + rotation * (.0126f * Vector3.up), rotation);

            float pitch = pose.Pitch * Mathf.Rad2Deg, roll = -pose.Roll * Mathf.Rad2Deg;
            float neck = pose.Neck * Mathf.Rad2Deg, kick = pose.Kick * 68;
            float air = Mathf.SmoothStep(0, 1, Mathf.Clamp01((pose.Hop + Mathf.Max(0, kick - 10) * .004f) / .45f));
            SetJoint(1, pitch * .12f, 0, -roll * .27f);
            SetJoint(2, -pitch * .05f, 0, 0);
            SetJoint(5, -neck * .65f, 0, roll * .1f);
            SetJoint(6, -neck * .4f, 0, 0);
            SetJoint(8, -neck * .3f - pitch * .05f, 0, roll * .12f);
            for (int i = 0; i < Tuck.Length; i++)
                bones[9 + i].localRotation = Quaternion.Slerp(rest[9 + i], Tuck[i].normalized, air);

            float stride = Mathf.Sin(sim.Distance * 5.4f) * .20f * Mathf.Clamp01(pose.Speed / 4) *
                (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(lift / .10f)));
            Swing(9, stride);
            Swing(12, -stride);
            Swing(15, -stride);
            Swing(19, stride);
            float wag = Mathf.Sin(sim.IdleTime * 6) * 14;
            for (int i = 0; i < 7; i++) SetJoint(23 + i, 6 + i * 3 + kick * .04f, wag * (.3f + i * .08f), 0);

            seat.position = bones[3].TransformPoint(new Vector3(.0000745f, -.71008f, 1.403989f));
            seat.localRotation = RodeoArt.Rotation(new Vector3(pitch * .25f, 0, -roll * .4f));
            handle.position = bones[4].TransformPoint(new Vector3(.0000049f, -.173718f, 1.027828f)) + rotation * (.12f * Vector3.up);
            handle.localRotation = RodeoArt.Rotation(new Vector3(pitch * .25f, 0, -roll * .3f));
            if (detached)
            {
                ragdoll.PresentFall();
                return;
            }

            bool riding = sim.Phase == RidePhase.Ride || sim.Phase == RidePhase.Done || sim.Phase == RidePhase.Fall;
            PoseRider(Vector2.ClampMagnitude(sim.Lean + stick * .72f, 1.35f), sim.Yaw, riding, dt);
            rope.SetPosition(0, rig["armR_hand"].position);
            rope.SetPosition(1, handle.position);
        }

        void SetJoint(int joint, float x, float y, float z) =>
            bones[joint].localRotation = rest[joint] * RodeoArt.Rotation(new Vector3(x, y, z));

        void Swing(int joint, float amount) => bones[joint].rotation =
            Quaternion.AngleAxis(amount * Mathf.Rad2Deg, transform.right) * bones[joint].rotation;

        void PoseRider(Vector2 lean, float yaw, bool riding, float dt)
        {
            float x = lean.x, y = lean.y;
            RiderPose("hips", -14, 0, 0);
            RiderPose("spine", 8 + y * 18, -x * 4, -x * 26);
            RiderPose("chest", 4 + y * 10, 0, -x * 10);
            RiderPose("neck", -4 - y * 8, 0, x * 8);
            RiderPose("head", -6 - y * 12, 0, x * 14);
            if (riding) ragdoll.SwayTorso(dt);
            Quaternion heading = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
            Vector3 right = heading * Vector3.right, front = heading * Vector3.forward;
            Vector3 freeHand = rig["chest"].TransformPoint(new Vector3(-.62f - x * .42f, .48f, -.08f - y * .32f));
            AimLimb("armR", .32f, .29f, handle.position, right * .45f - front * .2f);
            AimLimb("armL", .32f, .29f, freeHand, -right * .45f - front * .15f);
            if (riding) ragdoll.SwayFreeArm(dt);
            // Feet and knees are braced against fixed points in the saddle frame.
            Vector3 legCenter = seat.TransformPoint(new Vector3(0, -.34f, .05f));
            Vector3 kneePole = seat.TransformDirection(new Vector3(0, .08f, .55f));
            AimLimb("legR", .40f, .38f, legCenter + seat.right * .46f, kneePole + seat.right * .5f);
            AimLimb("legL", .40f, .38f, legCenter - seat.right * .46f, kneePole - seat.right * .5f);
        }

        void AimLimb(string name, float upperLength, float lowerLength, Vector3 target, Vector3 pole)
        {
            Transform upper = rig[name + "_upper"], lower = rig[name + "_lower"];
            Vector3 origin = upper.position, delta = target - origin;
            Vector3 direction = delta.sqrMagnitude > 1e-8f ? delta.normalized : Vector3.down;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperLength - lowerLength) + .001f,
                upperLength + lowerLength - .001f);
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
            float rise = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            Vector3 bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            if (bend == Vector3.zero) bend = Vector3.ProjectOnPlane(Vector3.forward, direction).normalized;
            Vector3 elbow = origin + direction * along + bend * rise;
            upper.rotation = AimDown(elbow - origin, pole);
            lower.rotation = AimDown(origin + direction * distance - lower.position, pole);
        }

        static Quaternion AimDown(Vector3 direction, Vector3 pole)
        {
            Vector3 up = -direction.normalized;
            Vector3 right = Vector3.Cross(pole, up).normalized;
            if (right == Vector3.zero) right = Vector3.Cross(Vector3.forward, up).normalized;
            return Quaternion.LookRotation(Vector3.Cross(right, up), up);
        }

        void RiderPose(string joint, float x, float y, float z) =>
            rig[joint].localRotation = RodeoArt.Rotation(new Vector3(x, y, z));

        public void ThrowRider(RideSim sim, System.Action onImpact = null)
        {
            detached = true;
            rope.enabled = false;
            Rider.SetParent(transform.parent, true);
            Quaternion heading = Quaternion.Euler(0, sim.Yaw * Mathf.Rad2Deg, 0);
            Vector2 lean = sim.Lean + sim.LeanVelocity * .18f;
            float side = Mathf.Abs(lean.x) > .14f ? lean.x : sim.Pose.Force.x;
            // Clear a flank first, even on a mostly forward or backward loss of balance.
            Vector3 away = heading * new Vector3(side < 0 ? -1 : 1, 0,
                Mathf.Clamp(lean.y * .54f, -.58f, .48f)).normalized;
            float impact = Mathf.Clamp01(sim.Pose.Kick * .6f + sim.Pose.Hop * .75f + sim.Pose.Force.magnitude * .45f);
            Vector3 carry = new Vector3(sim.WorldVelocity.x, 0, sim.WorldVelocity.y) * .55f;
            Vector3 velocity = carry + away * Mathf.Lerp(7.4f, 9.3f, impact) +
                Vector3.up * Mathf.Lerp(2.15f, 3.25f, impact);
            Vector3 spin = Vector3.Cross(Vector3.up, away) * Mathf.Lerp(5.1f, 6.5f, impact) +
                Vector3.up * Mathf.Clamp(sim.Pose.Turn, -1.3f, 1.3f);
            ragdoll.Release(velocity, spin, onImpact);
        }
    }
}
