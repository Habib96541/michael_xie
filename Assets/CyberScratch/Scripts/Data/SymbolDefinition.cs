using System;
using UnityEngine;

namespace CyberScratch.Data
{
    [Serializable]
    public class SymbolDefinition
    {
        [SerializeField] private SymbolType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private float _multiplier;
        [SerializeField] private float _weight;
        [SerializeField] private Color _themeColor = Color.white;
        [SerializeField] private Sprite _icon;

        public SymbolType Type => _type;
        public string DisplayName => _displayName;
        public float Multiplier => _multiplier;
        public float Weight => _weight;
        public Color ThemeColor => _themeColor;
        public Sprite Icon => _icon;

        public SymbolDefinition(SymbolType type, string displayName, float multiplier, float weight, Color color, Sprite icon = null)
        {
            _type = type;
            _displayName = displayName;
            _multiplier = multiplier;
            _weight = weight;
            _themeColor = color;
            _icon = icon;
        }

        public void SetIcon(Sprite sprite)
        {
            _icon = sprite;
        }
    }
}
