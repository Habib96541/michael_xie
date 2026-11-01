using TMPro;
using UnityEngine;

namespace MineGrid
{
    [CreateAssetMenu(menuName = "MineGrid/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public TMP_FontAsset displayFont;
        public TMP_FontAsset bodyFont;
        public Sprite logo;
        public Sprite coin;
        public Sprite gem;
        public Sprite mine;
        public Sprite rounded;
        public Sprite hidden;
        public Sprite safeTile;
        public Sprite mineTile;
        public Sprite wheel;
        public Sprite backdrop;
        public Color ink = new Color(.055f, .065f, .16f);
        public Color panel = new Color(.11f, .14f, .30f);
        public Color cyan = new Color(.27f, .90f, 1);
        public Color gold = new Color(1, .80f, .28f);
    }
}
