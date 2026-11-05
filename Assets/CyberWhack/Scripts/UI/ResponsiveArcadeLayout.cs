using CyberWhack.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.UI
{
    /// <summary>
    /// Reflows the presentation between portrait and landscape mobile screens.
    /// Gameplay targets stay in a touch-friendly 3x3 matrix while the HUD moves
    /// to a compact side rail on wider phones and tablets.
    /// </summary>
    public sealed class ResponsiveArcadeLayout : MonoBehaviour
    {
        private RectTransform _grid;
        private RectTransform _menu;
        private RectTransform _header;
        private RectTransform _targetIntel;
        private RectTransform _gameOver;
        private RectTransform _pauseCard;
        private HatchNode[] _nodes;
        private int _lastWidth;
        private int _lastHeight;
        private Rect _lastSafeArea;

        public void Configure(
            RectTransform grid,
            RectTransform menu,
            RectTransform header,
            RectTransform targetIntel,
            RectTransform gameOver,
            HatchNode[] nodes,
            RectTransform pauseCard)
        {
            _grid = grid;
            _menu = menu;
            _header = header;
            _targetIntel = targetIntel;
            _gameOver = gameOver;
            _pauseCard = pauseCard;
            _nodes = nodes;
            ApplyLayout(true);
        }

        private void Update()
        {
            if (_lastWidth != Screen.width || _lastHeight != Screen.height || _lastSafeArea != Screen.safeArea)
            {
                ApplyLayout(false);
            }
        }

        private void ApplyLayout(bool force)
        {
            if (!force && _grid == null) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;

            bool landscape = Screen.width > Screen.height * 1.12f;
            if (landscape)
            {
                SetLayout(_grid, new Vector2(0.37f, 0.06f), new Vector2(0.985f, 0.94f));
                SetLayout(_header, new Vector2(0.015f, 0.06f), new Vector2(0.35f, 0.94f));
                SetLayout(_menu, Vector2.zero, Vector2.one);

                SetLayout(Child(_header, "AbortBtn"), new Vector2(0.04f, 0.90f), new Vector2(0.46f, 0.99f));
                SetLayout(Child(_header, "InGameAudioToggle"), new Vector2(0.54f, 0.90f), new Vector2(0.96f, 0.99f));
                SetLayout(Child(_header, "TimerBox"), new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.88f));
                SetLayout(Child(_header, "ScoreBox"), new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.66f));
                SetLayout(Child(_header, "ComboPanel"), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.42f));

                // Keep the whole landing flow in a tidy left column and give
                // the preview art a clear, separate column on wide screens.
                SetLayout(Child(_menu, "HeroTitleBanner"), new Vector2(0.06f, 0.76f), new Vector2(0.56f, 0.94f));
                SetLayout(Child(_menu, "RecordCard"), new Vector2(0.06f, 0.59f), new Vector2(0.40f, 0.72f));
                SetLayout(Child(_menu, "AudioToggleBtn"), new Vector2(0.44f, 0.59f), new Vector2(0.56f, 0.72f));
                SetLayout(Child(_menu, "IntelButton"), new Vector2(0.06f, 0.42f), new Vector2(0.28f, 0.55f));
                SetLayout(Child(_menu, "StartButton"), new Vector2(0.32f, 0.42f), new Vector2(0.56f, 0.55f));
                SetLayout(Child(_menu, "LevelBadge"), new Vector2(0.66f, 0.62f), new Vector2(0.83f, 0.88f));
                SetLayout(Child(_menu, "PreviewShowcaseRow"), new Vector2(0.60f, 0.12f), new Vector2(0.97f, 0.51f));
                SetLayout(Child(_menu, "SeedBox"), new Vector2(0.06f, 0.25f), new Vector2(0.56f, 0.36f));
                SetLayout(Child(_menu, "Footer"), new Vector2(0.06f, 0.12f), new Vector2(0.56f, 0.19f));
                Transform preview = Child(_menu, "PreviewShowcaseRow");
                if (preview != null) preview.gameObject.SetActive(true);
            }
            else
            {
                SetLayout(_grid, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.68f));
                SetLayout(_header, new Vector2(0f, 0.70f), Vector2.one);
                SetLayout(_menu, Vector2.zero, Vector2.one);

                // Row 1: Pause button (left), Time plaque (center-right), Audio toggle (right)
                SetLayout(Child(_header, "AbortBtn"), new Vector2(0.04f, 0.70f), new Vector2(0.24f, 0.98f));
                SetLayout(Child(_header, "TimerBox"), new Vector2(0.26f, 0.70f), new Vector2(0.82f, 0.98f));
                SetLayout(Child(_header, "InGameAudioToggle"), new Vector2(0.84f, 0.72f), new Vector2(0.96f, 0.96f));

                // Row 2: Score card (full width)
                SetLayout(Child(_header, "ScoreBox"), new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.66f));

                // Row 3: Combo card (full width)
                SetLayout(Child(_header, "ComboPanel"), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.32f));

                SetLayout(Child(_menu, "HeroTitleBanner"), new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.96f));
                SetLayout(Child(_menu, "RecordCard"), new Vector2(0.07f, 0.68f), new Vector2(0.72f, 0.78f));
                SetLayout(Child(_menu, "AudioToggleBtn"), new Vector2(0.76f, 0.68f), new Vector2(0.93f, 0.78f));
                SetLayout(Child(_menu, "IntelButton"), new Vector2(0.08f, 0.54f), new Vector2(0.46f, 0.64f));
                SetLayout(Child(_menu, "StartButton"), new Vector2(0.49f, 0.54f), new Vector2(0.92f, 0.64f));
                SetLayout(Child(_menu, "LevelBadge"), new Vector2(0.42f, 0.36f), new Vector2(0.58f, 0.50f));
                SetLayout(Child(_menu, "SeedBox"), new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.29f));
                SetLayout(Child(_menu, "Footer"), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.14f));

                Transform preview = Child(_menu, "PreviewShowcaseRow");
                if (preview != null) preview.gameObject.SetActive(false);
            }

            HideHudClutter(_header);
            ArrangeStatBox(Child(_header, "TimerBox"), true);
            ArrangeStatBox(Child(_header, "ScoreBox"), false);
            ArrangeCombo(Child(_header, "ComboPanel"));
            ApplyNodeSizing(landscape);
            SetLayout(_targetIntel,
                landscape ? new Vector2(0.28f, 0.04f) : new Vector2(0.04f, 0.04f),
                landscape ? new Vector2(0.72f, 0.96f) : new Vector2(0.96f, 0.96f));
            SetLayout(_gameOver,
                landscape ? new Vector2(0.24f, 0.04f) : new Vector2(0.06f, 0.04f),
                landscape ? new Vector2(0.76f, 0.96f) : new Vector2(0.94f, 0.96f));
            SetLayout(_pauseCard,
                landscape ? new Vector2(0.25f, 0.08f) : new Vector2(0.10f, 0.18f),
                landscape ? new Vector2(0.75f, 0.92f) : new Vector2(0.90f, 0.82f));
        }

        private static void HideHudClutter(RectTransform header)
        {
            if (header == null) return;
            Transform seed = header.Find("SeedBadge");
            if (seed != null) seed.gameObject.SetActive(false);
            Transform looseSeed = header.Find("InGameSeed");
            if (looseSeed != null) looseSeed.gameObject.SetActive(false);
        }

        private static void ArrangeStatBox(RectTransform box, bool timer)
        {
            if (box == null) return;
            Transform icon = box.Find(timer ? "ClockIcon" : "CoinIcon");
            if (icon != null) icon.gameObject.SetActive(false);

            string titleName = timer ? "TimerTitle" : "ScoreTitle";
            string digitsName = timer ? "TimerDigits" : "ScoreDigits";

            if (timer)
            {
                SetLayout(box.Find(titleName) as RectTransform, new Vector2(0.34f, 0.61f), new Vector2(0.94f, 0.92f));
                SetLayout(box.Find(digitsName) as RectTransform, new Vector2(0.33f, 0.20f), new Vector2(0.94f, 0.66f));
                RectTransform track = box.Find("TimerProgressTrack") as RectTransform;
                SetLayout(track, new Vector2(0.35f, 0.07f), new Vector2(0.92f, 0.18f));
            }
            else
            {
                SetLayout(box.Find(titleName) as RectTransform, new Vector2(0.28f, 0.61f), new Vector2(0.94f, 0.92f));
                SetLayout(box.Find(digitsName) as RectTransform, new Vector2(0.26f, 0.12f), new Vector2(0.94f, 0.64f));
            }
        }

        private static void ArrangeCombo(RectTransform combo)
        {
            if (combo == null) return;
            // ComboTrack is the full plaque background
            RectTransform track = combo.Find("ComboTrack") as RectTransform;
            SetLayout(track, Vector2.zero, Vector2.one);

            // Keep the trophy artwork clear and place the multiplier in the open center lane.
            SetLayout(combo.Find("MultiplierBadge") as RectTransform, new Vector2(0.30f, 0.38f), new Vector2(0.73f, 0.94f));
            SetLayout(combo.Find("ComboProgressTrack") as RectTransform, new Vector2(0.30f, 0.14f), new Vector2(0.73f, 0.30f));
            SetLayout(combo.Find("StreakText") as RectTransform, new Vector2(0.74f, 0.12f), new Vector2(0.96f, 0.88f));

            if (track == null) return;

            // Fill bar extends horizontally between the vertical divider and the text
            RectTransform fill = track.Find("ComboFill") as RectTransform;
            SetLayout(fill, new Vector2(0.30f, 0.14f), new Vector2(0.73f, 0.30f));

            // Streak/Combo text sits on the right
            SetLayout(track.Find("StreakText") as RectTransform, new Vector2(0.74f, 0.12f), new Vector2(0.96f, 0.88f));
        }

        private static void PlaceIcon(RectTransform icon)
        {
            if (icon == null) return;
            icon.gameObject.SetActive(true);
            SetLayout(icon, new Vector2(0.045f, 0.16f), new Vector2(0.26f, 0.84f));
            Image image = icon.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }

        private void ApplyNodeSizing(bool landscape)
        {
            if (_grid == null || _nodes == null) return;

            float availableWidth = Mathf.Max(320f, Screen.width * (_grid.anchorMax.x - _grid.anchorMin.x));
            float availableHeight = Mathf.Max(320f, Screen.height * (_grid.anchorMax.y - _grid.anchorMin.y));
            float nodeSize = Mathf.Min(availableWidth / 3.45f, availableHeight / 3.45f);
            nodeSize = Mathf.Clamp(nodeSize, landscape ? 150f : 190f, 330f);

            for (int i = 0; i < _nodes.Length; i++)
            {
                HatchNode node = _nodes[i];
                if (node == null) continue;
                RectTransform nodeRect = node.transform as RectTransform;
                if (nodeRect != null) nodeRect.sizeDelta = new Vector2(nodeSize, nodeSize);
                node.ConfigureResponsiveSize(nodeSize);
            }
        }

        private static RectTransform Child(RectTransform parent, string name)
        {
            if (parent == null) return null;
            Transform child = parent.Find(name);
            return child as RectTransform;
        }

        private static void SetLayout(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
