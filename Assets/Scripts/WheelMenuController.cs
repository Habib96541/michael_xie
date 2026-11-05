using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpinTheWheel
{
    public sealed class WheelMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject gameRoot;
        [SerializeField] private WheelController wheel;
        [SerializeField] private WheelResponsiveLayout gameLayout;
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button spinButton;
        [SerializeField] private EventSystem eventSystem;

        public bool IsMenuOpen => menuRoot.activeSelf;

        private void OnEnable()
        {
            playButton.onClick.AddListener(PlayGame);
            quitButton.onClick.AddListener(QuitGame);
            menuButton.onClick.AddListener(ShowMenu);
        }

        private void Awake()
        {
            ShowMenu();
        }

        private void LateUpdate()
        {
            menuButton.interactable = !wheel.IsSpinning;
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(PlayGame);
            quitButton.onClick.RemoveListener(QuitGame);
            menuButton.onClick.RemoveListener(ShowMenu);
        }

        public void PlayGame()
        {
            gameRoot.SetActive(true);
            menuRoot.SetActive(false);
            gameLayout.RefreshLayout();
            eventSystem.SetSelectedGameObject(spinButton.gameObject);
        }

        public void ShowMenu()
        {
            if (wheel.IsSpinning)
                return;

            menuRoot.SetActive(true);
            gameRoot.SetActive(false);
            eventSystem.SetSelectedGameObject(playButton.gameObject);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
