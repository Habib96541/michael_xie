using System.IO;
using UnityEngine;
using UnityEditor;

namespace DropStack.Editor
{
    public static class DropStackArtGenerator
    {
        private const string BackdropPath = "Assets/Sprites/Theme/arcade_backdrop.png";

        [MenuItem("Drop Stack/Generate Arcade Backdrop")]
        public static void GenerateBackdrop()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BackdropPath));
            const int width = 1200, height = 900;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)width, ny = y / (float)height;
                float glow = Mathf.Exp(-((nx - .50f) * (nx - .50f) * 7 + (ny - .48f) * (ny - .48f) * 3));
                Color color = Color.Lerp(new Color(.012f, .025f, .045f), new Color(.035f, .10f, .15f), glow * .85f);
                if (x % 48 == 0 || y % 48 == 0) color = Color.Lerp(color, new Color(.08f, .19f, .25f), .17f);
                int bx = x / 96, by = y / 96;
                float seed = Mathf.Abs(Mathf.Sin(bx * 127.1f + by * 311.7f));
                int dx = x % 96, dy = y % 96;
                if (seed > .82f && dx > 10 && dx < 84 && dy > 10 && dy < 84)
                {
                    bool edge = dx < 12 || dx > 81 || dy < 12 || dy > 81;
                    color = Color.Lerp(color, new Color(.06f, .23f, .3f), edge ? .35f : .05f);
                }
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(BackdropPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(BackdropPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(BackdropPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
