using System.Collections.Generic;
using UnityEngine;

namespace TeslaRoadster
{
    internal sealed partial class RoadsterWorld
    {
        public const float LaneWidth = 3.45f;
        const float SegmentLength = 28f;
        const float CoinSpacing = 4.4f;
        const float MissileNose = 1.15f;
        static readonly Vector3 RotationCenter = Vector3.up * 0.56f;
        static readonly Bounds CarHitbox = new(RotationCenter, new Vector3(1.78f, 1.08f, 4.20f));

        sealed class Pickup
        {
            public Transform model;
            public float distance, x;
        }

        readonly Shader shader;
        readonly Transform root, car, shadow, missile, target, flash, landingRing;
        readonly Transform[] road = new Transform[12];
        readonly Transform[] exhaust = new Transform[8];
        readonly Transform[] debris = new Transform[24];
        readonly Vector3[] debrisVelocity = new Vector3[24];
        readonly Pickup[] coins = new Pickup[27];
        readonly Pickup[] nitroPickups = new Pickup[5];
        readonly Mesh cube, cylinder;
        readonly Material paint, dark, chrome, redLight, gold, brightGold, scenery, hot, smoke, shadowColor;
        readonly RoadsterVehicle vehicle;
        float nextGroup, nextNitro;
        Vector3 impactPosition, lockedAim, previousMissileTip;
        Quaternion impactRotation;
        float impactDirection;
        bool blast;
        int groupIndex, coinLane, nitroGroup;

        public RoadsterWorld(Transform parent, Shader shader, Shader skyShader, Shader particleShader, RoadsterVehicle playerVehicle,
            GameObject trafficFleet, GameObject marineDrive)
        {
            this.shader = shader;
            root = Group(parent, "Marine Drive");
            RenderSettings.skybox = new Material(skyShader) { name = "Marine Drive dusk" };
            cube = CreateCube();
            cylinder = CreateCylinder(28, 1f, 1f);
            paint = Material("Ano White", 240, 242, 247, 0.53f, 0.10f);
            dark = Material("Carbon trim", 31, 25, 36, 0.23f);
            chrome = Material("Machined alloys", 127, 142, 151, 0.91f);
            redLight = Material("Tail lights", 255, 56, 55, glow: 1.15f);
            gold = Material("Coin edge", 222, 132, 28, 0.76f, 0.3f);
            brightGold = Material("Coin face", 255, 197, 64, 0.64f, 0.65f);
            scenery = Material("Highway scenery", 255, 255, 255);
            hot = Material("Exhaust and sparks", 255, 125, 49, glow: 1.7f);
            smoke = Material("Missile trail", 211, 175, 163, glow: 0.2f);
            shadowColor = Material("Contact shadow", 40, 29, 37, glow: 0.05f);

            CreateTrack(marineDrive);
            CreateTraffic(trafficFleet);
            car = Group(root, "Roadster");
            vehicle = Object.Instantiate(playerVehicle, car, false);
            shadow = Model(root, "Roadster contact shadow", cylinder, shadowColor);
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.localScale = new Vector3(1.18f, 2.28f, 0.02f);

            CreateParticles(particleShader);

            for (int i = 0; i < coins.Length; i++)
                coins[i] = new Pickup { model = CreateCoin() };
            var bottle = Material("Nitro canister", 22, 143, 242, 0.48f, 0.60f);
            var label = Material("Nitro band", 133, 231, 255, 0.24f, 0.85f);
            Mesh shoulder = CreateCylinder(16, 1f, 0.42f);
            for (int i = 0; i < nitroPickups.Length; i++)
                nitroPickups[i] = new Pickup { model = CreateNitroPickup(bottle, label, shoulder) };

            missile = Group(root, "Lock-on missile");
            CreateMissile();
            for (int i = 0; i < exhaust.Length; i++)
                exhaust[i] = Model(root, "Missile exhaust", cube, i < 2 ? hot : smoke);
            target = Model(root, "Lock indicator", CreateTarget(), redLight);
            var landingColor = Material("Landing dust", 220, 224, 220, glow: 0.35f);
            landingRing = Model(root, "Slam impact ring", CreateTarget(), landingColor);
            flash = Model(root, "Impact fireball", cube, hot);
            for (int i = 0; i < debris.Length; i++)
                debris[i] = Model(root, "Impact debris", cube, i < 14 ? hot : i % 2 == 0 ? dark : paint);

            Reset();
        }

