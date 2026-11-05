using UnityEngine;

namespace FlapDash
{
    public sealed class BackdropMotion : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private VisualEffectsSettings settings;
        [SerializeField] private Transform[] layers;
        [SerializeField] private float[] speeds;

        public Transform FirstLayer => layers[0];

        private void Update()
        {
            if (round.State == RoundState.Results || settings.Intensity == 0f) return;
            for (int index = 0; index < layers.Length; index++)
            {
                Vector3 position = layers[index].localPosition;
                position.x -= speeds[index] * settings.Intensity * Time.unscaledDeltaTime;
                if (position.x < -12f) position.x += 24f;
                layers[index].localPosition = position;
            }
        }
    }
}
