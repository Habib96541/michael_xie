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
                for (int column = 0; column <= row; column++)
                {
                    GameObject peg = Instantiate(_pegPrefab, transform);
                    peg.transform.position = new Vector3(column - row * 0.5f, rows - row + 1f, 0f);
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
            Wall(new Vector2(-half - 0.35f, (rows + 3) * 0.5f), new Vector2(0.18f, rows + 5), 0f);
            Wall(new Vector2(half + 0.35f, (rows + 3) * 0.5f), new Vector2(0.18f, rows + 5), 0f);
        }
        private void Wall(Vector2 position, Vector2 size, float rotation)
        {
            GameObject wall = new GameObject("Boundary");
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.sharedMaterial = _material;
            _parts.Add(wall);
        }
    }
}