        public void Reset()
        {
            groupIndex = 0;
            coinLane = 0;
            nextGroup = 17f;
            nextNitro = 36f;
            nitroGroup = 0;
            foreach (Pickup coin in coins)
                coin.model.gameObject.SetActive(false);
            foreach (Pickup nitro in nitroPickups)
                nitro.model.gameObject.SetActive(false);
            foreach (Transform piece in debris)
                piece.gameObject.SetActive(false);
            ClearParticles();
            ClearMissile();
            landingRing.gameObject.SetActive(false);
            flash.gameObject.SetActive(false);
            ramp.gameObject.SetActive(false);
            car.gameObject.SetActive(true);
            shadow.gameObject.SetActive(true);
            ResetTraffic();
            PlaceCar(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0);
            Advance(0f, 0f, 0f, 0f, out _);
        }

        public void PlaceCar(float x, float yaw, float distance, float height, float pitch,
            float roll, float thrust, float dt, int sideJet)
        {
            PoseCar(new Vector3(x, height, 0f),
                Quaternion.Euler(pitch, yaw, roll), distance);
            shadow.localPosition = new Vector3(x, 0.018f, -0.12f);
            float scale = 1f - Mathf.Clamp01(height / 4f) * 0.38f;
            shadow.localScale = new Vector3(1.18f * scale, 2.28f * scale, 0.02f);
            UpdateVapor(dt, thrust, sideJet, true);
        }

        public int Advance(float previousDistance, float distance, float carX, float time, out int nitroCollected)
        {
            for (int i = 0; i < road.Length; i++)
            {
                float z = Mathf.Repeat(i * SegmentLength - distance + SegmentLength, road.Length * SegmentLength)
                    - 2f * SegmentLength;
                road[i].localPosition = new Vector3(0f, 0f, z);
            }

            while (nextGroup - distance < 150f)
            {
                if (groupIndex == 1)
                    coinLane = -1;
                else if (groupIndex >= 2)
                {
                    if (coinLane == 0)
                        coinLane = Random.value < 0.5f ? -1 : 1;
                    else if (Random.value < 0.85f)
                        coinLane = 0;
                }
                for (int i = 0; i < 3; i++)
                    SpawnPickup(coins, nextGroup + i * CoinSpacing, coinLane * LaneWidth);
                nextGroup += Random.Range(27f, 32f);
                groupIndex++;
            }

            while (nextNitro - distance < 150f)
            {
                int lane = (nitroGroup + 1) % 3 - 1;
                SpawnPickup(nitroPickups, nextNitro, lane * LaneWidth);
                nextNitro += 66f + nitroGroup % 3 * 7f;
                nitroGroup++;
            }

            nitroCollected = UpdatePickups(nitroPickups, previousDistance, distance, carX, time, 1.12f, 89f);
            return UpdatePickups(coins, previousDistance, distance, carX, time, 0.95f, 118f);
        }

        static int UpdatePickups(Pickup[] items, float previousDistance, float distance, float carX, float time, float height, float spin)
        {
            int collected = 0;
            foreach (Pickup item in items)
            {
                if (!item.model.gameObject.activeSelf)
                    continue;
                bool crossed = item.distance > previousDistance && item.distance <= distance;
                bool pickedUp = crossed && Mathf.Abs(item.x - carX) < 1.24f;
                if (pickedUp || item.distance < distance - 6f)
                {
                    item.model.gameObject.SetActive(false);
                    if (pickedUp)
                        collected++;
                    continue;
                }
                float bob = Mathf.Sin(time * 4f + item.distance * 0.3f) * 0.11f;
                item.model.localPosition = new Vector3(item.x, height + bob, item.distance - distance);
                item.model.localRotation = Quaternion.Euler(0f, time * spin + item.distance * 4f, 0f);
            }
            return collected;
        }

        static void SpawnPickup(Pickup[] items, float distance, float x)
        {
            foreach (Pickup item in items)
            {
                if (item.model.gameObject.activeSelf)
                    continue;
                item.distance = distance;
                item.x = x;
                item.model.gameObject.SetActive(true);
                return;
            }
        }

