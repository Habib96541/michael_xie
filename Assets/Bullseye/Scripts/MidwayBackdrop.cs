using UnityEngine;

namespace BullseyeBlitz
{
    public sealed class MidwayBackdrop : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _art;

        public void Configure(Camera camera, SpriteRenderer art)
        {
            _camera = camera;
            _art = art;
            Fit();
        }

        private void LateUpdate()
        {
            Fit();
        }

        private void Fit()
        {
            if (_camera == null || _art == null || _art.sprite == null)
            {
                return;
            }

            Vector2 size = _art.sprite.bounds.size;
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            float scale = Mathf.Max(width / size.x, height / size.y);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(-(size.x * scale - width) * .35f, 0f, 0f);
        }
    }
}
