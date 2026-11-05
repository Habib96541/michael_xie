using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crash
{
    public enum Phase { Menu, Countdown, Flying, CashedOut, Crashed, GameOver }

    /// Boots the whole game in code: builds the UI, runs the round state machine,
    /// handles input, scoring and all animation.
    public class GameApp : MonoBehaviour
    {
        public static GameApp I;

        // ---------- model ----------
        System.Random _rng;
        public Phase phase = Phase.Menu;
        float _phaseT, _flightT, _mult = 1f, _crashAt = 2f;
        long _total, _best, _roundPts, _lastPts;
        float _displayTotal;
        int _shields = Rules.MaxShields, _streak, _round, _rounds;
        float _bestMultRun = 1f;
        bool _newBest;
        bool _paused;

        // ---------- view ----------
        Canvas _canvas;
        RectTransform _shake, _field, _hud, _graphRoot, _fxLayer, _menuRt, _howRt, _overRt;
        CanvasGroup _menuG, _howG, _overG;
        CurveGraph _graph;
        RocketViz _rocket;
        Image _danger, _flash, _topScrim, _botScrim, _multGlow;
        Text _totalText, _bestText, _multText, _ptsText, _statusText, _streakText, _roundText, _hintText;
        Text _countText, _menuBest, _overScore, _overBest, _overStats, _overTitle;
        Image _countRing;
        UIButton _cash, _play, _howBtn, _howClose, _again, _toMenu, _soundBtn, _infoBtn;
        Text _cashLabel, _cashSub, _soundIcon;
        readonly Image[] _shieldIcons = new Image[3];
        readonly Text[] _chipText = new Text[6];
        readonly Image[] _chipBg = new Image[6];
        readonly Text[] _axis = new Text[5];

        float _trailT, _cashPulseCooldown;

        // =====================================================================
        // bootstrap
        // =====================================================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("[CrashGame]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = -1;
            _rng = new System.Random(Environment.TickCount);
            _best = PlayerPrefs.GetInt("crash_best", 0);

            EnsureCamera();

            var s = new GameObject("[Sfx]");
            s.transform.SetParent(transform, false);
            s.AddComponent<Sfx>();

            var br = new GameObject("[Bridge]");
            br.transform.SetParent(transform, false);
            br.AddComponent<WebBridge>();

            var dbg = new GameObject("CGDebug");
            dbg.AddComponent<DebugApi>();

            _canvas = UIKit.MakeCanvas("Canvas", new Vector2(1920f, 1080f));
            _canvas.transform.SetParent(transform, false);

            _shake = UIKit.Rect(_canvas.transform, "Shake");
            _shake.Stretch();

            Backdrop.Create(_shake);

            _field = UIKit.Rect(_shake, "Field");
            _field.Stretch();

            _hud = UIKit.Rect(_shake, "HUD");
            _hud.Stretch();

            BuildField();
            BuildHud();
            BuildMenu();
            BuildHowTo();
            BuildGameOver();

            _fxLayer = UIKit.Rect(_canvas.transform, "FxLayer");
            _fxLayer.Stretch();
            _fxLayer.gameObject.AddComponent<Fx>().Init(_fxLayer, _shake);

            _flash = UIKit.Img(_canvas.transform, "Flash", null, new Color(1f, 1f, 1f, 0f));
            _flash.rectTransform.Stretch(-200, -200, -200, -200);
            _flash.transform.SetAsLastSibling();

            var tap = UIKit.Rect(_field, "TapCatcher");
            tap.Stretch();
            var tapImg = tap.gameObject.AddComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            tapImg.raycastTarget = true;
            tap.gameObject.AddComponent<TapCatcher>();

            EnterMenu();
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.BgBot;
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10f;
        }

        // =====================================================================
        // build: playfield
        // =====================================================================

        void BuildField()
        {
            _graphRoot = UIKit.Rect(_field, "GraphRoot");
            _graphRoot.anchorMin = new Vector2(0f, 0f);
            _graphRoot.anchorMax = new Vector2(1f, 1f);
            _graphRoot.offsetMin = new Vector2(96f, 250f);
            _graphRoot.offsetMax = new Vector2(-96f, -300f);

            var graphGo = UIKit.Rect(_graphRoot, "Curve");
            graphGo.Stretch();
            _graph = graphGo.gameObject.AddComponent<CurveGraph>();
            _graph.raycastTarget = false;
            _graph.color = Color.white;

            for (int i = 0; i < _axis.Length; i++)
            {
                var t = UIKit.Label(_graphRoot, "Axis" + i, "", 22, new Color(1f, 1f, 1f, 0.22f), TextAnchor.MiddleLeft);
                t.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-30f, 0f), new Vector2(120f, 30f));
                _axis[i] = t;
            }

            _rocket = RocketViz.Create(_graph.rectTransform);
        }

        // =====================================================================
        // build: HUD
        // =====================================================================

        void BuildHud()
        {
            _topScrim = UIKit.Img(_hud, "TopScrim", Tex.Fade(128), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            _topScrim.rectTransform.anchorMin = new Vector2(0f, 1f);
            _topScrim.rectTransform.anchorMax = new Vector2(1f, 1f);
            _topScrim.rectTransform.pivot = new Vector2(0.5f, 1f);
            _topScrim.rectTransform.anchoredPosition = Vector2.zero;
            _topScrim.rectTransform.sizeDelta = new Vector2(0f, 300f);

            _botScrim = UIKit.Img(_hud, "BotScrim", Tex.Fade(128), new Color(0.01f, 0.01f, 0.03f, 0.78f));
            _botScrim.rectTransform.anchorMin = new Vector2(0f, 0f);
            _botScrim.rectTransform.anchorMax = new Vector2(1f, 0f);
            _botScrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            _botScrim.rectTransform.anchoredPosition = Vector2.zero;
            _botScrim.rectTransform.sizeDelta = new Vector2(0f, 300f);
            _botScrim.rectTransform.localScale = new Vector3(1f, -1f, 1f);

            // logo
            var logo = UIKit.Label(_hud, "Logo", "CRASH", 46, Pal.Ink, TextAnchor.MiddleLeft, true);
            logo.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -38f), new Vector2(400f, 58f));

            var logoDot = UIKit.Img(_hud, "LogoDot", Tex.Circle(32), Pal.Cyan);
            logoDot.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(248f, -62f), new Vector2(12f, 12f));

            // shields
            for (int i = 0; i < 3; i++)
            {
                var sh = UIKit.Img(_hud, "Shield" + i, Tex.Shield(128), Color.white);
                sh.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f + i * 62f, -112f), new Vector2(52f, 58f));
                _shieldIcons[i] = sh;
            }

            _streakText = UIKit.Label(_hud, "Streak", "", 22, Pal.InkDim, TextAnchor.MiddleLeft);
            _streakText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -188f), new Vector2(360f, 30f));

            // total score
            var totalCap = UIKit.Label(_hud, "TotalCap", "TOTAL", 22, Pal.InkDim.WithAlpha(0.85f), TextAnchor.MiddleCenter, true);
            totalCap.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(600f, 30f));

            _totalText = UIKit.Label(_hud, "Total", "0", 92, Pal.Ink, TextAnchor.MiddleCenter, true);
            _totalText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(900f, 110f));

            _bestText = UIKit.Label(_hud, "Best", "BEST 0", 24, Pal.Gold.WithAlpha(0.75f), TextAnchor.MiddleCenter, true);
            _bestText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(600f, 30f));

            // history chips
            var chips = UIKit.Rect(_hud, "History");
            chips.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(6f * 96f, 52f));
            for (int i = 0; i < 6; i++)
            {
                var bg = UIKit.Panel(chips, "Chip" + i, Pal.Card.WithAlpha(0.65f), 16);
                bg.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 96f, 0f), new Vector2(88f, 46f));
                var txt = UIKit.Label(bg.transform, "Lbl", "—", 24, Pal.InkDim.WithAlpha(0.45f), TextAnchor.MiddleCenter, true);
                txt.rectTransform.Stretch();
                _chipBg[i] = bg;
                _chipText[i] = txt;
            }

            // top-right buttons
            _infoBtn = UIKit.Button(_hud, "Info", "?", new Vector2(64f, 64f), Pal.Card.WithAlpha(0.8f), Pal.InkDim, 32);
            _infoBtn.body.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -40f), new Vector2(64f, 64f));
            _infoBtn.onClick = () => { if (phase == Phase.Menu) return; ShowHowTo(true); };

            _soundBtn = UIKit.Button(_hud, "Sound", "SFX", new Vector2(64f, 64f), Pal.Card.WithAlpha(0.8f), Pal.InkDim, 22, 30);
            _soundBtn.body.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -40f), new Vector2(64f, 64f));
            _soundBtn.onClick = ToggleSound;
            _soundIcon = _soundBtn.label;

            // readout: top-right card so the middle of the table stays clear
            var readout = UIKit.Panel(_hud, "Readout", Pal.Card.WithAlpha(0.82f), 22);
            readout.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -124f), new Vector2(296f, 148f));

            _multGlow = UIKit.Img(readout.transform, "MultGlow", Tex.Glow(128), Pal.Cyan.WithAlpha(0.0f));
            _multGlow.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(420f, 420f));

            _multText = UIKit.Label(readout.transform, "Mult", "1.00x", 74, Pal.Ink, TextAnchor.MiddleCenter, true);
            _multText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(288f, 86f));

            var ptsCap = UIKit.Label(readout.transform, "PtsCap", "ROUND POINTS", 15, Pal.InkDim.WithAlpha(0.75f), TextAnchor.MiddleCenter, true);
            ptsCap.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(272f, 20f));

            _ptsText = UIKit.Label(readout.transform, "Pts", "+0", 34, Pal.Cyan, TextAnchor.MiddleCenter, true);
            _ptsText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(272f, 40f));

            _statusText = UIKit.Label(_hud, "Status", "", 32, Pal.Gold, TextAnchor.MiddleCenter, true);
            _statusText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 252f), new Vector2(900f, 42f));

            // cash button
            _cash = UIKit.Button(_hud, "Cash", "CASH OUT", new Vector2(580f, 136f), Pal.Cyan, Pal.Hex("#04211E"), 46);
            _cash.body.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(580f, 136f));
            _cash.onClick = () => { if (phase == Phase.Flying) CashOut(); else if (phase == Phase.Countdown) TryLaunch(); };
            _cashLabel = _cash.label;
            _cashLabel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(540f, 60f));
            _cashSub = UIKit.Label(_cash.transform, "Sub", "", 26, new Color(0.02f, 0.13f, 0.11f, 0.85f), TextAnchor.MiddleCenter, true);
            _cashSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(540f, 32f));

            _hintText = UIKit.Label(_hud, "Hint", "TAP ANYWHERE OR PRESS SPACE TO CASH OUT", 20, Pal.InkDim.WithAlpha(0.55f), TextAnchor.MiddleCenter, true);
            _hintText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(900f, 30f));

            _roundText = UIKit.Label(_hud, "Round", "ROUND 1", 22, Pal.InkDim.WithAlpha(0.65f), TextAnchor.MiddleLeft, true);
            _roundText.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 44f), new Vector2(300f, 30f));

            // countdown
            _countRing = UIKit.Img(_hud, "CountRing", Tex.Ring(256, 14), Pal.Cyan.WithAlpha(0.9f));
            _countRing.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f), new Vector2(190f, 190f));
            _countRing.type = Image.Type.Filled;
            _countRing.fillMethod = Image.FillMethod.Radial360;
            _countRing.fillOrigin = (int)Image.Origin360.Top;
            _countRing.fillClockwise = true;
            _countRing.gameObject.SetActive(false);

            _countText = UIKit.Label(_hud, "Count", "3", 96, Pal.Ink, TextAnchor.MiddleCenter, true);
            _countText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f), new Vector2(200f, 120f));
            _countText.gameObject.SetActive(false);

            // danger vignette
            _danger = UIKit.Img(_hud, "Danger", Tex.Vignette(256), Pal.Red.WithAlpha(0f));
            _danger.rectTransform.Stretch(-100, -100, -100, -100);
            _danger.transform.SetAsFirstSibling();
        }

        // =====================================================================
        // build: overlays
        // =====================================================================

        CanvasGroup MakeOverlay(string name, float dim, out RectTransform rt, out RectTransform content)
        {
            rt = UIKit.Rect(_canvas.transform, name);
            rt.Stretch();
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.blocksRaycasts = false;
            g.interactable = false;

            var bg = UIKit.Img(rt, "Dim", null, new Color(0.01f, 0.01f, 0.04f, dim), true);
            bg.rectTransform.Stretch();

            content = UIKit.Rect(rt, "Content");
            content.Stretch();
            return g;
        }

        void BuildMenu()
        {
            _menuG = MakeOverlay("Menu", 0.62f, out _menuRt, out var content);

            var glow = UIKit.Img(content, "LogoGlow", Tex.Glow(128), Pal.Violet.WithAlpha(0.30f));
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 620f));

            var title = UIKit.Label(content, "Title", "CRASH", 168, Pal.Ink, TextAnchor.MiddleCenter, true);
            title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 246f), new Vector2(1400f, 210f));

            var tag = UIKit.Label(content, "Tag", "CASH OUT BEFORE THE CRASH", 26, Pal.Cyan.WithAlpha(0.85f), TextAnchor.MiddleCenter, true);
            tag.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 128f), new Vector2(1200f, 40f));

            _play = UIKit.Button(content, "Play", "LAUNCH", new Vector2(460f, 132f), Pal.Cyan, Pal.Hex("#04211E"), 52);
            _play.body.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(460f, 132f));
            _play.onClick = StartRun;

            _howBtn = UIKit.Button(content, "HowTo", "HOW TO PLAY", new Vector2(460f, 92f), Pal.CardHi.WithAlpha(0.9f), Pal.Ink, 34);
            _howBtn.body.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -166f), new Vector2(460f, 92f));
            _howBtn.onClick = () => ShowHowTo(true);

            _menuBest = UIKit.Label(content, "Best", "BEST 0", 28, Pal.Gold, TextAnchor.MiddleCenter, true);
            _menuBest.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -276f), new Vector2(800f, 40f));

            var footer = UIKit.Label(content, "Footer", "TAP / CLICK / SPACE = CASH OUT", 20, Pal.InkDim.WithAlpha(0.5f), TextAnchor.MiddleCenter, true);
            footer.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(900f, 30f));

            var snd = UIKit.Button(content, "Sound", "SOUND: ON", new Vector2(210f, 60f), Pal.Card.WithAlpha(0.7f), Pal.InkDim, 22);
            snd.body.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 40f), new Vector2(210f, 60f));
            snd.onClick = ToggleSound;
            _menuSound = snd;
        }

        UIButton _menuSound;

        void BuildHowTo()
        {
            _howG = MakeOverlay("HowTo", 0.72f, out _howRt, out var content);
            _howRt.SetAsLastSibling();

            var card = UIKit.Panel(content, "Card", Pal.Card.WithAlpha(0.98f), 40);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 640f));

            var edge = UIKit.Panel(card.transform, "Edge", Pal.Cyan.WithAlpha(0.5f), 40);
            edge.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(240f, 6f));

            var title = UIKit.Label(card.transform, "Title", "HOW TO PLAY", 54, Pal.Ink, TextAnchor.MiddleCenter, true);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(800f, 70f));

            string body =
                "The rocket climbs and your round points grow faster the higher it goes.\n\n" +
                "<color=#39EBD2><b>TAP / CLICK / SPACE</b></color> to cash out and bank the round points.\n\n" +
                "If the rocket crashes first you lose the round points and <color=#FF4D6D><b>one shield</b></color>.\n\n" +
                "You start with <b>3 shields</b>. Cash out <b>4 rounds in a row</b> to earn one back.\n\n" +
                "Run out of shields and the next crash <color=#FF4D6D><b>wipes your total</b></color>.";

            var text = UIKit.Label(card.transform, "Body", body, 27, Pal.InkDim, TextAnchor.UpperLeft);
            text.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(820f, 380f));

            _howClose = UIKit.Button(card.transform, "Close", "GOT IT", new Vector2(320f, 86f), Pal.Cyan, Pal.Hex("#04211E"), 36);
            _howClose.body.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 86f));
            _howClose.onClick = () => ShowHowTo(false);
        }

        void BuildGameOver()
        {
            _overG = MakeOverlay("GameOver", 0.80f, out _overRt, out var content);
            _overRt.SetAsLastSibling();

            var glow = UIKit.Img(content, "Glow", Tex.Glow(128), Pal.Red.WithAlpha(0.16f));
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 900f));

            _overTitle = UIKit.Label(content, "Title", "RUN OVER", 96, Pal.Ink, TextAnchor.MiddleCenter, true);
            _overTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1200f, 120f));

            var cap = UIKit.Label(content, "Cap", "FINAL SCORE", 22, Pal.InkDim, TextAnchor.MiddleCenter, true);
            cap.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 148f), new Vector2(600f, 30f));

            _overScore = UIKit.Label(content, "Score", "0", 130, Pal.Ink, TextAnchor.MiddleCenter, true);
            _overScore.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(1200f, 150f));

            _overBest = UIKit.Label(content, "Best", "BEST 0", 30, Pal.Gold, TextAnchor.MiddleCenter, true);
            _overBest.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(900f, 40f));

            _overStats = UIKit.Label(content, "Stats", "", 24, Pal.InkDim, TextAnchor.MiddleCenter, true);
            _overStats.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1000f, 34f));

            _again = UIKit.Button(content, "Again", "PLAY AGAIN", new Vector2(460f, 124f), Pal.Cyan, Pal.Hex("#04211E"), 44);
            _again.body.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(460f, 124f));
            _again.onClick = StartRun;

            _toMenu = UIKit.Button(content, "Menu", "MENU", new Vector2(460f, 86f), Pal.CardHi.WithAlpha(0.9f), Pal.Ink, 30);
            _toMenu.body.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(460f, 86f));
            _toMenu.onClick = EnterMenu;
        }

        // =====================================================================
        // flow
        // =====================================================================

        void EnterMenu()
        {
            phase = Phase.Menu;
            _phaseT = 0f;
            _demoT = 0f;
            _hud.gameObject.SetActive(false);
            _field.gameObject.SetActive(true);
            _graph.ResetCurve(0f, 1f);
            _graph.viewT = Rules.TableSeconds;
            _rocket.ResetShip();
            _rocket.SetVisible(true);
            _rocket.HideBodyOff();
            _menuBest.text = "BEST " + Pal.N(_best);
            if (_menuSound != null) _menuSound.label.text = Sfx.I != null && Sfx.I.Muted ? "SOUND: OFF" : "SOUND: ON";
            Overlay(_menuG, true);
            Overlay(_howG, false);
            Overlay(_overG, false);
            Sfx.I.EngineRun(1f, false);
            WebBridge.Event("menu");
        }

        public void StartRun()
        {
            _pendingGameOver = false;
            _total = 0;
            _displayTotal = 0f;
            _shields = Rules.MaxShields;
            _streak = 0;
            _round = 0;
            _rounds = 0;
            _bestMultRun = 1f;
            _newBest = false;
            for (int i = 0; i < 6; i++)
            {
                _chipText[i].text = "—";
                _chipBg[i].color = Pal.Card.WithAlpha(0.65f);
                _chipText[i].color = Pal.InkDim.WithAlpha(0.6f);
            }
            RefreshShields();
            _menuBest.text = "BEST " + Pal.N(_best);
            Overlay(_menuG, false);
            Overlay(_overG, false);
            Overlay(_howG, false);
            _hud.gameObject.SetActive(true);
            _field.gameObject.SetActive(true);
            UpdateHudText();
            BeginRound();
            WebBridge.Event("run_start");
        }

        float _demoT;

        void BeginRound()
        {
            phase = Phase.Countdown;
            _phaseT = 0f;
            _flightT = 0f;
            _mult = 1f;
            _roundPts = 0;
            _round++;
            _crashAt = Rules.ClampToTable(Rules.SampleCrashMultiplier(_rng));
            _countAt = -1;

            _graph.ResetCurve(0f, 1f);
            _graph.viewT = Rules.TableSeconds;
            _rocket.ResetShip();
            _rocket.HideBodyOff();
            _rocket.SetVisible(true);
            _rocket.SetTension(1f);
            if (_rocket.rt != null) _rocket.rt.localRotation = Quaternion.identity;

            _countRing.gameObject.SetActive(true);
            _countText.gameObject.SetActive(true);
            _countRing.fillAmount = 1f;
            _statusText.text = "";
            _multText.text = "1.00x";
            _multText.color = Pal.Ink;
            _multText.rectTransform.localScale = Vector3.one;
            _ptsText.text = "+0";
            _ptsText.color = Pal.Cyan;
            _roundText.text = "ROUND " + _round;
            SetCash("GET READY", "ROUND " + _round, false, Pal.CardHi);
            _hintText.text = "TAP TO LAUNCH EARLY";
            RefreshShields();
            UpdateHudText();
            WebBridge.Event("round_start");
        }

        int _countAt = -1;

        void TryLaunch()
        {
            if (phase != Phase.Countdown || _phaseT < 0.30f) return;
            Launch();
        }

        void Launch()
        {
            phase = Phase.Flying;
            _phaseT = 0f;
            _flightT = 0f;
            _countRing.gameObject.SetActive(false);
            _countText.gameObject.SetActive(false);
            SetCash("CASH OUT", "+0 PTS", true, Pal.Cyan);
            _hintText.text = "TAP ANYWHERE OR PRESS SPACE TO CASH OUT";
            Sfx.I.Go();
            WebBridge.Event("launch");
        }

        void CashOut()
        {
            if (phase != Phase.Flying) return;
            phase = Phase.CashedOut;
            _phaseT = 0f;

            _lastPts = Rules.RoundPoints(_mult);
            _total += _lastPts;
            _streak++;
            _rounds++;
            if (_mult > _bestMultRun) _bestMultRun = _mult;

            bool shieldUp = false;
            if (_streak % Rules.StreakForShield == 0 && _shields < Rules.MaxShields)
            {
                _shields++;
                shieldUp = true;
            }

            _rocket.CashOutLook();
            _graph.crashed = false;
            var col = Color.Lerp(Pal.Cyan, Pal.Gold, 0.35f);
            Fx.I.Burst(_rocket.rt.position, Pal.Gold, 22, 520f, 11f);
            Fx.I.Float(SafeFloat(_rocket.rt.position) + new Vector3(0f, 90f, 0f), "+" + Pal.N(_lastPts), Pal.Gold, 62, 190f, 1.2f);
            Fx.I.Flash(Pal.Gold, 0.10f, 0.35f);
            Fx.I.Shake(7f);
            Sfx.I.Cash();
            if (shieldUp)
            {
                TweenRunner.Delay(0.35f, () =>
                {
                    Sfx.I.ShieldUp();
                    Fx.I.Float(_shieldIcons[_shields - 1].rectTransform.position, "+1 SHIELD", Pal.Cyan, 34, 130f, 1.1f);
                });
            }
            Sfx.I.EngineRun(_mult, false);

            _statusText.text = "CASHED OUT";
            _statusText.color = Pal.Gold;
            PopText(_statusText);
            _multText.color = Pal.Gold;
            _multGlow.color = Pal.Gold.WithAlpha(0.16f);

            SetCash("CASHED OUT", "+" + Pal.N(_lastPts), false, Pal.Gold);
            _hintText.text = "NEXT ROUND...";
            RefreshShields();
            UpdateHudText();
            WebBridge.Event("cashout");

            TweenRunner.Delay(1.45f, () => { if (phase == Phase.CashedOut) BeginRound(); });
        }

        void Crash()
        {
            phase = Phase.Crashed;
            _phaseT = 0f;
            _rounds++;
            _total = Math.Max(0, _total);
            bool runOver = _shields <= 0;
            if (!runOver) _shields--;
            _streak = 0;
            if (_mult > _bestMultRun) _bestMultRun = _mult;

            Vector3 at = _rocket.rt.position;
            Vector3 txt = SafeFloat(at);
            Fx.I.Burst(at, Pal.Amber, 26, 700f, 13f);
            Fx.I.Burst(at, Pal.Red, 18, 480f, 17f);
            Fx.I.Burst(at, Pal.Ink, 10, 320f, 9f);
            Fx.I.Flash(Pal.Red, 0.30f, 0.55f);
            Fx.I.Shake(30f, 0.7f);
            long lostPoints = Rules.RoundPoints(_mult);
            if (lostPoints > 0) Fx.I.Float(txt + new Vector3(0f, 18f, 0f), "-" + Pal.N(lostPoints), Pal.Red, 38, 52f, 1.0f);

            _rocket.HideBody();
            _graph.crashed = true;
            _graph.SetVerticesDirty();
            Sfx.I.Crash();
            Sfx.I.EngineRun(1f, false);

            _statusText.text = "BUSTED";
            _statusText.color = Pal.Red;
            PopText(_statusText);
            _multText.color = Pal.Red;
            _multGlow.color = Pal.Red.WithAlpha(0.20f);
            _ptsText.color = Pal.Red.WithAlpha(0.75f);
            _hintText.text = runOver ? "NO SHIELDS LEFT" : "SHIELD LOST — " + _shields + " LEFT";

            SetCash("BUSTED", "AT " + Pal.Mult(_mult), false, Pal.Red);
            SetStatusLine("BUSTED");
            PushHistory(_mult);
            RefreshShields();
            UpdateHudText();

            _phaseT = 0f;
            if (runOver)
            {
                _pendingGameOver = true;
                TweenRunner.Delay(1.5f, ShowGameOver);
            }
            else
            {
                TweenRunner.Delay(1.7f, () => { if (phase == Phase.Crashed) BeginRound(); });
            }
            WebBridge.Event(runOver ? "bust_final" : "bust");
        }

        void SetStatusLine(string s)
        {
            _statusText.text = s;
        }

        bool _pendingGameOver;

        void ShowGameOver()
        {
            if (!_pendingGameOver) return;
            _pendingGameOver = false;
            phase = Phase.GameOver;
            _phaseT = 0f;

            long final = _total;
            if (final > _best)
            {
                _best = final;
                _newBest = true;
                PlayerPrefs.SetInt("crash_best", (int)Math.Min(int.MaxValue, _best));
                PlayerPrefs.Save();
            }
            _total = 0;
            _displayTotal = 0f;

            _overTitle.text = _newBest ? "NEW RECORD" : "RUN OVER";
            _overTitle.color = _newBest ? Pal.Gold : Pal.Ink;
            _overScore.text = Pal.N(final);
            _overBest.text = _newBest ? "★ BEST " + Pal.N(_best) + " ★" : "BEST " + Pal.N(_best);
            _overStats.text = "ROUNDS CASHED OUT " + _rounds + "   •   TOP MULTIPLIER " + Pal.Mult(_bestMultRun);

            _hud.gameObject.SetActive(false);
            _field.gameObject.SetActive(true);
            _rocket.SetVisible(false);
            Overlay(_overG, true);
            _menuBest.text = "BEST " + Pal.N(_best);
            Sfx.I.GameOver();
            Fx.I.Shake(18f, 0.6f);
            Fx.I.Flash(Pal.Red, 0.22f, 0.6f);
            UpdateHudText();
            WebBridge.Event("game_over");
        }

        void PushHistory(float m)
        {
            for (int i = 5; i > 0; i--)
            {
                _chipText[i].text = _chipText[i - 1].text;
                _chipBg[i].color = _chipBg[i - 1].color;
                _chipText[i].color = _chipText[i - 1].color;
            }
            Color c = m < 2f ? Pal.Red : m < 4f ? Pal.Violet : m < 8f ? Pal.Cyan : Pal.Gold;
            _chipText[0].text = m >= 100f ? Mathf.RoundToInt(m) + "x" : m.ToString("0.00") + "x";
            _chipText[0].color = c;
            _chipBg[0].color = c.WithAlpha(0.16f);
            _chipText[0].rectTransform.localScale = Vector3.one * 1.2f;
            TweenRunner.To(0.25f, k => { if (_chipText[0] != null) _chipText[0].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, k); }, Ease.OutCubic);
        }

        void RefreshShields()
        {
            for (int i = 0; i < 3; i++)
            {
                bool has = i < _shields;
                var c = has ? Color.white : new Color(0.55f, 0.58f, 0.72f, 0.18f);
                _shieldIcons[i].color = c;
                _shieldIcons[i].rectTransform.localScale = has ? Vector3.one : Vector3.one * 0.86f;
            }
            int prog = _streak % Rules.StreakForShield;
            _streakText.text = "STREAK " + _streak + "  ·  " + prog + "/" + Rules.StreakForShield + " TO SHIELD";
            _streakText.color = prog == 0 && _streak > 0 ? Pal.Cyan : Pal.InkDim;
        }

        void SetCash(string label, string sub, bool enabled, Color color)
        {
            _cashLabel.text = label;
            _cashSub.text = sub;
            _cash.interactable = enabled;
            _cash.baseColor = color;
            _cash.label.color = enabled ? Pal.Hex("#04211E") : new Color(0.02f, 0.06f, 0.08f, 0.55f);
            _cashSub.color = enabled ? new Color(0.02f, 0.13f, 0.11f, 0.8f) : new Color(0.02f, 0.06f, 0.08f, 0.5f);
        }

        void UpdateHudText()
        {
            _totalText.text = Pal.N((long)_displayTotal);
            _bestText.text = "BEST " + Pal.N(_best);
            RefreshShields();
        }

        void PopText(Text t)
        {
            if (t == null) return;
            t.rectTransform.localScale = Vector3.one * 1.35f;
            TweenRunner.To(0.35f, k => { if (t != null) t.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, k); }, Ease.OutBack);
        }

        // =====================================================================
        // overlays
        // =====================================================================

        void Overlay(CanvasGroup g, bool show)
        {
            if (g == null) return;
            g.gameObject.SetActive(true);
            float from = g.alpha;
            float to = show ? 1f : 0f;
            g.blocksRaycasts = show;
            g.interactable = show;
            TweenRunner.To(0.22f, k => { if (g != null) g.alpha = Mathf.Lerp(from, to, k); }, Ease.OutCubic, () =>
            {
                if (g != null && to <= 0f) { g.alpha = 0f; g.gameObject.SetActive(false); }
            });
            if (show)
            {
                var rt = (RectTransform)g.transform;
                rt.localScale = Vector3.one * 0.985f;
                TweenRunner.To(0.28f, k => { if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, k); }, Ease.OutCubic);
            }
        }

        void ShowHowTo(bool show)
        {
            Overlay(_howG, show);
            if (show) _howRt.SetAsLastSibling();
        }

        void ToggleSound()
        {
            if (Sfx.I == null) return;
            bool muted = Sfx.I.ToggleMute();
            if (_menuSound != null) _menuSound.label.text = muted ? "SOUND: OFF" : "SOUND: ON";
            if (_soundIcon != null) _soundIcon.text = muted ? "OFF" : "SFX";
        }

        // =====================================================================
        // input
        // =====================================================================

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.06f) dt = 0.06f;
            _phaseT += dt;

            HandleKeys();

            switch (phase)
            {
                case Phase.Menu: UpdateDemo(dt); break;
                case Phase.Countdown: UpdateCountdown(dt); break;
                case Phase.Flying: UpdateFlying(dt); break;
                case Phase.CashedOut:
                case Phase.Crashed:
                    _graph.StepView(dt);
                    UpdateRocket(dt, false);
                    break;
            }

            _displayTotal = Mathf.Lerp(_displayTotal, _total, Mathf.Min(1f, dt * 7f));
            if (Mathf.Abs(_displayTotal - _total) < 1f) _displayTotal = _total;
            if (_hud != null && _hud.gameObject.activeSelf) UpdateHudText();

            if (WebBridge.I != null)
                WebBridge.I.Tick(phase.ToString().ToLowerInvariant(), _mult, _total, _roundPts, _shields,
                    _streak, _crashAt, _best, _rounds, Sfx.I != null && Sfx.I.Muted,
                    Sfx.I != null && Sfx.I.EngineOn, Sfx.I != null ? Sfx.I.EngineLevel : 0f);

            if (_danger != null)
            {
                float pulse = 0f;
                if (phase == Phase.Flying && _mult > 3f)
                {
                    float k = Mathf.Clamp01((_mult - 3f) / 9f);
                    pulse = k * (0.10f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f));
                }
                var c = _danger.color;
                c.a = Mathf.Lerp(c.a, pulse, dt * 5f);
                _danger.color = c;
            }

            UpdateAxisLabels();
        }

        void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                switch (phase)
                {
                    case Phase.Flying: CashOut(); break;
                    case Phase.Countdown: TryLaunch(); break;
                    case Phase.Menu: StartRun(); break;
                    case Phase.GameOver: StartRun(); break;
                }
            }
            if (Input.GetKeyDown(KeyCode.M)) ToggleSound();
            if (Input.GetKeyDown(KeyCode.Escape) && (_howG != null && _howG.alpha > 0.5f)) ShowHowTo(false);
        }

        public void DebugForceCrash(float m)
        {
            if (phase != Phase.Flying) return;
            _crashAt = Rules.ClampToTable(Mathf.Max(_mult + 0.02f, m));
        }

        public void OnTap()
        {
            switch (phase)
            {
                case Phase.Flying: CashOut(); break;
                case Phase.Countdown: TryLaunch(); break;
            }
        }

        // =====================================================================
        // per-phase updates
        // =====================================================================

        void UpdateDemo(float dt)
        {
            _demoT += dt * 0.85f;
            float m = Rules.Multiplier(_demoT);
            if (m > 5.0f || _demoT < 0f)
            {
                _demoT = 0f;
                _graph.ResetCurve(0f, 1f);
                _rocket.ResetShip();
            }
            _graph.Push(_demoT, m);
            _graph.StepView(dt);
            _rocket.SetTension(m);
            UpdateRocket(dt, true);
        }

        void UpdateCountdown(float dt)
        {
            const float dur = 2.4f;
            float k = Mathf.Clamp01(_phaseT / dur);
            _countRing.fillAmount = 1f - k;
            int n = Mathf.Clamp(3 - Mathf.FloorToInt(_phaseT / 0.8f), 1, 3);
            if (n != _countAt)
            {
                _countAt = n;
                _countText.text = n.ToString();
                _countText.rectTransform.localScale = Vector3.one * 1.3f;
                TweenRunner.To(0.3f, t => { if (_countText != null) _countText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, t); }, Ease.OutBack);
                Sfx.I.Tick();
            }
            _graph.StepView(dt);
            UpdateRocket(dt, false);
            if (_phaseT >= dur) Launch();
        }

        void UpdateFlying(float dt)
        {
            _flightT += dt;
            _mult = Rules.Multiplier(_flightT);
            // the rocket always bursts when it hits the end of the table
            if (_mult >= _crashAt || _flightT >= Rules.TableSeconds)
            {
                _mult = Mathf.Min(_crashAt, Rules.Multiplier(Mathf.Min(_flightT, Rules.TableSeconds)));
                _roundPts = Rules.RoundPoints(_mult);
                _multText.text = Pal.Mult(_mult);
                _ptsText.text = "+" + Pal.N(_roundPts);
                Crash();
                return;
            }

            _roundPts = Rules.RoundPoints(_mult);
            _graph.Push(_flightT, _mult);
            _graph.StepView(dt);
            UpdateRocket(dt, true);

            _multText.text = Pal.Mult(_mult);
            _ptsText.text = "+" + Pal.N(_roundPts);
            _ptsText.color = Color.Lerp(Pal.Cyan, Pal.Gold, Mathf.Clamp01((_mult - 2f) / 6f));
            _cashSub.text = "+" + Pal.N(_roundPts) + " PTS";

            float heat = Mathf.Clamp01((_mult - 1.5f) / 6f);
            _multText.color = Color.Lerp(Pal.Ink, Pal.Gold, heat);
            _multGlow.color = Color.Lerp(Pal.Cyan, Pal.Gold, heat).WithAlpha(0.05f + heat * 0.10f);
            float sc = 1f + Mathf.Clamp01((_mult - 1f) / 20f) * 0.16f;
            _multText.rectTransform.localScale = Vector3.one * sc;

            Sfx.I.EngineRun(_mult, true);

            _cashPulseCooldown -= dt;
            if (_cashPulseCooldown <= 0f && _mult > 1.5f)
            {
                _cashPulseCooldown = Mathf.Lerp(1.1f, 0.4f, Mathf.Clamp01((_mult - 1.5f) / 6f));
                _cash.Pulse(0.035f);
            }

            _trailT -= dt;
            if (_trailT <= 0f && _rocket.rt != null)
            {
                _trailT = 0.045f;
                Fx.I.Trail(_rocket.rt.position + new Vector3(0f, -3f, 0f),
                    Color.Lerp(Pal.Cyan, Pal.Amber, Mathf.Clamp01((_mult - 2f) / 8f)), 6f);
            }
        }

        /// Keeps floating text on screen when the ship is against the table edge.
        Vector3 SafeFloat(Vector3 at)
        {
            // overlay canvas world positions are screen pixels
            float mx = Mathf.Min(240f, Screen.width * 0.22f);
            float my = Mathf.Min(170f, Screen.height * 0.22f);
            return new Vector3(Mathf.Clamp(at.x, mx, Screen.width - mx), Mathf.Clamp(at.y, my, Screen.height - my), at.z);
        }

        void UpdateRocket(float dt, bool showFlame)
        {
            if (_rocket == null || _graph == null) return;
            if (_rocket.rt == null) return;

            float t = Mathf.Max(0f, _graph.HeadT);
            Vector2 p = _graph.Local(t, _graph.HeadM);
            _rocket.rt.anchoredPosition = p;

            float t0 = t - 0.18f;
            if (t0 > 0f)
            {
                Vector2 q = _graph.Local(t0, Rules.Multiplier(t0));
                Vector2 d = p - q;
                if (d.sqrMagnitude > 0.001f)
                {
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
                    ang = Mathf.Clamp(ang, -78f, 10f);
                    var target = Quaternion.Euler(0f, 0f, ang);
                    _rocket.rt.localRotation = Quaternion.Slerp(_rocket.rt.localRotation, target, Mathf.Min(1f, dt * 7f));
                }
            }
            else
            {
                _rocket.rt.localRotation = Quaternion.Slerp(_rocket.rt.localRotation, Quaternion.identity, Mathf.Min(1f, dt * 7f));
            }

            var sh = _rocket.rt.localScale;
            // a small ship so the run across the table reads as a long flight
            float targetScale = 0.34f - Mathf.Clamp01((_graph.HeadM - 1f) / 60f) * 0.04f;
            _rocket.rt.localScale = Vector3.Lerp(sh, Vector3.one * targetScale, Mathf.Min(1f, dt * 3f));
        }

        void UpdateAxisLabels()
        {
            if (_graph == null || _axis[0] == null) return;
            var rect = _graph.rectTransform.rect;
            for (int i = 0; i < _axis.Length; i++)
            {
                float n = (i + 1) / (float)(_axis.Length + 1);
                float m = _graph.MultAtNorm(n);
                _axis[i].text = m >= 10f ? Mathf.RoundToInt(m) + "x" : m.ToString("0.0") + "x";
                _axis[i].rectTransform.anchoredPosition = new Vector2(rect.xMin + 34f, rect.yMin + n * rect.height);
            }
        }
    }

    /// Invisible full-screen catcher: tapping the play area cashes out.
    public class TapCatcher : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameApp.I != null) GameApp.I.OnTap();
        }
    }

    /// Commands exposed to the page for automated play tests.
    public class DebugApi : MonoBehaviour
    {
        void Start() { gameObject.name = "CGDebug"; }

        public void Cash() { if (GameApp.I != null) GameApp.I.OnTap(); }
        public void Play() { if (GameApp.I != null) GameApp.I.StartRun(); }
        public void ForceCrash(string m)
        {
            float v;
            if (float.TryParse(m, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out v) && GameApp.I != null)
                GameApp.I.DebugForceCrash(v);
        }
    }

    public static class RocketVizExtensions
    {
        public static void HideBodyOff(this RocketViz r)
        {
            if (r == null) return;
            if (r.body != null) r.body.enabled = true;
            if (r.flame != null) r.flame.enabled = true;
            if (r.flameCore != null) r.flameCore.enabled = true;
            if (r.halo != null) r.halo.enabled = true;
        }
    }
}