        public Vector3 CarCenter => car.localPosition + car.localRotation * RotationCenter;

        public Bounds PlayerBounds
        {
            get
            {
                Quaternion rotation = car.localRotation;
                Vector3 x = Abs(rotation * (Vector3.right * CarHitbox.extents.x));
                Vector3 y = Abs(rotation * (Vector3.up * CarHitbox.extents.y));
                Vector3 z = Abs(rotation * (Vector3.forward * CarHitbox.extents.z));
                return new Bounds(CarCenter, 2f * (x + y + z));
            }
        }

        static Vector3 Abs(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        public void LockMissile(Vector3 aim)
        {
            lockedAim = aim;
            previousMissileTip = MissilePosition(0f, lockedAim);
            missile.localRotation = Quaternion.LookRotation(MissilePosition(0.01f, lockedAim) - previousMissileTip);
            missile.localPosition = previousMissileTip - missile.localRotation * Vector3.forward * MissileNose;
            missile.gameObject.SetActive(true);
            target.localPosition = new Vector3(lockedAim.x, 0.038f, lockedAim.z);
            target.gameObject.SetActive(true);
        }

        public bool MoveMissile(float progress)
        {
            Vector3 tip = MissilePosition(progress, lockedAim);
            Vector3 travel = tip - previousMissileTip;
            if (travel.sqrMagnitude > 0.00001f)
                missile.localRotation = Quaternion.LookRotation(travel);
            missile.localPosition = tip - missile.localRotation * Vector3.forward * MissileNose;
            for (int i = 0; i < exhaust.Length; i++)
            {
                Transform puff = exhaust[i];
                float t = progress - (i + 2) * 0.029f;
                puff.gameObject.SetActive(t > 0f);
                puff.localPosition = MissilePosition(t, lockedAim);
                puff.localRotation = Quaternion.Euler(31f * i, 67f * i, 24f * i);
                puff.localScale = Vector3.one * (0.26f + i * 0.073f);
            }
            target.localRotation = Quaternion.Euler(0f, progress * 130f, 0f);
            // Test in the Roadster's local coordinates so a rotating car can clear the shot.
            Quaternion inverse = Quaternion.Inverse(car.localRotation);
            Vector3 from = inverse * (previousMissileTip - car.localPosition);
            Bounds hitbox = CarHitbox;
            hitbox.Expand(0.34f);
            float length = travel.magnitude;
            bool hit = hitbox.Contains(from) || (length > 0.0001f
                && hitbox.IntersectRay(new Ray(from, inverse * (travel / length)), out float entry)
                && entry <= length);
            previousMissileTip = tip;
            return hit;
        }

        public void ClearMissile()
        {
            missile.gameObject.SetActive(false);
            target.gameObject.SetActive(false);
            foreach (Transform puff in exhaust)
                puff.gameObject.SetActive(false);
        }

        static Vector3 MissilePosition(float progress, Vector3 targetPosition)
        {
            float t = progress * (0.7f + 0.3f * progress);
            return Vector3.LerpUnclamped(new Vector3(-17f, 13.7f, 63f), targetPosition, t)
                + Vector3.up * ((1f - t) * t * 14f);
        }

        public void Impact(bool missileHit)
        {
            impactPosition = car.localPosition;
            impactRotation = car.localRotation;
            impactDirection = impactPosition.x > 0f ? -1f : 1f;
            blast = missileHit;
            ClearMissile();
            landingRing.gameObject.SetActive(false);
            BurstAt(impactPosition + Vector3.up * 0.18f, missileHit ? 1.65f : 1.25f);
            flash.gameObject.SetActive(blast);
            flash.localPosition = impactPosition + Vector3.up * 0.70f;
            flash.localScale = Vector3.one * 1.6f;
            for (int i = 0; i < debris.Length; i++)
            {
                Transform piece = debris[i];
                piece.gameObject.SetActive(true);
                piece.localPosition = flash.localPosition + Random.insideUnitSphere * 0.65f;
                piece.localRotation = Random.rotation;
                piece.localScale = Vector3.one * Random.Range(0.12f, 0.48f);
                debrisVelocity[i] = new Vector3(Random.Range(-5f, 5f), Random.Range(2.2f, 7f), Random.Range(-5f, 5f));
            }
        }

        public void AnimateImpact(float dt, float time)
        {
            float jump = Mathf.Sin(Mathf.Clamp01(time / 0.65f) * Mathf.PI);
            float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.9f));
            car.localPosition = new Vector3(impactPosition.x + impactDirection * 1.55f * slide,
                Mathf.Lerp(impactPosition.y, 0f, slide) + jump * 0.43f, Mathf.Lerp(impactPosition.z, -1.5f, slide));
            Quaternion spin = Quaternion.Euler(-jump * 12f, 148f * slide * impactDirection, (jump * 31f + 12f * slide) * impactDirection);
            car.localRotation = Quaternion.Slerp(impactRotation, spin, Mathf.Clamp01(time * 9f));
            flash.gameObject.SetActive(blast && time < 0.22f);
            flash.localScale = Vector3.one * (1.6f + time * 9f);
            flash.Rotate(71f * dt, 183f * dt, 60f * dt);
            for (int i = 0; i < debris.Length; i++)
            {
                Transform piece = debris[i];
                debrisVelocity[i].y -= 16f * dt;
                Vector3 position = piece.localPosition + debrisVelocity[i] * dt;
                if (position.y < 0.10f)
                {
                    position.y = 0.10f;
                    debrisVelocity[i] *= 0.55f;
                    debrisVelocity[i].y = 0f;
                }
                piece.localPosition = position;
                piece.Rotate(130f * dt, 91f * dt, 180f * dt);
                if (i < 14 && time > 0.28f)
                    piece.localScale *= Mathf.Exp(-5f * dt);
            }
        }

