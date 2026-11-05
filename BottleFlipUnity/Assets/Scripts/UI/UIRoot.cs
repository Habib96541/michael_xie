using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BottleFlip
{
    /// <summary>Builds and animates every screen: title, HUD, game over sheet, toasts and flashes.</summary>
    public class UIRoot : MonoBehaviour
    {
        public Canvas Canvas;
        public Camera WorldCamera;

        public event Action PlayPressed;
        public event Action MenuPressed;
        public event Action SoundPressed;

        // Roots
        RectTransform _canvasRect;
        RectTransform _hudRoot, _titleRoot, _overRoot, _floaters, _toastRoot;
        CanvasGroup _hudGroup, _titleGroup, _sheetGroup, _hintGroup, _toastGroup;

        // HUD
        RectTransform _scoreRect;
        TextMeshProUGUI _scoreText, _bestChipText, _soundLabel, _hintText;

        // Title
        RectTransform _logoRoot, _playBtnRoot, _tagRect, _rulesRect, _titleBestRect;
        CanvasGroup _logoGroup, _playGroup, _tagGroup, _rulesGroup, _titleBestGroup;

        // Game over
        RectTransform _sheet, _overStatsRow, _overTitleRect;
        TextMeshProUGUI _overTitle, _overSub, _overFlipsValue, _overBestValue, _newBestText;
        RectTransform _newBestRect;
        CanvasGroup _newBestGroup;
        Image _dim, _flash;

        // Sprites
        Sprite _pill64, _pill48, _pill40, _pill24, _pill12, _roundBtn;
        TextMeshProUGUI _toastText;
        bool _built;

        public void Build()
        {
            if (_built) return;
            _built = true;

            var canvasGo = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.pixelPerfect = false;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            _canvasRect = (RectTransform)canvasGo.transform;

            _pill64 = Art.RoundedSprite(64, Pal.Panel, Pal.Panel, 0);
            _pill48 = Art.RoundedSprite(48, Pal.Panel, Pal.Panel, 0);
            _pill40 = Art.RoundedSprite(40, Pal.Panel, Pal.Panel, 0);
            _pill24 = Art.RoundedSprite(24, Pal.Panel, Pal.Panel, 0);
            _pill12 = Art.RoundedSprite(12, Pal.Panel, Pal.Panel, 0);
            _roundBtn = Art.RoundedSprite(46, Pal.Panel, Pal.Panel, 0);

            BuildBackdrop();
            BuildHud();
            BuildTitle();
            BuildGameOver();
            BuildFloatingLayers();
            BuildSoundButton();
            BuildFlash();
        }

        // ---------------------------------------------------------------- build

        void BuildBackdrop()
        {
            var vignette = Ui.Img("Vignette", Canvas.transform, Art.TextureSprite(Art.Vignette), Color.white);
            Ui.FullStretch((RectTransform)vignette.transform);
            vignette.preserveAspect = false;
            vignette.raycastTarget = false;
        }

        /// <summary>Small expanding ripple at the tap point. Gameplay input itself is polled by GameManager.</summary>
        public void TapFeedback(Vector2 screenPos)
        {
            Ripple(screenPos);
        }

        void BuildHud()
        {
            _hudRoot = Ui.Stretch("HUD", Canvas.transform);
            _hudGroup = Ui.Group(_hudRoot.gameObject);
            _hudRoot.gameObject.SetActive(false);

            // Score label
            var label = Ui.Text("ScoreLabel", _hudRoot, "LANDED", 30f, Pal.Muted, TextAlignmentOptions.Center);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -84f);
            lrt.sizeDelta = new Vector2(700f, 44f);
            label.characterSpacing = 16f;

            // Big counter
            _scoreText = Ui.Text("ScoreValue", _hudRoot, "0", 164f, Pal.Text, TextAlignmentOptions.Center);
            _scoreRect = (RectTransform)_scoreText.transform;
            _scoreRect.anchorMin = _scoreRect.anchorMax = new Vector2(0.5f, 1f);
            _scoreRect.pivot = new Vector2(0.5f, 1f);
            _scoreRect.anchoredPosition = new Vector2(0f, -112f);
            _scoreRect.sizeDelta = new Vector2(900f, 210f);

            var scoreShadow = Ui.Shadowed(_scoreText, Pal.Ink.Alpha(0.45f), new Vector2(0f, -10f));
            scoreShadow.text = "0";
            _scoreShadow = scoreShadow;

            // Best chip (top left)
            var chip = Ui.Img("BestChip", _hudRoot, _pill40, new Color(1f, 1f, 1f, 0.10f), Image.Type.Sliced);
            var crt = (RectTransform)chip.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = new Vector2(44f, -52f);
            crt.sizeDelta = new Vector2(300f, 88f);
            AddOutline(crt, 3f, new Color(1f, 1f, 1f, 0.10f));

            var bestLabel = Ui.Text("BestLabel", crt, "BEST", 26f, Pal.Muted, TextAlignmentOptions.Left);
            var blrt = (RectTransform)bestLabel.transform;
            blrt.anchorMin = blrt.anchorMax = new Vector2(0f, 0.5f);
            blrt.pivot = new Vector2(0f, 0.5f);
            blrt.anchoredPosition = new Vector2(30f, 0f);
            blrt.sizeDelta = new Vector2(140f, 40f);
            bestLabel.characterSpacing = 12f;

            _bestChipText = Ui.Text("BestValue", crt, "0", 40f, Pal.Amber, TextAlignmentOptions.Right);
            var bcrt = (RectTransform)_bestChipText.transform;
            bcrt.anchorMin = bcrt.anchorMax = new Vector2(1f, 0.5f);
            bcrt.pivot = new Vector2(1f, 0.5f);
            bcrt.anchoredPosition = new Vector2(-30f, 0f);
            bcrt.sizeDelta = new Vector2(160f, 50f);

            // Hint pill
            var hint = Ui.Img("HintPill", _hudRoot, _pill48, new Color(1f, 1f, 1f, 0.05f), Image.Type.Sliced);
            var hrt = (RectTransform)hint.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0f, 120f);
            hrt.sizeDelta = new Vector2(520f, 100f);
            _hintGroup = Ui.Group(hint.gameObject);
            AddOutline(hrt, 3f, new Color(1f, 1f, 1f, 0.10f));

            _hintText = Ui.Text("HintText", hrt, "TAP TO FLIP", 34f, Pal.Text, TextAlignmentOptions.Center);
            var htrt = (RectTransform)_hintText.transform;
            Ui.FullStretch(htrt);
            _hintText.characterSpacing = 8f;
        }

        void BuildTitle()
        {
            _titleRoot = Ui.Stretch("TitleScreen", Canvas.transform);
            _titleGroup = Ui.Group(_titleRoot.gameObject);

            // Logo
            _logoRoot = Ui.Rect("Logo", _titleRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1000f, 400f));
            _logoGroup = Ui.Group(_logoRoot.gameObject);

            var bottle = Ui.Text("LogoBottle", _logoRoot, "BOTTLE", 118f, Pal.Text, TextAlignmentOptions.Center);
            var brt = (RectTransform)bottle.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, 0f);
            brt.sizeDelta = new Vector2(1000f, 150f);
            bottle.characterSpacing = 16f;
            Ui.Shadowed(bottle, Pal.InkDeep.Alpha(0.55f), new Vector2(0f, -9f));

            // FLIP sits BELOW BOTTLE and is the hero word.
            var flip = Ui.Text("LogoFlip", _logoRoot, "FLIP", 196f, Pal.Amber, TextAlignmentOptions.Center);
            var frt = (RectTransform)flip.transform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 1f);
            frt.pivot = new Vector2(0.5f, 1f);
            frt.anchoredPosition = new Vector2(0f, -136f);
            frt.sizeDelta = new Vector2(1000f, 240f);
            frt.localRotation = Quaternion.Euler(0f, 0f, -3.5f);
            flip.characterSpacing = 2f;
            Ui.Shadowed(flip, Pal.Hex("8A3A16").Alpha(0.75f), new Vector2(0f, -11f));

            // Tagline
            var tag = Ui.Text("Tagline", _titleRoot, "How many can you land in a row?", 36f, Pal.Muted, TextAlignmentOptions.Center);
            _tagRect = (RectTransform)tag.transform;
            _tagRect.anchorMin = _tagRect.anchorMax = new Vector2(0.5f, 1f);
            _tagRect.pivot = new Vector2(0.5f, 1f);
            _tagRect.anchoredPosition = new Vector2(0f, -500f);
            _tagRect.sizeDelta = new Vector2(950f, 56f);
            _tagGroup = Ui.Group(tag.gameObject);
            tag.characterSpacing = 2f;

            // Primary play button
            var glow = Ui.Img("PlayGlow", _titleRoot, Art.SoftSprite(), Pal.Amber.Alpha(0.14f));
            var glrt = (RectTransform)glow.transform;
            glrt.anchorMin = glrt.anchorMax = new Vector2(0.5f, 1f);
            glrt.pivot = new Vector2(0.5f, 0.5f);
            glrt.anchoredPosition = new Vector2(0f, -1382f);
            glrt.sizeDelta = new Vector2(980f, 420f);

            _playBtnRoot = Ui.Rect("PlayButton", _titleRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1382f), new Vector2(640f, 156f));
            _playGroup = Ui.Group(_playBtnRoot.gameObject);
            CreateButton(_playBtnRoot, "LET'S FLIP", new Vector2(640f, 156f), 56f, out _);
            _playBtnRoot.GetComponent<UIButton>().Clicked = () => PlayPressed?.Invoke();

            // Best line under the button
            var bestLine = Ui.Rect("TitleBest", _titleRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1582f), new Vector2(700f, 50f));
            _titleBestRect = bestLine;
            _titleBestGroup = Ui.Group(bestLine.gameObject);
            var bl = Ui.Text("TitleBestText", bestLine, "BEST 0 LANDED", 32f, Pal.Muted, TextAlignmentOptions.Center);
            Ui.FullStretch((RectTransform)bl.transform);
            _titleBestText = bl;
            bl.characterSpacing = 6f;

            // Rules
            var rules = Ui.Text("Rules", _titleRoot,
                "Land the bottle to keep going.\n1 in 10 flips topples over - and that ends the run.",
                29f, Pal.Muted.Alpha(0.75f), TextAlignmentOptions.Center, FontStyles.Normal);
            _rulesRect = (RectTransform)rules.transform;
            _rulesRect.anchorMin = _rulesRect.anchorMax = new Vector2(0.5f, 1f);
            _rulesRect.pivot = new Vector2(0.5f, 1f);
            _rulesRect.anchoredPosition = new Vector2(0f, -1682f);
            _rulesRect.sizeDelta = new Vector2(900f, 120f);
            _rulesGroup = Ui.Group(rules.gameObject);
            rules.lineSpacing = 16f;
        }

        TextMeshProUGUI _titleBestText;
        TextMeshProUGUI _scoreShadow;

        void BuildGameOver()
        {
            _overRoot = Ui.Stretch("GameOver", Canvas.transform);
            _overRoot.gameObject.SetActive(false);

            _dim = Ui.Img("Dim", _overRoot, null, new Color(0.02f, 0.02f, 0.06f, 0f));
            Ui.FullStretch((RectTransform)_dim.transform);
            _dim.raycastTarget = true;

            _sheet = Ui.Rect("Sheet", _overRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -980f), new Vector2(1120f, 840f));
            var sheetBg = _sheet.gameObject.AddComponent<Image>();
            sheetBg.sprite = _pill64;
            sheetBg.type = Image.Type.Sliced;
            sheetBg.color = Pal.Hex("151936");
            sheetBg.raycastTarget = true;
            _sheetGroup = Ui.Group(_sheet.gameObject, true);

            AddOutline(_sheet, 4f, new Color(1f, 1f, 1f, 0.10f));

            // Decorative grabber
            var grabber = Ui.Img("Grabber", _sheet, _pill12, new Color(1f, 1f, 1f, 0.14f), Image.Type.Sliced);
            var grt = (RectTransform)grabber.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1f);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.anchoredPosition = new Vector2(0f, -26f);
            grt.sizeDelta = new Vector2(110f, 12f);

            // Headline
            _overTitle = Ui.Text("OverTitle", _sheet, "IT FELL!", 104f, Pal.Coral, TextAlignmentOptions.Center);
            _overTitleRect = (RectTransform)_overTitle.transform;
            _overTitleRect.anchorMin = _overTitleRect.anchorMax = new Vector2(0.5f, 1f);
            _overTitleRect.pivot = new Vector2(0.5f, 1f);
            _overTitleRect.anchoredPosition = new Vector2(0f, -84f);
            _overTitleRect.sizeDelta = new Vector2(1000f, 150f);
            _overTitle.characterSpacing = 4f;
            Ui.Shadowed(_overTitle, Pal.InkDeep.Alpha(0.6f), new Vector2(0f, -8f));

            _overSub = Ui.Text("OverSub", _sheet, "Your run ends here", 32f, Pal.Muted, TextAlignmentOptions.Center);
            var osrt = (RectTransform)_overSub.transform;
            osrt.anchorMin = osrt.anchorMax = new Vector2(0.5f, 1f);
            osrt.pivot = new Vector2(0.5f, 1f);
            osrt.anchoredPosition = new Vector2(0f, -232f);
            osrt.sizeDelta = new Vector2(900f, 50f);

            // Stats row
            _overStatsRow = Ui.Rect("Stats", _sheet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(780f, 200f));
            _overFlipsValue = BuildStatColumn(_overStatsRow, new Vector2(-196f, 0f), StatCaptionLanded, Pal.Text);

            var divider = Ui.Img("Divider", _overStatsRow, null, new Color(1f, 1f, 1f, 0.10f));
            var drt = (RectTransform)divider.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(3f, 120f);

            _overBestValue = BuildStatColumn(_overStatsRow, new Vector2(196f, 0f), "BEST", Pal.Amber);

            // Buttons
            var again = Ui.Rect("PlayAgain", _sheet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -486f), new Vector2(760f, 148f));
            CreateButton(again, "FLIP AGAIN", new Vector2(760f, 148f), 52f, out _);
            again.GetComponent<UIButton>().Clicked = () => PlayPressed?.Invoke();

            var menu = Ui.Rect("MenuButton", _sheet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -656f), new Vector2(460f, 96f));
            CreateButton(menu, "MAIN MENU", new Vector2(460f, 96f), 34f, out _, ghost: true);
            menu.GetComponent<UIButton>().Clicked = () => MenuPressed?.Invoke();

            // New best badge floating above the sheet
            _newBestRect = Ui.Rect("NewBest", _sheet, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(330f, 92f));
            _newBestGroup = Ui.Group(_newBestRect.gameObject);
            // Baked amber sprite tinted white: the shared pill sprites carry a dark panel
            // colour in their texture, so tinting one amber would render almost black.
            var nbBg = _newBestRect.gameObject.AddComponent<Image>();
            nbBg.sprite = Art.RoundedSprite(40, Pal.AmberDeep, Pal.Amber, 0);
            nbBg.type = Image.Type.Sliced;
            nbBg.color = Color.white;
            nbBg.raycastTarget = false;
            _newBestText = Ui.Text("NewBestText", _newBestRect, "NEW BEST!", 38f, Pal.OnAmber, TextAlignmentOptions.Center);
            Ui.FullStretch((RectTransform)_newBestText.transform);
            _newBestText.characterSpacing = 4f;
            _newBestRect.localRotation = Quaternion.Euler(0f, 0f, -4f);
        }

        const string StatCaptionLanded = "LANDED";

        /// <summary>Stat block drawn as one rich-text element so the number and its caption can never collide.</summary>
        TextMeshProUGUI BuildStatColumn(Transform parent, Vector2 pos, string caption, Color valueColor)
        {
            var text = Ui.Text(caption + "Stat", parent, FormatStat(caption, 0), 82f, valueColor, TextAlignmentOptions.Top);
            var rt = (RectTransform)text.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320f, 200f);
            text.lineSpacing = -8f;
            text.characterSpacing = 0f;
            return text;
        }

        static string FormatStat(string caption, int value)
        {
            return $"<size=84><b>{value}</b></size>\n<size=26><b><color=#9C99C4>{caption}</color></b></size>";
        }

        void SetStat(TextMeshProUGUI text, string caption, int value)
        {
            if (text != null) text.text = FormatStat(caption, value);
        }

        void BuildFloatingLayers()
        {
            _floaters = Ui.Stretch("Floaters", Canvas.transform);
            _floaters.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            _toastRoot = Ui.Rect("Toast", Canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(880f, 100f));
            _toastGroup = Ui.Group(_toastRoot.gameObject);
            var bg = _toastRoot.gameObject.AddComponent<Image>();
            bg.sprite = _pill48;
            bg.type = Image.Type.Sliced;
            bg.color = Pal.Hex("1A1D46").Alpha(0.94f);
            bg.raycastTarget = false;
            AddOutline(_toastRoot, 3f, Pal.Amber.Alpha(0.35f));

            _toastText = Ui.Text("ToastText", _toastRoot, "", 36f, Pal.Amber, TextAlignmentOptions.Center);
            Ui.FullStretch((RectTransform)_toastText.transform);
            _toastText.characterSpacing = 6f;
            _toastGroup.alpha = 0f;
            _toastRoot.gameObject.SetActive(false);
        }

        void BuildSoundButton()
        {
            var root = Ui.Rect("SoundButton", Canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -52f), new Vector2(96f, 88f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = _pill40;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.10f);
            bg.raycastTarget = true;
            AddOutline(root, 3f, new Color(1f, 1f, 1f, 0.10f));

            _soundLabel = Ui.Text("SoundLabel", root, "SFX", 30f, Pal.Amber, TextAlignmentOptions.Center);
            Ui.FullStretch((RectTransform)_soundLabel.transform);

            var btn = root.gameObject.AddComponent<UIButton>();
            btn.Init();
            btn.Clicked = () => SoundPressed?.Invoke();
        }

        void BuildFlash()
        {
            _flash = Ui.Img("Flash", Canvas.transform, null, new Color(1f, 1f, 1f, 0f));
            Ui.FullStretch((RectTransform)_flash.transform);
            _flash.raycastTarget = false;
        }

        Image CreateButton(RectTransform root, string label, Vector2 size, float fontSize, out UIButton button, bool ghost = false)
        {
            var bg = root.gameObject.AddComponent<Image>();
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            if (ghost)
            {
                bg.sprite = _pill40;
                bg.color = new Color(1f, 1f, 1f, 0.02f);
                AddOutline(root, 3f, new Color(1f, 1f, 1f, 0.16f));
            }
            else
            {
                bg.sprite = Art.RoundedSprite(64, Pal.AmberDeep, Pal.Amber, 0);
                bg.color = Color.white;
            }

            var txt = Ui.Text("Label", root, label, fontSize, ghost ? Pal.Muted : Pal.OnAmber, TextAlignmentOptions.Center);
            Ui.FullStretch((RectTransform)txt.transform);
            txt.characterSpacing = 6f;
            if (!ghost) Ui.Shadowed(txt, Pal.Hex("9A5200").Alpha(0.35f), new Vector2(0f, -4f));

            button = root.gameObject.AddComponent<UIButton>();
            button.Init();
            return bg;
        }

        static void AddOutline(RectTransform target, float width, Color color)
        {
            var outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, width);
            outline.useGraphicAlpha = false;
        }

        // ------------------------------------------------------------- animation

        public void ShowTitle(bool animate = true)
        {
            _titleRoot.gameObject.SetActive(true);
            _overRoot.gameObject.SetActive(false);
            _hudRoot.gameObject.SetActive(false);
            _titleGroup.alpha = 1f;
            _titleGroup.blocksRaycasts = true;
            _titleGroup.interactable = true;

            ResetTitleElements();
            AnimateTitleIn(animate ? 0f : -1f);
        }

        void ResetTitleElements()
        {
            _logoGroup.alpha = 0f;
            _logoRoot.localScale = Vector3.one * 0.82f;
            _tagGroup.alpha = 0f;
            _tagRect.anchoredPosition = new Vector2(0f, -530f);
            _playGroup.alpha = 0f;
            _playBtnRoot.localScale = Vector3.one * 0.86f;
            _titleBestGroup.alpha = 0f;
            _rulesGroup.alpha = 0f;
            _rulesRect.anchoredPosition = new Vector2(0f, -1712f);
            _playBtnRoot.GetComponent<UIButton>()?.SetPulse(0f);
        }

        void AnimateTitleIn(float delay)
        {
            // Logo pops in with a spring, then the smaller bits follow.
            Tween.Delay(delay + 0.02f, () =>
            {
                Tween.To(0.62f, t =>
                {
                    _logoGroup.alpha = Mathf.Clamp01(t * 2.2f);
                    _logoRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, Ease.OutBack(t, 1.5f));
                }, Ease.Linear);
            });

            Tween.Delay(delay + 0.20f, () =>
            {
                Tween.To(0.42f, t =>
                {
                    _tagGroup.alpha = t;
                    _tagRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-530f, -500f, Ease.OutCubic(t)));
                }, Ease.Linear);
            });

            Tween.Delay(delay + 0.30f, () =>
            {
                Tween.To(0.46f, t =>
                {
                    _playGroup.alpha = Mathf.Clamp01(t * 2f);
                    _playBtnRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, Ease.OutBack(t, 1.7f));
                }, Ease.Linear, () => _playBtnRoot.GetComponent<UIButton>()?.SetPulse(0.028f, 0.45f));
            });

            Tween.Delay(delay + 0.40f, () =>
            {
                Tween.To(0.4f, t => _titleBestGroup.alpha = t, Ease.OutQuad);
            });

            Tween.Delay(delay + 0.48f, () =>
            {
                Tween.To(0.5f, t =>
                {
                    _rulesGroup.alpha = t * 0.9f;
                    _rulesRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-1712f, -1682f, Ease.OutCubic(t)));
                }, Ease.OutQuad);
            });
        }

        public void HideTitle(Action done = null)
        {
            _titleGroup.blocksRaycasts = false;
            _titleGroup.interactable = false;
            _playBtnRoot.GetComponent<UIButton>()?.SetPulse(0f);
            Tween.To(0.22f, t =>
            {
                _titleGroup.alpha = 1f - t;
                _titleRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.04f, t);
            }, Ease.InQuad, () =>
            {
                _titleRoot.gameObject.SetActive(false);
                _titleRoot.localScale = Vector3.one;
                done?.Invoke();
            });
        }

        public void ShowHud(bool animate = true)
        {
            _hudRoot.gameObject.SetActive(true);
            _hudGroup.blocksRaycasts = false;
            _hudGroup.interactable = false;
            _scoreText.text = "0";
            if (_scoreShadow != null) _scoreShadow.text = "0";
            _scoreRect.localScale = Vector3.one;

            if (!animate)
            {
                _hudGroup.alpha = 1f;
                SetHintVisible(true, false);
                return;
            }

            _hudGroup.alpha = 0f;
            Tween.To(0.3f, t => _hudGroup.alpha = t, Ease.OutQuad);
            SetHintVisible(true, true);
        }

        public void HideHud()
        {
            _hudGroup.alpha = 0f;
            _hudRoot.gameObject.SetActive(false);
        }

        public void SetScore(int value, bool pop = true)
        {
            _scoreText.text = value.ToString();
            if (_scoreShadow != null) _scoreShadow.text = value.ToString();
            if (!pop) return;

            _scoreRect.localScale = Vector3.one * 1.28f;
            Tween.To(0.26f, t => _scoreRect.localScale = Vector3.one * Mathf.LerpUnclamped(1.28f, 1f, Ease.OutBack(t, 2.2f)), Ease.Linear);
            _scoreText.color = Pal.Amber;
            Tween.To(0.5f, t => _scoreText.color = Color.Lerp(Pal.Amber, Pal.Text, t), Ease.OutQuad);
        }

        public void SetBest(int value)
        {
            _bestChipText.text = value.ToString();
            _titleBestText.text = value > 0 ? $"BEST {value} LANDED" : "NO RECORD YET";
        }

        public void SetHintVisible(bool visible, bool animate = true)
        {
            if (_hintGroup == null) return;
            _hintGroup.gameObject.SetActive(true);
            if (!animate)
            {
                _hintGroup.alpha = visible ? 1f : 0f;
                return;
            }
            float from = _hintGroup.alpha;
            float to = visible ? 1f : 0f;
            Tween.To(0.25f, t => _hintGroup.alpha = Mathf.Lerp(from, to, t), Ease.OutQuad);
        }

        public void SetSoundState(bool muted)
        {
            _soundLabel.color = muted ? Pal.Muted.Alpha(0.5f) : Pal.Amber;
            _soundLabel.text = muted ? "OFF" : "SFX";
        }

        public void ShowGameOver(int score, int best, bool newBest)
        {
            _overRoot.gameObject.SetActive(true);
            SetStat(_overFlipsValue, StatCaptionLanded, score);
            SetStat(_overBestValue, "BEST", best);
            _overTitle.text = "IT FELL!";
            _overTitle.color = Pal.Coral;
            _overSub.text = newBest ? "That is a new personal best." : "The bottle toppled over.";
            _overTitleRect.localScale = Vector3.one;

            // Darken behind the sheet.
            var dimColor = new Color(0.02f, 0.02f, 0.06f, 0f);
            _dim.color = dimColor;
            Tween.To(0.32f, t => _dim.color = new Color(0.02f, 0.02f, 0.06f, 0.52f * t), Ease.OutQuad);

            // Slide the sheet up.
            _sheet.anchoredPosition = new Vector2(0f, -1000f);
            _sheetGroup.alpha = 1f;
            Tween.Delay(0.05f, () =>
            {
                Tween.To(0.46f, t => _sheet.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-1000f, -90f, Ease.OutCubic(t))), Ease.Linear);
                Tween.To(0.34f, t => _sheetGroup.alpha = Mathf.Clamp01(t * 1.8f), Ease.Linear);
            });

            // Headline punch.
            _overTitleRect.localScale = Vector3.one * 1.5f;
            Tween.Delay(0.16f, () =>
            {
                Tween.To(0.5f, t => _overTitleRect.localScale = Vector3.one * Mathf.LerpUnclamped(1.5f, 1f, Ease.OutBack(t, 2.4f)), Ease.Linear);
            });

            // Stats count up for a bit of drama.
            Tween.Delay(0.22f, () =>
            {
                Tween.Delay(0.02f, () =>
                    Tween.Float(0f, score, 0.5f, v => SetStat(_overFlipsValue, StatCaptionLanded, Mathf.RoundToInt(v)), Ease.OutCubic));
            });

            if (newBest)
            {
                _newBestGroup.gameObject.SetActive(true);
                _newBestGroup.alpha = 0f;
                _newBestRect.localScale = Vector3.zero;
                Tween.Delay(0.44f, () =>
                {
                    Tween.To(0.55f, t =>
                    {
                        _newBestGroup.alpha = Mathf.Clamp01(t * 2f);
                        _newBestRect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Ease.OutElastic(t));
                    }, Ease.Linear);
                });
            }
            else
            {
                _newBestGroup.gameObject.SetActive(false);
            }
        }

        public void HideGameOver()
        {
            Tween.To(0.28f, t =>
            {
                _sheet.anchoredPosition = new Vector2(0f, Mathf.Lerp(-90f, -1050f, Ease.InCubic(t)));
                _dim.color = new Color(0.02f, 0.02f, 0.06f, Mathf.Lerp(0.52f, 0f, t));
            }, Ease.Linear, () => _overRoot.gameObject.SetActive(false));
        }

        public void PlusOne(Vector3 worldPos, int amount = 1)
        {
            if (WorldCamera == null) return;
            Vector3 sp = WorldCamera.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) sp = new Vector3(Screen.width * 0.5f, Screen.height * 0.55f, 1f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, null, out Vector2 local);

            var txt = Ui.Text("PlusOne", _floaters, "+" + amount, 54f, Pal.Amber, TextAlignmentOptions.Center);
            var rt = (RectTransform)txt.transform;
            rt.anchoredPosition = local;
            rt.localScale = Vector3.zero;
            txt.characterSpacing = 2f;

            Tween.To(0.85f, t =>
            {
                float pop = Mathf.LerpUnclamped(0f, 1f, Ease.OutBack(Mathf.Clamp01(t * 4.5f), 2.4f));
                float shrink = Mathf.Lerp(1f, 0.82f, Mathf.Clamp01((t - 0.6f) * 2.5f));
                rt.localScale = Vector3.one * pop * shrink;
                rt.anchoredPosition = local + new Vector2(Mathf.Sin(t * 9f) * 6f, Mathf.Lerp(0f, 150f, Ease.OutCubic(t)));
                txt.color = Pal.Amber.Alpha(1f - Ease.InQuad(Mathf.Clamp01((t - 0.5f) * 2f)));
            }, Ease.Linear, () => { if (txt != null) Destroy(txt.gameObject); });
        }

        public void Toast(string message, Color color)
        {
            _toastRoot.gameObject.SetActive(true);
            _toastText.text = message;
            _toastText.color = color;
            _toastGroup.alpha = 0f;
            _toastRoot.anchoredPosition = new Vector2(0f, 250f);
            _toastRoot.localScale = Vector3.one * 0.9f;

            Tween.To(0.4f, t =>
            {
                _toastGroup.alpha = Mathf.Clamp01(t * 2.5f);
                _toastRoot.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(250f, 320f, Ease.OutBack(t, 1.4f)));
                _toastRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, Ease.OutBack(t, 2f));
            }, Ease.Linear);

            Tween.Delay(1.5f, () =>
            {
                Tween.To(0.35f, t =>
                {
                    _toastGroup.alpha = 1f - t;
                    _toastRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(320f, 360f, t));
                }, Ease.InQuad, () => _toastRoot.gameObject.SetActive(false));
            });
        }

        public void Flash(float strength = 1f, Color? color = null)
        {
            Color c = color ?? Color.white;
            _flash.color = new Color(c.r, c.g, c.b, 0f);
            Tween.To(0.36f, t =>
            {
                float a = t < 0.22f ? Mathf.Lerp(0f, 0.72f * strength, t / 0.22f) : Mathf.Lerp(0.72f * strength, 0f, (t - 0.22f) / 0.78f);
                _flash.color = new Color(c.r, c.g, c.b, a);
            }, Ease.Linear);
        }

        void Ripple(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, null, out Vector2 local);
            var img = Ui.Img("Ripple", _canvasRect, Art.SoftSprite(), Pal.Text.Alpha(0.22f));
            var rt = (RectTransform)img.transform;
            rt.anchoredPosition = local;
            rt.sizeDelta = new Vector2(120f, 120f);

            Tween.To(0.5f, t =>
            {
                float s = Mathf.Lerp(120f, 420f, Ease.OutCubic(t));
                rt.sizeDelta = new Vector2(s, s);
                img.color = Pal.Text.Alpha(0.22f * (1f - t));
            }, Ease.Linear, () => Destroy(img.gameObject));
        }

        void Update()
        {
            // Gentle breathing on the tap hint so the screen never feels static.
            if (_hintGroup != null && _hintGroup.gameObject.activeSelf && _hintGroup.alpha > 0.02f)
            {
                float k = 0.86f + 0.14f * Mathf.Sin(Time.unscaledTime * 3.1f);
                _hintText.color = Pal.Text.Alpha(k);
            }
        }
    }
}
