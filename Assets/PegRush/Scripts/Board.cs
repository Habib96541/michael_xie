using System.Collections.Generic;
using UnityEngine;

namespace PegRush
{
    public sealed class Board : MonoBehaviour
    {
        [SerializeField] private GameObject _pegPrefab;
        [SerializeField] private GameObject _pocketPrefab;
        [SerializeField] private PhysicsMaterial2D _material;
        private readonly List<GameObject> _parts = new List<GameObject>();
        private GameConfig _config;
        private PhysicsMaterial2D _runtimeMaterial;
        public PhysicsMaterial2D Material => _runtimeMaterial != null ? _runtimeMaterial : _material;
        public void Configure(GameConfig config)
        {
            _config = config;
            _runtimeMaterial = Instantiate(_material);
            _runtimeMaterial.bounciness = config.Restitution;
            _runtimeMaterial.friction = config.Friction;
        }
        public int Rows { get; private set; }
        public void Wire(GameObject peg, GameObject pocket, PhysicsMaterial2D material)
        {
            _pegPrefab = peg;
            _pocketPrefab = pocket;
            _material = material;
        }
        public void Generate(int rows)
        {
            foreach (GameObject part in _parts) Destroy(part);
            _parts.Clear();
            Rows = rows;
            for (int row = 0; row < rows; row++)
            {
                int count = row + (_config == null ? 1 : _config.TopPegCount);
                for (int column = 0; column < count; column++)
                {
                    GameObject peg = Instantiate(_pegPrefab, transform);
                    peg.transform.position = new Vector3(column - (count - 1) * 0.5f, _config == null ? rows - row + 1f : 2f + (rows - row - 1) * _config.RowSpacing, 0f);
                    if (_config != null) peg.transform.localScale = Vector3.one * _config.PegDiameter;
                    peg.GetComponent<Collider2D>().sharedMaterial = Material;
                    _parts.Add(peg);
                }
            }
            for (int i = 0; i <= rows; i++)
            {
                GameObject pocket = Instantiate(_pocketPrefab, transform);
                pocket.transform.position = new Vector3(i - rows * 0.5f, 0.65f, 0f);
                pocket.GetComponent<Pocket>().Index = i;
                _parts.Add(pocket);
            }
            for (int i = 0; i <= rows + 1; i++) Wall(new Vector2(i - (rows + 1) * 0.5f, 0.65f), new Vector2(0.05f, 1.3f), 0f);
            float half = (rows + 1) * 0.5f;
            float padding = _config == null ? 0.35f : _config.BoundaryPadding;
            Wall(new Vector2(-half - padding, (rows + 3) * 0.5f), new Vector2(0.18f, rows + 5), 0f);
            Wall(new Vector2(half + padding, (rows + 3) * 0.5f), new Vector2(0.18f, rows + 5), 0f);
        }
        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }
        private void Wall(Vector2 position, Vector2 size, float rotation)
        {
            GameObject wall = new GameObject("Boundary");
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.sharedMaterial = Material;
            _parts.Add(wall);
        }
    }
}
