using System;
using System.IO;
using System.Text;
using DiceGame.Audio;
using DiceGame.Config;
using DiceGame.Core;
using DiceGame.Effects;
using DiceGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceGame.Editor
{
    public static class DiceSceneBuilder
    {
        private static TMP_FontAsset _defaultFont;
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;
        private static Sprite _backgroundSprite;
        private static Sprite _panelSprite;
        private static Sprite _buttonSprite;
        private static Sprite _primaryButtonSprite;
        private static Sprite _logoSprite;
        private static Sprite _octagonSprite;
        private static Sprite _rollPadSprite;
        private static Sprite _sliderRingSprite;
        private static Sprite _tickerDotSprite;

        [MenuItem("DiceGame/Build Dice Overdrive Scene", false, 10)]
        public static void BuildScene()
        {
            EnsureTMPResources();

            GenerateUISprites();
            GenerateAudioAssets();

            LoadDefaultFont();

            string scenePath = "Assets/Scenes/DiceOverdrive.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();

            SetupEventSystem();

            DiceGameConfig config = SetupConfigAsset();

            GameObject canvasObj = CreateCanvas(out RectTransform canvasRect);
            GameObject shakeRootObj = CreateShakeRoot(canvasRect, out RectTransform shakeRoot);

            SetupBackground(canvasRect);
            shakeRootObj.transform.SetAsLastSibling();
            DiceStatsHeaderView statsHeader = SetupHeader(shakeRoot);
            DiceRoundTimerView timerView = SetupTimer(shakeRoot);
            VisualDiceRoller diceRoller = SetupDiceArena(shakeRoot);
            DiceHistoryView historyView = SetupHistoryBar(shakeRoot);
            DiceSliderView sliderView = SetupSlider(shakeRoot);
            DiceBetControlsView betControls = SetupBetControls(shakeRoot);
            DiceGameOverModalView gameOverModal = SetupGameOverModal(shakeRoot);

            FloatingTextController floatingText = SetupFloatingText(shakeRoot);
            ParticleFxController particleFx = SetupParticleSystems(shakeRoot);
            DiceAudioSynthesizer audioSynth = SetupAudioSynthesizer();
            ScreenShakeController screenShake = shakeRootObj.GetComponent<ScreenShakeController>();

            SetupGameManager(
                config,
                audioSynth,
                screenShake,
                floatingText,
                particleFx,
                diceRoller,
                statsHeader,
                timerView,
                sliderView,
                betControls,
                gameOverModal,
                historyView);

            DiceResponsiveLayout responsiveLayout = canvasObj.AddComponent<DiceResponsiveLayout>();
            SetPrivateField(responsiveLayout, "canvasScaler", canvasObj.GetComponent<CanvasScaler>());
            SetPrivateField(responsiveLayout, "safeAreaRoot", shakeRoot);
            SetPrivateField(responsiveLayout, "headerContainer", statsHeader.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "timerContainer", timerView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "arenaContainer", diceRoller.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "historyContainer", historyView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "sliderContainer", sliderView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "betControlsContainer", betControls.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "_modalContainer", gameOverModal.transform.Find("ModalCard") as RectTransform);

            responsiveLayout.AutoDiscoverReferences();
            responsiveLayout.ApplyLayout(force: true);

            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Transform rollControl = betControls.transform.Find("RollButton");
            if (eventSystem != null && rollControl != null)
            {
                eventSystem.firstSelectedGameObject = rollControl.gameObject;
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[DiceGame] Successfully built and saved Dice Overdrive scene at " + scenePath);

            AddSceneToBuildSettings(scenePath);
        }

        [MenuItem("DiceGame/Build All Scenes", false, 12)]
        public static void BuildAllScenes()
        {
            BuildScene();
            LoadingSceneBuilder.BuildScene();
        }

        private static void EnsureTMPResources()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                string packagePath = "Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Package Resources/TMP Essential Resources.unitypackage";
                if (!File.Exists(packagePath))
                {
                    string[] matches = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
                    if (matches.Length > 0)
                    {
                        packagePath = matches[0];
                    }
                }
                if (File.Exists(packagePath))
                {
                    AssetDatabase.ImportPackage(packagePath, false);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }

        private static void LoadDefaultFont()
        {
            _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_defaultFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }
        }

        public static void GenerateUISprites()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }

            string roundedPath = "Assets/Sprites/UIRoundedBox.png";
            if (!File.Exists(roundedPath))
            {
                int size = 64;
                int radius = 16;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color transparent = new Color(0, 0, 0, 0);
                Color white = Color.white;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int cx = (x < radius) ? radius : ((x >= size - radius) ? size - radius - 1 : x);
                        int cy = (y < radius) ? radius : ((y >= size - radius) ? size - radius - 1 : y);
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                        if (dist <= radius - 0.5f)
                            tex.SetPixel(x, y, white);
                        else if (dist <= radius + 0.5f)
                            tex.SetPixel(x, y, new Color(1, 1, 1, radius + 0.5f - dist));
                        else
                            tex.SetPixel(x, y, transparent);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(roundedPath, ImageConversion.EncodeToPNG(tex));
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(roundedPath, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = AssetImporter.GetAtPath(roundedPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteBorder = new Vector4(radius, radius, radius, radius);
                    importer.SaveAndReimport();
                }
            }
            _roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(roundedPath);

            string circlePath = "Assets/Sprites/UICircle.png";
            if (!File.Exists(circlePath))
            {
                int size = 64;
                float radius = size * 0.5f;
                Vector2 center = new Vector2(radius, radius);
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color transparent = new Color(0, 0, 0, 0);
                Color white = Color.white;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        if (dist <= radius - 0.75f)
                            tex.SetPixel(x, y, white);
                        else if (dist <= radius + 0.75f)
                            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius + 0.75f - dist)));
                        else
                            tex.SetPixel(x, y, transparent);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(circlePath, ImageConversion.EncodeToPNG(tex));
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(circlePath, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter importer = AssetImporter.GetAtPath(circlePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
            }
            _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);

            GeneratePremiumUIArt();
        }

        private static void GeneratePremiumUIArt()
        {
            const string folder = "Assets/Sprites";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Sprites");

            _backgroundSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceNeonBackground.png", 512, 1024, 0,
                (x, y) => CreateBackgroundPixel(x, y, 512, 1024));
            _panelSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceGlassPanel.png", 256, 144, 28,
                (x, y) => CreatePanelPixel(x, y, 256, 144, false));
            _buttonSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceButtonSurface.png", 256, 112, 26,
                (x, y) => CreatePanelPixel(x, y, 256, 112, false, true));
            _primaryButtonSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceButtonPrimary.png", 256, 112, 26,
                (x, y) => CreatePanelPixel(x, y, 256, 112, true));
            _logoSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceOverdriveLogo.png", 256, 256, 0,
                (x, y) => CreateLogoPixel(x, y, 256));
            _octagonSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceOctagonPanel.png", 256, 256, 36,
                (x, y) => CreateOctagonPixel(x, y, 256));
            _rollPadSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceRollPad.png", 512, 256, 0,
                (x, y) => CreateRollPadPixel(x, y, 512, 256));
            _sliderRingSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceSliderRing.png", 256, 256, 0,
                (x, y) => CreateRingPixel(x, y, 256));
            _tickerDotSprite = CreateSpriteAssetIfMissing(
                "Assets/Sprites/DiceTickerPill.png", 128, 64, 16,
                (x, y) => CreateTickerPixel(x, y, 128, 64));
        }

        private static Sprite CreateSpriteAssetIfMissing(string path, int width, int height, int border, Func<int, int, Color32> pixelFactory)
        {
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++) pixels[y * width + x] = pixelFactory(x, y);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color32 CreateBackgroundPixel(int x, int y, int width, int height)
        {
            float u = x / (float)(width - 1);
            float v = y / (float)(height - 1);
            Color color = Color.Lerp(new Color(0.025f, 0.045f, 0.09f), new Color(0.055f, 0.035f, 0.12f), v);

            color += Glow(u, v, 0.12f, 0.78f, 0.52f, new Color(0.0f, 0.28f, 0.43f));
            color += Glow(u, v, 0.94f, 0.28f, 0.47f, new Color(0.31f, 0.07f, 0.37f));
            color += Glow(u, v, 0.55f, 0.02f, 0.36f, new Color(0.14f, 0.12f, 0.38f));

            bool grid = (x % 48 == 0) || (y % 48 == 0);
            bool diagonal = ((x + y * 2) % 192) < 2;
            if (grid) color = Color.Lerp(color, new Color(0.22f, 0.47f, 0.61f), 0.075f);
            if (diagonal) color = Color.Lerp(color, new Color(0.15f, 0.31f, 0.55f), 0.085f);

            float vignette = Mathf.Clamp01(Mathf.Max(Mathf.Abs(u - 0.5f) * 1.2f, Mathf.Abs(v - 0.5f) * 0.9f));
            color *= 1.0f - vignette * 0.26f;
            return ToColor32(color, 255);
        }

        private static Color Glow(float u, float v, float cx, float cy, float radius, Color tint)
        {
            float dx = (u - cx) * 1.15f;
            float dy = (v - cy) * 0.82f;
            float strength = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Sqrt(dx * dx + dy * dy) / radius), 2.4f);
            return tint * (strength * 0.55f);
        }

        private static Color32 CreatePanelPixel(int x, int y, int width, int height, bool primary, bool darkButton = false)
        {
            float radius = Mathf.Min(height * 0.27f, 30.0f);
            float px = Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - radius);
            float py = Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - radius);
            float outside = Mathf.Sqrt(Mathf.Max(px, 0.0f) * Mathf.Max(px, 0.0f) + Mathf.Max(py, 0.0f) * Mathf.Max(py, 0.0f)) + Mathf.Min(Mathf.Max(px, py), 0.0f) - radius;
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(1.0f - Mathf.Max(0.0f, outside) * 0.9f) * 255.0f);
            if (outside > 1.0f) return new Color32(0, 0, 0, 0);

            float v = y / Mathf.Max(1.0f, height - 1.0f);
            Color color;
            if (primary)
            {
                color = Color.Lerp(new Color(0.20f, 0.96f, 0.65f), new Color(0.0f, 0.63f, 0.48f), v);
                if (outside > -2.0f) color = Color.Lerp(new Color(0.75f, 1.0f, 0.74f), color, 0.40f);
            }
            else if (darkButton)
            {
                color = Color.Lerp(new Color(0.11f, 0.23f, 0.36f), new Color(0.07f, 0.14f, 0.26f), v);
                if (outside > -2.0f) color = new Color(0.17f, 0.72f, 0.80f);
                else if (outside > -5.0f) color = Color.Lerp(color, new Color(0.17f, 0.62f, 0.75f), 0.16f);
            }
            else
            {
                color = Color.Lerp(new Color(0.075f, 0.14f, 0.25f), new Color(0.045f, 0.085f, 0.17f), v);
                if (outside > -2.0f) color = new Color(0.12f, 0.46f, 0.58f);
                else if (outside > -5.0f) color = Color.Lerp(color, new Color(0.10f, 0.34f, 0.46f), 0.18f);
            }

            if (x > 30 && x < width - 30 && y > height - 9) color = Color.Lerp(color, new Color(0.0f, 0.95f, 0.77f), 0.25f);
            return ToColor32(color, alpha);
        }

        private static Color32 CreateLogoPixel(int x, int y, int size)
        {
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float radius = size * 0.47f;
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > radius + 1.0f) return new Color32(0, 0, 0, 0);

            Color color = new Color(0.045f, 0.105f, 0.18f);
            if (dist > radius - 7.0f) color = new Color(0.98f, 0.73f, 0.28f);
            else if (dist > radius - 13.0f) color = new Color(0.08f, 0.84f, 0.82f);
            else if (dist > radius - 20.0f) color = new Color(0.08f, 0.18f, 0.29f);

            float dieHalf = size * 0.255f;
            float dieRadius = size * 0.07f;
            float qx = Mathf.Abs(dx) - (dieHalf - dieRadius);
            float qy = Mathf.Abs(dy) - (dieHalf - dieRadius);
            float dieDistance = Mathf.Sqrt(Mathf.Max(qx, 0.0f) * Mathf.Max(qx, 0.0f) + Mathf.Max(qy, 0.0f) * Mathf.Max(qy, 0.0f)) + Mathf.Min(Mathf.Max(qx, qy), 0.0f) - dieRadius;
            if (dieDistance <= 1.0f)
            {
                color = dieDistance > -3.0f ? new Color(0.34f, 0.98f, 0.84f) : new Color(0.84f, 0.97f, 0.96f);
                float pip = size * 0.034f;
                Vector2[] pips =
                {
                    new Vector2(-0.105f, -0.105f), new Vector2(0.105f, -0.105f),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(-0.105f, 0.105f), new Vector2(0.105f, 0.105f)
                };
                for (int i = 0; i < pips.Length; i++)
                {
                    float pdx = dx - pips[i].x * size;
                    float pdy = dy - pips[i].y * size;
                    if (pdx * pdx + pdy * pdy < pip * pip) color = new Color(0.025f, 0.12f, 0.19f);
                }
            }

            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 1.0f - dist) * 255.0f);
            return ToColor32(color, alpha);
        }

        private static Color32 CreateOctagonPixel(int x, int y, int size)
        {
            float cut = size * 0.12f;
            bool inside = true;
            if (x < cut && y < cut && (cut - x) + (cut - y) > cut) inside = false;
            if (x >= size - cut && y < cut && (x - (size - cut)) + (cut - y) > cut) inside = false;
            if (x < cut && y >= size - cut && (cut - x) + (y - (size - cut)) > cut) inside = false;
            if (x >= size - cut && y >= size - cut && (x - (size - cut)) + (y - (size - cut)) > cut) inside = false;
            if (!inside) return new Color32(0, 0, 0, 0);
            float edgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
            Color c;
            if (edgeDist < 6) c = new Color(0.12f, 0.16f, 0.20f, 1);
            else if (edgeDist < 10) c = new Color(0.0f, 0.85f, 0.95f, 0.9f);
            else if (edgeDist < 18) c = new Color(0.06f, 0.10f, 0.13f, 0.98f);
            else c = Color.white;
            return ToColor32(c, 255);
        }

        private static Color32 CreateRollPadPixel(int x, int y, int w, int h)
        {
            float nx = (x - w * 0.5f) / (w * 0.48f);
            float ny = (y - h * 0.5f) / (h * 0.48f);
            float d = nx * nx + ny * ny;
            if (d > 1.0f) return new Color32(0, 0, 0, 0);
            Color c;
            if (d > 0.92f) c = new Color(0.10f, 0.13f, 0.16f, 1);
            else if (d > 0.78f) c = Color.Lerp(new Color(0.18f, 0.22f, 0.26f), new Color(0.02f, 1.0f, 0.45f), 0.25f);
            else c = new Color(0.08f, 0.12f, 0.10f, 1);
            if (d > 0.6f && d < 0.78f && (x % 32 < 6) && Mathf.Abs(nx) > 0.3f) c = new Color(0.02f, 1.0f, 0.45f);
            return ToColor32(c, 255);
        }

        private static Color32 CreateRingPixel(int x, int y, int size)
        {
            float rc = size * 0.5f;
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(rc, rc));
            float r = dist / rc;
            if (r > 1.0f) return new Color32(0, 0, 0, 0);
            Color c;
            if (r > 0.95f) c = new Color(0.15f, 0.20f, 0.24f, 1);
            else if (r > 0.88f) c = new Color(0.0f, 0.92f, 1.0f, 1);
            else if (r > 0.72f) c = new Color(0.10f, 0.14f, 0.16f, 0.95f);
            else if (r > 0.68f) c = new Color(0.02f, 1.0f, 0.45f, 1);
            else if (r > 0.45f) c = new Color(0.07f, 0.10f, 0.12f, 0.98f);
            else if (r > 0.40f) c = new Color(0.0f, 0.9f, 0.6f, 1);
            else c = Color.white;
            return ToColor32(c, 255);
        }

        private static Color32 CreateTickerPixel(int x, int y, int w, int h)
        {
            bool inside = x >= 6 && x < w - 6 && y >= 4 && y < h - 4;
            if (!inside) return new Color32(0, 0, 0, 0);
            return ToColor32(Color.white, 255);
        }

        private static Color32 ToColor32(Color color, byte alpha)
        {
            return new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255.0f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255.0f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255.0f), alpha);
        }

        private static void GenerateAudioAssets()
        {
            const int sampleRate = 44100;
            const string audioFolder = "Assets/Audio";
            if (!AssetDatabase.IsValidFolder(audioFolder)) AssetDatabase.CreateFolder("Assets", "Audio");

            CreateAudioAssetIfMissing("Assets/Audio/DiceAmbientLoop.wav", CreateAmbientLoop(sampleRate), 2, sampleRate);
            CreateAudioAssetIfMissing("Assets/Audio/UI_Click.wav", CreateSoftClick(sampleRate), 1, sampleRate);
            CreateAudioAssetIfMissing("Assets/Audio/Bet_Chip.wav", CreateBetChipSound(sampleRate), 1, sampleRate);
        }

        private static float[] CreateSoftClick(int sampleRate)
        {
            int length = Mathf.RoundToInt(sampleRate * 0.085f);
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-t * 34.0f);
                float drop = Mathf.Lerp(920.0f, 560.0f, Mathf.Clamp01(t / 0.06f));
                float tone = Mathf.Sin(2.0f * Mathf.PI * drop * t) * 0.42f;
                float overtone = Mathf.Sin(2.0f * Mathf.PI * 1420.0f * t) * 0.12f;
                float transient = (i < 500 ? Mathf.Sin(2.0f * Mathf.PI * 1800.0f * t) * 0.16f : 0.0f);
                samples[i] = (tone + overtone + transient) * envelope;
            }
            return samples;
        }

        private static float[] CreateBetChipSound(int sampleRate)
        {
            int length = Mathf.RoundToInt(sampleRate * 0.16f);
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float clickEnvelope = Mathf.Exp(-t * 24.0f);
                float chipEnvelope = Mathf.Exp(-t * 15.0f);
                float chip = Mathf.Sin(2.0f * Mathf.PI * 630.0f * t) * 0.37f;
                float brightEdge = Mathf.Sin(2.0f * Mathf.PI * 1260.0f * t) * 0.18f;
                float tap = i < 360 ? Mathf.Sin(2.0f * Mathf.PI * 1560.0f * t) * clickEnvelope * 0.34f : 0.0f;
                samples[i] = (chip * chipEnvelope) + brightEdge * clickEnvelope * 0.32f + tap;
            }
            return samples;
        }

        private static float[] CreateAmbientLoop(int sampleRate)
        {
            const int seconds = 8;
            int frames = sampleRate * seconds;
            var samples = new float[frames * 2];
            float[][] chords =
            {
                new[] { 130.81f, 164.81f, 196.00f, 246.94f },
                new[] { 110.00f, 130.81f, 164.81f, 196.00f },
                new[] { 87.31f, 110.00f, 130.81f, 164.81f },
                new[] { 98.00f, 123.47f, 146.83f, 174.61f }
            };
            float[] arpeggio = { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 698.46f, 880.00f, 698.46f };

            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)sampleRate;
                int chordIndex = Mathf.FloorToInt(t / 2.0f) % chords.Length;
                float chordLocal = t % 2.0f;
                int nextIndex = (chordIndex + 1) % chords.Length;
                float blend = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(1.72f, 1.98f, chordLocal));
                float pad = 0.0f;
                for (int note = 0; note < 4; note++)
                {
                    float current = Mathf.Sin(2.0f * Mathf.PI * chords[chordIndex][note] * t + note * 0.12f);
                    float next = Mathf.Sin(2.0f * Mathf.PI * chords[nextIndex][note] * t + note * 0.12f);
                    pad += Mathf.Lerp(current, next, blend) * (note == 0 ? 0.13f : 0.085f);
                }

                float chordPulse = 0.90f + Mathf.Sin(chordLocal * Mathf.PI) * 0.10f;
                float bassFreq = chords[chordIndex][0] * 0.5f;
                float bass = Mathf.Sin(2.0f * Mathf.PI * bassFreq * t) * 0.11f;
                int arpIndex = Mathf.FloorToInt(t * 2.0f) % arpeggio.Length;
                float arpLocal = t % 0.5f;
                float arpEnv = Mathf.Exp(-arpLocal * 8.0f);
                float arp = (Mathf.Sin(2.0f * Mathf.PI * arpeggio[arpIndex] * t) + Mathf.Sin(4.0f * Mathf.PI * arpeggio[arpIndex] * t) * 0.18f) * arpEnv * 0.025f;
                float center = (pad * chordPulse + bass + arp) * 0.70f;

                float width = Mathf.Sin(2.0f * Mathf.PI * 392.0f * t + Mathf.Sin(t * 0.8f) * 0.18f) * 0.018f;
                samples[i * 2] = Mathf.Clamp(center + width, -0.8f, 0.8f);
                samples[i * 2 + 1] = Mathf.Clamp(center - width, -0.8f, 0.8f);
            }

            int fadeFrames = Mathf.RoundToInt(sampleRate * 0.12f);
            for (int i = 0; i < fadeFrames; i++)
            {
                float blend = i / (float)(fadeFrames - 1);
                int tailFrame = frames - fadeFrames + i;
                for (int channel = 0; channel < 2; channel++)
                    samples[tailFrame * 2 + channel] = Mathf.Lerp(samples[tailFrame * 2 + channel], samples[i * 2 + channel], blend);
            }
            return samples;
        }

        private static void CreateAudioAssetIfMissing(string path, float[] samples, int channels, int sampleRate)
        {
            if (!File.Exists(path)) WritePcmWave(path, samples, channels, sampleRate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void WritePcmWave(string path, float[] samples, int channels, int sampleRate)
        {
            int bytesPerSample = 2;
            int dataLength = samples.Length * bytesPerSample;
            using (var stream = new MemoryStream(44 + dataLength))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * bytesPerSample);
                writer.Write((short)(channels * bytesPerSample));
                writer.Write((short)(bytesPerSample * 8));
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);
                for (int i = 0; i < samples.Length; i++)
                {
                    short value = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767.0f), short.MinValue, short.MaxValue);
                    writer.Write(value);
                }
                writer.Flush();
                File.WriteAllBytes(path, stream.ToArray());
            }
        }

        private static void SetupCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1.0f);
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private static DiceGameConfig SetupConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            string configPath = "Assets/Settings/DiceGameConfig.asset";
            DiceGameConfig config = AssetDatabase.LoadAssetAtPath<DiceGameConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DiceGameConfig>();
                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        private static GameObject CreateCanvas(out RectTransform canvasRect)
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.0f;

            canvasObj.AddComponent<GraphicRaycaster>();
            canvasRect = canvasObj.GetComponent<RectTransform>();
            return canvasObj;
        }

        private static GameObject CreateShakeRoot(RectTransform parent, out RectTransform shakeRect)
        {
            GameObject shakeObj = new GameObject("ShakeRoot", typeof(RectTransform));
            shakeObj.transform.SetParent(parent, false);
            shakeRect = shakeObj.GetComponent<RectTransform>();
            shakeRect.anchorMin = Vector2.zero;
            shakeRect.anchorMax = Vector2.one;
            shakeRect.offsetMin = Vector2.zero;
            shakeRect.offsetMax = Vector2.zero;

            shakeObj.AddComponent<ScreenShakeController>();
            return shakeObj;
        }

        private static void SetupBackground(RectTransform parent)
        {
            GameObject bgObj = CreateUIElement("Background", parent);
            RectTransform rect = bgObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = bgObj.AddComponent<Image>();
            img.sprite = _backgroundSprite;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static DiceStatsHeaderView SetupHeader(RectTransform parent)
        {
            GameObject headerObj = CreateUIElement("StatsHeader", parent);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1.0f);
            headerRect.anchorMax = new Vector2(0.5f, 1.0f);
            headerRect.pivot = new Vector2(0.5f, 1.0f);
            headerRect.anchoredPosition = new Vector2(0.0f, -20.0f);
            headerRect.sizeDelta = new Vector2(1200.0f, 112.0f);

            Image bg = headerObj.AddComponent<Image>();
            bg.sprite = _panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            Outline headerOutline = headerObj.AddComponent<Outline>();
            headerOutline.effectColor = new Color(0.20f, 0.27f, 0.40f, 0.55f);
            headerOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject logoObj = CreateUIElement("LogoBadge", headerRect);
            RectTransform logoRect = logoObj.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.0f, 0.5f);
            logoRect.anchorMax = new Vector2(0.0f, 0.5f);
            logoRect.pivot = new Vector2(0.0f, 0.5f);
            logoRect.anchoredPosition = new Vector2(22.0f, 0.0f);
            logoRect.sizeDelta = new Vector2(58.0f, 58.0f);
            Image logoImage = logoObj.AddComponent<Image>();
            logoImage.sprite = _logoSprite;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            GameObject brandObj = CreateUIElement("BrandTitle", headerRect);
            RectTransform brandRect = brandObj.GetComponent<RectTransform>();
            brandRect.anchorMin = new Vector2(0.0f, 0.5f);
            brandRect.anchorMax = new Vector2(0.0f, 0.5f);
            brandRect.pivot = new Vector2(0.0f, 0.5f);
            brandRect.anchoredPosition = new Vector2(91.0f, 17.0f);
            brandRect.sizeDelta = new Vector2(270.0f, 34.0f);
            TextMeshProUGUI brandTitle = CreateText("BrandName", brandRect, "DICE", 32, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            brandTitle.color = new Color(0.98f, 0.86f, 0.60f, 1.0f);
            brandTitle.enableAutoSizing = true;
            brandTitle.fontSizeMin = 20.0f;
            brandTitle.fontSizeMax = 34.0f;

            GameObject subtitleObj = CreateUIElement("BrandSubtitle", headerRect);
            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.0f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.0f, 0.5f);
            subtitleRect.pivot = new Vector2(0.0f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(91.0f, -8.0f);
            subtitleRect.sizeDelta = new Vector2(270.0f, 24.0f);
            TextMeshProUGUI brandSub = CreateText("BrandSub", subtitleRect, "OVERDRIVE", 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            brandSub.color = new Color(0.23f, 0.91f, 0.88f, 1.0f);
            brandSub.characterSpacing = 2.0f;

            GameObject modeBtnObj = CreateUIElement("ModeBadgeButton", headerRect);
            RectTransform modeBtnRect = modeBtnObj.GetComponent<RectTransform>();
            modeBtnRect.anchorMin = new Vector2(0.0f, 0.5f);
            modeBtnRect.anchorMax = new Vector2(0.0f, 0.5f);
            modeBtnRect.pivot = new Vector2(0.0f, 0.5f);
            modeBtnRect.anchoredPosition = new Vector2(91.0f, -39.0f);
            modeBtnRect.sizeDelta = new Vector2(205.0f, 28.0f);

            Image modeBtnBg = modeBtnObj.AddComponent<Image>();
            modeBtnBg.sprite = _buttonSprite;
            modeBtnBg.type = Image.Type.Sliced;
            modeBtnBg.color = Color.white;
            Button modeBtn = modeBtnObj.AddComponent<Button>();
            AddButtonFeedback(modeBtn);

            TextMeshProUGUI modeText = CreateText("ModeText", modeBtnRect, "20s BLITZ", 19, FontStyles.Bold, TextAlignmentOptions.Center);
            modeText.color = new Color(0.0f, 0.9f, 1.0f, 1.0f);
            modeText.enableAutoSizing = true;
            modeText.fontSizeMin = 14.0f;
            modeText.fontSizeMax = 20.0f;

            GameObject bankrollObj = CreateUIElement("BankrollContainer", headerRect);
            RectTransform bankrollRect = bankrollObj.GetComponent<RectTransform>();
            bankrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            bankrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            bankrollRect.pivot = new Vector2(0.5f, 0.5f);
            bankrollRect.anchoredPosition = new Vector2(52.0f, 0.0f);
            bankrollRect.sizeDelta = new Vector2(350.0f, 90.0f);

            TextMeshProUGUI bankrollLabel = CreateText("BankrollLabel", bankrollRect, "YOUR BANKROLL", 18, FontStyles.Bold, TextAlignmentOptions.Top);
            bankrollLabel.rectTransform.anchoredPosition = new Vector2(0.0f, -5.0f);
            bankrollLabel.color = new Color(0.76f, 0.85f, 0.96f, 1.0f);

            TextMeshProUGUI bankrollVal = CreateText("BankrollValue", bankrollRect, "$1,000", 48, FontStyles.Bold, TextAlignmentOptions.Bottom);
            bankrollVal.rectTransform.anchoredPosition = new Vector2(0.0f, 2.0f);
            bankrollVal.color = new Color(0.22f, 1.0f, 0.70f, 1.0f);
            bankrollVal.enableAutoSizing = true;
            bankrollVal.fontSizeMin = 25.0f;
            bankrollVal.fontSizeMax = 48.0f;

            GameObject streakObj = CreateUIElement("StreakContainer", headerRect);
            RectTransform streakRect = streakObj.GetComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(1.0f, 0.5f);
            streakRect.anchorMax = new Vector2(1.0f, 0.5f);
            streakRect.pivot = new Vector2(1.0f, 0.5f);
            streakRect.anchoredPosition = new Vector2(-22.0f, 0.0f);
            streakRect.sizeDelta = new Vector2(290.0f, 74.0f);

            TextMeshProUGUI streakVal = CreateText("StreakValue", streakRect, "STREAK x3", 27, FontStyles.Bold, TextAlignmentOptions.TopRight);
            streakVal.rectTransform.anchoredPosition = new Vector2(0.0f, -2.0f);
            streakVal.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);
            streakVal.enableAutoSizing = true;
            streakVal.fontSizeMin = 17.0f;
            streakVal.fontSizeMax = 28.0f;

            GameObject feverBarBg = CreateUIElement("FeverBarBg", streakRect);
            RectTransform feverBarBgRect = feverBarBg.GetComponent<RectTransform>();
            feverBarBgRect.anchorMin = new Vector2(0.0f, 0.0f);
            feverBarBgRect.anchorMax = new Vector2(1.0f, 0.0f);
            feverBarBgRect.pivot = new Vector2(0.5f, 0.0f);
            feverBarBgRect.anchoredPosition = new Vector2(0.0f, 7.0f);
            feverBarBgRect.sizeDelta = new Vector2(0.0f, 14.0f);

            Image feverBgImg = feverBarBg.AddComponent<Image>();
            feverBgImg.sprite = _roundedSprite;
            feverBgImg.type = Image.Type.Sliced;
            feverBgImg.color = new Color(0.15f, 0.18f, 0.25f, 1.0f);

            GameObject feverFill = CreateUIElement("FeverFill", feverBarBgRect);
            RectTransform feverFillRect = feverFill.GetComponent<RectTransform>();
            feverFillRect.anchorMin = Vector2.zero;
            feverFillRect.anchorMax = Vector2.one;
            feverFillRect.offsetMin = Vector2.zero;
            feverFillRect.offsetMax = Vector2.zero;

            Image feverFillImg = feverFill.AddComponent<Image>();
            feverFillImg.sprite = _roundedSprite;
            feverFillImg.type = Image.Type.Filled;
            feverFillImg.fillMethod = Image.FillMethod.Horizontal;
            feverFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            feverFillImg.color = new Color(1.0f, 0.35f, 0.85f, 1.0f);

            GameObject feverBanner = CreateUIElement("FeverBanner", headerRect);
            RectTransform bannerRect = feverBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.0f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.0f);
            bannerRect.pivot = new Vector2(0.5f, 1.0f);
            bannerRect.anchoredPosition = new Vector2(0.0f, -4.0f);
            bannerRect.sizeDelta = new Vector2(350.0f, 36.0f);

            Image bannerBg = feverBanner.AddComponent<Image>();
            bannerBg.sprite = _buttonSprite;
            bannerBg.type = Image.Type.Sliced;
            bannerBg.color = new Color(1.0f, 0.15f, 0.65f, 0.98f);
            Outline bannerOutline = feverBanner.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(1.0f, 0.85f, 0.2f, 0.8f);
            bannerOutline.effectDistance = new Vector2(1.0f, -1.0f);

            TextMeshProUGUI bannerText = CreateText("BannerText", bannerRect, "FEVER SURGE! x1.5 WIN", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            bannerText.color = Color.white;
            bannerText.enableAutoSizing = true;
            bannerText.fontSizeMin = 15.0f;
            bannerText.fontSizeMax = 20.0f;

            DiceStatsHeaderView view = headerObj.AddComponent<DiceStatsHeaderView>();
            SetPrivateField(view, "bankrollText", bankrollVal);
            SetPrivateField(view, "bankrollContainer", bankrollRect);
            SetPrivateField(view, "streakText", streakVal);
            SetPrivateField(view, "streakContainer", streakRect);
            SetPrivateField(view, "feverMeterFill", feverFillImg);
            SetPrivateField(view, "feverActiveBanner", feverBanner);
            SetPrivateField(view, "modeBadgeText", modeText);
            SetPrivateField(view, "buttonModeSelect", modeBtn);

            return view;
        }

        private static DiceRoundTimerView SetupTimer(RectTransform parent)
        {
            GameObject timerObj = CreateUIElement("RoundTimer", parent);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1.0f);
            timerRect.anchorMax = new Vector2(0.5f, 1.0f);
            timerRect.pivot = new Vector2(0.5f, 1.0f);
            timerRect.anchoredPosition = new Vector2(0.0f, -125.0f);
            timerRect.sizeDelta = new Vector2(600.0f, 52.0f);

            Image bg = timerObj.AddComponent<Image>();
            bg.sprite = _panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            GameObject fillObj = CreateUIElement("TimerFill", timerRect);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3, 3);
            fillRect.offsetMax = new Vector2(-3, -3);

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = _roundedSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.0f, 0.85f, 1.0f, 1.0f);

            TextMeshProUGUI timerText = CreateText("TimerText", timerRect, "20.0s", 32, FontStyles.Bold, TextAlignmentOptions.Center);
            timerText.color = Color.white;

            GameObject urgency = CreateUIElement("UrgencyPulse", timerRect);
            RectTransform urgRect = urgency.GetComponent<RectTransform>();
            urgRect.anchorMin = Vector2.zero;
            urgRect.anchorMax = Vector2.one;
            urgRect.offsetMin = Vector2.zero;
            urgRect.offsetMax = Vector2.zero;

            Image urgImg = urgency.AddComponent<Image>();
            urgImg.sprite = _roundedSprite;
            urgImg.type = Image.Type.Sliced;
            urgImg.color = new Color(1.0f, 0.1f, 0.2f, 0.35f);

            DiceRoundTimerView view = timerObj.AddComponent<DiceRoundTimerView>();
            SetPrivateField(view, "timerFillBar", fillImg);
            SetPrivateField(view, "timerText", timerText);
            SetPrivateField(view, "timerContainer", timerRect);
            SetPrivateField(view, "urgencyOverlay", urgency);

            return view;
        }

        private static VisualDiceRoller SetupDiceArena(RectTransform parent)
        {
            GameObject arenaObj = CreateUIElement("DiceArena", parent);
            RectTransform arenaRect = arenaObj.GetComponent<RectTransform>();
            arenaRect.anchorMin = new Vector2(0.5f, 0.5f);
            arenaRect.anchorMax = new Vector2(0.5f, 0.5f);
            arenaRect.pivot = new Vector2(0.5f, 0.5f);
            arenaRect.anchoredPosition = new Vector2(0.0f, 155.0f);
            arenaRect.sizeDelta = new Vector2(650.0f, 230.0f);

            Image arenaBg = arenaObj.AddComponent<Image>();
            arenaBg.sprite = _panelSprite;
            arenaBg.type = Image.Type.Sliced;
            arenaBg.color = Color.white;
            Outline arenaOutline = arenaObj.AddComponent<Outline>();
            arenaOutline.effectColor = new Color(0.20f, 0.27f, 0.40f, 0.55f);
            arenaOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject shadowL = CreateUIElement("ShadowLeft", arenaRect);
            RectTransform shadowLRect = shadowL.GetComponent<RectTransform>();
            shadowLRect.anchoredPosition = new Vector2(-110.0f, -45.0f);
            shadowLRect.sizeDelta = new Vector2(140.0f, 30.0f);
            Image shadowLImg = shadowL.AddComponent<Image>();
            shadowLImg.sprite = _circleSprite;
            shadowLImg.color = new Color(0.0f, 0.0f, 0.0f, 0.45f);

            GameObject shadowR = CreateUIElement("ShadowRight", arenaRect);
            RectTransform shadowRRect = shadowR.GetComponent<RectTransform>();
            shadowRRect.anchoredPosition = new Vector2(110.0f, -45.0f);
            shadowRRect.sizeDelta = new Vector2(140.0f, 30.0f);
            Image shadowRImg = shadowR.AddComponent<Image>();
            shadowRImg.sprite = _circleSprite;
            shadowRImg.color = new Color(0.0f, 0.0f, 0.0f, 0.45f);

            GameObject dieL = CreateUIElement("DieLeft", arenaRect);
            RectTransform dieLRect = dieL.GetComponent<RectTransform>();
            dieLRect.anchoredPosition = new Vector2(-110.0f, 10.0f);
            dieLRect.sizeDelta = new Vector2(130.0f, 130.0f);

            Image dieLBg = dieL.AddComponent<Image>();
            dieLBg.sprite = _panelSprite;
            dieLBg.type = Image.Type.Sliced;
            dieLBg.color = new Color(0.88f, 0.97f, 1.0f, 1.0f);

            TextMeshProUGUI textL = CreateText("DigitL", dieLRect, "5", 72, FontStyles.Bold, TextAlignmentOptions.Center);
            textL.color = Color.white;

            GameObject dieR = CreateUIElement("DieRight", arenaRect);
            RectTransform dieRRect = dieR.GetComponent<RectTransform>();
            dieRRect.anchoredPosition = new Vector2(110.0f, 10.0f);
            dieRRect.sizeDelta = new Vector2(130.0f, 130.0f);

            Image dieRBg = dieR.AddComponent<Image>();
            dieRBg.sprite = _panelSprite;
            dieRBg.type = Image.Type.Sliced;
            dieRBg.color = new Color(0.78f, 1.0f, 0.91f, 1.0f);

            TextMeshProUGUI textR = CreateText("DigitR", dieRRect, "0", 72, FontStyles.Bold, TextAlignmentOptions.Center);
            textR.color = Color.white;

            GameObject resultObj = CreateUIElement("ResultNumberContainer", arenaRect);
            RectTransform resultRect = resultObj.GetComponent<RectTransform>();
            resultRect.anchoredPosition = new Vector2(0.0f, -75.0f);
            resultRect.sizeDelta = new Vector2(260.0f, 45.0f);

            TextMeshProUGUI resultText = CreateText("ResultNumberText", resultRect, "50.00", 32, FontStyles.Bold, TextAlignmentOptions.Center);
            resultText.color = new Color(0.35f, 1.0f, 0.78f, 1.0f);

            VisualDiceRoller roller = arenaObj.AddComponent<VisualDiceRoller>();
            SetPrivateField(roller, "dieLeft", dieLRect);
            SetPrivateField(roller, "dieRight", dieRRect);
            SetPrivateField(roller, "dieLeftBg", dieLBg);
            SetPrivateField(roller, "dieRightBg", dieRBg);
            SetPrivateField(roller, "dieLeftText", textL);
            SetPrivateField(roller, "dieRightText", textR);
            SetPrivateField(roller, "shadowLeft", shadowLRect);
            SetPrivateField(roller, "shadowRight", shadowRRect);
            SetPrivateField(roller, "rollResultNumberText", resultText);
            SetPrivateField(roller, "rollResultContainer", resultRect);

            return roller;
        }

        private static DiceHistoryView SetupHistoryBar(RectTransform parent)
        {
            GameObject barObj = CreateUIElement("HistoryBar", parent);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(0.0f, 20.0f);
            barRect.sizeDelta = new Vector2(900.0f, 44.0f);

            HorizontalLayoutGroup layout = barObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8.0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            DiceHistoryView.HistoryPill[] pills = new DiceHistoryView.HistoryPill[8];

            for (int i = 0; i < 8; i++)
            {
                GameObject pillObj = CreateUIElement("Pill_" + i, barRect);
                RectTransform pillRect = pillObj.GetComponent<RectTransform>();
                pillRect.sizeDelta = new Vector2(72.0f, 40.0f);

                Image bg = pillObj.AddComponent<Image>();
                bg.sprite = _buttonSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;

                TextMeshProUGUI val = CreateText("Val", pillRect, "50.0", 22, FontStyles.Bold, TextAlignmentOptions.Center);
                val.color = Color.white;
                val.enableAutoSizing = true;
                val.fontSizeMin = 16.0f;
                val.fontSizeMax = 22.0f;

                pills[i] = new DiceHistoryView.HistoryPill
                {
                    root = pillObj,
                    background = bg,
                    valueText = val
                };
            }

            DiceHistoryView view = barObj.AddComponent<DiceHistoryView>();
            SetPrivateField(view, "pills", pills);
            return view;
        }

        private static DiceSliderView SetupSlider(RectTransform parent)
        {
            GameObject container = CreateUIElement("SliderContainer", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0.0f, -80.0f);
            containerRect.sizeDelta = new Vector2(900.0f, 120.0f);

            Image containerBg = container.AddComponent<Image>();
            containerBg.sprite = _panelSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = Color.white;
            Outline sliderOutline = container.AddComponent<Outline>();
            sliderOutline.effectColor = new Color(0.20f, 0.27f, 0.40f, 0.55f);
            sliderOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject trackObj = CreateUIElement("Track", containerRect);
            RectTransform trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.05f, 0.5f);
            trackRect.anchorMax = new Vector2(0.95f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0.0f, -10.0f);
            trackRect.sizeDelta = new Vector2(0.0f, 28.0f);

            Image trackBg = trackObj.AddComponent<Image>();
            trackBg.sprite = _roundedSprite;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.15f, 0.18f, 0.25f, 1.0f);

            GameObject winZone = CreateUIElement("WinZone", trackRect);
            RectTransform winRect = winZone.GetComponent<RectTransform>();
            Image winImg = winZone.AddComponent<Image>();
            winImg.sprite = _roundedSprite;
            winImg.type = Image.Type.Sliced;
            winImg.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            GameObject lossZone = CreateUIElement("LossZone", trackRect);
            RectTransform lossRect = lossZone.GetComponent<RectTransform>();
            Image lossImg = lossZone.AddComponent<Image>();
            lossImg.sprite = _roundedSprite;
            lossImg.type = Image.Type.Sliced;
            lossImg.color = new Color(0.95f, 0.22f, 0.38f, 1.0f);

            GameObject critZone = CreateUIElement("CritZone", trackRect);
            RectTransform critRect = critZone.GetComponent<RectTransform>();
            Image critImg = critZone.AddComponent<Image>();
            critImg.sprite = _roundedSprite;
            critImg.type = Image.Type.Sliced;
            critImg.color = new Color(1.0f, 0.84f, 0.0f, 0.85f);

            GameObject pinObj = CreateUIElement("ResultPin", trackRect);
            RectTransform pinRect = pinObj.GetComponent<RectTransform>();
            pinRect.anchorMin = new Vector2(0.5f, 0.5f);
            pinRect.anchorMax = new Vector2(0.5f, 0.5f);
            pinRect.pivot = new Vector2(0.5f, 0.5f);
            pinRect.anchoredPosition = Vector2.zero;
            pinRect.sizeDelta = new Vector2(16.0f, 44.0f);

            Image pinImg = pinObj.AddComponent<Image>();
            pinImg.sprite = _roundedSprite;
            pinImg.type = Image.Type.Sliced;
            pinImg.color = Color.white;

            GameObject handleObj = CreateUIElement("Handle", trackRect);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(32.0f, 54.0f);

            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = _roundedSprite;
            handleImg.type = Image.Type.Sliced;
            handleImg.color = new Color(0.95f, 0.98f, 1.0f, 1.0f);

            GameObject toggleBtnObj = CreateUIElement("ModeToggleBtn", containerRect);
            RectTransform toggleBtnRect = toggleBtnObj.GetComponent<RectTransform>();
            toggleBtnRect.anchorMin = new Vector2(0.05f, 1.0f);
            toggleBtnRect.anchorMax = new Vector2(0.05f, 1.0f);
            toggleBtnRect.pivot = new Vector2(0.0f, 1.0f);
            toggleBtnRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            toggleBtnRect.sizeDelta = new Vector2(180.0f, 36.0f);

            Image toggleBtnBg = toggleBtnObj.AddComponent<Image>();
            toggleBtnBg.sprite = _buttonSprite;
            toggleBtnBg.type = Image.Type.Sliced;
            toggleBtnBg.color = Color.white;
            Button toggleBtn = toggleBtnObj.AddComponent<Button>();
            AddButtonFeedback(toggleBtn);

            TextMeshProUGUI toggleText = CreateText("ToggleText", toggleBtnRect, "ROLL UNDER", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            toggleText.color = new Color(0.0f, 0.9f, 1.0f, 1.0f);
            toggleText.enableAutoSizing = true;
            toggleText.fontSizeMin = 14.0f;
            toggleText.fontSizeMax = 20.0f;

            GameObject multObj = CreateUIElement("MultiplierDisplay", containerRect);
            RectTransform multRect = multObj.GetComponent<RectTransform>();
            multRect.anchorMin = new Vector2(0.38f, 1.0f);
            multRect.anchorMax = new Vector2(0.38f, 1.0f);
            multRect.pivot = new Vector2(0.5f, 1.0f);
            multRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            multRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI multText = CreateText("MultVal", multRect, "1.98x", 29, FontStyles.Bold, TextAlignmentOptions.Center);
            multText.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            GameObject targetObj = CreateUIElement("TargetDisplay", containerRect);
            RectTransform targetRect = targetObj.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.62f, 1.0f);
            targetRect.anchorMax = new Vector2(0.62f, 1.0f);
            targetRect.pivot = new Vector2(0.5f, 1.0f);
            targetRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            targetRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI targetText = CreateText("TargetVal", targetRect, "50", 29, FontStyles.Bold, TextAlignmentOptions.Center);
            targetText.color = Color.white;

            GameObject chanceObj = CreateUIElement("ChanceDisplay", containerRect);
            RectTransform chanceRect = chanceObj.GetComponent<RectTransform>();
            chanceRect.anchorMin = new Vector2(0.95f, 1.0f);
            chanceRect.anchorMax = new Vector2(0.95f, 1.0f);
            chanceRect.pivot = new Vector2(1.0f, 1.0f);
            chanceRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            chanceRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI chanceText = CreateText("ChanceVal", chanceRect, "50.0%", 29, FontStyles.Bold, TextAlignmentOptions.Center);
            chanceText.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            GameObject presetsRow = CreateUIElement("PresetsRow", containerRect);
            RectTransform presetsRect = presetsRow.GetComponent<RectTransform>();
            presetsRect.anchorMin = new Vector2(0.05f, 0.0f);
            presetsRect.anchorMax = new Vector2(0.95f, 0.0f);
            presetsRect.pivot = new Vector2(0.5f, 0.0f);
            presetsRect.anchoredPosition = new Vector2(0.0f, 10.0f);
            presetsRect.sizeDelta = new Vector2(0.0f, 32.0f);

            var (btn25, img25) = CreatePresetButton("Preset25", presetsRect, new Vector2(0.00f, 0.0f), new Vector2(0.23f, 1.0f), "25% SAFE");
            var (btn50, img50) = CreatePresetButton("Preset50", presetsRect, new Vector2(0.25f, 0.0f), new Vector2(0.48f, 1.0f), "50% BAL");
            var (btn75, img75) = CreatePresetButton("Preset75", presetsRect, new Vector2(0.51f, 0.0f), new Vector2(0.74f, 1.0f), "75% RISK");
            var (btn90, img90) = CreatePresetButton("Preset90", presetsRect, new Vector2(0.76f, 0.0f), new Vector2(1.00f, 1.0f), "90% WILD");

            DiceSliderView view = container.AddComponent<DiceSliderView>();
            SetPrivateField(view, "trackRect", trackRect);
            SetPrivateField(view, "winZoneRect", winRect);
            SetPrivateField(view, "lossZoneRect", lossRect);
            SetPrivateField(view, "critZoneRect", critRect);
            SetPrivateField(view, "handleRect", handleRect);
            SetPrivateField(view, "resultPinRect", pinRect);
            SetPrivateField(view, "winZoneImage", winImg);
            SetPrivateField(view, "lossZoneImage", lossImg);
            SetPrivateField(view, "critZoneImage", critImg);
            SetPrivateField(view, "resultPinImage", pinImg);
            SetPrivateField(view, "targetValueText", targetText);
            SetPrivateField(view, "winChanceText", chanceText);
            SetPrivateField(view, "multiplierText", multText);
            SetPrivateField(view, "modeToggleButtonText", toggleText);

            SetPrivateField(view, "buttonPreset25", btn25);
            SetPrivateField(view, "buttonPreset50", btn50);
            SetPrivateField(view, "buttonPreset75", btn75);
            SetPrivateField(view, "buttonPreset90", btn90);
            SetPrivateField(view, "bgPreset25", img25);
            SetPrivateField(view, "bgPreset50", img50);
            SetPrivateField(view, "bgPreset75", img75);
            SetPrivateField(view, "bgPreset90", img90);

            toggleBtn.onClick.AddListener(view.ToggleModeClicked);

            return view;
        }

        private static (Button btn, Image img) CreatePresetButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = _buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            AddButtonFeedback(btn);
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.0f, 0.85f, 1.0f, 0.5f);
            cb.pressedColor = new Color(0.0f, 0.5f, 0.8f, 0.6f);
            btn.colors = cb;

            TextMeshProUGUI txt = CreateText("Label", rect, label, 20, FontStyles.Bold, TextAlignmentOptions.Center);
            txt.color = new Color(0.85f, 0.9f, 0.98f, 0.95f);
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 14.0f;
            txt.fontSizeMax = 20.0f;

            return (btn, img);
        }

        private static DiceBetControlsView SetupBetControls(RectTransform parent)
        {
            GameObject container = CreateUIElement("BetControls", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.0f);
            containerRect.anchorMax = new Vector2(0.5f, 0.0f);
            containerRect.pivot = new Vector2(0.5f, 0.0f);
            containerRect.anchoredPosition = new Vector2(0.0f, 30.0f);
            containerRect.sizeDelta = new Vector2(900.0f, 130.0f);

            Image bg = container.AddComponent<Image>();
            bg.sprite = _panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            Outline betOutline = container.AddComponent<Outline>();
            betOutline.effectColor = new Color(0.20f, 0.27f, 0.40f, 0.55f);
            betOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject betBox = CreateUIElement("BetAmountBox", containerRect);
            RectTransform betBoxRect = betBox.GetComponent<RectTransform>();
            betBoxRect.anchorMin = new Vector2(0.04f, 0.5f);
            betBoxRect.anchorMax = new Vector2(0.04f, 0.5f);
            betBoxRect.pivot = new Vector2(0.0f, 0.5f);
            betBoxRect.anchoredPosition = Vector2.zero;
            betBoxRect.sizeDelta = new Vector2(240.0f, 85.0f);

            Image betBoxBg = betBox.AddComponent<Image>();
            betBoxBg.sprite = _panelSprite;
            betBoxBg.type = Image.Type.Sliced;
            betBoxBg.color = new Color(0.93f, 0.98f, 1.0f, 1.0f);

            TextMeshProUGUI betLabel = CreateText("BetLabel", betBoxRect, "BET AMOUNT", 18, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            betLabel.rectTransform.anchoredPosition = new Vector2(16.0f, -8.0f);
            betLabel.color = new Color(0.78f, 0.86f, 0.97f, 1.0f);

            TextMeshProUGUI betVal = CreateText("BetValue", betBoxRect, "$50", 38, FontStyles.Bold, TextAlignmentOptions.Left);
            betVal.rectTransform.anchoredPosition = new Vector2(16.0f, -37.0f);
            betVal.color = new Color(0.28f, 1.0f, 0.72f, 1.0f);
            betVal.enableAutoSizing = true;
            betVal.fontSizeMin = 22.0f;
            betVal.fontSizeMax = 38.0f;

            GameObject quickBtnRow = CreateUIElement("QuickBtnRow", containerRect);
            RectTransform quickRect = quickBtnRow.GetComponent<RectTransform>();
            quickRect.anchorMin = new Vector2(0.33f, 0.5f);
            quickRect.anchorMax = new Vector2(0.33f, 0.5f);
            quickRect.pivot = new Vector2(0.0f, 0.5f);
            quickRect.anchoredPosition = Vector2.zero;
            quickRect.sizeDelta = new Vector2(200.0f, 85.0f);

            Button btnHalf = CreateButton("BtnHalf", quickRect, new Vector2(0, 24), new Vector2(92, 38), "1/2", 16);
            Button btnDouble = CreateButton("BtnDouble", quickRect, new Vector2(102, 24), new Vector2(92, 38), "2X", 16);
            Button btnMin = CreateButton("BtnMin", quickRect, new Vector2(0, -24), new Vector2(92, 38), "MIN", 16);
            Button btnMax = CreateButton("BtnMax", quickRect, new Vector2(102, -24), new Vector2(92, 38), "MAX", 16);

            GameObject rollBtnObj = CreateUIElement("RollButton", containerRect);
            RectTransform rollRect = rollBtnObj.GetComponent<RectTransform>();
            rollRect.anchorMin = new Vector2(0.60f, 0.5f);
            rollRect.anchorMax = new Vector2(0.60f, 0.5f);
            rollRect.pivot = new Vector2(0.0f, 0.5f);
            rollRect.anchoredPosition = Vector2.zero;
            rollRect.sizeDelta = new Vector2(210.0f, 85.0f);

            Image rollImg = rollBtnObj.AddComponent<Image>();
            rollImg.sprite = _primaryButtonSprite;
            rollImg.type = Image.Type.Sliced;
            rollImg.color = Color.white;
            Button rollBtn = rollBtnObj.AddComponent<Button>();
            AddButtonFeedback(rollBtn);
            rollBtn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            ColorBlock cb = rollBtn.colors;
            cb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            cb.pressedColor = new Color(0.75f, 0.9f, 0.84f, 1.0f);
            rollBtn.colors = cb;

            TextMeshProUGUI rollText = CreateText("RollText", rollRect, "ROLL DICE", 36, FontStyles.Bold, TextAlignmentOptions.Center);
            rollText.color = new Color(0.035f, 0.14f, 0.12f, 1.0f);
            rollText.enableAutoSizing = true;
            rollText.fontSizeMin = 22.0f;
            rollText.fontSizeMax = 40.0f;

            GameObject autoBtnObj = CreateUIElement("AutoRollButton", containerRect);
            RectTransform autoRect = autoBtnObj.GetComponent<RectTransform>();
            autoRect.anchorMin = new Vector2(0.86f, 0.5f);
            autoRect.anchorMax = new Vector2(0.86f, 0.5f);
            autoRect.pivot = new Vector2(0.0f, 0.5f);
            autoRect.anchoredPosition = Vector2.zero;
            autoRect.sizeDelta = new Vector2(105.0f, 85.0f);

            Image autoImg = autoBtnObj.AddComponent<Image>();
            autoImg.sprite = _buttonSprite;
            autoImg.type = Image.Type.Sliced;
            autoImg.color = Color.white;
            Button autoBtn = autoBtnObj.AddComponent<Button>();
            AddButtonFeedback(autoBtn);

            TextMeshProUGUI autoText = CreateText("AutoText", autoRect, "AUTO\nROLL", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            autoText.color = new Color(0.0f, 0.85f, 1.0f, 1.0f);

            DiceBetControlsView view = container.AddComponent<DiceBetControlsView>();
            SetPrivateField(view, "betAmountText", betVal);
            SetPrivateField(view, "buttonHalf", btnHalf);
            SetPrivateField(view, "buttonDouble", btnDouble);
            SetPrivateField(view, "buttonMin", btnMin);
            SetPrivateField(view, "buttonMax", btnMax);
            SetPrivateField(view, "buttonRoll", rollBtn);
            SetPrivateField(view, "rollButtonText", rollText);
            SetPrivateField(view, "buttonAutoRoll", autoBtn);
            SetPrivateField(view, "autoRollIndicator", autoImg);
            SetPrivateField(view, "autoRollText", autoText);

            return view;
        }

        private static DiceGameOverModalView SetupGameOverModal(RectTransform parent)
        {
            GameObject root = CreateUIElement("GameOverModal", parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            GameObject cardObj = CreateUIElement("ModalCard", rootRect);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(650.0f, 540.0f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = _panelSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;

            TextMeshProUGUI headerTitle = CreateText("HeaderTitle", cardRect, "BLITZ RUN COMPLETE!", 34, FontStyles.Bold, TextAlignmentOptions.Top);
            headerTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -25.0f);
            headerTitle.color = Color.white;

            GameObject rankBadgeObj = CreateUIElement("RankBadge", cardRect);
            RectTransform rankBadgeRect = rankBadgeObj.GetComponent<RectTransform>();
            rankBadgeRect.anchoredPosition = new Vector2(0.0f, 130.0f);
            rankBadgeRect.sizeDelta = new Vector2(170.0f, 90.0f);

            Image rankBg = rankBadgeObj.AddComponent<Image>();
            rankBg.sprite = _buttonSprite;
            rankBg.type = Image.Type.Sliced;
            rankBg.color = new Color(1.0f, 0.84f, 0.0f, 0.2f);

            TextMeshProUGUI rankText = CreateText("RankText", rankBadgeRect, "SSS", 64, FontStyles.Bold, TextAlignmentOptions.Center);
            rankText.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            TextMeshProUGUI rankTitle = CreateText("RankTitle", cardRect, "HIGH ROLLER LEGEND", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            rankTitle.rectTransform.anchoredPosition = new Vector2(0.0f, 65.0f);
            rankTitle.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            TextMeshProUGUI finalScore = CreateStatRow(cardRect, "Final Bankroll:", "$5,420", new Vector2(0.0f, 25.0f));
            TextMeshProUGUI netProfit = CreateStatRow(cardRect, "Net Profit:", "+$4,420", new Vector2(0.0f, -10.0f));
            TextMeshProUGUI totalRolls = CreateStatRow(cardRect, "Total Rolls:", "38", new Vector2(0.0f, -45.0f));
            TextMeshProUGUI winRate = CreateStatRow(cardRect, "Win Rate:", "68.4%", new Vector2(0.0f, -80.0f));
            TextMeshProUGUI bestStreak = CreateStatRow(cardRect, "Best Streak:", "x7", new Vector2(0.0f, -115.0f));
            TextMeshProUGUI critsHit = CreateStatRow(cardRect, "Jackpot Crits:", "3", new Vector2(0.0f, -150.0f));

            Button btnPlayAgain = CreateButton("BtnPlayAgain", cardRect, new Vector2(-130.0f, -210.0f), new Vector2(240.0f, 62.0f), "PLAY AGAIN (20s)", 22);
            Image playAgainImg = btnPlayAgain.GetComponent<Image>();
            playAgainImg.sprite = _primaryButtonSprite;
            playAgainImg.color = Color.white;
            btnPlayAgain.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.035f, 0.14f, 0.12f, 1.0f);

            Button btnEndless = CreateButton("BtnEndless", cardRect, new Vector2(130.0f, -210.0f), new Vector2(240.0f, 62.0f), "FREE PLAY MODE", 22);
            Image endlessImg = btnEndless.GetComponent<Image>();
            endlessImg.color = Color.white;
            btnEndless.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;

            DiceGameOverModalView view = root.AddComponent<DiceGameOverModalView>();
            SetPrivateField(view, "modalRoot", root);
            SetPrivateField(view, "cardRect", cardRect);
            SetPrivateField(view, "rankBadgeText", rankText);
            SetPrivateField(view, "rankTitleText", rankTitle);
            SetPrivateField(view, "rankBgImage", rankBg);
            SetPrivateField(view, "finalScoreText", finalScore);
            SetPrivateField(view, "netProfitText", netProfit);
            SetPrivateField(view, "totalRollsText", totalRolls);
            SetPrivateField(view, "winRateText", winRate);
            SetPrivateField(view, "bestStreakText", bestStreak);
            SetPrivateField(view, "critsHitText", critsHit);
            SetPrivateField(view, "buttonPlayAgain", btnPlayAgain);
            SetPrivateField(view, "buttonEndlessMode", btnEndless);

            return view;
        }

        private static TextMeshProUGUI CreateStatRow(RectTransform parent, string label, string defaultValue, Vector2 pos)
        {
            GameObject row = CreateUIElement("Row_" + label.Replace(" ", "").Replace(":", ""), parent);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchoredPosition = pos;
            rowRect.sizeDelta = new Vector2(520.0f, 34.0f);

            TextMeshProUGUI labelText = CreateText("Label", rowRect, label, 22, FontStyles.Normal, TextAlignmentOptions.Left);
            labelText.color = new Color(0.84f, 0.9f, 0.99f, 1.0f);

            TextMeshProUGUI valText = CreateText("Value", rowRect, defaultValue, 24, FontStyles.Bold, TextAlignmentOptions.Right);
            valText.color = Color.white;

            return valText;
        }

        private static FloatingTextController SetupFloatingText(RectTransform parent)
        {
            GameObject container = CreateUIElement("FloatingTextContainer", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            GameObject itemObj = CreateUIElement("FloatingTextPrefab", containerRect);
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(400.0f, 60.0f);

            TextMeshProUGUI itemText = CreateText("Text", itemRect, "+$500", 38, FontStyles.Bold, TextAlignmentOptions.Center);
            FloatingTextItem item = itemObj.AddComponent<FloatingTextItem>();
            SetPrivateField(item, "textMesh", itemText);
            SetPrivateField(item, "rectTransform", itemRect);

            itemObj.SetActive(false);

            FloatingTextController controller = container.AddComponent<FloatingTextController>();
            SetPrivateField(controller, "prefab", item);
            SetPrivateField(controller, "container", containerRect);
            SetPrivateField(controller, "poolSize", 16);

            return controller;
        }

        private static ParticleFxController SetupParticleSystems(RectTransform parent)
        {
            GameObject fxContainer = new GameObject("ParticleSystems");
            fxContainer.transform.SetParent(parent, false);

            ParticleSystem winSparkles = CreateProceduralParticleSystem(fxContainer, "WinSparkles", new Color(0.0f, 1.0f, 0.6f, 1.0f), 25);
            ParticleSystem confetti = CreateProceduralParticleSystem(fxContainer, "ConfettiBurst", new Color(1.0f, 0.85f, 0.0f, 1.0f), 60);
            ParticleSystem jackpotCrit = CreateProceduralParticleSystem(fxContainer, "JackpotCrit", new Color(1.0f, 0.9f, 0.2f, 1.0f), 40);
            ParticleSystem diceImpact = CreateProceduralParticleSystem(fxContainer, "DiceImpact", new Color(0.3f, 0.4f, 0.6f, 1.0f), 15);
            ParticleSystem feverAura = CreateProceduralParticleSystem(fxContainer, "FeverAura", new Color(1.0f, 0.2f, 0.7f, 1.0f), 30);

            ParticleFxController controller = fxContainer.AddComponent<ParticleFxController>();
            SetPrivateField(controller, "winSparkleParticles", winSparkles);
            SetPrivateField(controller, "confettiBurstParticles", confetti);
            SetPrivateField(controller, "jackpotCritParticles", jackpotCrit);
            SetPrivateField(controller, "diceImpactParticles", diceImpact);
            SetPrivateField(controller, "feverAuraParticles", feverAura);

            return controller;
        }

        private static ParticleSystem CreateProceduralParticleSystem(GameObject parent, string name, Color color, int maxParticles)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 8.0f;
            main.startSize = 0.25f;
            main.startColor = color;
            main.maxParticles = maxParticles;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, (short)maxParticles) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return ps;
        }

        private static DiceAudioSynthesizer SetupAudioSynthesizer()
        {
            GameObject audioObj = new GameObject("AudioSynthesizer");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
            source.priority = 64;
            source.volume = 0.9f;
            AudioSource loopSource = audioObj.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = 0.0f;
            loopSource.priority = 128;
            loopSource.volume = 0.18f;

            DiceAudioSynthesizer synth = audioObj.AddComponent<DiceAudioSynthesizer>();
            SetPrivateField(synth, "audioSource", source);
            SetPrivateField(synth, "loopSource", loopSource);
            SetPrivateField(synth, "backgroundLoopClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/DiceAmbientLoop.wav"));
            SetPrivateField(synth, "clickClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI_Click.wav"));
            SetPrivateField(synth, "betAdjustClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Bet_Chip.wav"));
            SetPrivateField(synth, "backgroundMusicVolume", 0.18f);
            return synth;
        }

        private static void SetupGameManager(
            DiceGameConfig config,
            DiceAudioSynthesizer audioSynth,
            ScreenShakeController screenShake,
            FloatingTextController floatingText,
            ParticleFxController particleFx,
            VisualDiceRoller diceRoller,
            DiceStatsHeaderView statsHeader,
            DiceRoundTimerView timerView,
            DiceSliderView sliderView,
            DiceBetControlsView betControls,
            DiceGameOverModalView gameOverModal,
            DiceHistoryView historyView)
        {
            GameObject mgrObj = new GameObject("DiceGameManager");
            DiceGameManager mgr = mgrObj.AddComponent<DiceGameManager>();

            SetPrivateField(mgr, "config", config);
            SetPrivateField(mgr, "audioSynth", audioSynth);
            SetPrivateField(mgr, "screenShake", screenShake);
            SetPrivateField(mgr, "floatingText", floatingText);
            SetPrivateField(mgr, "particleFx", particleFx);
            SetPrivateField(mgr, "diceRoller", diceRoller);
            SetPrivateField(mgr, "statsHeader", statsHeader);
            SetPrivateField(mgr, "timerView", timerView);
            SetPrivateField(mgr, "sliderView", sliderView);
            SetPrivateField(mgr, "betControls", betControls);
            SetPrivateField(mgr, "gameOverModal", gameOverModal);
            SetPrivateField(mgr, "historyView", historyView);
        }

        private static GameObject CreateUIElement(string name, RectTransform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            if (_defaultFont != null) tmp.font = _defaultFont;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        private static Button CreateButton(string name, RectTransform parent, Vector2 anchoredPos, Vector2 size, string text, float fontSize)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = _buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = obj.AddComponent<Button>();
            AddButtonFeedback(btn);
            TextMeshProUGUI tmp = CreateText("Label", rect, text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(15.0f, fontSize * 0.65f);
            tmp.fontSizeMax = fontSize;

            return btn;
        }

        private static void AddButtonFeedback(Button button)
        {
            if (button != null && button.GetComponent<UIButtonFeel>() == null) button.gameObject.AddComponent<UIButtonFeel>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning("[DiceGame] Field not found: " + fieldName + " on " + target.GetType().Name);
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            string loadingPath = "Assets/Scenes/LoadingScene.unity";
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(loadingPath, true),
                new EditorBuildSettingsScene(scenePath, true)
            };
        }
    }
}

