using UnityEngine;

namespace CoinFlip
{
    [CreateAssetMenu(menuName = "Coin Flip/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Range(1.2f, 1.8f)] public float flipDuration = 1.55f;
        [Range(40, 110)] public float launchHeight = 80;
        [Range(16, 48)] public int particlePoolSize = 32;
    }
}
