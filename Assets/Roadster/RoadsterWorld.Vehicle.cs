using UnityEngine;

namespace TeslaRoadster
{
    internal sealed partial class RoadsterWorld
    {
        const float Tail = -2.16f;
        readonly Transform[] vapor = new Transform[18];

        void PoseCar(Vector3 floor, Quaternion rotation, float distance, float centerCompensation = 1f)
        {
            car.localRotation = rotation;
            car.localPosition = floor + (RotationCenter - rotation * RotationCenter) * centerCompensation;
            vehicle.RollWheels(distance);
        }

        static Mesh CreateVaporPuff()
        {
            var mesh = new Geometry();
            for (int row = 0; row < 5; row++)
            {
                float low = Mathf.PI * (row / 5f - 0.5f);
                float high = Mathf.PI * ((row + 1) / 5f - 0.5f);
                for (int side = 0; side < 9; side++)
                {
                    float a = side * Mathf.PI * 2f / 9f;
                    float b = (side + 1) * Mathf.PI * 2f / 9f;
                    mesh.Quad(PuffPoint(low, a), PuffPoint(high, a), PuffPoint(high, b), PuffPoint(low, b), Color.white);
                }
            }
            return mesh.ToMesh("Vapor puff");
        }

        static Vector3 PuffPoint(float latitude, float longitude) => new(
            Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));

        public void ShowLanding(float x, float pulse)
        {
            landingRing.gameObject.SetActive(pulse > 0.06f);
            if (landingRing.gameObject.activeSelf)
            {
                landingRing.localPosition = new Vector3(x, 0.045f, 0f);
                landingRing.localScale = new Vector3(1.35f - pulse * 0.63f, 1f, 1.35f - pulse * 0.63f);
            }
        }

        void UpdateVapor(float elapsed, float thrust, int sideJet = 0, bool driving = false)
        {
            for (int i = 0; i < vapor.Length; i++)
            {
                float age = (i + 1f) / vapor.Length;
                float size = thrust * Mathf.Sin(age * Mathf.PI) * (0.13f + age * 0.64f);
                Transform puff = vapor[i];
                puff.gameObject.SetActive(size > 0.055f);
                if (!puff.gameObject.activeSelf)
                    continue;

                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 offset;
                if (sideJet != 0)
                    offset = new Vector3(-sideJet * (0.83f + age * 3.2f), 0.37f - age * 0.22f, side * 0.85f - age * 1.65f);
                else if (driving)
                    offset = new Vector3(side * (0.55f + age * 0.13f), 0.22f - age * 1.5f, -0.54f - age * 2.9f);
                else
                    offset = new Vector3(side * (0.30f + age * 0.34f), 0.42f - age * 0.62f, Tail - age * 5.3f);
                puff.localPosition = car.localPosition + car.localRotation * offset
                    + new Vector3(Mathf.Sin(elapsed * 36f + i) * age * 0.15f, 0f, 0f);
                puff.localScale = new Vector3(size, size * 0.78f, size * 1.7f);
                puff.localRotation = Quaternion.Euler(i * 17f, i * 43f, i * 62f);
            }
        }
    }
}
