using System.Collections.Generic;
using UnityEngine;

namespace BullRide
{
    sealed class RiderRagdoll
    {
        sealed class Segment
        {
            public Transform Bone;
            public Rigidbody Body;
            public CapsuleCollider Collider;
            public ConfigurableJoint Joint;
        }

        readonly List<Segment> segments = new List<Segment>();
        readonly Transform rider, seat, world;
        readonly Dictionary<string, Transform> rig;
        Transform proxies;
        Vector3 previousSpeed, previousPosition, inertia;
        Quaternion previousRotation;
        bool sampled;
        AngularSpring spine, chest, neck, head, upperArm, forearm;

        // Mounted physics acts on angles only. The seat and every visible joint keep
        // their original parent and offset, even if a buck produces a large impulse.
        struct AngularSpring
        {
            Vector3 displacement, velocity;

            public Quaternion Step(Vector3 force, float dt, float stiffness, float damping, float limit)
            {
                while (dt > 0)
                {
                    float step = Mathf.Min(dt, 1f / 120);
                    velocity += (force - displacement * stiffness - velocity * damping) * step;
                    displacement += velocity * step;
                    if (displacement.sqrMagnitude > limit * limit)
                    {
                        displacement = displacement.normalized * limit;
                        velocity = Vector3.ProjectOnPlane(velocity, displacement);
                    }
                    dt -= step;
                }
                return Quaternion.Euler(displacement);
            }
        }

        public RiderRagdoll(Dictionary<string, Transform> rig, Transform seat, Transform world)
        {
            this.rig = rig;
            rider = rig["Rider"];
            this.seat = seat;
            this.world = world;
        }

        public void SwayTorso(float dt)
        {
            if (!sampled)
            {
                previousPosition = seat.position;
                previousRotation = seat.rotation;
                sampled = true;
            }
            else if (dt > 0)
            {
                Vector3 speed = (seat.position - previousPosition) / dt;
                Vector3 acceleration = Quaternion.Inverse(seat.rotation) * ((speed - previousSpeed) / dt);
                Quaternion turn = Quaternion.Inverse(previousRotation) * seat.rotation;
                turn.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180) angle -= 360;
                Vector3 force = -axis * (angle / dt) * 4.2f +
                    new Vector3(-acceleration.z, 0, acceleration.x) * 12f;
                inertia = Vector3.Lerp(inertia, Vector3.ClampMagnitude(force, 1750), 1 - Mathf.Exp(-20 * dt));
                previousSpeed = speed;
                previousPosition = seat.position;
                previousRotation = seat.rotation;
            }

            rig["spine"].localRotation *= spine.Step(inertia, dt, 48, 8, 25);
            rig["chest"].localRotation *= chest.Step(inertia * .60f, dt, 58, 8.5f, 17);
            rig["neck"].localRotation *= neck.Step(inertia * .38f, dt, 75, 8, 12);
            rig["head"].localRotation *= head.Step(inertia * .72f, dt, 76, 8.5f, 20);
        }

        public void SwayFreeArm(float dt)
        {
            rig["armL_upper"].localRotation *= upperArm.Step(inertia * 1.5f, dt, 43, 7, 36);
            rig["armL_lower"].localRotation *= forearm.Step(inertia, dt, 35, 6.5f, 31);
        }

        public void Release(Vector3 velocity, Vector3 spin, System.Action onImpact)
        {
            proxies = new GameObject("Rider physics").transform;
            proxies.SetParent(world, false);
            // Bodies live on a separate flat rig. PhysX can resolve collisions
            // without translating or reparenting any of the visible limbs.
            Rigidbody root = Add("Rider", null, 4, .29f, .13f, .03f, 0);
            Rigidbody torso = Add("spine", root, 3, .30f, .105f, .13f, 28);
            Rigidbody shoulders = Add("chest", torso, 2.5f, .24f, .10f, .09f, 25);
            Add("head", shoulders, 1, .22f, .094f, .025f, 34);
            Limb("armL", shoulders, .32f, .29f, .054f, 65);
            Limb("armR", shoulders, .32f, .29f, .054f, 65);
            Limb("legL", root, .40f, .38f, .072f, 55);
            Limb("legR", root, .40f, .38f, .072f, 55);

            for (int i = 0; i < segments.Count; i++)
                for (int j = 0; j < i; j++)
                    Physics.IgnoreCollision(segments[i].Collider, segments[j].Collider);

            Vector3 origin = root.position;
            foreach (Segment segment in segments)
            {
                var body = segment.Body;
                if (onImpact != null)
                    body.gameObject.AddComponent<RiderGroundContact>().Impact = () =>
                    {
                        System.Action play = onImpact;
                        onImpact = null;
                        play?.Invoke();
                    };
                body.isKinematic = false;
                body.linearVelocity = velocity + Vector3.Cross(spin, body.position - origin);
                body.angularVelocity = spin;
            }

            void Limb(string name, Rigidbody parent, float upper, float lower, float radius, float swing)
            {
                Rigidbody first = Add(name + "_upper", parent, 1, upper, radius, -upper * .5f, swing);
                Add(name + "_lower", first, .85f, lower, radius * .85f, -lower * .5f, 55);
            }
        }

        Rigidbody Add(string name, Rigidbody parent, float mass, float height, float radius, float center, float limit)
        {
            Transform bone = rig[name];
            Transform proxy = new GameObject(name).transform;
            proxy.SetParent(proxies, false);
            proxy.SetPositionAndRotation(bone.position, bone.rotation);

            var body = proxy.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.mass = mass;
            body.maxAngularVelocity = 14;
            body.angularDamping = .75f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 16;
            body.solverVelocityIterations = 8;
            var collider = proxy.gameObject.AddComponent<CapsuleCollider>();
            collider.height = height;
            collider.radius = radius;
            collider.center = Vector3.up * center;

            ConfigurableJoint joint = null;
            if (parent != null)
            {
                joint = proxy.gameObject.AddComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = parent;
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = Quaternion.Inverse(parent.rotation) * (bone.position - parent.position);
                joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
                joint.highAngularXLimit = new SoftJointLimit { limit = limit };
                joint.angularYLimit = new SoftJointLimit { limit = limit * .8f };
                joint.angularZLimit = new SoftJointLimit { limit = limit * .85f };
                joint.projectionMode = JointProjectionMode.PositionAndRotation;
                joint.projectionDistance = .025f;
                joint.projectionAngle = 12;
                joint.breakForce = joint.breakTorque = float.PositiveInfinity;
                joint.enableCollision = false;
            }

            segments.Add(new Segment { Bone = bone, Body = body, Collider = collider, Joint = joint });
            return body;
        }

        public void PresentFall()
        {
            if (segments.Count == 0) return;
            Transform root = segments[0].Body.transform;
            rider.SetPositionAndRotation(root.position, root.rotation);
            // Apply rotations from the pelvis outward; bone lengths and the hat,
            // hands, and boots still come from the original rider hierarchy.
            for (int i = 1; i < segments.Count; i++)
                segments[i].Bone.rotation = segments[i].Body.transform.rotation;
        }

        public void Reset()
        {
            if (proxies != null)
            {
                proxies.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(proxies.gameObject);
                else Object.DestroyImmediate(proxies.gameObject);
                proxies = null;
                segments.Clear();
            }
            sampled = false;
            previousSpeed = inertia = Vector3.zero;
            spine = chest = neck = head = upperArm = forearm = default;
        }
    }
}
