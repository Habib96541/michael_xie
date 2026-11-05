using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using com.javierquevedo.events;
using com.javierquevedo.gui;

namespace com.javierquevedo
{
    public class GameController : MonoBehaviour
    {
        protected Game _game;

        /// <summary>Scene loaded when the player returns to the main menu.</summary>
        public string MenuSceneName = "MainMenu";

        private bool _returningToLobby;

        private const string _bubbleShooterPrefabName = "Prefabs/BubbleShooterPrefab";
        private GameObject _bubbleShooterPrefab;
        private GameObject _camera;
        private Camera _camComponent;
        private BubbleMatrixController _bubbleMatrixController;
        private HUD _hud;
        private GameFinishedGUI _finishedGUI;
        private int _lastScreenW;
        private int _lastScreenH;
        private Rect _lastRawSafeArea;

        void Awake()
        {
            _game = new com.javierquevedo.Game();
            _game.isTimedMode = SplashScreenGUI.SelectedTimedMode;
        }

        void Start()
        {
            _camera = GameObject.Find("Camera");
            if (_camera == null && Camera.main != null)
                _camera = Camera.main.gameObject;
            if (_camera == null)
            {
                _camera = new GameObject("Camera");
                _camera.tag = "MainCamera";
                _camera.AddComponent<Camera>();
            }

            _camComponent = _camera.GetComponent<Camera>();
            ConfigureCyberCamera();

            _hud = _camera.GetComponent<HUD>();
            if (_hud == null)
                _hud = _camera.AddComponent<HUD>();

            _hud.game = this._game;
            _hud.OnRestartRequested = RestartMatchInPlace;
            _hud.OnSwapRequested = () =>
            {
                if (_bubbleMatrixController != null)
                    _bubbleMatrixController.SwapCurrentAndNextOrb();
            };
            _hud.OnReturnToLobbyRequested = ReturnToLobby;

            // Read the mode again here: with a same-scene menu the controller may have
            // been enabled before the player picked a mode in Awake.
            _game.isTimedMode = SplashScreenGUI.SelectedTimedMode;
            this.startGame();
        }

        private void ConfigureCyberCamera()
        {
            if (_camComponent == null) return;

            _camComponent.orthographic = true;
            _camComponent.backgroundColor = new Color(0.035f, 0.045f, 0.075f, 1f); // Deep obsidian (#0b0e14)
            _camComponent.clearFlags = CameraClearFlags.SolidColor;
            _camComponent.nearClipPlane = 0.1f;
            _camComponent.farClipPlane = 100f;

            // Everything the player must see: playfield frame plus the turret and next-orb chamber.
            const float boardLeft = -0.35f;
            const float boardRight = 10.85f;
            const float boardBottom = -1.85f;
            const float boardTop = 10.5f;
            float boardWidth = boardRight - boardLeft;
            float boardHeight = boardTop - boardBottom;

            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float aspect = sw / sh;

            // Compare the raw value in Update; an invalid/empty safe area must not cause a
            // rebuild on every frame, so cache what the platform actually reported.
            _lastRawSafeArea = Screen.safeArea;
            Rect safe = _lastRawSafeArea;
            if (safe.width <= 1f || safe.height <= 1f)
                safe = new Rect(0f, 0f, sw, sh);

            // Reserve a screen band for the IMGUI top bar and a little air under the turret.
            // Measured in pixels, so a short landscape window can never slide the board under the HUD.
            float hudBand = Mathf.Clamp(sh * 0.14f, 60f, 132f);
            float bottomBand = Mathf.Clamp(sh * 0.03f, 8f, 32f);

            float leftBand = safe.x;
            float rightBand = sw - (safe.x + safe.width);
            float topBand = sh - (safe.y + safe.height) + hudBand;
            float bottomInset = safe.y + bottomBand;

            // Fit the board into whatever the HUD band and safe area leave behind.
            float widthFraction = Mathf.Clamp01(1f - (leftBand + rightBand) / sw);
            float heightFraction = Mathf.Clamp01(1f - (topBand + bottomInset) / sh);
            float orthoForWidth = boardWidth * 0.5f / Mathf.Max(0.05f, aspect * widthFraction);
            float orthoForHeight = boardHeight * 0.5f / Mathf.Max(0.05f, heightFraction);
            _camComponent.orthographicSize = Mathf.Max(orthoForWidth, orthoForHeight);

            // Centre the board inside that free area; it is not the screen centre when an inset
            // or the HUD band is asymmetric, which is exactly the phone / tablet case.
            float worldPerPixelY = _camComponent.orthographicSize * 2f / sh;
            float worldPerPixelX = worldPerPixelY * aspect;
            float offsetX = (leftBand - rightBand) * 0.5f * worldPerPixelX;
            float offsetY = (bottomInset - topBand) * 0.5f * worldPerPixelY;

            Vector3 restPos = new Vector3(
                (boardLeft + boardRight) * 0.5f - offsetX,
                (boardBottom + boardTop) * 0.5f - offsetY,
                -10f);
            _camera.transform.position = restPos;
            _camera.transform.rotation = Quaternion.identity;

            CyberVFXManager.Instance.RegisterCamera(_camComponent, restPos);
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
        }

