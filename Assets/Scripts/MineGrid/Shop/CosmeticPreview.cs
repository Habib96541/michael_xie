using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class CosmeticPreview : MonoBehaviour
    {
        [SerializeField]
        private Image _frame;
        [SerializeField]
        private Image[] _tiles;
        [SerializeField]
        private Image _gem;
        [SerializeField]
        private RevealEffectPlayer _effect;
        private CosmeticItem _effectItem;
        private GameUI _owner;
        public void Initialize(GameUI owner) => _owner = owner;
        public void Render(CosmeticItem selected, CosmeticService service)
        {
            CosmeticItem board = selected.Category == CosmeticCategory.Board ? selected : service.Equipped(CosmeticCategory.Board);
            CosmeticItem tile = selected.Category == CosmeticCategory.Tile ? selected : service.Equipped(CosmeticCategory.Tile);
            CosmeticItem gem = selected.Category == CosmeticCategory.Gem ? selected : service.Equipped(CosmeticCategory.Gem);
            _effectItem = selected.Category == CosmeticCategory.Reveal ? selected : service.Equipped(CosmeticCategory.Reveal);
            _frame.color = board.Primary;
            foreach (Image image in _tiles)
            {
                image.sprite = tile.Artwork;
            }

            _gem.sprite = gem.Artwork;
            _gem.material = gem.Material;
        }

        public void PlayEffect()
        {
            if (_owner.IsPopupOpen)
            {
                return;
            }

            _effect.Play(_effectItem.RevealStyle, _effectItem.Primary, _owner.ReducedEffects);
            _owner.Audio.Play(0);
        }
    }
}
