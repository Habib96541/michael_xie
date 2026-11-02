using System.Collections.Generic;
using UnityEngine;

namespace ShatterLeap
{
    public sealed class BridgeView : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Material _structure;
        [SerializeField] private Material _pink;
        private readonly List<GameObject> _generated = new List<GameObject>();
        private GlassPanel[,] _panels;
        public const float Spacing = 3.2f;
        public Vector3 StartPoint => new Vector3(0, 0.12f, 0);
        public Vector3 FinishPoint { get; private set; }
        public float Length { get; private set; }
        public Collider FinishSupport { get; private set; }

        public void Generate(RunRules run)
        {
            foreach (GameObject item in _generated)
            {
                item.SetActive(false);
                Destroy(item);
            }
            _generated.Clear();
            _panels = new GlassPanel[run.Difficulty.Rows, 2];
            Length = (run.Difficulty.Rows + 1) * Spacing;
            FinishPoint = new Vector3(0, 0.12f, Length);
            Block("Departure", new Vector3(0, -0.3f, -0.6f), new Vector3(6, 0.6f, 4.8f), _structure, true);
            FinishSupport = Block("Arrival", new Vector3(0, -0.3f, Length + 0.7f), new Vector3(6, 0.6f, 4.8f), _structure, true).GetComponent<Collider>();
            for (int row = 0; row < run.Difficulty.Rows; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    GameObject instance = Instantiate(_config.PanelPrefab, transform);
                    instance.name = $"Row {row + 1} / {(side == 0 ? "Left" : "Right")}";
                    instance.transform.position = new Vector3(side == 0 ? -1.1f : 1.1f, 0, (row + 1) * Spacing);
                    GlassPanel panel = instance.GetComponent<GlassPanel>();
                    panel.Initialize(row, side);
                    _panels[row, side] = panel;
                    _generated.Add(instance);
                }
                Block("Cross brace", new Vector3(0, -0.28f, (row + 1) * Spacing), new Vector3(5, 0.16f, 0.18f), _structure, false);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Block("Suspended beam", new Vector3(side * 2.65f, -0.45f, Length / 2), new Vector3(0.22f, 0.45f, Length + 4), _structure, false);
                Block("Light ribbon", new Vector3(side * 2.55f, -0.15f, Length / 2), new Vector3(0.055f, 0.06f, Length + 4), _pink, false);
                for (int i = 0; i <= run.Difficulty.Rows + 1; i += 2)
                {
                    Block("Light pillar", new Vector3(side * 3.4f, 1.5f, i * Spacing), new Vector3(0.14f, 3.6f, 0.14f), _pink, false);
                }
            }
        }
        public GlassPanel Panel(int row, int side) => _panels[row, side];
        public void Reveal(RunRules run, bool visible)
        {
            for (int row = 0; row < run.Difficulty.Rows; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    _panels[row, side].Reveal(visible && run.SafeSide(row) == side);
                }
            }
        }
        private GameObject Block(string label, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = label;
            block.transform.SetParent(transform);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            if (solid) { block.layer = 8; }
            if (!solid) { Destroy(block.GetComponent<Collider>()); }
            _generated.Add(block);
            return block;
        }
    }
}
