using UnityEngine;

namespace com.javierquevedo
{
    /// <summary>
    /// Shared screen-space layout for the IMGUI screens.
    ///
    /// OnGUI works in GUI coordinates (origin top-left, y growing downwards) while
    /// Screen.safeArea is bottom-left based, so every screen used to lay itself out from
    /// raw Screen.width/height. On phones that ignores notches, rounded corners and the
    /// browser's own chrome, and the hand-tuned scale clamps made short landscape windows
    /// overflow. These helpers centralise the conversion and the "fit inside what is left"
    /// maths so the HUD, splash and results cards behave the same on every screen.
    /// </summary>
    public static class ResponsiveLayout
    {
        private static Rect _guiSafeArea;
        private static Rect _rawSafeArea;
        private static int _screenWidth;
        private static int _screenHeight;
        private static bool _initialised;

        /// <summary>Recomputes the cached safe area. Cheap to call every OnGUI frame.</summary>
        public static void Refresh()
        {
            int w = Mathf.Max(1, Screen.width);
            int h = Mathf.Max(1, Screen.height);
            Rect raw = Screen.safeArea;

            if (_initialised && w == _screenWidth && h == _screenHeight && raw == _rawSafeArea)
                return;

            _screenWidth = w;
            _screenHeight = h;
            _rawSafeArea = raw;
            _initialised = true;

            // WebGL and some embedded browsers report an empty safe area; treat that as full screen.
            if (raw.width <= 1f || raw.height <= 1f)
            {
                _guiSafeArea = new Rect(0f, 0f, w, h);
                return;
            }

            float x = Mathf.Clamp(raw.x, 0f, w - 1f);
            float y = Mathf.Clamp(raw.y, 0f, h - 1f);
            float width = Mathf.Clamp(raw.width, 1f, w - x);
            float height = Mathf.Clamp(raw.height, 1f, h - y);

            // Flip the y axis: the top inset becomes the GUI y offset.
            _guiSafeArea = new Rect(x, h - (y + height), width, height);
        }

        /// <summary>Usable area in GUI coordinates (origin top-left).</summary>
        public static Rect SafeAreaGui
        {
            get { Refresh(); return _guiSafeArea; }
        }

        /// <summary>The whole window in GUI coordinates.</summary>
        public static Rect ScreenGui
        {
            get { Refresh(); return new Rect(0f, 0f, _screenWidth, _screenHeight); }
        }

        /// <summary>
        /// Largest uniform scale at which a design-sized card (designWidth x designHeight)
        /// still fits inside the safe area with the given pixel margin.
        /// </summary>
        public static float FitScale(float designWidth, float designHeight, float margin, float minScale, float maxScale)
        {
            Rect safe = SafeAreaGui;
            float availableW = Mathf.Max(1f, safe.width - margin * 2f);
            float availableH = Mathf.Max(1f, safe.height - margin * 2f);

            float scale = Mathf.Min(
                availableW / Mathf.Max(1f, designWidth),
                availableH / Mathf.Max(1f, designHeight));

            return Mathf.Clamp(scale, minScale, maxScale);
        }

        /// <summary>Centres a box of the given GUI size inside the safe area.</summary>
        public static Rect Centered(float width, float height)
        {
            Rect safe = SafeAreaGui;
            return new Rect(
                safe.x + (safe.width - width) * 0.5f,
                safe.y + (safe.height - height) * 0.5f,
                width,
                height);
        }

        public static bool IsLandscape
        {
            get
            {
                Rect safe = SafeAreaGui;
                return safe.width > safe.height;
            }
        }
    }
}
