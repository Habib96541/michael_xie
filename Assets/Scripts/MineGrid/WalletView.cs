using TMPro;
using UnityEngine;

namespace MineGrid
{
    public sealed class WalletView : MonoBehaviour
    {
        private TMP_Text _label;
        private CoinFeedback _feedback;
        private int _lastCoins = -1;
        public void Initialize(TMP_Text label, CoinFeedback feedback)
        {
            _label = label;
            _feedback = feedback;
        }

        public void Refresh(int coins)
        {
            _label.text = coins.ToString("N0");
            if (_lastCoins >= 0 && coins > _lastCoins)
            {
                _feedback.Reward();
            }

            if (_lastCoins >= 0 && coins < _lastCoins)
            {
                _feedback.Spend();
            }

            _lastCoins = coins;
        }
    }
}
