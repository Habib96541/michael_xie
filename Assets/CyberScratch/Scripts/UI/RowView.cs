using System.Collections;
using CyberScratch.Audio;
using CyberScratch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class RowView : MonoBehaviour
    {
        [SerializeField] private int _rowIndex;
        [SerializeField] private ScratchCellView[] _cells = new ScratchCellView[3];
        [SerializeField] private Image _ledBorder;
        [SerializeField] private Sprite _idleBorderSprite;
        [SerializeField] private Sprite _winBorderSprite;

        [Header("Prize Tag")]
        [SerializeField] private Image _prizeTagBg;
        [SerializeField] private Text _prizeCaption;
        [SerializeField] private Text _prizeText;
        [SerializeField] private Sprite _idlePrizeSprite;
        [SerializeField] private Sprite _winPrizeSprite;
        [SerializeField] private Color _idlePrizeBgColor = Color.white;
        [SerializeField] private Color _winPrizeBgColor = Color.white;
        [SerializeField] private Color _idlePrizeTextColor = new Color(0.95f, 0.97f, 1f, 1f);
        [SerializeField] private Color _winPrizeTextColor = Color.white;

        [Header("Win Flourish")]
        [SerializeField] private Image _winGlow;
        [SerializeField] private Image _laurelLeft;
        [SerializeField] private Image _laurelRight;
        [SerializeField] private Image _sparkleLeft;
        [SerializeField] private Image _sparkleRight;

        [Header("Row Canvas Group")]
        [SerializeField] private CanvasGroup _rowCanvasGroup;

        private bool _isWinningRow;
        private bool _isCompleted;
        private bool _nearMissActive;
        private Coroutine _winBorderPulseRoutine;

        public int RowIndex => _rowIndex;
        public ScratchCellView[] Cells => _cells;
        public bool IsCompleted => _isCompleted;
        public bool IsWinningRow => _isWinningRow;

        private void Awake()
        {
            if (_rowCanvasGroup == null) _rowCanvasGroup = GetComponent<CanvasGroup>();
            if (_rowCanvasGroup == null) _rowCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetupRow(int rowIndex, Sprite idleBorder, Sprite winBorder)
        {
            _rowIndex = rowIndex;
            if (idleBorder != null) _idleBorderSprite = idleBorder;
            if (winBorder != null) _winBorderSprite = winBorder;
            ResetRow();
        }

        public void SetPrizeText(string text)
        {
            if (_prizeText != null)
            {
                _prizeText.text = text;
            }
        }

        public void SetPrizeAmount(float amount)
        {
            if (_prizeCaption != null)
            {
                _prizeCaption.text = "PRIZE";
            }

            if (_prizeText != null)
            {
                _prizeText.text = _prizeCaption != null ? FormatPrize(amount) : "PRIZE: " + FormatPrize(amount);
            }
        }

        public static string FormatPrize(float amount)
        {
            if (amount >= 1000f)
            {
                float thousands = amount / 1000f;
                return Mathf.Approximately(thousands, Mathf.Round(thousands))
                    ? $"${thousands:0}K"
                    : $"${thousands:0.#}K";
            }

            if (Mathf.Abs(amount - Mathf.Round(amount)) < 0.01f)
            {
                return $"${Mathf.RoundToInt(amount)}";
            }

            return $"${amount:0.##}";
        }

        public void UpdateCellSymbols(TicketModel ticket, CyberScratchConfig config)
        {
            if (ticket == null || config == null) return;

            for (int c = 0; c < 3; c++)
            {
                SymbolType type = ticket.GetSymbol(_rowIndex, c);
                SymbolDefinition def = GetDefinition(type, config);
                if (_cells[c] != null)
                {
                    _cells[c].SetSymbol(def);
                }
            }

            SetPrizeAmount(ticket.GetRowPrize(_rowIndex));
        }

        private SymbolDefinition GetDefinition(SymbolType type, CyberScratchConfig config)
        {
            SymbolDefinition[] defs = config.Symbols;
            if (defs == null) return null;
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].Type == type) return defs[i];
            }
            return null;
        }

        public void EvaluateRowProgress(TicketModel ticket)
        {
            if (ticket == null || _isCompleted) return;

            bool cell0 = ticket.IsCellRevealed(_rowIndex, 0);
            bool cell1 = ticket.IsCellRevealed(_rowIndex, 1);
            bool cell2 = ticket.IsCellRevealed(_rowIndex, 2);

            // Near-miss check: cells 0 & 1 revealed and match, cell 2 still covered
            if (cell0 && cell1 && !cell2)
            {
                bool match01 = ticket.GetSymbol(_rowIndex, 0) == ticket.GetSymbol(_rowIndex, 1);
                if (match01 && !_nearMissActive)
                {
                    _nearMissActive = true;
                    if (_cells[2] != null)
                    {
                        _cells[2].SetNearMissHalo(true);
                    }
                    CyberAudioService.Instance?.PlayAnticipationPulse();
                }
            }
            else if (cell2 && _nearMissActive)
            {
                // Cell 2 has been scratched
                _nearMissActive = false;
                if (_cells[2] != null)
                {
                    _cells[2].SetNearMissHalo(false);
                }
                CyberAudioService.Instance?.StopAnticipationPulse();
            }

            // Check if all 3 cells are revealed
            if (cell0 && cell1 && cell2)
            {
                _isCompleted = true;
                bool won = ticket.IsRowWin(_rowIndex);
                _isWinningRow = won;

                if (won)
                {
                    TriggerWinState();
                }
                else
                {
                    TriggerLossState();
                }
            }
        }

        private void TriggerWinState()
        {
            if (_winBorderPulseRoutine != null) StopCoroutine(_winBorderPulseRoutine);
            _winBorderPulseRoutine = StartCoroutine(WinBorderPulseRoutine());

            for (int i = 0; i < 3; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].SetWinning(true);
                    _cells[i].PlayWinBounce();
                }
            }

            if (_prizeTagBg != null)
            {
                if (_winPrizeSprite != null) _prizeTagBg.sprite = _winPrizeSprite;
                _prizeTagBg.color = _winPrizeBgColor;
            }

            if (_prizeCaption != null) _prizeCaption.color = _winPrizeTextColor;
            if (_prizeText != null)
            {
                _prizeText.color = _winPrizeTextColor;
                _prizeText.fontStyle = FontStyle.Bold;
            }

            SetFlourishActive(true);
            CyberAudioService.Instance?.PlayRowWinStinger();
        }

        private IEnumerator WinBorderPulseRoutine()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = 0.55f + 0.45f * Mathf.Sin(elapsed * 7.5f);
                if (_winGlow != null)
                {
                    Color glow = _winGlow.color;
                    glow.a = 0.45f + pulse * 0.55f;
                    _winGlow.color = glow;
                    float scale = 1.0f + (pulse - 0.5f) * 0.04f;
                    _winGlow.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }

                float spin = elapsed * 40f;
                if (_sparkleLeft != null) _sparkleLeft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin);
                if (_sparkleRight != null) _sparkleRight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -spin);
                yield return null;
            }
        }

        private void TriggerLossState()
        {
            // Completed rows stay fully lit. The gold frame is what marks a win.
            if (_rowCanvasGroup != null)
            {
                _rowCanvasGroup.alpha = 1f;
            }
        }

        private void SetFlourishActive(bool active)
        {
            if (_winGlow != null) _winGlow.gameObject.SetActive(active);
            if (_laurelLeft != null) _laurelLeft.gameObject.SetActive(active);
            if (_laurelRight != null) _laurelRight.gameObject.SetActive(active);
            if (_sparkleLeft != null) _sparkleLeft.gameObject.SetActive(active);
            if (_sparkleRight != null) _sparkleRight.gameObject.SetActive(active);
        }

        public void ResetRow()
        {
            if (_winBorderPulseRoutine != null)
            {
                StopCoroutine(_winBorderPulseRoutine);
                _winBorderPulseRoutine = null;
            }

            _isWinningRow = false;
            _isCompleted = false;
            _nearMissActive = false;

            if (_rowCanvasGroup != null)
            {
                _rowCanvasGroup.alpha = 1.0f;
            }

            if (_ledBorder != null)
            {
                if (_idleBorderSprite != null) _ledBorder.sprite = _idleBorderSprite;
                _ledBorder.color = Color.white;
            }

            if (_prizeTagBg != null)
            {
                if (_idlePrizeSprite != null) _prizeTagBg.sprite = _idlePrizeSprite;
                _prizeTagBg.color = _idlePrizeBgColor;
            }

            if (_prizeCaption != null)
            {
                _prizeCaption.color = _idlePrizeTextColor;
                _prizeCaption.text = "PRIZE";
            }

            if (_prizeText != null)
            {
                _prizeText.color = _idlePrizeTextColor;
                _prizeText.fontStyle = FontStyle.Bold;
            }

            if (_winGlow != null)
            {
                _winGlow.rectTransform.localScale = Vector3.one;
                Color glow = _winGlow.color;
                glow.a = 0.85f;
                _winGlow.color = glow;
            }

            SetFlourishActive(false);

            for (int i = 0; i < 3; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].ResetView();
                }
            }
        }
    }
}
