using UnityEngine;

namespace CyberScratch.Data
{
    [CreateAssetMenu(fileName = "CyberScratchConfig", menuName = "CyberScratch/Config")]
    public class CyberScratchConfig : ScriptableObject
    {
        [Header("Economy")]
        [SerializeField] private float _startingBalance = 1250f;
        [SerializeField] private float[] _wagerTiers = new float[] { 1f, 2f, 5f, 10f };
        [SerializeField] private int _defaultWagerIndex = 1; // $2

        [Header("Gameplay Timings & Thresholds")]
        [SerializeField] private float _cellRevealThreshold = 0.667f; // 6 of 9 points
        [SerializeField] private float _cellFadeDuration = 0.15f;
        [SerializeField] private float _revealAllDuration = 0.8f;

        [Header("Probabilities")]
        [SerializeField] private float _rowWinChance = 0.35f;
        [SerializeField] private float _nearMissRate = 0.45f;

        [Header("Symbols")]
        [SerializeField] private SymbolDefinition[] _symbols;

        public float StartingBalance => _startingBalance;
        public float[] WagerTiers => _wagerTiers;
        public int DefaultWagerIndex => _defaultWagerIndex;
        public float CellRevealThreshold => _cellRevealThreshold;
        public float CellFadeDuration => _cellFadeDuration;
        public float RevealAllDuration => _revealAllDuration;
        public float RowWinChance => _rowWinChance;
        public float NearMissRate => _nearMissRate;
        public SymbolDefinition[] Symbols => _symbols;

        public void InitializeDefaultsIfNeeded()
        {
            if (_symbols != null && _symbols.Length == 6)
            {
                return;
            }

            _symbols = new SymbolDefinition[]
            {
                new SymbolDefinition(SymbolType.BronzeGear, "Bronze Gear", 1f, 35.0f, new Color(0.85f, 0.48f, 0.22f)),
                new SymbolDefinition(SymbolType.SilverIngot, "Silver Ingot", 2f, 22.0f, new Color(0.78f, 0.85f, 0.95f)),
                new SymbolDefinition(SymbolType.GoldCore, "Gold Core", 5f, 12.0f, new Color(1.0f, 0.82f, 0.15f)),
                new SymbolDefinition(SymbolType.PlasmaGem, "Plasma Gem", 15f, 4.5f, new Color(0.72f, 0.25f, 1.0f)),
                new SymbolDefinition(SymbolType.CyberCrown, "Cyber Crown", 50f, 1.0f, new Color(0.15f, 0.85f, 1.0f)),
                new SymbolDefinition(SymbolType.Lucky777, "Lucky 777", 250f, 0.1f, new Color(1.0f, 0.18f, 0.35f))
            };
        }
    }
}
