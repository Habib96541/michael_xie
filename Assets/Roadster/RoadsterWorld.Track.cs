using UnityEngine;

namespace TeslaRoadster
{
    internal sealed partial class RoadsterWorld
    {
        // Distances along the deck in the Marine Drive Blender model.
        const float LowerRadius = 4.25f;
        const float UpperRadius = 3.2f;
        const float Vertical = 5.3f;
        const float Exit = 7f;
        const float Approach = 5.8f;
        const float LowerArc = LowerRadius * Mathf.PI * 0.5f;
        const float UpperArc = UpperRadius * Mathf.PI * 0.5f;
        const float StageDistance = Approach + LowerArc + 2.35f;
        const float LipDistance = Approach + LowerArc + Vertical + UpperArc + Exit;
        // The road deck reverses the car, so the inverted exit faces highway traffic.
        static readonly Vector3 RampOrigin = new(0f, 0.035f, -20f);
        static readonly Quaternion RampHeading = Quaternion.Euler(0f, 180f, 0f);

        sealed class Traffic
        {
            public Transform model;
            public float x, z, speed;
            public Bounds bounds;
        }

        Traffic[] traffic;
        Transform ramp;
        float nextTraffic;

        void CreateTrack(GameObject marineDrive)
        {
            MeshFilter[] parts = marineDrive.GetComponentsInChildren<MeshFilter>();
            CreateTrackMesh(root, parts, "Waterfront");
            for (int i = 0; i < road.Length; i++)
                road[i] = CreateTrackMesh(root, parts, "Drive Segment " + (i % 3 + 1));

            ramp = Group(root, "Launch platform");
            ramp.localPosition = RampOrigin;
            ramp.localRotation = RampHeading;
            CreateTrackMesh(ramp, parts, "Launch Ramp");
            var edgeLights = Material("Ramp edge lights", 255, 255, 255, glow: 1.35f);
            CreateTrackMesh(ramp, parts, "Ramp Edge Lights", edgeLights);
        }

        Transform CreateTrackMesh(Transform parent, MeshFilter[] parts, string name, Material material = null)
        {
            foreach (MeshFilter part in parts)
            {
                if (part.name != name)
                    continue;
                var model = Model(parent, name, part.sharedMesh, material ?? scenery);
                model.localRotation = part.transform.rotation;
                model.localScale = part.transform.lossyScale;
                return model;
            }
            throw new System.InvalidOperationException($"Missing Blender track mesh: {name}");
        }

        static Vector3 RampPoint(float distance, out float angle)
        {
            float d = distance - Approach;
            angle = 0f;
            if (d <= 0f)
                return Vector3.forward * d;
            if (d < LowerArc)
            {
                angle = d / LowerRadius;
                return new Vector3(0f, LowerRadius * (1f - Mathf.Cos(angle)), LowerRadius * Mathf.Sin(angle));
            }
            d -= LowerArc;
            angle = Mathf.PI * 0.5f;
            if (d < Vertical)
                return new Vector3(0f, LowerRadius + d, LowerRadius);
            d -= Vertical;
            if (d < UpperArc)
            {
                float turn = d / UpperRadius;
                angle += turn;
                return new Vector3(0f, LowerRadius + Vertical + UpperRadius * Mathf.Sin(turn),
                    LowerRadius - UpperRadius * (1f - Mathf.Cos(turn)));
            }
            angle = Mathf.PI;
            return new Vector3(0f, LowerRadius + Vertical + UpperRadius, LowerRadius - UpperRadius - (d - UpperArc));
        }

        public void PoseRampClimb(float progress)
        {
            ramp.gameObject.SetActive(true);
            shadow.gameObject.SetActive(false);
            PoseRampCar(Mathf.SmoothStep(0f, StageDistance, progress));
            UpdateVapor(0f, 0f);
        }

