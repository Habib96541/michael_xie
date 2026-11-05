using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using Screen = UnityEngine.Device.Screen;
#endif

namespace TeslaRoadster
{
    internal sealed class RoadsterHud
    {
        const float Width = 1302f;
        const float Height = 600f;
        static readonly Color Cream = new Color32(254, 246, 229, 255);
        static readonly Color Ink = new Color32(21, 34, 40, 255);
        static readonly Color Scrim = new Color32(21, 34, 40, 191);
        static readonly Color Seafoam = new Color32(188, 239, 219, 255);
        static readonly Color NitroBlue = new Color32(108, 218, 255, 255);
        static readonly Color Dim = new Color32(231, 229, 218, 255);

        readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        readonly RectTransform design, staging, driving, results;
        readonly RectTransform[] bars = new RectTransform[4];
        readonly Image impactFlash;
        readonly RoadsterSpeedLines speedLines;
        readonly Text stageHint, countdown, runScore, runCredits, runBest, runSpeed, warning, comboLabel, stuntNotice;
        readonly Text resultCause, resultScore, resultDetail, resultBest;
        readonly CanvasGroup nitroIcon;
        readonly Button nitroButton;
        readonly Image nitroFill;
        readonly GraphicRaycaster raycaster;
        readonly List<RaycastResult> touchHits = new();
        PointerEventData webTouch;
        readonly GameObject startButton;
        Rect lastSafeArea;
        int screenWidth, screenHeight, shownCoins, shownSpeed, shownScore, shownCombo, shownCountdown;
        string shownNotice;

