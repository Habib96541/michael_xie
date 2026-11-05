using UnityEngine;
using System.Collections.Generic;

namespace com.javierquevedo
{
    /// <summary>
    /// Procedurally generates crisp vector-style sprites and UI textures for Cyber Orbs:
    /// 1. Six Cyber Energy Orbs with distinct internal crystalline facets and geometric core glyphs:
    ///    - Electric Cyan (Blue): Sharp diamond core
    ///    - Plasma Magenta (Red): Double energy ring inside
    ///    - Toxic Lime (Green): Circuit-board geometric veins
    ///    - Solar Amber (Yellow): Fiery starburst center
    ///    - Void Violet (Black): Orbital pulsar swirl
    ///    - Chrome Ice Silver (White): Glowing titanium triangular rune
    /// 2. Playfield container backdrop (#0b0e14) with faint hexagonal grid & chamfered neon titanium borders
    /// 3. Railgun turret base, barrel with glowing blue heat vents, loading chamber, and laser dots
    /// 4. Pop shockwave ring, crystalline polygon shards, and spark flash VFX sprites
    /// 5. Glassmorphism HUD panels, power cell battery sprites, gold trophy emblem, and crisp vector icons
    /// </summary>
    public static class CyberArtGenerator
    {
        private static Dictionary<BubbleColor, Sprite> _orbSprites = new Dictionary<BubbleColor, Sprite>();
        private static Sprite _ghostOrbSprite;
        private static Sprite _shockwaveSprite;
        private static Sprite _shardSprite;
        private static Sprite _sparkSprite;
        private static Sprite _turretBaseSprite;
        private static Sprite _turretBarrelSprite;
        private static Sprite _chamberRingSprite;
        private static Sprite _hexBackdropSprite;
        private static Sprite _laserDotSprite;
        private static Texture2D _whiteTex;
        private static Dictionary<string, Texture2D> _uiTextures = new Dictionary<string, Texture2D>();
        private static Sprite _aimDotSprite;
        private static readonly Dictionary<string, Texture2D> _artTextures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _artSprites = new Dictionary<string, Sprite>();

        private const string ArtPath = "Art/Sprites/";

