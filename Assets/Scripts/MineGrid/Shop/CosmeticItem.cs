using System;
using UnityEngine;

namespace MineGrid
{
    public enum CosmeticCategory
    {
        Gem,
        Board,
        Tile,
        Reveal
    }

    public enum RevealStyle
    {
        Classic,
        Starburst,
        GoldenTrail
    }

    public enum CosmeticResult
    {
        Success,
        InsufficientCoins,
        AlreadyOwned,
        InvalidItem,
        NotOwned,
        CategoryMismatch
    }

    [CreateAssetMenu(menuName = "MineGrid/Shop/Cosmetic Item")]
    public sealed class CosmeticItem : ScriptableObject
    {
        [SerializeField]
        private string _id;
        [SerializeField]
        private string _displayName;
        [SerializeField]
        private string _description;
        [SerializeField]
        private CosmeticCategory _category;
        [SerializeField]
        private int _price;
        [SerializeField]
        private bool _isDefault;
        [SerializeField]
        private Sprite _artwork;
        [SerializeField]
        private Material _material;
        [SerializeField]
        private Color _primary = Color.white;
        [SerializeField]
        private Color _secondary = Color.white;
        [SerializeField]
        private RevealStyle _revealStyle;
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public CosmeticCategory Category => _category;
        public int Price => _price;
        public bool IsDefault => _isDefault;
        public Sprite Artwork => _artwork;
        public Material Material => _material;
        public Color Primary => _primary;
        public Color Secondary => _secondary;
        public RevealStyle RevealStyle => _revealStyle;
    }
}
