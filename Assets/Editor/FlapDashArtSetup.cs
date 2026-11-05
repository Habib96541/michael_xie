using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlapDash.Editor
{
    public static class FlapDashArtSetup
    {
        private const string Root = "Assets/ThirdParty/";

        public static void Import()
        {
            foreach (string directory in new[] { "BevouliinBlueBird", "Bhu1cPipes", "KenneyUI", "KenneyBackgrounds" })
                foreach (string path in Directory.GetFiles(Root + directory, "*.png"))
                    ImportSprite(path.Replace('\\', '/'));
        }

        public static Sprite[] BirdFrames()
        {
            var frames = new Sprite[8];
            for (int index = 0; index < frames.Length; index++)
                frames[index] = Load(Root + $"BevouliinBlueBird/frame-{index + 1}.png");
            return frames;
        }

        public static Sprite Panel => Load(Root + "KenneyUI/button_rectangle_depth_gradient.png");
        public static Sprite FooterPanel => Load(Root + "KenneyUI/button_rectangle_depth_flat.png");
        public static Sprite Button => Load(Root + "KenneyUI/button_rectangle_depth_gloss.png");
        public static Sprite SliderTrack => Load(Root + "KenneyUI/slide_horizontal_grey.png");
        public static Sprite SliderFill => Load(Root + "KenneyUI/slide_horizontal_color.png");
        public static Sprite SliderHandle => Load(Root + "KenneyUI/slide_hangle.png");
        public static Sprite Background(string name) => Load(Root + $"KenneyBackgrounds/{name}.png");
        public static Sprite PipeBody => Region("PipeBody", new Rect(848, 0, 328, 500));
        public static Sprite PipeRim => Region("PipeRim", new Rect(786, 525, 454, 280));

        private static Sprite Load(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new FileNotFoundException("Art sprite was not imported.", path);
            return sprite;
        }

        private static void ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = path.Contains("Bhu1cPipes") ? 2048 : 1024;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = path.Contains("button_rectangle") ? new Vector4(24, 32, 24, 24)
                : path.Contains("slide_horizontal") ? new Vector4(16, 0, 16, 0) : Vector4.zero;
            importer.SaveAndReimport();
        }

        private static Sprite Region(string name, Rect region)
        {
            string path = $"Assets/Art/{name}.asset";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Bhu1cPipes/pipe.png");
            sprite = Sprite.Create(texture, region, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }
    }
}
