using System.IO;
using UnityEngine;

namespace CyberWhack.Art
{
    /// <summary>
    /// Loads the painted cartoon set (desert, moles, plaques, buttons).
    /// Falls back to the runtime painter when a file is missing, so a build
    /// still has a complete sunny arcade look.
    /// </summary>
    public static class CartoonArtLibrary
    {
        public static readonly Color32 Sand = new Color32(237, 151, 62, 255);
        public static readonly Color32 Ink = new Color32(58, 30, 12, 255);
        public static readonly Color32 Caramel = new Color32(122, 64, 24, 255);
        public static readonly Color32 Cream = new Color32(255, 248, 228, 255);
        public static readonly Color32 Gold = new Color32(184, 98, 8, 255);
        public static readonly Color32 Leaf = new Color32(28, 122, 52, 255);
        public static readonly Color32 Danger = new Color32(176, 36, 28, 255);
        public static readonly Color32 Outline = new Color32(92, 42, 12, 255);

        private static bool _ready;

        public static Sprite Background { get; private set; }
        public static Sprite Title { get; private set; }
        public static Sprite Hatch { get; private set; }
        public static Sprite DirtMound { get; private set; }
        public static Sprite StandardMole { get; private set; }
        public static Sprite GoldMole { get; private set; }
        public static Sprite HardHatMole { get; private set; }
        public static Sprite HardHatCracked { get; private set; }
        public static Sprite Bomb { get; private set; }
        public static Sprite Sunburst { get; private set; }
        public static Sprite ButtonGold { get; private set; }
        public static Sprite ButtonGreen { get; private set; }
        public static Sprite ButtonWood { get; private set; }
        public static Sprite ButtonExit { get; private set; }
        public static Sprite AudioOn { get; private set; }
        public static Sprite AudioOff { get; private set; }
        public static Sprite ButtonPause { get; private set; }
        public static Sprite SeedBar { get; private set; }
        public static Sprite TimeCard { get; private set; }
        public static Sprite ScoreCard { get; private set; }
        public static Sprite Plaque { get; private set; }
        public static Sprite BestScorePlaque { get; private set; }
        public static Sprite ParchmentRow { get; private set; }
        public static Sprite Parchment { get; private set; }
        public static Sprite ComboTrack { get; private set; }
        public static Sprite ComboFill { get; private set; }
        public static Sprite Badge { get; private set; }
        public static Sprite Clock { get; private set; }
        public static Sprite Coin { get; private set; }

        public static void EnsureLoaded()
        {
            if (_ready) return;
            _ready = true;

            Background = Load("CartoonBackground.png", () => null);
            Title = Load("WhackTitle.png", () => null);
            Hatch = Load("HatchRing.png", () => CartoonAtlasGenerator.CreateCartoonDirtMoundSprite(320, 180));
            DirtMound = Load("CartoonDirtMound.png", () => null);
            StandardMole = Load("StandardCyberMole.png", () => CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Normal));
            GoldMole = Load("OverclockedGoldMole.png", () => CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Golden));
            HardHatMole = Load("ArmoredTankMole.png", () => CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Helmet));
            HardHatCracked = Load("ArmoredTankMole_Cracked.png", () => CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.HelmetCracked));
            Bomb = Load("EmpDecoyMine.png", () => CartoonAtlasGenerator.CreateCartoonBombSprite(256));
            Sunburst = Load("CartoonCountdownBurst.png", () => CartoonAtlasGenerator.CreateCartoonStarburstSprite(256));
            ButtonGold = Load("CartoonBtn_Gold.png", () => CartoonAtlasGenerator.CreateCartoonStartButtonSprite(280, 90));
            ButtonGreen = Load("CartoonBtn_Green.png", () => CartoonAtlasGenerator.CreateCartoonStartButtonSprite(280, 90));
            ButtonWood = Load("CartoonBtn_Wood.png", () => CartoonAtlasGenerator.CreateCartoonStartButtonSprite(280, 90));
            ButtonPause = Load("CartoonBtn_Pause.png", () => ButtonWood);
            ButtonExit = Load("CartoonBtn_Exit.png", () => CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(90));
            AudioOn = Load("CartoonBtn_AudioOn.png", () => CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(96));
            AudioOff = Load("CartoonBtn_AudioOff.png", () => CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(96));
            Plaque = Load("CartoonStatPlaque.png", () => null);
            TimeCard = Load("CartoonCard_Time.png", () => Plaque);
            BestScorePlaque = Load("CartoonCard_BestScore.png", () => Load("PanelFrame.png", () => Plaque));
            ScoreCard = Load("CartoonCard_Score.png", () => ParchmentRow);
            SeedBar = Load("CartoonSeedBar.png", () => null);
            ParchmentRow = Load("CartoonParchmentRow.png", () => null);
            Parchment = Load("CartoonParchmentModal.png", () => null);
            ComboTrack = Load("CartoonComboTrack.png", () => null);
            ComboFill = Load("CartoonComboFill.png", () => CartoonAtlasGenerator.CreateCartoonStartButtonSprite(256, 48));
            Badge = Load("CartoonBadge_Multiplier.png", () => CartoonAtlasGenerator.CreateCartoonStarburstSprite(192));
            Clock = Load("CartoonIcon_Clock.png", () => null);
            Coin = Load("CartoonIcon_StarCoin.png", () => null);
        }

        private static Sprite Load(string filename, System.Func<Sprite> fallback)
        {
            string path = Path.Combine(Application.dataPath, "CyberWhack/Art", filename);
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(bytes))
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.name = filename;
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
                Object.Destroy(texture);
            }

            return fallback != null ? fallback() : null;
        }
    }
}