        Transform CreateCoin()
        {
            var group = Group(root, "Credit coin");
            var edge = Model(group, "Gold edge", cylinder, gold);
            edge.localScale = new Vector3(0.49f, 0.49f, 0.15f);
            for (int side = -1; side <= 1; side += 2)
            {
                var face = Model(group, "Raised gold face", cylinder, brightGold);
                face.localPosition = new Vector3(0f, 0f, side * 0.078f);
                face.localScale = new Vector3(0.403f, 0.403f, 0.018f);
                var mark = Block(group, "Credit mark", new Vector3(0f, 0f, side * 0.095f), new Vector3(0.25f, 0.25f, 0.021f), gold);
                mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            return group;
        }

        Transform CreateNitroPickup(Material bottle, Material label, Mesh shoulder)
        {
            var group = Group(root, "Nitro pickup");
            var body = Model(group, "Bottle body", cylinder, bottle);
            body.localScale = new Vector3(0.30f, 0.30f, 0.69f);
            body.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var top = Model(group, "Bottle shoulder", shoulder, bottle);
            top.localPosition = Vector3.up * 0.43f;
            top.localRotation = body.localRotation;
            top.localScale = new Vector3(0.30f, 0.30f, 0.17f);
            var band = Model(group, "Cyan band", cylinder, label);
            band.localScale = new Vector3(0.306f, 0.306f, 0.15f);
            band.localRotation = body.localRotation;
            var cap = Model(group, "Valve", cylinder, chrome);
            cap.localPosition = Vector3.up * 0.55f;
            cap.localRotation = body.localRotation;
            cap.localScale = new Vector3(0.115f, 0.115f, 0.08f);
            return group;
        }

        void CreateMissile()
        {
            var casing = Model(missile, "Missile casing", cylinder, chrome);
            casing.localScale = new Vector3(0.23f, 0.23f, 1.38f);
            var nose = Model(missile, "Missile nose", CreateCylinder(20, 1f, 0f), redLight);
            nose.localPosition = Vector3.forward * 0.92f;
            nose.localScale = new Vector3(0.23f, 0.23f, 0.46f);
            var band = Model(missile, "Missile band", cylinder, dark);
            band.localPosition = Vector3.back * 0.51f;
            band.localScale = new Vector3(0.245f, 0.245f, 0.16f);
            for (int i = 0; i < 4; i++)
            {
                Quaternion rotation = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                var fin = Block(missile, "Missile fin", rotation * new Vector3(0f, 0.32f, -0.51f),
                    new Vector3(0.055f, 0.35f, 0.49f), dark);
                fin.localRotation = rotation;
            }
            var flame = Model(missile, "Missile flame", CreateCylinder(12, 0f, 1f), hot);
            flame.localPosition = Vector3.back * 1.0f;
            flame.localScale = new Vector3(0.2f, 0.2f, 0.72f);
        }

        Material Material(string name, byte r, byte g, byte b, float shine = 0f, float glow = 0f)
        {
            var material = new Material(shader) { name = name, color = new Color32(r, g, b, 255) };
            material.SetFloat("_Shine", shine);
            material.SetFloat("_Glow", glow);
            return material;
        }

        static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static Transform Model(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        Transform Block(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            var part = Model(parent, name, cube, material);
            part.localPosition = center;
            part.localScale = size;
            return part;
        }

        static Mesh CreateCube()
        {
            var geometry = new Geometry();
            geometry.Block(Vector3.zero, Vector3.one, Color.white);
            return geometry.ToMesh("Unit block");
        }

        static Mesh CreateCylinder(int sides, float backRadius, float frontRadius)
        {
            var geometry = new Geometry();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 q = new(Mathf.Cos(b), Mathf.Sin(b), 0f);
                Vector3 p0 = p * backRadius + Vector3.back * 0.5f;
                Vector3 q0 = q * backRadius + Vector3.back * 0.5f;
                Vector3 p1 = p * frontRadius + Vector3.forward * 0.5f;
                Vector3 q1 = q * frontRadius + Vector3.forward * 0.5f;
                geometry.Quad(p0, q0, q1, p1, Color.white);
                if (backRadius > 0f)
                    geometry.Triangle(Vector3.back * 0.5f, q0, p0, Color.white);
                if (frontRadius > 0f)
                    geometry.Triangle(Vector3.forward * 0.5f, p1, q1, Color.white);
            }
            return geometry.ToMesh("Cylinder");
        }

        static Mesh CreateTarget()
        {
            var geometry = new Geometry();
            for (int i = 0; i < 32; i++)
            {
                if (i % 8 < 2)
                    continue;
                float a = i * Mathf.PI / 16f;
                float b = (i + 1) * Mathf.PI / 16f;
                Vector3 p = new(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 q = new(Mathf.Cos(b), 0f, Mathf.Sin(b));
                geometry.Quad(q * 1.66f, q * 1.8f, p * 1.8f, p * 1.66f, Color.white);
            }
            return geometry.ToMesh("Segmented target ring");
        }

        sealed class Geometry
        {
            readonly List<Vector3> vertices = new();
            readonly List<int> indices = new();
            readonly List<Color> colors = new();

            public void Vertex(Vector3 point, Color color)
            {
                vertices.Add(point);
                colors.Add(color);
            }

            public void Face(int a, int b, int c)
            {
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }

            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                int i = vertices.Count;
                Vertex(a, color);
                Vertex(b, color);
                Vertex(c, color);
                Face(i, i + 1, i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
            {
                int i = vertices.Count;
                Vertex(a, color);
                Vertex(b, color);
                Vertex(c, color);
                Vertex(d, color);
                Face(i, i + 1, i + 2);
                Face(i, i + 2, i + 3);
            }

            public void Block(Vector3 center, Vector3 size, Color color, float yaw = 0f, float roll = 0f)
                => Block(center, size, color, Quaternion.Euler(0f, yaw, roll));

            void Block(Vector3 center, Vector3 size, Color color, Quaternion rotation)
            {
                Vector3 x = rotation * Vector3.right * (size.x * 0.5f);
                Vector3 y = rotation * Vector3.up * (size.y * 0.5f);
                Vector3 z = rotation * Vector3.forward * (size.z * 0.5f);
                Vector3 a = center - x - y - z;
                Vector3 b = center - x - y + z;
                Vector3 c = center + x - y + z;
                Vector3 d = center + x - y - z;
                Quad(a + 2f * y, b + 2f * y, c + 2f * y, d + 2f * y, color);
                Quad(d, c, b, a, color);
                Quad(a, b, b + 2f * y, a + 2f * y, color);
                Quad(c, d, d + 2f * y, c + 2f * y, color);
                Quad(b, c, c + 2f * y, b + 2f * y, color);
                Quad(d, a, a + 2f * y, d + 2f * y, color);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(indices, 0);
                mesh.SetColors(colors);
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
