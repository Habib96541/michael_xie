using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceBetControlsView : MonoBehaviour
    {
        [Header("Bet Display & Controls")]
        [SerializeField] private TextMeshProUGUI betAmountText;
        [SerializeField] private Button buttonHalf;
        [SerializeField] private Button buttonDouble;
        [SerializeField] private Button buttonMin;
        [SerializeField] private Button buttonMax;

        [Header("Roll Buttons")]
        [SerializeField] private Button buttonRoll;
        [SerializeField] private TextMeshProUGUI rollButtonText;
        [SerializeField] private Button buttonAutoRoll;
        [SerializeField] private Image autoRollIndicator;
        [SerializeField] private TextMeshProUGUI autoRollText;

        [Header("Colors")]
        [SerializeField] private Color autoRollInactiveColor = new Color(0.2f, 0.25f, 0.35f, 1.0f);
        [SerializeField] private Color autoRollActiveColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);

        public event Action OnHalfClicked;
        public event Action OnDoubleClicked;
        public event Action OnMinClicked;
        public event Action OnMaxClicked;
        public event Action OnRollClicked;
        public event Action OnAutoRollToggled;

        private bool _isAutoRolling;

        private void Awake()
        {
            if (buttonHalf != null) buttonHalf.onClick.AddListener(() => OnHalfClicked?.Invoke());
            if (buttonDouble != null) buttonDouble.onClick.AddListener(() => OnDoubleClicked?.Invoke());
            if (buttonMin != null) buttonMin.onClick.AddListener(() => OnMinClicked?.Invoke());
            if (buttonMax != null) buttonMax.onClick.AddListener(() => OnMaxClicked?.Invoke());

            if (buttonRoll != null) buttonRoll.onClick.AddListener(() => OnRollClicked?.Invoke());
            if (buttonAutoRoll != null) buttonAutoRoll.onClick.AddListener(() => OnAutoRollToggled?.Invoke());

            UpdateAutoRollUI(false);
        }

        private void OnDestroy()
        {
            if (buttonHalf != null) buttonHalf.onClick.RemoveAllListeners();
            if (buttonDouble != null) buttonDouble.onClick.RemoveAllListeners();
            if (buttonMin != null) buttonMin.onClick.RemoveAllListeners();
            if (buttonMax != null) buttonMax.onClick.RemoveAllListeners();
            if (buttonRoll != null) buttonRoll.onClick.RemoveAllListeners();
            if (buttonAutoRoll != null) buttonAutoRoll.onClick.RemoveAllListeners();
        }

        public void SetBetAmount(long amount)
        {
            if (betAmountText != null)
            {
                betAmountText.text = "$" + amount.ToString("N0");
            }
        }

        public void SetRollInteractable(bool interactable)
        {
            if (buttonRoll != null)
            {
                buttonRoll.interactable = interactable;
            }
        }

        public void SetAutoRollState(bool isAutoRolling)
        {
            _isAutoRolling = isAutoRolling;
            UpdateAutoRollUI(_isAutoRolling);
        }

        private void UpdateAutoRollUI(bool active)
        {
            if (autoRollIndicator != null)
            {
                autoRollIndicator.color = active ? autoRollActiveColor : autoRollInactiveColor;
            }
            if (autoRollText != null)
            {
                autoRollText.text = active ? "STOP AUTO" : "AUTO ROLL";
            }
        }
    }
}

