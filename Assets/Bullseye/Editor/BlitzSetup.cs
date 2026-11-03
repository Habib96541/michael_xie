using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace BullseyeBlitz.Editor
{
    [InitializeOnLoad]
    public static class BlitzSetup
    {
        private const string Root = "Assets/Bullseye/";
        static BlitzSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/BlitzCommand.txt")) { return; }
            string command = File.ReadAllText("Temp/BlitzCommand.txt").Trim();
            File.Delete("Temp/BlitzCommand.txt");
            try
            {
                if (command == "setup") { Setup(); }
                if (command == "build") { Build(); }
                if (command == "test") { BlitzChecks.Run(); }
                if (command == "polish") { BoothPolish.Apply(); }
                if (command == "prepare") { BoothPolish.Apply(); BlitzChecks.Run(); }
                if (command == "play") { EditorApplication.isPlaying = true; }
                if (command == "stop") { EditorApplication.isPlaying = false; }
                File.WriteAllText("Temp/BlitzResult.txt", command + " OK " + DateTime.UtcNow);
            }
            catch (Exception e) { File.WriteAllText("Temp/BlitzResult.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Bullseye Blitz/Generate initial game assets")]
        public static void Setup()
        {
            if (File.Exists(Root + "Scenes/BullseyeBlitz.unity")) { throw new InvalidOperationException("Scene already exists. Setup preserves existing game assets; edit them directly."); }
            Directory.CreateDirectory(Root + "Scenes");
            Directory.CreateDirectory(Root + "Prefabs");
            Directory.CreateDirectory(Root + "Art");
            SerializedObject tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tags.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "BlitzTarget";
            layers.GetArrayElementAtIndex(9).stringValue = "BlitzBlocker";
            SerializedProperty sorting = tags.FindProperty("m_SortingLayers");
            string[] names = { "Backdrop", "Targets", "ShelfFronts", "Effects", "UI" };
            foreach (string name in names)
            {
                bool found = false;
                for (int i = 0; i < sorting.arraySize; i++) { if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) { found = true; } }
                if (found) { continue; }
                int index = sorting.arraySize;
                sorting.InsertArrayElementAtIndex(index);
                sorting.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue = name;
                sorting.GetArrayElementAtIndex(index).FindPropertyRelative("uniqueID").intValue = 100001 + index;
            }
            tags.ApplyModifiedProperties();
            BlitzConfig config = ScriptableObject.CreateInstance<BlitzConfig>();
            config.Coin = Sprite("Assets/Violet Theme Ui/Colored Icons/Coin.png");
            config.Bomb = Sprite("Assets/Violet Theme Ui/Colored Icons/Bomb.png");
            config.Button = Sprite("Assets/Violet Theme Ui/Buttons/Button Purple.png");
            config.Music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FREE CASUAL MUSIC PACK/CASUAL THEME 2/CASUAL THEME 2 - LOOP 1.wav");
            config.Pop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FREE CASUAL MUSIC PACK/BONUS 2 - SFX/POP 1.wav");
            config.Reward = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FREE CASUAL MUSIC PACK/BONUS 2 - SFX/SPECIAL COLECT 1.wav");
            AssetDatabase.CreateAsset(config, Root + "BlitzConfig.asset");
            Texture2D texture = new Texture2D(8, 8);
            texture.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); texture.Apply();
            File.WriteAllBytes(Root + "Art/White.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Root + "Art/White.png");
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 8; importer.SaveAndReimport();
            Sprite white = Sprite(Root + "Art/White.png");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Orthographic Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 6; camera.backgroundColor = new Color(.08f, .04f, .16f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Material material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, Root + "Art/UnlitSprites.mat");
            Draw("Booth back", white, material, new Vector2(0, 0), new Vector2(18, 22), new Color(.12f, .065f, .24f), "Backdrop", 0);
            for (int i = -8; i <= 8; i++)
            {
                Draw("Backdrop stripe", white, material, new Vector2(i, 0), new Vector2(.43f, 20), new Color(.17f, .085f, .30f), "Backdrop", 1);
            }
            for (int row = 0; row < 3; row++)
            {
                float y = 2.25f - row * 2.05f;
                Draw("Shelf recess", white, material, new Vector2(0, y), new Vector2(12, 1.7f), new Color(.075f, .035f, .12f), "Backdrop", 2);
                Draw("Shelf front", white, material, new Vector2(0, y - .98f), new Vector2(12, .9f), new Color(.38f, .17f, .12f), "ShelfFronts", 0);
                Draw("Shelf gold trim", white, material, new Vector2(0, y - .55f), new Vector2(12, .08f), new Color(.95f, .63f, .20f), "ShelfFronts", 1);
            }
            Draw("Curtain left", white, material, new Vector2(-5.3f, .7f), new Vector2(1.2f, 10), new Color(.56f, .08f, .24f), "Backdrop", 4);
            Draw("Curtain right", white, material, new Vector2(5.3f, .7f), new Vector2(1.2f, 10), new Color(.56f, .08f, .24f), "Backdrop", 4);
            Draw("Canopy", white, material, new Vector2(0, 4.4f), new Vector2(12, .55f), new Color(.65f, .1f, .27f), "Backdrop", 5);
            BoothTarget[] slots = new BoothTarget[9];
            for (int i = 0; i < slots.Length; i++)
            {
                GameObject go = new GameObject("Target Slot " + (i + 1)); go.layer = 8;
                SpriteRenderer sprite = go.AddComponent<SpriteRenderer>(); sprite.sharedMaterial = material;
                sprite.sprite = config.Coin;
                float size = Mathf.Max(sprite.sprite.bounds.size.x, sprite.sprite.bounds.size.y);
                go.transform.localScale = Vector3.one * (1.3f / size);
                CircleCollider2D collider = go.AddComponent<CircleCollider2D>(); collider.radius = size * .43f;
                GameObject labelObject = new GameObject("Reward value"); labelObject.transform.SetParent(go.transform, false);
                labelObject.transform.localPosition = new Vector3(0, 0, -.01f);
                TextMesh label = labelObject.AddComponent<TextMesh>(); label.fontSize = 80; label.characterSize = size * .075f;
                label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                BoothTarget target = go.AddComponent<BoothTarget>(); target.Configure(sprite, collider, label);
                slots[i] = target;
                if (i == 0) { PrefabUtility.SaveAsPrefabAsset(go, Root + "Prefabs/BoothTarget.prefab"); }
            }
            GameObject root = new GameObject("Bullseye Blitz");
            BoothSpawner spawner = root.AddComponent<BoothSpawner>(); spawner.Configure(slots);
            root.AddComponent<BlitzGame>().Configure(config, spawner, camera);
            EditorSceneManager.SaveScene(scene, Root + "Scenes/BullseyeBlitz.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "Scenes/BullseyeBlitz.unity", true) };
            AssetDatabase.SaveAssets();
        }
        private static Sprite Sprite(string path) { return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(); }
        private static void Draw(string name, Sprite sprite, Material material, Vector2 position, Vector2 scale, Color color, string layer, int order)
        {
            SpriteRenderer renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.color = color;
            renderer.sortingLayerName = layer; renderer.sortingOrder = order;
            renderer.transform.position = position; renderer.transform.localScale = scale;
        }
        [MenuItem("Bullseye Blitz/Build Web")]
        public static void Build()
        {
            BlitzConfig config = AssetDatabase.LoadAssetAtPath<BlitzConfig>(Root + "BlitzConfig.asset");
            if (config == null || !config.IsValid()) { throw new InvalidOperationException("Invalid game configuration."); }
            PlayerSettings.companyName = "BullseyeBlitz";
            PlayerSettings.productName = "Bullseye Blitz";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:Bullseye";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Root + "Scenes/BullseyeBlitz.unity" },
                locationPathName = "Builds/Web", target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) { throw new Exception("Web build failed: " + report.summary.result); }
        }
    }
}
