using UnityEngine;

namespace TeslaRoadster
{
    public sealed class RoadsterVehicle : MonoBehaviour
    {
        [SerializeField] Transform[] wheels;
        [SerializeField, Min(0.01f)] float wheelRadius = 0.33f;
        Quaternion[] parkedRotations;

        void Awake()
        {
            parkedRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
                parkedRotations[i] = wheels[i].localRotation;
        }

        internal void RollWheels(float distance)
        {
            Quaternion spin = Quaternion.AngleAxis(distance / wheelRadius * Mathf.Rad2Deg, Vector3.right);
            for (int i = 0; i < wheels.Length; i++)
                wheels[i].localRotation = spin * parkedRotations[i];
        }
    }
}