        /// <summary>
        /// Loads one of the hand-authored PNGs from Assets/Resources/Art/Sprites.
        /// Returns null when the file is missing so callers can fall back to procedural art.
        /// </summary>
        public static Texture2D LoadArtTexture(string name)
        {
            Texture2D tex;
            if (_artTextures.TryGetValue(name, out tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>(ArtPath + name);
            if (tex != null) _artTextures[name] = tex;
            return tex;
        }

        /// <summary>
        /// Wraps an authored PNG in a Sprite whose full width equals worldWidth units, so gameplay scale
        /// never depends on the texture resolution.
        /// </summary>
        private static Sprite LoadArtSprite(string name, float worldWidth, float pivotX = 0.5f, float pivotY = 0.5f, Vector4 border = default(Vector4))
        {
            string key = name + "|" + worldWidth + "|" + pivotX + "|" + pivotY + "|" + border;
            Sprite sprite;
            if (_artSprites.TryGetValue(key, out sprite) && sprite != null) return sprite;

            Texture2D tex = LoadArtTexture(name);
            if (tex == null) return null;

            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(pivotX, pivotY),
                tex.width / worldWidth, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            _artSprites[key] = sprite;
            return sprite;
        }

        /// <summary>Opaque full-screen backdrop (circuit traces and vignette). Sized 1 unit wide; ScreenBackdrop scales it to cover the camera.</summary>
        public static Sprite GetScreenBackgroundSprite()
        {
            return LoadArtSprite("background", 1f);
        }

        /// <summary>Opaque playfield window texture. Sized 1 unit wide; callers scale it to the board.</summary>
        public static Sprite GetBoardBackdropSprite()
        {
            return LoadArtSprite("board_backdrop", 1f);
        }

        /// <summary>
        /// Hollow chamfered steel frame, 9-sliced. At its native size the art is 8.64 units wide and the
        /// opening starts 0.243 units in from every edge (18 source px at 1280 px / 8.64 units).
        /// </summary>
        public const float BoardFrameInnerMargin = 0.243f;
        public static Sprite GetBoardFrameSprite()
        {
            return LoadArtSprite("board_frame", 8.64f, 0.5f, 0.5f, new Vector4(80f, 80f, 80f, 80f));
        }

        /// <summary>1x1 unit solid white sprite for tinted bars and lines.</summary>
        public static Sprite GetWhiteSprite()
        {
            Sprite art = LoadArtSprite("px_white", 1f);
            if (art != null) return art;
            return LoadFallbackWhiteSprite();
        }

        private static Sprite _fallbackWhiteSprite;
        private static Sprite LoadFallbackWhiteSprite()
        {
            if (_fallbackWhiteSprite == null)
            {
                Texture2D tex = WhiteTexture;
                _fallbackWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return _fallbackWhiteSprite;
        }

        /// <summary>Shield cell, 0.30 units wide (art is 1:2, so 0.60 tall).</summary>
        public static Sprite GetBatteryCellSprite(bool charged)
        {
            return LoadArtSprite(charged ? "battery_full" : "battery_empty", 0.30f);
        }

        private static string GetOrbArtName(BubbleColor color)
        {
            switch (color)
            {
                case BubbleColor.Red:    return "orb_fire";
                case BubbleColor.Black:  return "orb_void";
                case BubbleColor.Green:  return "orb_circuit";
                case BubbleColor.Blue:   return "orb_storm";
                case BubbleColor.Yellow: return "orb_solar";
                case BubbleColor.White:  return "orb_ice";
                default:                 return "orb_storm";
            }
        }

        // Accent colors sampled from each authored orb sprite (used for VFX, aim line and chamber glow)
        public static Color GetPrimaryColor(BubbleColor color)
        {
            switch (color)
            {
                case BubbleColor.Red:    return new Color(1.00f, 0.58f, 0.16f, 1f); // Fire core
                case BubbleColor.Blue:   return new Color(0.30f, 0.62f, 1.00f, 1f); // Storm
                case BubbleColor.Green:  return new Color(0.48f, 0.90f, 0.32f, 1f); // Circuit
                case BubbleColor.Yellow: return new Color(1.00f, 0.82f, 0.28f, 1f); // Solar network
                case BubbleColor.Black:  return new Color(0.68f, 0.44f, 0.96f, 1f); // Void
                case BubbleColor.White:  return new Color(0.46f, 0.86f, 0.94f, 1f); // Ice crystal
                default:                 return new Color(0.30f, 0.62f, 1.00f, 1f);
            }
        }

        public static Color GetCoreColor(BubbleColor color)
        {
            switch (color)
            {
                case BubbleColor.Red:    return new Color(1.00f, 0.88f, 0.55f, 1f);
                case BubbleColor.Blue:   return new Color(0.80f, 0.93f, 1.00f, 1f);
                case BubbleColor.Green:  return new Color(0.80f, 0.98f, 0.62f, 1f);
                case BubbleColor.Yellow: return new Color(1.00f, 0.95f, 0.68f, 1f);
                case BubbleColor.Black:  return new Color(0.86f, 0.74f, 1.00f, 1f);
                case BubbleColor.White:  return new Color(0.84f, 0.98f, 1.00f, 1f);
                default:                 return Color.white;
            }
        }

        public static string GetOrbName(BubbleColor color)
        {
            switch (color)
            {
                case BubbleColor.Red:    return "FIRE CORE";
                case BubbleColor.Blue:   return "STORM";
                case BubbleColor.Green:  return "CIRCUIT";
                case BubbleColor.Yellow: return "SOLAR NET";
                case BubbleColor.Black:  return "VOID";
                case BubbleColor.White:  return "CRYSTAL";
                default:                 return "ORB";
            }
        }

        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _whiteTex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        public static Sprite GetOrbSprite(BubbleColor color)
        {
            if (_orbSprites.ContainsKey(color) && _orbSprites[color] != null)
                return _orbSprites[color];

            // The art has a dark ink outline and ~8% transparent margin: 1.04 world width puts a
            // 0.96-unit visible orb on the 1-unit grid, leaving a thin gap between neighbours.
            Sprite authored = LoadArtSprite(GetOrbArtName(color), 1.04f);
            if (authored != null)
            {
                _orbSprites[color] = authored;
                return authored;
            }

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color primary = GetPrimaryColor(color);
            Color coreCol = GetCoreColor(color);
            Color deepCol = Color.Lerp(primary, new Color(0.03f, 0.04f, 0.08f, 1f), 0.72f);

            float center = (size - 1) * 0.5f;
            float maxR = size * 0.46f; // Leave slight outer margin for neon glow

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float r = dist / maxR; // Normalized radius: 0..1 inside sphere, >1 outer glow

                    if (r > 1.08f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Outer neon halo (1.0 .. 1.08)
                    if (r > 0.96f)
                    {
                        float haloT = Mathf.Clamp01(1f - (r - 0.96f) / 0.12f);
                        Color halo = primary;
                        halo.a = haloT * haloT * 0.85f;
                        pixels[y * size + x] = halo;
                        continue;
                    }

                    float nx = dx / maxR;
                    float ny = dy / maxR;
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                    float angle = Mathf.Atan2(ny, nx);

                    // Base dark obsidian/crystalline interior gradient
                    Color col = Color.Lerp(deepCol, primary, Mathf.Pow(r, 1.5f) * 0.65f + (1f - r) * 0.25f);

                    // Internal crystalline facets (low-poly geometric facet lines)
                    float facetAngle = Mathf.Repeat(angle + Mathf.PI, Mathf.PI / 3f) - (Mathf.PI / 6f);
                    float facetCoord = r * Mathf.Cos(facetAngle);
                    float facetLine1 = Mathf.Abs(facetCoord - 0.58f);
                    float facetLine2 = Mathf.Abs(facetCoord - 0.34f);
                    float radialSpoke = Mathf.Abs(Mathf.Sin(angle * 3f));
                    float facetHighlight = 0f;
                    if (facetLine1 < 0.025f) facetHighlight += (1f - facetLine1 / 0.025f) * 0.35f;
                    if (facetLine2 < 0.022f) facetHighlight += (1f - facetLine2 / 0.022f) * 0.28f;
                    if (radialSpoke < 0.04f && r > 0.25f && r < 0.85f) facetHighlight += (1f - radialSpoke / 0.04f) * 0.22f;

                    // Alternate facet face shading
                    int sector = Mathf.FloorToInt(((angle + Mathf.PI) / (2f * Mathf.PI)) * 6f) % 2;
                    if (sector == 0 && r > 0.3f && r < 0.82f)
                        col = Color.Lerp(col, primary, 0.14f);

                    col = Color.Lerp(col, coreCol, Mathf.Clamp01(facetHighlight));

                    // Distinct geometric core glyph per color variation
                    float glyphIntensity = EvaluateCoreGlyph(color, nx, ny, r, angle);
                    if (glyphIntensity > 0f)
                    {
                        Color glyphCol = Color.Lerp(primary, Color.white, Mathf.Clamp01(glyphIntensity * 0.85f));
                        col = Color.Lerp(col, glyphCol, Mathf.Clamp01(glyphIntensity));
                    }

                    // Crisp outer neon rim (0.86 .. 0.96)
                    if (r > 0.84f)
                    {
                        float rimT = 1f - Mathf.Abs(r - 0.91f) / 0.07f;
                        if (rimT > 0f)
                        {
                            Color rimCol = Color.Lerp(primary, Color.white, 0.55f);
                            col = Color.Lerp(col, rimCol, rimT * 0.92f);
                        }
                    }

                    // Top-left crisp vector specular arc
                    float specDist = Mathf.Sqrt((nx + 0.32f) * (nx + 0.32f) + (ny - 0.36f) * (ny - 0.36f));
                    if (specDist < 0.28f)
                    {
                        float spec = Mathf.Pow(1f - specDist / 0.28f, 2.2f) * 0.7f;
                        col = Color.Lerp(col, Color.white, spec);
                    }

                    // Subtle bottom-right bounce light
                    float bounce = Mathf.Clamp01((nx * 0.5f - ny * 0.5f) - 0.15f) * (1f - nz) * 0.45f;
                    col = Color.Lerp(col, coreCol, bounce);

                    col.a = 1f;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 0.92f);
            _orbSprites[color] = sprite;
            return sprite;
        }

        private static float EvaluateCoreGlyph(BubbleColor color, float nx, float ny, float r, float angle)
        {
            float intensity = 0f;
            switch (color)
            {
                case BubbleColor.Blue:
                {
                    // 1. Electric Cyan: Sharp diamond core + inner diamond outline
                    float manhattan = Mathf.Abs(nx) * 1.15f + Mathf.Abs(ny) * 0.95f;
                    if (manhattan < 0.26f)
                        intensity = Mathf.Clamp01((0.26f - manhattan) / 0.12f) * 0.95f;
                    float outerDiamond = Mathf.Abs(manhattan - 0.44f);
                    if (outerDiamond < 0.045f)
                        intensity = Mathf.Max(intensity, (1f - outerDiamond / 0.045f) * 0.9f);
                    // Crosshair ticks
                    if ((Mathf.Abs(nx) < 0.03f || Mathf.Abs(ny) < 0.03f) && r > 0.36f && r < 0.62f)
                        intensity = Mathf.Max(intensity, 0.65f);
                    break;
                }
                case BubbleColor.Red:
                {
                    // 2. Plasma Magenta: Double energy ring inside + glowing center pip
                    float ring1 = Mathf.Abs(r - 0.26f);
                    float ring2 = Mathf.Abs(r - 0.48f);
                    if (ring1 < 0.05f)
                        intensity = Mathf.Max(intensity, (1f - ring1 / 0.05f) * 0.95f);
                    if (ring2 < 0.045f)
                        intensity = Mathf.Max(intensity, (1f - ring2 / 0.045f) * 0.9f);
                    if (r < 0.11f)
                        intensity = Mathf.Max(intensity, (1f - r / 0.11f));
                    break;
                }
                case BubbleColor.Green:
                {
                    // 3. Toxic Lime: Circuit-board geometric veins + central microchip square
                    float maxAxis = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    if (maxAxis < 0.18f)
                        intensity = 0.95f;
                    else if (Mathf.Abs(maxAxis - 0.26f) < 0.035f)
                        intensity = 0.85f;

                    // Orthogonal & 45-degree circuit traces with node pads
                    float traceOrth = Mathf.Min(Mathf.Abs(nx), Mathf.Abs(ny));
                    if (traceOrth < 0.035f && r > 0.24f && r < 0.64f)
                        intensity = Mathf.Max(intensity, 0.85f);

                    float diag = Mathf.Abs(Mathf.Abs(nx) - Mathf.Abs(ny));
                    if (diag < 0.035f && r > 0.26f && r < 0.56f)
                        intensity = Mathf.Max(intensity, 0.75f);

                    // Circuit node pads at cardinal & diagonal ends
                    if (Mathf.Abs(r - 0.60f) < 0.06f && (traceOrth < 0.08f || diag < 0.07f))
                        intensity = Mathf.Max(intensity, 0.92f);
                    break;
                }
                case BubbleColor.Yellow:
                {
                    // 4. Solar Amber: Fiery 8-point starburst center
                    float rays8 = Mathf.Abs(Mathf.Cos(angle * 4f));
                    float starReach = 0.18f + 0.42f * Mathf.Pow(rays8, 3.0f);
                    if (r < starReach)
                    {
                        float t = 1f - (r / starReach);
                        intensity = Mathf.Clamp01(t * 1.35f);
                    }
                    if (r < 0.20f)
                        intensity = Mathf.Max(intensity, (1f - r / 0.20f));
                    break;
                }
                case BubbleColor.Black:
                {
                    // 5. Void Violet: Orbital pulsar swirl (3-arm logarithmic spiral + bright singularity core)
                    if (r < 0.16f)
                        intensity = 1f - (r / 0.16f) * 0.3f;
                    if (r > 0.10f && r < 0.68f)
                    {
                        float spiralPhase = angle * 3f - r * 9.5f;
                        float arm = Mathf.Cos(spiralPhase);
                        if (arm > 0.45f)
                        {
                            float armT = (arm - 0.45f) / 0.55f;
                            float radialFade = 1f - Mathf.Clamp01((r - 0.15f) / 0.53f);
                            intensity = Mathf.Max(intensity, armT * radialFade * 0.95f);
                        }
                    }
                    break;
                }
                case BubbleColor.White:
                {
                    // 6. Chrome Ice Silver: Glowing titanium upward triangular rune
                    // Equilateral triangle centered around (0, -0.04)
                    float ty = ny + 0.06f;
                    float dTop = ty - 0.46f;
                    float dLeft = -0.866f * nx - 0.5f * ty - 0.23f;
                    float dRight = 0.866f * nx - 0.5f * ty - 0.23f;
                    float triDist = Mathf.Max(dTop, Mathf.Max(dLeft, dRight));

                    // Outer triangle rune border
                    if (Mathf.Abs(triDist) < 0.045f && triDist < 0.045f)
                        intensity = (1f - Mathf.Abs(triDist) / 0.045f) * 0.98f;
                    // Inner solid triangle core
                    if (triDist < -0.11f)
                        intensity = Mathf.Max(intensity, 0.88f);
                    break;
                }
            }
            return intensity;
        }

        public static Sprite GetGhostOrbSprite()
        {
            if (_ghostOrbSprite != null) return _ghostOrbSprite;
            _ghostOrbSprite = LoadArtSprite("orb_ghost", 1.04f);
            if (_ghostOrbSprite != null) return _ghostOrbSprite;
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxR = size * 0.45f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                    float angle = Mathf.Atan2(dy, dx);
                    float a = 0f;

                    // Segmented holographic ring + crosshair
                    if (Mathf.Abs(r - 0.90f) < 0.06f)
                    {
                        float dash = Mathf.Cos(angle * 8f);
                        if (dash > -0.1f)
                            a = (1f - Mathf.Abs(r - 0.90f) / 0.06f) * 0.85f;
                    }
                    else if (r < 0.90f)
                    {
                        a = (1f - r) * 0.18f;
                    }
                    if (Mathf.Abs(r - 0.22f) < 0.04f)
                        a = Mathf.Max(a, 0.65f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _ghostOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 0.92f);
            return _ghostOrbSprite;
        }

        public static Sprite GetShockwaveSprite()
        {
            if (_shockwaveSprite != null) return _shockwaveSprite;
            _shockwaveSprite = LoadArtSprite("vfx_ring", 1.3f);
            if (_shockwaveSprite != null) return _shockwaveSprite;
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxR = size * 0.48f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / maxR;
                    float ring = 1f - Mathf.Abs(r - 0.82f) / 0.16f;
                    float inner = r < 0.82f ? Mathf.Pow(r / 0.82f, 3f) * 0.35f : 0f;
                    float a = Mathf.Clamp01(Mathf.Max(ring, inner));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _shockwaveSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _shockwaveSprite;
        }

        public static Sprite GetShardSprite()
        {
            if (_shardSprite != null) return _shardSprite;
            _shardSprite = LoadArtSprite("vfx_shard", 0.5f);
            if (_shardSprite != null) return _shardSprite;
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / (size * 0.45f);
                    float ny = (y - center) / (size * 0.45f);
                    // Elongated crystalline diamond shard
                    float d = Mathf.Abs(nx) * 1.8f + Mathf.Abs(ny) * 0.85f;
                    float a = Mathf.Clamp01((1f - d) * 2.5f);
                    Color c = Color.Lerp(new Color(0.85f, 0.96f, 1f, a), Color.white, Mathf.Clamp01(1f - d * 1.4f));
                    c.a = a;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _shardSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _shardSprite;
        }

        public static Sprite GetAimDotSprite()
        {
            if (_aimDotSprite != null) return _aimDotSprite;
            _aimDotSprite = LoadArtSprite("aim_dot", 1f);
            return _aimDotSprite != null ? _aimDotSprite : GetSparkSprite();
        }

        public static Sprite GetSparkSprite()
        {
            if (_sparkSprite != null) return _sparkSprite;
            _sparkSprite = LoadArtSprite("vfx_spark", 1f);
            if (_sparkSprite != null) return _sparkSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float cross = Mathf.Max(
                        Mathf.Clamp01(1f - Mathf.Abs(nx) * 6f) * Mathf.Clamp01(1f - Mathf.Abs(ny) * 1.2f),
                        Mathf.Clamp01(1f - Mathf.Abs(ny) * 6f) * Mathf.Clamp01(1f - Mathf.Abs(nx) * 1.2f)
                    );
                    float core = Mathf.Clamp01(1f - r * 2.2f);
                    float a = Mathf.Clamp01(cross + core);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _sparkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return _sparkSprite;
        }

        public static Sprite GetTurretBaseSprite()
        {
            if (_turretBaseSprite != null) return _turretBaseSprite;
            _turretBaseSprite = LoadArtSprite("turret_base", 1.5f);
            if (_turretBaseSprite != null) return _turretBaseSprite;
            const int w = 160;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color darkAlloy = new Color(0.09f, 0.12f, 0.17f, 1f);
            Color midAlloy = new Color(0.16f, 0.21f, 0.29f, 1f);
            Color edgeTitanium = new Color(0.30f, 0.39f, 0.52f, 1f);
            Color cyanVent = new Color(0.0f, 0.92f, 1.0f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.5f); // -1..1
                    float ny = (y - h * 0.42f) / (h * 0.5f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);

                    // Octagonal/beveled aerospace turret housing
                    float oct = Mathf.Max(Mathf.Abs(nx) * 0.85f + Mathf.Abs(ny) * 0.45f, Mathf.Max(Mathf.Abs(nx) * 0.92f, Mathf.Abs(ny) * 0.95f));
                    if (oct > 0.82f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = Color.Lerp(midAlloy, darkAlloy, oct / 0.82f);

                    // Beveled titanium outer rim
                    if (oct > 0.72f)
                        c = Color.Lerp(c, edgeTitanium, (oct - 0.72f) / 0.10f);

                    // Glowing blue heat dissipation vents on left & right wings
                    if (Mathf.Abs(nx) > 0.42f && Mathf.Abs(nx) < 0.68f && Mathf.Abs(ny) < 0.38f)
                    {
                        float ventStripe = Mathf.Repeat((ny + 0.4f) * 14f, 1f);
                        if (ventStripe < 0.45f)
                            c = Color.Lerp(c, cyanVent, 0.88f);
                    }

                    // Circular loading chamber rim
                    if (Mathf.Abs(r - 0.48f) < 0.045f)
                        c = Color.Lerp(c, cyanVent, (1f - Mathf.Abs(r - 0.48f) / 0.045f) * 0.9f);

                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _turretBaseSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.42f), 85f);
            return _turretBaseSprite;
        }

        public static Sprite GetTurretBarrelSprite()
        {
            if (_turretBarrelSprite != null) return _turretBarrelSprite;
            // Barrel art points up; pivot sits at the bottom centre so it swivels around the base.
            _turretBarrelSprite = LoadArtSprite("turret_barrel", 0.78f, 0.5f, 0f);
            if (_turretBarrelSprite != null) return _turretBarrelSprite;
            const int w = 96;
            const int h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color darkBarrel = new Color(0.07f, 0.10f, 0.15f, 1f);
            Color titanium = new Color(0.24f, 0.32f, 0.42f, 1f);
            Color cyanGlow = new Color(0.0f, 0.94f, 1.0f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.5f)); // 0..1 from center axis
                    float ny = (float)y / h; // 0 (pivot) .. 1 (muzzle tip)

                    // Sleek dual-prong railgun barrel profile
                    float halfWidth = Mathf.Lerp(0.52f, 0.34f, ny);
                    if (ny < 0.15f || nx > halfWidth)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // Center energy channel gap near muzzle
                    if (ny > 0.55f && nx < 0.10f)
                    {
                        float coreGlow = 1f - (nx / 0.10f);
                        pixels[y * w + x] = new Color(0f, 0.95f, 1f, coreGlow * 0.75f);
                        continue;
                    }

                    Color c = Color.Lerp(titanium, darkBarrel, nx / halfWidth);

                    // Glowing cyan rail strips along the inner barrel rails
                    if (Mathf.Abs(nx - 0.16f) < 0.045f && ny > 0.25f && ny < 0.94f)
                        c = Color.Lerp(c, cyanGlow, (1f - Mathf.Abs(nx - 0.16f) / 0.045f) * 0.95f);

                    // Heat dissipation vents along outer barrel
                    if (nx > halfWidth - 0.14f && nx < halfWidth - 0.04f && ny > 0.32f && ny < 0.78f)
                    {
                        if (Mathf.Repeat(ny * 18f, 1f) < 0.45f)
                            c = Color.Lerp(c, cyanGlow, 0.8f);
                    }

                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _turretBarrelSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.18f), 78f);
            return _turretBarrelSprite;
        }

