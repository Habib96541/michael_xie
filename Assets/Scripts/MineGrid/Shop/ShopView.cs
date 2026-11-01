using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField]
        private Button[] _categories;
        [SerializeField]
        private ShopItemCard[] _cards;
        [SerializeField]
        private CosmeticPreview _preview;
        [SerializeField]
        private TMP_Text _details;
        [SerializeField]
        private Button _action;
        [SerializeField]
        private TMP_Text _actionLabel;
        [SerializeField]
        private Button _previewEffect;
        [SerializeField]
        private Button _defaults;
        private GameUI _owner;
        private CosmeticService _service;
        private CosmeticCategory _category;
        private CosmeticItem _selected;
        private UnityAction[] _categoryActions;
        public CosmeticItem Selected => _selected;
        public Button ActionControl => _action;
        public CosmeticPreview Preview => _preview;

        public void Initialize(GameUI owner, CosmeticService service)
        {
            _owner = owner;
            _service = service;
            _categoryActions = new UnityAction[_categories.Length];
            for (int i = 0; i < _categories.Length; i++)
            {
                int index = i;
                _categoryActions[i] = () => SelectCategory((CosmeticCategory)index);
                _categories[i].onClick.AddListener(_categoryActions[i]);
            }

            foreach (ShopItemCard card in _cards)
            {
                card.Initialize(SelectItem);
            }

            _action.onClick.AddListener(PurchaseOrEquip);
            _previewEffect.onClick.AddListener(_preview.PlayEffect);
            _defaults.onClick.AddListener(Restore);
            _service.Changed += Refresh;
            _preview.Initialize(owner);
            SelectCategory(CosmeticCategory.Gem);
        }

        public void SelectCategory(CosmeticCategory category)
        {
            if (_owner.IsPopupOpen)
            {
                return;
            }

            _category = category;
            _selected = _service.Equipped(category);
            Refresh();
        }

        public void SelectItem(string id)
        {
            if (_owner.IsPopupOpen)
            {
                return;
            }

            CosmeticItem item = _service.Catalog.Find(id);
            if (!item || item.Category != _category)
            {
                return;
            }

            _selected = item;
            Refresh();
        }

        public void Refresh()
        {
            if (!_selected)
            {
                return;
            }

            var options = new List<CosmeticItem>();
            foreach (CosmeticItem item in _service.Catalog.Items)
            {
                if (item.Category == _category)
                {
                    options.Add(item);
                }
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].gameObject.SetActive(i < options.Count);
                if (i < options.Count)
                {
                    CosmeticItem item = options[i];
                    _cards[i].Render(item, _service.Owns(item.Id), _service.Equipped(_category).Id == item.Id, _selected.Id == item.Id);
                }
            }

            for (int i = 0; i < _categories.Length; i++)
            {
                _categories[i].interactable = i != (int)_category;
            }

            bool owned = _service.Owns(_selected.Id);
            bool equipped = _service.Equipped(_category).Id == _selected.Id;
            int missing = Math.Max(0, _selected.Price - _service.Coins);
            _details.text = _selected.DisplayName + "\n" + _selected.Description;
            _actionLabel.text = equipped ? "Equipped" : owned ? "Equip" : missing > 0 ? "Need " + missing.ToString("N0") + " more" : "Buy • " + _selected.Price.ToString("N0");
            _action.interactable = !equipped;
            _previewEffect.gameObject.SetActive(_category == CosmeticCategory.Reveal);
            _preview.Render(_selected, _service);
        }

        public void PurchaseOrEquip()
        {
            if (_owner.IsPopupOpen || !_selected)
            {
                return;
            }

            CosmeticItem item = _selected;
            if (_service.Owns(item.Id))
            {
                _service.Equip(item.Category, item.Id);
                _owner.Audio.Play(3);
                return;
            }

            CosmeticResult result = _service.Purchase(item.Id);
            if (result == CosmeticResult.Success)
            {
                _owner.Audio.Play(2);
                _owner.ShowPopup("Purchase complete!", item.DisplayName + " is yours.\n" + item.Price.ToString("N0") + " coins spent.\nEquip it now or keep browsing.", "Equip", () =>
                {
                    _service.Equip(item.Category, item.Id);
                    _owner.ClosePopup();
                }, "Later", _owner.ClosePopup);
            }
            else if (result == CosmeticResult.InsufficientCoins)
            {
                int missing = item.Price - _service.Coins;
                _owner.ShowPopup("Keep collecting", item.DisplayName + " costs " + item.Price.ToString("N0") + " coins.\nYou need " + missing.ToString("N0") + " more.\nPlay Mines to earn coins.", "Got it", _owner.ClosePopup);
            }
        }

        private void Restore()
        {
            if (_owner.IsPopupOpen)
            {
                return;
            }

            _service.RestoreDefaults();
            _selected = _service.Equipped(_category);
            Refresh();
            _owner.ShowPopup("Classic look restored", "Your purchased cosmetics are still yours.\nEquip them again whenever you like.", "Continue", _owner.ClosePopup);
        }

        private void OnDestroy()
        {
            if (_service == null)
            {
                return;
            }

            _service.Changed -= Refresh;
            for (int i = 0; i < _categories.Length; i++)
            {
                _categories[i].onClick.RemoveListener(_categoryActions[i]);
            }

            _action.onClick.RemoveListener(PurchaseOrEquip);
            _previewEffect.onClick.RemoveListener(_preview.PlayEffect);
            _defaults.onClick.RemoveListener(Restore);
        }
    }
}