        public void PoseLaunch(float progress)
        {
            const float rampPortion = 0.34f;
            if (progress < rampPortion)
            {
                float distance = Mathf.Lerp(StageDistance, LipDistance, progress / rampPortion);
                PoseRampCar(distance);
                UpdateVapor(progress * 2f, Mathf.InverseLerp(0.12f, rampPortion, progress) * 0.68f);
                return;
            }

            float flight = Mathf.InverseLerp(rampPortion, 1f, progress);
            Vector3 lip = RampOrigin + RampHeading * RampPoint(LipDistance, out _);
            // From the lip to touchdown, roll around the car's center over the lane.
            Vector3 floor = new(0f, Mathf.Lerp(lip.y - 2f * RotationCenter.y, 0f, flight * flight)
                + 1.35f * Mathf.Sin(flight * Mathf.PI), Mathf.Lerp(lip.z, 0f, flight));
            float roll = Mathf.Lerp(180f, 0f, Mathf.SmoothStep(0f, 1f, flight));
            Quaternion rotation = Quaternion.Euler(0f, 0f, roll);
            PoseCar(floor, rotation, LipDistance + flight * -lip.z);
            shadow.gameObject.SetActive(progress >= 0.96f);
            ramp.gameObject.SetActive(progress < 1f);
            UpdateVapor(progress * 2f, 0.85f * Mathf.Sin(flight * Mathf.PI));
        }

        void PoseRampCar(float distance)
        {
            Vector3 point = RampOrigin + RampHeading * RampPoint(distance, out float angle);
            PoseCar(point, RampHeading * Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f), distance, 0f);
        }

        void CreateTraffic(GameObject trafficFleet)
        {
            Transform models = Object.Instantiate(trafficFleet, root, false).transform;
            traffic = new Traffic[models.childCount];
            for (int i = 0; i < traffic.Length; i++)
            {
                Transform model = models.GetChild(i);
                // Each fleet model is centered and aligned to the road at spawn.
                Bounds bounds = new(Vector3.zero, Vector3.zero);
                foreach (MeshFilter part in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    Bounds mesh = part.sharedMesh.bounds;
                    Vector3 center = model.InverseTransformPoint(part.transform.TransformPoint(mesh.center));
                    Vector3 x = Abs(model.InverseTransformVector(part.transform.TransformVector(Vector3.right * mesh.extents.x)));
                    Vector3 y = Abs(model.InverseTransformVector(part.transform.TransformVector(Vector3.up * mesh.extents.y)));
                    Vector3 z = Abs(model.InverseTransformVector(part.transform.TransformVector(Vector3.forward * mesh.extents.z)));
                    bounds.Encapsulate(new Bounds(center, 2f * (x + y + z)));
                }
                bounds.Expand(-0.12f);
                traffic[i] = new Traffic { model = model, bounds = bounds };
            }
        }

        void ResetTraffic()
        {
            nextTraffic = 1.05f;
            foreach (Traffic item in traffic)
                item.model.gameObject.SetActive(false);
            int first = Random.Range(0, traffic.Length);
            PlaceTraffic(traffic[first], LaneWidth, 51f, 12.5f);
            PlaceTraffic(traffic[(first + Random.Range(1, traffic.Length)) % traffic.Length], -LaneWidth, 94f, 13.5f);
        }

        public bool UpdateTraffic(float dt, float elapsed, float speed, Bounds player)
        {
            bool contact = false;
            foreach (Traffic item in traffic)
            {
                if (!item.model.gameObject.activeSelf)
                    continue;
                item.z -= (speed - item.speed) * dt;
                item.model.localPosition = new Vector3(item.x, 0f, item.z);
                var obstacle = new Bounds(item.model.localPosition + item.bounds.center, item.bounds.size);
                contact |= player.Intersects(obstacle);
                if (item.z < -10f)
                    item.model.gameObject.SetActive(false);
            }
            if (elapsed >= nextTraffic)
            {
                int start = Random.Range(0, traffic.Length);
                for (int i = 0; i < traffic.Length; i++)
                {
                    Traffic item = traffic[(start + i) % traffic.Length];
                    if (item.model.gameObject.activeSelf)
                        continue;
                    PlaceTraffic(item, Random.Range(-1, 2) * LaneWidth, 101f, Random.Range(9f, 15f));
                    break;
                }
                nextTraffic = elapsed + Random.Range(1.25f, 1.85f);
            }
            return contact;
        }

        static void PlaceTraffic(Traffic item, float x, float z, float speed)
        {
            item.x = x;
            item.z = z;
            item.speed = speed;
            item.model.localPosition = new Vector3(x, 0f, z);
            item.model.localRotation = Quaternion.identity;
            item.model.gameObject.SetActive(true);
        }
    }
}
