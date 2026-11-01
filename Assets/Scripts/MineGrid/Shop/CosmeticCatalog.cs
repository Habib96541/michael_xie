using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineGrid
{
    [CreateAssetMenu(menuName = "MineGrid/Shop/Catalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        [SerializeField]
        private CosmeticItem[] _items;
        public IReadOnlyList<CosmeticItem> Items => _items;

        public CosmeticItem Find(string id)
        {
            if (_items == null)
            {
                return null;
            }

            foreach (CosmeticItem item in _items)
            {
                if (item && item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }

        public CosmeticItem Default(CosmeticCategory category)
        {
            foreach (CosmeticItem item in _items)
            {
                if (item.Category == category && item.IsDefault)
                {
                    return item;
                }
            }

            throw new InvalidOperationException("Missing default for " + category);
        }

        public void Validate()
        {
            if (_items == null || _items.Length == 0)
            {
                throw new InvalidOperationException("Empty cosmetic catalog.");
            }

            var identifiers = new HashSet<string>();
            var defaults = new int[4];
            foreach (CosmeticItem item in _items)
            {
                if (!item || string.IsNullOrWhiteSpace(item.Id) || !identifiers.Add(item.Id))
                {
                    throw new InvalidOperationException("Missing or duplicate cosmetic ID.");
                }

                if (!Enum.IsDefined(typeof(CosmeticCategory), item.Category) || item.Price < 0 || !item.Artwork)
                {
                    throw new InvalidOperationException("Invalid cosmetic configuration: " + item.Id);
                }

                if (item.IsDefault)
                {
                    if (item.Price != 0)
                    {
                        throw new InvalidOperationException("Default cosmetics must be free.");
                    }

                    defaults[(int)item.Category]++;
                }
            }

            foreach (int count in defaults)
            {
                if (count != 1)
                {
                    throw new InvalidOperationException("Each category requires exactly one free default.");
                }
            }
        }
    }
}
