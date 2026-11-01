using System;
using System.Collections.Generic;

namespace MineGrid
{
    [Serializable]
    public sealed class EquippedCosmetic
    {
        public CosmeticCategory category;
        public string id;
    }

    public sealed class CosmeticService
    {
        private readonly SaveService _saves;
        private readonly CosmeticCatalog _catalog;
        public event Action Changed;
        public CosmeticCatalog Catalog => _catalog;
        public int Coins => _saves.Data.coins;

        public CosmeticService(SaveService saves, CosmeticCatalog catalog)
        {
            catalog.Validate();
            _saves = saves;
            _catalog = catalog;
            Normalize();
        }

        public bool Owns(string id) => _catalog.Find(id) && _saves.Data.ownedCosmetics.Contains(id);
        public CosmeticItem Equipped(CosmeticCategory category)
        {
            EquippedCosmetic entry = _saves.Data.equippedCosmetics.Find(value => value.category == category);
            CosmeticItem item = entry == null ? null : _catalog.Find(entry.id);
            return item && item.Category == category && Owns(item.Id) ? item : _catalog.Default(category);
        }

        public CosmeticResult Purchase(string id)
        {
            CosmeticItem item = _catalog.Find(id);
            if (!item)
            {
                return CosmeticResult.InvalidItem;
            }

            if (Owns(id))
            {
                return CosmeticResult.AlreadyOwned;
            }

            if (_saves.Data.coins < item.Price)
            {
                return CosmeticResult.InsufficientCoins;
            }

            // Currency and ownership are serialized in one snapshot before feedback.
            _saves.Data.coins -= item.Price;
            _saves.Data.ownedCosmetics.Add(id);
            _saves.Save();
            Changed?.Invoke();
            return CosmeticResult.Success;
        }

        public CosmeticResult Equip(CosmeticCategory category, string id)
        {
            CosmeticItem item = _catalog.Find(id);
            if (!item)
            {
                return CosmeticResult.InvalidItem;
            }

            if (item.Category != category)
            {
                return CosmeticResult.CategoryMismatch;
            }

            if (!Owns(id))
            {
                return CosmeticResult.NotOwned;
            }

            _saves.Data.equippedCosmetics.RemoveAll(value => value.category == category);
            _saves.Data.equippedCosmetics.Add(new EquippedCosmetic { category = category, id = id });
            _saves.Save();
            Changed?.Invoke();
            return CosmeticResult.Success;
        }

        public void RestoreDefaults()
        {
            _saves.Data.equippedCosmetics.Clear();
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                _saves.Data.equippedCosmetics.Add(new EquippedCosmetic { category = category, id = _catalog.Default(category).Id });
            }

            _saves.Save();
            Changed?.Invoke();
        }

        public void Normalize()
        {
            _saves.Data.ownedCosmetics ??= new List<string>();
            _saves.Data.ownedCosmetics = new List<string>(new HashSet<string>(_saves.Data.ownedCosmetics));
            _saves.Data.equippedCosmetics ??= new List<EquippedCosmetic>();
            var valid = new List<EquippedCosmetic>();
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                CosmeticItem fallback = _catalog.Default(category);
                if (!_saves.Data.ownedCosmetics.Contains(fallback.Id))
                {
                    _saves.Data.ownedCosmetics.Add(fallback.Id);
                }

                EquippedCosmetic entry = _saves.Data.equippedCosmetics.Find(value => value != null && value.category == category);
                CosmeticItem item = entry == null ? null : _catalog.Find(entry.id);
                valid.Add(new EquippedCosmetic { category = category, id = item && item.Category == category && Owns(item.Id) ? item.Id : fallback.Id });
            }

            _saves.Data.equippedCosmetics = valid;
            _saves.Save();
            Changed?.Invoke();
        }
    }
}
