using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlapDash
{
    public sealed class RoundView : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private RoundFeedback feedback;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField, Tooltip("Header heading shown on menus and replaced by the score during play.")]
        private GameObject headerTitle;
        [SerializeField, Tooltip("Optional round-panel title. Leave empty when using a separate static heading.")]
        private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private Button primaryButton;
        [SerializeField] private GameObject overlayPanel;

        private RoundState displayedState;
        private int displayedScore = -1;
        private int displayedSeconds = -1;
        private bool hasDisplayedState;

        private void OnEnable()
        {
            primaryButton.onClick.AddListener(HandlePrimaryButton);
            hasDisplayedState = false;
            displayedScore = -1;
            displayedSeconds = -1;
        }

        private void OnDisable()
        {
            primaryButton.onClick.RemoveListener(HandlePrimaryButton);
        }

        private void LateUpdate()
        {
            RefreshView();
        }

        public void RefreshView()
        {
            if (displayedScore != round.Score)
            {
                if (round.Score > 0 && displayedScore >= 0) feedback.PointScored();
                displayedScore = round.Score;
                scoreLabel.text = $"PIPES  {displayedScore:00}";
            }

            int seconds = Mathf.CeilToInt(round.RemainingTime);
            if (displayedSeconds != seconds)
            {
                displayedSeconds = seconds;
                timeLabel.text = $"TIME  {seconds:00}s";
            }

            if (hasDisplayedState && displayedState == round.State)
                return;

            displayedState = round.State;
            hasDisplayedState = true;
            scoreLabel.gameObject.SetActive(round.IsPlaying);
            timeLabel.gameObject.SetActive(round.IsPlaying);
            if (headerTitle != null)
                headerTitle.SetActive(!round.IsPlaying);
            feedback.ShowState(round.State);
            overlayPanel.SetActive(!round.IsPlaying);
            if (round.IsPlaying)
                return;

            bool ready = round.State == RoundState.Ready;
            if (titleLabel != null)
                titleLabel.text = ready ? "FLAP DASH" : "ROUND COMPLETE";
            messageLabel.text = ready
                ? "Tap, click, or press Space to flap.\nPass pipes. One point each."
                : $"{round.EndReason}\nPipes passed: {round.Score}";
            buttonLabel.text = ready ? "START ROUND" : "PLAY AGAIN";
            primaryButton.Select();
        }

        private void HandlePrimaryButton()
        {
            if (round.State == RoundState.Ready)
                round.StartRound();
            else
                round.RestartRound();
        }
    }
}
