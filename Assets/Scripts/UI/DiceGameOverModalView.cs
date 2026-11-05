using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceGameOverModalView : MonoBehaviour
    {
        [Header("Modal Containers")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private RectTransform cardRect;

        [Header("Rank Display")]
        [SerializeField] private TextMeshProUGUI rankBadgeText;
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private Image rankBgImage;

        [Header("Stat Texts")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI netProfitText;
        [SerializeField] private TextMeshProUGUI totalRollsText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI bestStreakText;
        [SerializeField] private TextMeshProUGUI critsHitText;
        [SerializeField] private GameObject newRecordBadge;

        [Header("Buttons")]
        [SerializeField] private Button buttonPlayAgain;
        [SerializeField] private Button buttonEndlessMode;

        [Header("Rank Colors")]
        [SerializeField] private Color rankSSSColor = new Color(1.0f, 0.84f, 0.0f, 1.0f);
        [SerializeField] private Color rankSColor = new Color(0.0f, 0.95f, 0.65f, 1.0f);
        [SerializeField] private Color rankAColor = new Color(0.0f, 0.75f, 1.0f, 1.0f);
        [SerializeField] private Color rankBColor = new Color(0.7f, 0.4f, 1.0f, 1.0f);
        [SerializeField] private Color rankCColor = new Color(0.9f, 0.5f, 0.2f, 1.0f);
        [SerializeField] private Color rankDColor = new Color(0.95f, 0.25f, 0.35f, 1.0f);

        public event Action OnPlayAgainClicked;
        public event Action OnEndlessClicked;

        private void Awake()
        {
            if (buttonPlayAgain != null)
            {
                buttonPlayAgain.onClick.AddListener(() => OnPlayAgainClicked?.Invoke());
            }
            if (buttonEndlessMode != null)
            {
                buttonEndlessMode.onClick.AddListener(() => OnEndlessClicked?.Invoke());
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (buttonPlayAgain != null) buttonPlayAgain.onClick.RemoveAllListeners();
            if (buttonEndlessMode != null) buttonEndlessMode.onClick.RemoveAllListeners();
        }

        public void Show(long finalBankroll, long netProfit, int totalRolls, int wins, int bestStreak, int critsHit, bool isNewRecord)
        {
            if (modalRoot != null) modalRoot.SetActive(true);

            if (finalScoreText != null) finalScoreText.text = "$" + finalBankroll.ToString("N0");

            if (netProfitText != null)
            {
                if (netProfit >= 0)
                {
                    netProfitText.text = "+$" + netProfit.ToString("N0");
                    netProfitText.color = rankSColor;
                }
                else
                {
                    netProfitText.text = "-$" + Math.Abs(netProfit).ToString("N0");
                    netProfitText.color = rankDColor;
                }
            }

            if (totalRollsText != null) totalRollsText.text = totalRolls.ToString();

            float winPct = (totalRolls > 0) ? (wins * 100.0f / totalRolls) : 0.0f;
            if (winRateText != null) winRateText.text = winPct.ToString("F1") + "%";

            if (bestStreakText != null) bestStreakText.text = "x" + bestStreak.ToString();
            if (critsHitText != null) critsHitText.text = critsHit.ToString();
            if (newRecordBadge != null) newRecordBadge.SetActive(isNewRecord);

            string rank;
            string title;
            Color rankColor;

            if (netProfit >= 10000 || finalBankroll >= 15000)
            {
                rank = "SSS";
                title = "HIGH ROLLER LEGEND";
                rankColor = rankSSSColor;
            }
            else if (netProfit >= 5000)
            {
                rank = "S";
                title = "LUCKY MASTER";
                rankColor = rankSColor;
            }
            else if (netProfit >= 2500)
            {
                rank = "A";
                title = "DICE SHARK";
                rankColor = rankAColor;
            }
            else if (netProfit >= 1000)
            {
                rank = "B";
                title = "CONSISTENT WINNER";
                rankColor = rankBColor;
            }
            else if (netProfit > 0)
            {
                rank = "C";
                title = "PROFIT RUNNER";
                rankColor = rankCColor;
            }
            else
            {
                rank = "D";
                title = "BUSTED OUT - TRY AGAIN!";
                rankColor = rankDColor;
            }

            if (rankBadgeText != null)
            {
                rankBadgeText.text = rank;
                rankBadgeText.color = rankColor;
            }
            if (rankTitleText != null)
            {
                rankTitleText.text = title;
                rankTitleText.color = rankColor;
            }
            if (rankBgImage != null)
            {
                rankBgImage.color = new Color(rankColor.r, rankColor.g, rankColor.b, 0.18f);
            }

            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one * 0.8f;
            }
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void Update()
        {
            if (modalRoot != null && modalRoot.activeSelf && cardRect != null && cardRect.localScale.x < 1.0f)
            {
                cardRect.localScale = Vector3.Lerp(cardRect.localScale, Vector3.one, Time.deltaTime * 12.0f);
            }
        }
    }
}