        public static Sprite GetChamberRingSprite()
        {
            if (_chamberRingSprite != null) return _chamberRingSprite;
            _chamberRingSprite = LoadArtSprite("chamber_ring", 1.5f);
            if (_chamberRingSprite != null) return _chamberRingSprite;
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxR = size * 0.47f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                    if (r > 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    Color c = new Color(0.05f, 0.08f, 0.13f, 0.85f);
                    if (r > 0.82f)
                    {
                        float rim = 1f - Mathf.Abs(r - 0.90f) / 0.08f;
                        c = Color.Lerp(new Color(0.18f, 0.26f, 0.36f, 0.95f), new Color(0f, 0.92f, 1f, 1f), Mathf.Clamp01(rim));
                    }
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _chamberRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
            return _chamberRingSprite;
        }

        public static Sprite GetHexBackdropSprite()
        {
            if (_hexBackdropSprite != null) return _hexBackdropSprite;
            const int w = 256;
            const int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            // Deep charcoal semi-translucent backdrop (#0b0e14) with faint hexagonal grid & circuit traces
            Color baseBg = new Color(0.043f, 0.055f, 0.078f, 0.96f);
            Color hexLine = new Color(0.0f, 0.85f, 1.0f, 0.08f);
            Color vignette = new Color(0.02f, 0.025f, 0.04f, 0.98f);

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;
                    float edgeDist = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    Color c = Color.Lerp(vignette, baseBg, Mathf.Clamp01(edgeDist * 4.5f));

                    // Procedural hexagonal grid pattern
                    float gx = x / 22f;
                    float gy = y / 22f;
                    float q = gx * 1.1547f;
                    float r = gy - gx * 0.57735f;
                    float fq = Mathf.Abs(q - Mathf.Round(q));
                    float fr = Mathf.Abs(r - Mathf.Round(r));
                    float fs = Mathf.Abs((-q - r) - Mathf.Round(-q - r));
                    float hexEdge = Mathf.Min(Mathf.Min(fq, fr), fs);

                    if (hexEdge < 0.065f)
                    {
                        float lineT = 1f - (hexEdge / 0.065f);
                        c = Color.Lerp(c, new Color(0.0f, 0.78f, 1.0f, 0.96f), lineT * 0.09f);
                    }

                    // Faint horizontal scanlines
                    if (y % 4 == 0)
                        c = Color.Lerp(c, Color.black, 0.10f);

                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _hexBackdropSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 256f);
            return _hexBackdropSprite;
        }

