using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceHistoryView : MonoBehaviour
    {
        [System.Serializable]
        public struct HistoryPill
        {
            public GameObject root;
            public Image background;
            public TextMeshProUGUI valueText;
        }

        [SerializeField] private HistoryPill[] pills;

        [Header("Colors")]
        [SerializeField] private Color winColor = new Color(0.0f, 0.85f, 0.5f, 0.9f);
        [SerializeField] private Color lossColor = new Color(0.9f, 0.2f, 0.35f, 0.9f);
        [SerializeField] private Color critColor = new Color(1.0f, 0.84f, 0.0f, 1.0f);

        private struct HistoryRecord
        {
            public float Value;
            public bool IsWin;
            public bool IsCrit;
            public bool Active;
        }

        private HistoryRecord[] _records;

        private void Awake()
        {
            int count = (pills != null) ? pills.Length : 0;
            _records = new HistoryRecord[count];
            ClearHistory();
        }

        public void ClearHistory()
        {
            if (_records == null) return;

            for (int i = 0; i < _records.Length; i++)
            {
                _records[i].Active = false;
                if (pills[i].root != null)
                {
                    pills[i].root.SetActive(false);
                }
            }
        }

        public void AddRecord(float rollValue, bool isWin, bool isCrit)
        {
            if (_records == null || _records.Length == 0) return;

            for (int i = _records.Length - 1; i > 0; i--)
            {
                _records[i] = _records[i - 1];
            }

            _records[0].Value = rollValue;
            _records[0].IsWin = isWin;
            _records[0].IsCrit = isCrit;
            _records[0].Active = true;

            for (int i = 0; i < _records.Length; i++)
            {
                if (i >= pills.Length) break;

                if (_records[i].Active)
                {
                    if (pills[i].root != null) pills[i].root.SetActive(true);
                    if (pills[i].valueText != null)
                    {
                        pills[i].valueText.text = _records[i].Value.ToString("F1");
                    }
                    if (pills[i].background != null)
                    {
                        pills[i].background.color = _records[i].IsCrit ? critColor : (_records[i].IsWin ? winColor : lossColor);
                    }
                }
                else
                {
                    if (pills[i].root != null) pills[i].root.SetActive(false);
                }
            }
        }
    }
}

