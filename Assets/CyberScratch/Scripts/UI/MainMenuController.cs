using CyberScratch.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberScratch.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuView _mainMenuView;
        [SerializeField] private HowToPlayModalView _howToPlayModal;

        private const string BalancePrefKey = "CyberScratch_Balance";
        private const float DefaultStartingBalance = 1250f;
        private const string GameSceneName = "GameScene";

        private void Start()
        {
            if (_mainMenuView == null)
            {
                _mainMenuView = Object.FindAnyObjectByType<MainMenuView>();
            }
            if (_howToPlayModal == null)
            {
                _howToPlayModal = Object.FindAnyObjectByType<HowToPlayModalView>();
            }

            float balance = PlayerPrefs.GetFloat(BalancePrefKey, DefaultStartingBalance);

            if (_mainMenuView != null)
            {
                _mainMenuView.Setup(
                    balance,
                    onPlay: OnPlayClicked,
                    onHowToPlay: OnHowToPlayClicked,
                    onResetBalance: OnResetBalanceClicked
                );
                _mainMenuView.UpdateBalance(balance);
                _mainMenuView.UpdateSoundState();
                _mainMenuView.ShowInstant();
            }

            if (_howToPlayModal != null)
            {
                _howToPlayModal.HideInstant();
            }
        }

        private void OnPlayClicked()
        {
            CyberAudioService.Instance?.PlayButtonClick();
            SceneManager.LoadScene(GameSceneName);
        }

        private void OnHowToPlayClicked()
        {
            if (_howToPlayModal != null)
            {
                _howToPlayModal.Show();
            }
        }

        private void OnResetBalanceClicked()
        {
            PlayerPrefs.SetFloat(BalancePrefKey, DefaultStartingBalance);
            PlayerPrefs.Save();
            if (_mainMenuView != null)
            {
                _mainMenuView.UpdateBalance(DefaultStartingBalance);
            }
        }
    }
}
