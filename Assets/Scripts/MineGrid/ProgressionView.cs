using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class ProgressionView : MonoBehaviour
    {
        private GameConfig _config;
        private SaveService _saves;
        private TMP_Text _summary;
        private TMP_Text[] _labels;
        private Button[] _buttons;
        private TMP_Text[] _buttonLabels;
        private Image[] _fills;
        private Button _claim;
        private UnityAction[] _callbacks;
        private Action<int> _claimRequested;
        public int NextMilestone { get; private set; }

        public void Initialize(GameConfig config, SaveService saves, TMP_Text summary, TMP_Text[] labels, Button[] buttons, Image[] fills, Button claim, Action<int> claimRequested)
        {
            _config = config;
            _saves = saves;
            _summary = summary;
            _labels = labels;
            _buttons = buttons;
            _fills = fills;
            _claim = claim;
            _claimRequested = claimRequested;
            _callbacks = new UnityAction[buttons.Length];
            _buttonLabels = new TMP_Text[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                _callbacks[i] = () => _claimRequested(index);
                buttons[i].onClick.AddListener(_callbacks[i]);
                _buttonLabels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
            }
        }

        public void Refresh()
        {
            NextMilestone = -1;
            for (int i = 0; i < _config.milestonePoints.Length; i++)
            {
                if (!_saves.Data.claimed.Contains(i))
                {
                    NextMilestone = i;
                    break;
                }
            }

            _summary.text = NextMilestone < 0 ? $"All milestones claimed!\n{_saves.Data.totalPoints:N0} lifetime points" : $"Trail: {_saves.Data.totalPoints:N0} / {_config.milestonePoints[NextMilestone]:N0}\nNext reward: {_config.milestoneCoins[NextMilestone]}";
            _claim.interactable = NextMilestone >= 0 && _saves.Data.totalPoints >= _config.milestonePoints[NextMilestone];
            for (int i = 0; i < _labels.Length; i++)
            {
                bool claimed = _saves.Data.claimed.Contains(i), ready = !claimed && _saves.Data.totalPoints >= _config.milestonePoints[i];
                _labels[i].text = $"{_config.milestonePoints[i]:N0} lifetime points\n+{_config.milestoneCoins[i]} coins • {(claimed ? "Claimed" : ready ? "Ready!" : "Locked")}";
                _fills[i].fillAmount = Mathf.Clamp01((float)_saves.Data.totalPoints / Mathf.Max(1, _config.milestonePoints[i]));
                _buttons[i].interactable = ready;
                _buttonLabels[i].text = claimed ? "Claimed" : ready ? "Claim" : "Locked";
            }
        }

        private void OnDestroy()
        {
            if (_callbacks == null)
            {
                return;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i])
                {
                    _buttons[i].onClick.RemoveListener(_callbacks[i]);
                }
            }
        }
    }
}
