using UnityEngine;

namespace PegRush
{
    public sealed class PegFlash : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _remaining;
        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
        public void Flash(bool reduced)
        {
            if (!reduced) _remaining = 0.12f;
        }
        private void Update()
        {
            _remaining -= Time.deltaTime;
            _renderer.color = _remaining > 0f ? Color.white : new Color(0.35f, 0.88f, 0.94f);
        }
    }
}
