using UnityEngine;

namespace FlapDash
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PipeGate : MonoBehaviour
    {
        private RoundController round;
        private bool hasScored;

        public void InitializeRound(RoundController controller)
        {
            round = controller;
            hasScored = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && other.TryGetComponent(out FlapPlayer player))
                Pass(player);
        }

        public void Pass(FlapPlayer player)
        {
            if (hasScored || round == null || !round.IsPlaying || player == null)
                return;

            hasScored = true;
            round.AddPoint();
        }
    }
}
