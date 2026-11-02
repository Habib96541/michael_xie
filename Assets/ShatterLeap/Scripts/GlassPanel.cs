using UnityEngine;

namespace ShatterLeap
{
    public sealed class GlassPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _previewMarker;
        [SerializeField] private Collider _support;
        [SerializeField] private Renderer _glass;
        public int Row { get; private set; }
        public int Side { get; private set; }
        public Collider Support => _support;
        public Vector3 LandingPoint => transform.position + Vector3.up * 0.12f;

        public void Initialize(int row, int side)
        {
            Row = row;
            Side = side;
            _support.enabled = true;
            _glass.enabled = true;
            Reveal(false);
        }
        public void Reveal(bool visible) => _previewMarker.SetActive(visible);
        public void Break()
        {
            _support.enabled = false;
            _glass.enabled = false;
            Reveal(false);
        }
    }
}