        public static Texture2D GetGlassPanelTexture(string key, Color fill, Color border, int borderPx = 2)
        {
            string fullKey = key + "_" + borderPx;
            if (_uiTextures.ContainsKey(fullKey) && _uiTextures[fullKey] != null)
                return _uiTextures[fullKey];

            const int s = 64;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[s * s];
            const int cornerRadius = 10;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = x < cornerRadius ? cornerRadius - x : (x >= s - cornerRadius ? x - (s - cornerRadius - 1) : 0);
                    int dy = y < cornerRadius ? cornerRadius - y : (y >= s - cornerRadius ? y - (s - cornerRadius - 1) : 0);
                    float cornerDist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (cornerDist > cornerRadius + 0.5f)
                    {
                        pixels[y * s + x] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x < borderPx || x >= s - borderPx || y < borderPx || y >= s - borderPx)
                                    || (cornerDist >= cornerRadius - borderPx);

                    // Subtle top glass sheen
                    float sheen = Mathf.Clamp01((float)(y - s * 0.5f) / (s * 0.5f)) * 0.08f;
                    Color bodyCol = new Color(
                        Mathf.Clamp01(fill.r + sheen),
                        Mathf.Clamp01(fill.g + sheen * 1.2f),
                        Mathf.Clamp01(fill.b + sheen * 1.5f),
                        fill.a
                    );

                    pixels[y * s + x] = isBorder ? border : bodyCol;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _uiTextures[fullKey] = tex;
            return tex;
        }

        public static Texture2D GetCircleButtonTexture(string key, Color fill, Color rimColor)
        {
            if (_uiTextures.ContainsKey(key) && _uiTextures[key] != null)
                return _uiTextures[key];

            const int s = 64;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float center = (s - 1) * 0.5f;
            float maxR = s * 0.46f;
            Color[] pixels = new Color[s * s];

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / maxR;
                    if (r > 1.04f)
                    {
                        pixels[y * s + x] = Color.clear;
                        continue;
                    }
                    if (r > 0.88f)
                    {
                        float rimT = 1f - Mathf.Abs(r - 0.95f) / 0.09f;
                        Color rc = rimColor;
                        rc.a = Mathf.Clamp01(rimT);
                        pixels[y * s + x] = rc;
                    }
                    else
                    {
                        pixels[y * s + x] = fill;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _uiTextures[key] = tex;
            return tex;
        }
    }
}
