using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace com.javierquevedo
{
    public class SplashScreenController : MonoBehaviour
    {
        /// <summary>Scene loaded when START MATCH is pressed. Empty keeps the in-scene bootstrap.</summary>
        public string GameSceneName = "";

        private GameObject _camera;
        private SplashScreenGUI _gui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCyberOrbsBootstrap()
        {
            // The boot screen owns the first scene; it creates this controller at hand-off.
            if (LoadingScreenController.BootActive) return;
            if (Object.FindAnyObjectByType<SplashScreenController>() != null) return;
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            if (Object.FindAnyObjectByType<BubbleMatrixControllerHelperTest>() != null) return;

            GameObject bootstrap = new GameObject("SplashScreenController");
            bootstrap.AddComponent<SplashScreenController>();
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

            Camera cam = _camera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.035f, 0.045f, 0.075f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // Prefer the Main UI object authored in the scene; the camera is only a fallback.
            this._gui = GetComponent<SplashScreenGUI>();
            if (this._gui == null)
                this._gui = Object.FindAnyObjectByType<SplashScreenGUI>();
            if (this._gui == null)
                this._gui = _camera.GetComponent<SplashScreenGUI>();
            if (this._gui == null)
                this._gui = _camera.AddComponent<SplashScreenGUI>();

            this._gui.StartGameDelegate = this.startGame;
        }

        void startGame()
        {
            if (this._gui != null)
                this._gui.StartGameDelegate = null;

            // A dedicated gameplay scene is the normal path. The fallback keeps the old
            // single-scene procedural bootstrap working if no scene is configured.
            if (!string.IsNullOrEmpty(GameSceneName) && Application.CanStreamedLevelBeLoaded(GameSceneName))
            {
                SceneManager.LoadScene(GameSceneName);
                return;
            }

            if (this._gui != null)
                Destroy(this._gui);

            GameObject game = new GameObject("Game");
            game.AddComponent<GameController>();
            Destroy(this.gameObject);
        }
    }
}