        public RoadsterHud(Transform parent, Action start, Action<int> steer, Action lift, Action slam, Action nitro)
        {
            var canvas = new GameObject("Roadster interface", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvas.transform.SetParent(parent, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            raycaster = canvas.GetComponent<GraphicRaycaster>();
            var screen = canvas.GetComponent<RectTransform>();
            for (int i = 0; i < bars.Length; i++)
                bars[i] = Box(screen, "Viewport matte", 0, 0, 0, 0, new Color32(8, 10, 13, 255)).rectTransform;

            design = CreateRect(screen, "2.17:1 layout", 0, 0, Width, Height);
            design.pivot = design.anchorMin = design.anchorMax = new Vector2(0.5f, 0.5f);
            staging = CreateRect(design, "Staging", 0, 0, Width, Height);
            driving = CreateRect(design, "Driving", 0, 0, Width, Height);
            results = CreateRect(design, "Results", 0, 0, Width, Height);
            speedLines = new RoadsterSpeedLines(CreateRect(driving, "Speed lines", 0, 0, Width, Height));

            Label(staging, "Roadster", 35, 22, 185, 25, 18, Cream, bold: true, shadow: true);
            Label(staging, "Marine Drive", 33, 43, 355, 58, 40, Cream, bold: true, shadow: true);
            stageHint = Label(staging, "", 37, 108, 320, 23, 16, Dim, shadow: true);
            var startArea = CreateRect(staging, "Start controls", 1017, 441, 251, 132);
            startButton = startArea.gameObject;
            Button(startArea, "Start", 0, 0, 251, 64, Seafoam, Ink, start, 25);
            Label(startArea, "Up to flip  /  Down to slam", 0, 72, 251, 24, 16, Cream, TextAnchor.MiddleCenter, shadow: true);
            Label(startArea, "Arrows / WASD · Shift for nitro", 0, 100, 251, 20, 13, Dim, TextAnchor.MiddleCenter, shadow: true);
            countdown = Label(design, "", 551, 199, 200, 100, 80, Cream, TextAnchor.MiddleCenter, true, true);

            Label(driving, "Score", 31, 22, 140, 21, 16, Dim, shadow: true);
            runScore = Label(driving, "", 30, 41, 216, 45, 34, Cream, bold: true, shadow: true);
            runCredits = Label(driving, "", 31, 85, 216, 24, 16, Cream, shadow: true);
            runBest = Label(driving, "", 1070, 25, 201, 30, 16, Cream, TextAnchor.MiddleRight, shadow: true);
            runSpeed = Label(driving, "", 1084, 524, 185, 43, 30, Cream, TextAnchor.MiddleRight, true, true);
            warning = Label(driving, "Missile incoming · boost to dodge", 405, 22, 492, 37, 22, new Color32(255, 184, 152, 255),
                TextAnchor.MiddleCenter, true, true);
            comboLabel = Label(driving, "", 1051, 55, 220, 32, 21, Seafoam, TextAnchor.MiddleRight, true, true);
            stuntNotice = Label(driving, "", 410, 491, 482, 35, 20, Seafoam, TextAnchor.MiddleCenter, true, true);

            DirectionButton(driving, "Up", 133, 261, 90f, lift);
            DirectionButton(driving, "Left", 24, 370, 180f, () => steer(-1));
            DirectionButton(driving, "Right", 242, 370, 0f, () => steer(1));
            DirectionButton(driving, "Down", 133, 479, -90f, slam);
            var nitroArea = ClickArea(driving, "Nitro", 1126, 405, 142, 110, Scrim, nitro, instant: true);
            nitroButton = nitroArea.GetComponent<Button>();
            var canister = CreateRect(nitroArea, "Nitro canister icon", 45, 9, 52, 71);
            nitroIcon = canister.gameObject.AddComponent<CanvasGroup>();
            Box(canister, "Shoulder", 12, 16, 28, 28, NitroBlue).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            Box(canister, "Canister body", 5, 24, 42, 43, NitroBlue);
            Box(canister, "Valve", 18, 7, 16, 11, Cream);
            Box(canister, "Canister stripe", 5, 41, 42, 7, Cream);
            Box(nitroArea, "Charge track", 15, 90, 112, 7, new Color32(108, 218, 255, 60));
            nitroFill = Box(nitroArea, "Nitro charge", 15, 90, 0, 7, NitroBlue);

            Box(results, "Scene dim", 0, 0, Width, Height, new Color32(15, 15, 15, 98));
            var report = CreateRect(results, "Run results", 459, 141, 384, 318);
            var charcoal = new Color32(26, 26, 26, 255);
            Box(report, "Result panel", 0, 0, 384, 318, charcoal);
            resultCause = Label(report, "", 31, 23, 321, 31, 20, Color.white, bold: true);
            resultScore = Label(report, "", 29, 57, 326, 66, 48, Color.white, bold: true);
            resultBest = Label(report, "", 32, 123, 320, 26, 16, Color.white, bold: true);
            resultDetail = Label(report, "", 32, 154, 320, 55, 16, new Color32(224, 224, 224, 255));
            Button(report, "Try again", 30, 225, 324, 58, Color.white, charcoal, start, 22);

            impactFlash = Box(design, "Impact flash", 0, 0, Width, Height, Color.white);
            impactFlash.gameObject.SetActive(false);

            if (EventSystem.current == null)
            {
                var events = new GameObject("Input event system", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(parent, false);
            }
        }

        public void FitViewport(Camera camera)
        {
            int w = Screen.width;
            int h = Screen.height;
            Rect safe = Screen.safeArea;
            if (w <= 0 || h <= 0 || (w == screenWidth && h == screenHeight && safe == lastSafeArea))
                return;

            screenWidth = w;
            screenHeight = h;
            lastSafeArea = safe;
            if (safe.width <= 0f || safe.height <= 0f)
                safe = new Rect(0, 0, w, h);

            float scale = Mathf.Min(safe.width / Width, safe.height / Height);
            Vector2 size = new(Width * scale, Height * scale);
            Vector2 min = safe.center - size * 0.5f;
            Vector2 max = min + size;
            float x0 = min.x / w, x1 = max.x / w;
            float y0 = min.y / h, y1 = max.y / h;
            camera.rect = Rect.MinMaxRect(x0, y0, x1, y1);
            camera.aspect = Width / Height;
            design.anchorMin = design.anchorMax = new Vector2(safe.center.x / w, safe.center.y / h);
            design.anchoredPosition = Vector2.zero;
            design.localScale = new Vector3(scale, scale, 1f);
            AnchorBar(bars[0], 0f, 0f, x0, 1f);
            AnchorBar(bars[1], x1, 0f, 1f, 1f);
            AnchorBar(bars[2], x0, 0f, x1, y0);
            AnchorBar(bars[3], x0, y1, x1, 1f);
        }

        static void AnchorBar(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public void ShowClimb()
        {
            Show(staging);
            stageHint.text = "Climbing to the start";
            startButton.SetActive(false);
        }

        public void ShowStaged(int bank, int best)
        {
            Show(staging);
            stageHint.text = $"Bank {bank:N0}   /   Best {best:N0}";
            startButton.SetActive(true);
        }

        public void ShowLaunch() => Show(null);

        public void ShowCountdown(int seconds)
        {
            if (countdown.gameObject.activeSelf && shownCountdown == seconds)
                return;
            Show(null);
            shownCountdown = seconds;
            countdown.text = seconds.ToString();
            countdown.gameObject.SetActive(true);
        }

        public void ShowDriving(int best)
        {
            speedLines.SetFlow(0f, 0f, 0f);
            ShowNitro(0f, false);
            Show(driving);
            runBest.text = $"Best {best:N0}";
            shownCoins = shownSpeed = shownScore = shownCombo = -1;
            shownNotice = null;
            UpdateDriving(0, 0, 24f, false, 0, "");
        }

        public void ShowSpeedLines(float distance, float speed, float thrust) => speedLines.SetFlow(distance, speed, thrust);

        public void ShowNitro(float charge, bool boosting)
        {
            nitroFill.rectTransform.sizeDelta = new Vector2(112f * charge, 7f);
            nitroButton.interactable = charge > 0f;
            nitroIcon.alpha = charge > 0f ? 1f : 0.36f;
            nitroFill.color = boosting ? Color.white : NitroBlue;
        }

        public void PressAt(Vector2 normalized)
        {
            webTouch ??= new PointerEventData(EventSystem.current);
            webTouch.position = Vector2.Scale(normalized, new Vector2(Screen.width, Screen.height));
            touchHits.Clear();
            raycaster.Raycast(webTouch, touchHits);
            if (touchHits.Count > 0 && touchHits[0].gameObject.TryGetComponent(out Button button)
                && button.IsActive() && button.IsInteractable())
                button.onClick.Invoke();
        }

        public void UpdateDriving(int score, int coins, float speed, bool lockOn, int combo, string notice)
        {
            if (shownScore != score)
            {
                shownScore = score;
                runScore.text = score.ToString("N0");
            }
            if (shownCoins != coins)
            {
                shownCoins = coins;
                runCredits.text = $"{coins * RoadsterGame.CoinValue:N0} credits";
            }
            if (shownCombo != combo)
            {
                shownCombo = combo;
                comboLabel.text = combo > 1 ? $"×{Mathf.Min(8, combo)} combo" : "";
            }
            if (shownNotice != notice)
            {
                shownNotice = notice;
                stuntNotice.text = notice;
            }
            int kmh = Mathf.RoundToInt(speed * 3.6f);
            if (shownSpeed != kmh)
            {
                shownSpeed = kmh;
                runSpeed.text = $"{kmh} km/h";
            }
            if (warning.gameObject.activeSelf != lockOn)
                warning.gameObject.SetActive(lockOn);
        }

        void Show(RectTransform group)
        {
            staging.gameObject.SetActive(group == staging);
            driving.gameObject.SetActive(group == driving);
            results.gameObject.SetActive(group == results);
            countdown.gameObject.SetActive(false);
            impactFlash.gameObject.SetActive(false);
        }

        public void ShowImpact()
        {
            Show(null);
            impactFlash.gameObject.SetActive(true);
        }

        public void FadeImpact(float elapsed)
        {
            Color color = impactFlash.color;
            color.a = 0.55f * Mathf.Clamp01(1f - elapsed * 3.5f);
            impactFlash.color = color;
        }

        public void ShowResults(bool missile, int score, int coins, int bank, int best, int combo, bool record)
        {
            Show(results);
            resultCause.text = missile ? "Missile strike" : "Traffic collision";
            resultScore.text = score.ToString("N0");
            resultBest.text = record ? "New record" : $"Best {best:N0}";
            string streak = combo > 1 ? $"   /   ×{Mathf.Min(8, combo)} combo" : "";
            resultDetail.text = $"+{coins * RoadsterGame.CoinValue:N0} credits   /   Bank {bank:N0}{streak}";
        }

        static RectTransform CreateRect(Transform designParent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(designParent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = CreateRect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, bool shadow = false)
        {
            var label = CreateRect(parent, value == "" ? "Value" : value, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.fontSize = size;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            if (shadow)
            {
                var effect = label.gameObject.AddComponent<Shadow>();
                effect.effectColor = new Color(0.04f, 0.08f, 0.10f, 0.75f);
                effect.effectDistance = new Vector2(1.2f, -1.4f);
            }
            return label;
        }

        RectTransform ClickArea(Transform parent, string title, float x, float y, float w, float h, Color fill, Action action,
            bool instant = false)
        {
            Image image = Box(parent, title, x, y, w, h, fill);
            image.raycastTarget = true;
            Button button = instant ? image.gameObject.AddComponent<RoadsterPressButton>() : image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.93f, 0.97f, 0.96f);
            colors.pressedColor = new Color(0.67f, 0.92f, 0.84f);
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return image.rectTransform;
        }

        void Button(Transform parent, string title, float x, float y, float w, float h, Color fill, Color text, Action action, int fontSize)
        {
            var area = ClickArea(parent, title, x, y, w, h, fill, action);
            Label(area, title, 0, 0, w, h, fontSize, text, TextAnchor.MiddleCenter, true);
        }

        void DirectionButton(Transform parent, string title, float x, float y, float angle, Action action)
        {
            const float size = 104f;
            var area = ClickArea(parent, title, x, y, size, size, Scrim, action, instant: true);
            var arrow = Label(area, ">", 0, 0, size, size, 47, Cream, TextAnchor.MiddleCenter, true).rectTransform;
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.anchoredPosition = Vector2.zero;
            arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
