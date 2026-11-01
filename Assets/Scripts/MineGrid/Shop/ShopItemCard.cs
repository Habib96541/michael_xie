using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class ShopItemCard : MonoBehaviour
    {
        [SerializeField]
        private Button _button;
        [SerializeField]
        private Image _background;
        [SerializeField]
        private Image _artwork;
        [SerializeField]
        private TMP_Text _name;
        [SerializeField]
        private TMP_Text _price;
        [SerializeField]
        private TMP_Text _state;
        private Action<string> _selected;
        private CosmeticItem _item;
        public void Initialize(Action<string> selected)
        {
            _selected = selected;
            _button.onClick.AddListener(Select);
        }

        public void Render(CosmeticItem item, bool owned, bool equipped, bool selected)
        {
            _item = item;
            _artwork.sprite = item.Artwork;
            _artwork.material = item.Material;
            _name.text = item.DisplayName;
            _price.text = item.IsDefault ? "Free default" : owned ? "Purchased" : item.Price.ToString("N0") + " coins";
            _state.text = equipped ? "Equipped" : owned ? "Owned" : "Preview";
            _state.color = equipped ? new Color(.4f, 1, .7f) : Color.white;
            _background.color = selected ? new Color(.20f, .31f, .47f) : new Color(.105f, .14f, .29f);
        }

        private void Select() => _selected?.Invoke(_item.Id);
        private void OnDestroy() => _button.onClick.RemoveListener(Select);
    }
}