        void OnEnable()
        {
            GameEvents.OnBubblesRemoved += onBubblesRemoved;
            GameEvents.OnGameFinished += onGameFinished;
        }

        void OnDisable()
        {
            GameEvents.OnBubblesRemoved -= onBubblesRemoved;
            GameEvents.OnGameFinished -= onGameFinished;
        }

        void Update()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH || Screen.safeArea != _lastRawSafeArea)
            {
                ConfigureCyberCamera();
            }

            if (_game != null && _game.state == GameState.Playing && !_game.isPaused)
            {
                _game.TickTimer(Time.deltaTime);
                if (_game.isTimedMode && _game.timeRemaining <= 0f && _bubbleMatrixController != null && _bubbleMatrixController.IsPlaying)
                {
                    _bubbleMatrixController.TriggerTimeUpFinish();
                }
            }
        }

        private void startGame()
        {
            _bubbleMatrixController = Object.FindAnyObjectByType<BubbleMatrixController>();
            if (_bubbleMatrixController == null)
            {
                if (_bubbleShooterPrefab != null)
                {
                    _bubbleShooterPrefab.SetActive(false);
                    Destroy(_bubbleShooterPrefab);
                    _bubbleShooterPrefab = null;
                }

                UnityEngine.Object loaded = Resources.Load(_bubbleShooterPrefabName);
                if (loaded != null)
                {
                    _bubbleShooterPrefab = Instantiate(loaded) as GameObject;
                }
                else
                {
                    _bubbleShooterPrefab = new GameObject("BubbleMatrixRoot");
                    _bubbleShooterPrefab.AddComponent<BubbleMatrixController>();
                }

                _bubbleShooterPrefab.transform.position = Vector3.zero;
                _bubbleMatrixController = _bubbleShooterPrefab.GetComponent<BubbleMatrixController>();
                if (_bubbleMatrixController == null)
                    _bubbleMatrixController = _bubbleShooterPrefab.AddComponent<BubbleMatrixController>();
            }

            _bubbleMatrixController.gameModel = this._game;
            _bubbleMatrixController.startGame();
        }

        protected virtual void onBubblesRemoved(int bubbleCount, bool exploded)
        {
            this._game.destroyBubbles(bubbleCount, exploded);
        }

        protected virtual void onGameFinished(GameState state)
        {
            this._game.state = state;
            if (_finishedGUI != null)
                Destroy(_finishedGUI);

            _finishedGUI = _camera.AddComponent<GameFinishedGUI>();
            _finishedGUI.StartNewGameSelectedDelegate = this.RestartMatchInPlace;
            _finishedGUI.ReturnToLobbyDelegate = this.ReturnToLobby;
            _finishedGUI.game = this._game;
        }

        private void RestartMatchInPlace()
        {
            if (_finishedGUI != null)
            {
                Destroy(_finishedGUI);
                _finishedGUI = null;
            }
            this._game.Reset();
            if (_bubbleMatrixController != null)
            {
                _bubbleMatrixController.gameModel = this._game;
                _bubbleMatrixController.startGame();
            }
            else
            {
                this.startGame();
            }
        }

        private void ReturnToLobby()
        {
            string menuScene = string.IsNullOrEmpty(MenuSceneName) ? "MainMenu" : MenuSceneName;
            if (!_returningToLobby && Application.CanStreamedLevelBeLoaded(menuScene))
            {
                _returningToLobby = true;
                StartCoroutine(LoadMenuScene(menuScene));
                return;
            }

            if (_finishedGUI != null)
            {
                Destroy(_finishedGUI);
                _finishedGUI = null;
            }
            if (_hud != null)
            {
                Destroy(_hud);
                _hud = null;
            }
            if (_bubbleShooterPrefab != null)
            {
                Destroy(_bubbleShooterPrefab);
                _bubbleShooterPrefab = null;
            }

            GameObject splashGo = new GameObject("SplashScreenController");
            splashGo.AddComponent<SplashScreenController>();
            Destroy(this.gameObject);
        }

        private IEnumerator LoadMenuScene(string sceneName)
        {
            // Let the results screen finish its click handling before the scene is unloaded.
            yield return null;
            SceneManager.LoadScene(sceneName);
        }
    }
}
