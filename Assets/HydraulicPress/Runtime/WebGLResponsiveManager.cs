using System.Runtime.InteropServices;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace HydraulicPress
{
    // Read-only browser telemetry supports smoke tests without exposing the committed hazards.
    public sealed class WebGLResponsiveManager : MonoBehaviour
    {
        public GameManager game;
        private int lastWidth, lastHeight;
        private int lastLayoutVersion = -1;
        [System.Serializable] private struct Status { public string state; public string navigation; public bool settings; public float timeScale; public int crushed; public long bank; public bool busy; public int width; public int height; public UIManager.ButtonBounds[] buttons; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HP_Report(string json);
#endif
        private void Start() { game.Updated += Report; Report(); }
        private void LateUpdate()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastLayoutVersion != game.hud.LayoutVersion) Report();
        }
        private void Report()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            lastLayoutVersion = game.hud.LayoutVersion;
#if UNITY_WEBGL && !UNITY_EDITOR
            var data = new Status { state = game.Round == null ? "Ready" : game.Round.State.ToString(), navigation=game.Navigation.ToString(),settings=game.SettingsOpen,timeScale=Time.timeScale,crushed = game.Round == null ? 0 : game.Round.Crushed, bank = game.Save == null ? 0 : game.Save.bank, busy = game.Busy, width = Screen.width, height = Screen.height,buttons=game.hud.GetButtonBounds() };
            HP_Report(JsonUtility.ToJson(data));
#endif
        }
        private void OnDestroy() { if (game != null) game.Updated -= Report; }
    }
}
