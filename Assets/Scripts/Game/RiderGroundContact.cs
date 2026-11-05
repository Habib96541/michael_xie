using System;
using UnityEngine;

namespace BullRide
{
    public sealed class RiderGroundContact : MonoBehaviour
    {
        public Action Impact { get; set; }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude < 1.2f) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < .5f) continue;
                Impact?.Invoke();
                break;
            }
        }
    }
}
