using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Crash
{
    /// Mirrors game state into window.__crashState (web) so the game can be inspected
    /// and driven by automated tests. Does nothing in the editor.
    public class WebBridge : MonoBehaviour
    {
        public static WebBridge I;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CG_Push(string json);
        [DllImport("__Internal")] static extern void CG_Event(string name);
#endif

        float _acc;

        void Awake()
        {
            I = this;
        }

        public static void Event(string name)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { CG_Event(name); } catch { }
#endif
        }

        public static void Push(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { CG_Push(json); } catch { }
#endif
        }

        public void Tick(string phase, float mult, long total, long round, int shields,
            int streak, float crashAt, long best, int rounds, bool muted,
            bool engineOn, float engineLevel)
        {
            _acc += Time.unscaledDeltaTime;
            if (_acc < 0.06f) return;
            _acc = 0f;
            if (I == null) I = this;
            string json = "{" +
                "\"phase\":\"" + phase + "\"," +
                "\"mult\":" + F(mult) + "," +
                "\"total\":" + total.ToString(CultureInfo.InvariantCulture) + "," +
                "\"round\":" + round.ToString(CultureInfo.InvariantCulture) + "," +
                "\"shields\":" + shields.ToString(CultureInfo.InvariantCulture) + "," +
                "\"streak\":" + streak.ToString(CultureInfo.InvariantCulture) + "," +
                "\"crashAt\":" + F(crashAt) + "," +
                "\"best\":" + best.ToString(CultureInfo.InvariantCulture) + "," +
                "\"rounds\":" + rounds.ToString(CultureInfo.InvariantCulture) + "," +
                "\"muted\":" + (muted ? "true" : "false") + "," +
                "\"engineOn\":" + (engineOn ? "true" : "false") + "," +
                "\"engineLevel\":" + F(engineLevel) +
                "}";
            Push(json);
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
