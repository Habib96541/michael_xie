using TMPro;
using UnityEngine;

namespace PegRush
{
    [CreateAssetMenu(menuName = "PegRush/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public TMP_FontAsset Regular;
        public TMP_FontAsset Bold;
        public Sprite Rounded;
        public Sprite Outline;
        public Color Background = new Color32(7, 20, 30, 255);
        public Color Surface = new Color32(14, 36, 49, 255);
        public Color Elevated = new Color32(23, 51, 65, 255);
        public Color Border = new Color32(41, 76, 88, 255);
        public Color Ink = new Color32(233, 247, 249, 255);
        public Color Muted = new Color32(137, 172, 184, 255);
        public Color Mint = new Color32(78, 234, 174, 255);
    }
}
