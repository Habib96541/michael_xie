using UnityEngine;

namespace BullRide
{
    public sealed class ArenaView : MonoBehaviour
    {
        Transform gate;

        public void Build()
        {
            gate = RodeoArt.Shared.BuildArena(transform)["Gate"];
            var floor = gameObject.AddComponent<BoxCollider>();
            floor.center = new Vector3(0, -.25f, 0);
            floor.size = new Vector3(72, .5f, 72);
        }

        public void SetGate(float openness) => gate.localRotation = Quaternion.Euler(0, openness * 90, 0);
    }
}
