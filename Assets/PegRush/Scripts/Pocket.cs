using UnityEngine;

namespace PegRush
{
    public sealed class Pocket : MonoBehaviour
    {
        public int Index;
        private void OnTriggerEnter2D(Collider2D other)
        {
            Ball ball = other.GetComponent<Ball>();
            if (ball != null) ball.Complete(Index);
        }
    }
}
