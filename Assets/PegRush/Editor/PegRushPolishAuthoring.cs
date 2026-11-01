using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PegRush;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class PegRushPolishAuthoring
{
    private const string Root = "Assets/PegRush/Polish";
    private const string Generated = "Assets/PegRush/Generated";

    [MenuItem("PegRush/Polish/Apply authored polish assets (once)")]
    public static void Apply()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Root + "/Applied.txt")) throw new InvalidOperationException("Polish already authored; normal builds preserve edits.");
        if (!Directory.Exists("Assets/TextMesh Pro/Resources"))
        {
            AssetDatabase.ImportPackage("Library/PackageCache/com.unity.ugui@bb329a87fcdc/Package Resources/TMP Essential Resources.unitypackage", false);
            AssetDatabase.Refresh();
        }
        UITheme theme = ScriptableObject.CreateInstance<UITheme>();
        theme.Regular = Font("Regular");
        theme.Bold = Font("Bold");
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        theme.Regular.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        theme.Bold.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        theme.Regular.fontWeightTable[7] = new TMP_FontWeightPair { regularTypeface = theme.Bold };
        theme.Rounded = Sprite("Rounded", 0);
        theme.Outline = Sprite("Outline", 1);
        Sprite orb = Sprite("GlossyOrb", 2);
        Sprite peg = Sprite("LuminousPeg", 3);
        AssetDatabase.CreateAsset(theme, Root + "/UITheme.asset");
        foreach (string name in new[] { "UI", "Ball", "Peg" })
        {
            string path = Generated + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            if (name == "UI") prefab.GetComponent<GameUI>().ApplyTheme(theme);
            else prefab.GetComponent<SpriteRenderer>().sprite = name == "Ball" ? orb : peg;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(Generated + "/GameConfig.asset");
        JsonUtility.FromJsonOverwrite(File.ReadAllText("SprintDoc/Polish/Distribution/tuning.json"), config);
        foreach (RewardTable table in config.Tables)
        {
            for (int i = 0; i <= table.Rows; i++)
            {
                float edge = Mathf.Abs(2f * i / table.Rows - 1f);
                table.Hundredths[i] = Mathf.RoundToInt(table.Profile == 0 ? 75 + 75 * edge * edge : table.Profile == 1 ? 50 + 250 * Mathf.Pow(edge, 3) : 25 + 575 * Mathf.Pow(edge, 5));
            }
        }
        EditorUtility.SetDirty(config);
        var scene = EditorSceneManager.OpenScene(Generated + "/PegRush.unity");
        Camera background = GameObject.Find("Background Camera").GetComponent<Camera>();
        background.backgroundColor = theme.Background;
        Camera camera = GameObject.Find("Orthographic Board Camera").GetComponent<Camera>();
        camera.backgroundColor = theme.Surface;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Root + "/Applied.txt", "Polish authored once. Menus Build and Validate do not regenerate assets or tuning.\n");
        AssetDatabase.Refresh();
        PegRushAuthoring.Verify();
    }

    private static TMP_FontAsset Font(string weight)
    {
        UnityEngine.Font source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/PegRush/Fonts/Outfit-" + weight + ".ttf");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        string characters = new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray()) + "×→↓·•–—−✓";
        if (!font.TryAddCharacters(characters, out string missing)) throw new Exception("Missing UI glyphs: " + missing);
        font.name = "Outfit " + weight + " SDF";
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font, Root + "/Outfit-" + weight + ".asset");
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        EditorUtility.SetDirty(font);
        return font;
    }

    private static Sprite Sprite(string name, int mode)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 centered = new Vector2(x - 63.5f, y - 63.5f);
                float distance;
                float alpha;
                float brightness = 1f;
                if (mode < 2)
                {
                    distance = new Vector2(Mathf.Max(Mathf.Abs(centered.x) - 43f, 0), Mathf.Max(Mathf.Abs(centered.y) - 43f, 0)).magnitude - 19f;
                    alpha = Mathf.Clamp01(0.5f - distance);
                    if (mode == 1) alpha *= Mathf.Clamp01(distance + 2f);
                }
                else
                {
                    distance = centered.magnitude - 60f;
                    alpha = Mathf.Clamp01(0.5f - distance);
                    float radial = centered.magnitude / 60f;
                    float highlight = Mathf.Exp(-((x - 46f) * (x - 46f) + (y - 85f) * (y - 85f)) / 160f);
                    brightness = mode == 2 ? Mathf.Clamp01(0.6f + 0.24f * (y / 128f) + 0.42f * highlight - radial * 0.1f) : Mathf.Clamp01(0.76f + (1 - radial) * 0.24f);
                }
                texture.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }
        texture.Apply();
        string path = Root + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.spriteBorder = mode < 2 ? new Vector4(24, 24, 24, 24) : Vector4.zero;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
