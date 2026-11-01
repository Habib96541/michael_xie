using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class CosmeticApplicator : MonoBehaviour
    {
        [SerializeField]
        private Image _frame;
        [SerializeField]
        private Image _well;
        [SerializeField]
        private TileView[] _tiles;
        private CosmeticService _service;
        public void Initialize(CosmeticService service)
        {
            _service = service;
            _service.Changed += Apply;
            Apply();
        }

        public void Apply()
        {
            CosmeticItem board = _service.Equipped(CosmeticCategory.Board);
            CosmeticItem tile = _service.Equipped(CosmeticCategory.Tile);
            CosmeticItem gem = _service.Equipped(CosmeticCategory.Gem);
            CosmeticItem effect = _service.Equipped(CosmeticCategory.Reveal);
            _frame.color = board.Primary;
            _well.color = board.Secondary;
            foreach (TileView view in _tiles)
            {
                view.ApplyCosmetics(tile.Artwork, gem.Artwork, gem.Material, effect.RevealStyle, effect.Primary);
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.Changed -= Apply;
            }
        }
    }
}
