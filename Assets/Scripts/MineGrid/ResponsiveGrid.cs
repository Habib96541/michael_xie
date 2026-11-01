using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class ResponsiveGrid : MonoBehaviour
    {
        private GridLayoutGroup _grid;
        private void Awake() => _grid = GetComponent<GridLayoutGroup>();
        private void OnRectTransformDimensionsChange()
        {
            if (!_grid)
            {
                _grid = GetComponent<GridLayoutGroup>();
            }

            var r = (RectTransform)transform;
            float size = Mathf.Max(1, Mathf.Min((r.rect.width - 40) / 5, (r.rect.height - 40) / 5));
            _grid.cellSize = new Vector2(size, size);
        }

        private void OnEnable() => OnRectTransformDimensionsChange();
    }
}
