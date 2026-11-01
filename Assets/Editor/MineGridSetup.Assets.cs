using System;
using System.IO;
using MineGrid;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static partial class MineGridSetup
{
    private static void PrepareTheme()
    {
        _theme = AssetDatabase.LoadAssetAtPath<UITheme>(Root + "/UITheme.asset");
        if (!_theme)
        {
            _theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(_theme, Root + "/UITheme.asset");
        }

        _theme.bodyFont = FontAsset("VarelaRound-Regular");
        _theme.displayFont = FontAsset("LilitaOne-Regular");
        _theme.displayFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>
        {
            _theme.bodyFont
        };
        _theme.bodyFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>
        {
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")
        };
        _theme.logo = ImportSprite(Root + "/Art/Brand/MineGridLogo.png");
        _theme.coin = ImportSprite(Root + "/Art/Brand/GoldCoin.png");
        _theme.gem = ImportSprite(Root + "/Art/Brand/CyanGem.png");
        _theme.mine = ImportSprite(Root + "/Art/Brand/NavyMine.png");
        _rounded = Art("Surface", 128, 128, (x, y) =>
        {
            if (!RoundPixel(x, y, 128, 128, 22))
            {
                return Color.clear;
            }

            bool edge = !RoundPixel(x - 3, y - 3, 122, 122, 20);
            float v = .82f + .18f * y / 127f;
            return edge ? new Color(.68f, .74f, .86f) : new Color(v, v, v);
        }, true);
        _theme.rounded = _rounded;
        _theme.backdrop = Art("MidnightCave", 512, 768, (x, y) =>
        {
            float t = y / 767f;
            var c = Color.Lerp(new Color(.025f, .075f, .16f), new Color(.115f, .10f, .29f), t);
            float glow = Mathf.Exp(-((x - 256f) * (x - 256f) + (y - 480f) * (y - 480f)) / 65000f);
            return Color.Lerp(c, new Color(.08f, .30f, .40f), glow * .38f);
        });
        _theme.hidden = TileArt("HiddenV3", 0);
        _theme.safeTile = TileArt("SafeV3", 1);
        _theme.mineTile = TileArt("MineV3", 2);
        _theme.wheel = Art("TreasureWheel", 768, 768, (x, y) =>
        {
            float dx = x - 383.5f, dy = y - 383.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 381)
            {
                return Color.clear;
            }

            if (r > 367)
            {
                return new Color(.18f, .16f, .36f);
            }

            if (r > 344)
            {
                return Color.Lerp(new Color(.8f, .43f, .08f), new Color(1, .91f, .57f), y / 768f);
            }

            if (r > 333)
            {
                return new Color(.04f, .065f, .16f);
            }

            float angle = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg + 30, 360);
            Color c = (int)(angle / 60) % 2 == 0 ? new Color(.18f, .43f, .58f) : new Color(.27f, .23f, .49f);
            if (angle % 60 < .55f || angle % 60 > 59.45f)
            {
                c = new Color(.5f, .67f, .83f);
            }

            return Color.Lerp(c * .72f, c, r / 333f);
        });
        EditorUtility.SetDirty(_theme);
        EditorUtility.SetDirty(_theme.displayFont);
        EditorUtility.SetDirty(_theme.bodyFont);
    }

    private static TMP_FontAsset FontAsset(string name)
    {
        string path = Root + "/Fonts/" + name + " SDF.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (!asset)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/" + name + ".ttf");
            if (!source)
            {
                throw new Exception("Missing font " + name);
            }

            asset = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            string chars = "";
            for (int i = 32; i < 256; i++)
            {
                chars += (char)i;
            }

            chars += "–—‘’“”•×";
            asset.TryAddCharacters(chars, out string missing);
        }

        return asset;
    }

    private static Sprite TileArt(string name, int kind) => Art(name, 192, 192, (x, y) =>
    {
        if (!RoundPixel(x, y, 192, 192, 23))
        {
            return Color.clear;
        }

        var low = kind == 1 ? new Color(.06f, .25f, .27f) : kind == 2 ? new Color(.32f, .10f, .20f) : new Color(.14f, .21f, .36f);
        var high = kind == 1 ? new Color(.16f, .48f, .46f) : kind == 2 ? new Color(.56f, .22f, .30f) : new Color(.31f, .43f, .60f);
        Color c = Color.Lerp(low, high, y / 192f);
        if (y < 11)
        {
            c = low * .57f;
        }
        else if (y > 180)
        {
            c = high * 1.15f;
        }

        if (x < 5 || x > 186)
        {
            c *= .82f;
        }

        if (kind == 0)
        {
            float dx = x - 96, dy = y - 100;
            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 21)
            {
                c = new Color(.40f, .57f, .72f);
            }

            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 13)
            {
                c = new Color(.23f, .34f, .49f);
            }
        }

        return c;
    });
    private static bool RoundPixel(float x, float y, int w, int h, float radius)
    {
        float dx = Mathf.Max(Mathf.Abs(x - w * .5f) - (w * .5f - radius), 0), dy = Mathf.Max(Mathf.Abs(y - h * .5f) - (h * .5f - radius), 0);
        return dx * dx + dy * dy < radius * radius;
    }

    private static Sprite Art(string name, int w, int h, Func<int, int, Color> pixel, bool sliced = false)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                pixels[y * w + x] = pixel(x, y);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        string path = Root + "/Art/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        return ImportSprite(path, sliced);
    }

    private static Sprite ImportSprite(string path, bool sliced = false)
    {
        if (!File.Exists(path))
        {
            throw new Exception("Missing artwork: " + path);
        }

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        if (sliced)
        {
            importer.spriteBorder = new Vector4(25, 25, 25, 25);
        }

        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
