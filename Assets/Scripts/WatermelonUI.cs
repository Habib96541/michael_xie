using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public sealed class WatermelonUI : MonoBehaviour
    {
        [SerializeField] private WatermelonGame game;
        [SerializeField] private Text multiplier;
        [SerializeField] private Text bandCount;
        [SerializeField] private Text status;
        [SerializeField] private Text instruction;
        [SerializeField] private Button addBandButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button menuButton;

        private void OnEnable()
        {
            addBandButton.onClick.AddListener(game.AddBand);
            restartButton.onClick.AddListener(game.RestartRound);
            playButton.onClick.AddListener(game.RestartRound);
            quitButton.onClick.AddListener(game.QuitGame);
            menuButton.onClick.AddListener(game.ReturnToMenu);
        }

        private void OnDisable()
        {
            addBandButton.onClick.RemoveListener(game.AddBand);
            restartButton.onClick.RemoveListener(game.RestartRound);
            playButton.onClick.RemoveListener(game.RestartRound);
            quitButton.onClick.RemoveListener(game.QuitGame);
            menuButton.onClick.RemoveListener(game.ReturnToMenu);
        }

        public void ShowMenu(bool show)
        {
            menuRoot.SetActive(show);
            gameplayRoot.SetActive(!show);
            if (show)
                playButton.Select();
            else
                addBandButton.Select();
        }

        public void Refresh(int count, float currentMultiplier, bool hasBurst)
        {
            multiplier.text = $"{currentMultiplier:0.00}x";
            bandCount.text = $"{count:00} {(count == 1 ? "BAND" : "BANDS")}";
            status.text = hasBurst ? "ONE BAND TOO MANY." : "HOW FAR CAN YOU GO?";
            instruction.text = hasBurst
                ? $"Burst at {count} bands. Try a fresh watermelon."
                : "Every band raises the multiplier.\nThe next one could be the last.";
            addBandButton.gameObject.SetActive(!hasBurst);
            restartButton.gameObject.SetActive(hasBurst);
        }
    }
}
