using System.Runtime.InteropServices;
using UnityEngine;

namespace PegRush
{
    // Read-only browser diagnostics, used to verify the actual shipped player.
    public sealed class BrowserStatus : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void PegRushReport(string json);
        private GameSession _session;
        private float _next;
        [System.Serializable]
        private sealed class Status
        {
            public string State;
            public int Active;
            public int Rows;
            public int Profile;
            public int Drops;
            public long Coins;
            public string Notice;
            public float FrameMilliseconds;
        }
        private void Awake()
        {
            _session = GetComponent<GameSession>();
        }
        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            PegRushReport(JsonUtility.ToJson(new Status
            {
                State = _session.State.ToString(),
                Active = _session.Active,
                Rows = _session.Rows,
                Profile = _session.Profile,
                Drops = _session.Drops,
                Coins = _session.Balance,
                Notice = _session.Notice,
                FrameMilliseconds = Time.unscaledDeltaTime * 1000f
            }));
#endif
        }
    }
}
