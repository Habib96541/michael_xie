using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BullseyeBlitz.Editor
{
    public static class MidwayTheme
    {
        private const string Art = "Assets/Bullseye/Art/";
        private const string ScenePath = "Assets/Bullseye/Scenes/BullseyeBlitz.unity";

        [MenuItem("Bullseye Blitz/Apply midway art")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var config = AssetDatabase.LoadAssetAtPath<BlitzConfig>("Assets/Bullseye/BlitzConfig.asset");
            config.DisplayFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/Rye-Regular.ttf");
            config.BodyFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/Lato-Bold.ttf");
            if (config.DisplayFont == null || config.BodyFont == null)
            {
                throw new System.InvalidOperationException("Midway font assets must be imported.");
            }
            EditorUtility.SetDirty(config);
            Camera.main.orthographicSize = 4.1f;

            Sprite backdrop = LoadSprite("MidwayTwilight.png");
            Sprite shelf = LoadSprite("MidwayShelf.png");
            Sprite recess = LoadSprite("MidwayRecess.png");
            Sprite awning = LoadSprite("MidwayAwning.png");
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (renderer.name)
                {
                    case "Booth back":
                        renderer.sprite = backdrop;
                        renderer.color = Color.white;
                        var fitting = renderer.GetComponent<MidwayBackdrop>() ?? renderer.gameObject.AddComponent<MidwayBackdrop>();
                        fitting.Configure(Camera.main, renderer);
                        break;
                    case "Backdrop stripe":
                    case "Marquee diamond":
                    case "Shelf gold trim":
                        renderer.gameObject.SetActive(false);
                        break;
                    case "Shelf recess":
                        Dress(renderer, recess, 7.45f, 1.65f);
                        break;
                    case "Shelf front":
                        Dress(renderer, shelf, 7.9f, .9f);
                        if (renderer.TryGetComponent(out BoxCollider2D blocker))
                        {
                            blocker.size = shelf.bounds.size;
                        }
                        break;
                    case "Canopy":
                        Dress(renderer, awning, 8.05f, .92f);
                        renderer.transform.position = new Vector3(0f, 3.46f, 0f);
                        break;
                    case "Curtain left":
                    case "Curtain right":
                        Dress(renderer, shelf, 6.75f, .43f);
                        renderer.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                        renderer.transform.position = new Vector3(renderer.name == "Curtain left" ? -3.66f : 3.66f, .15f, 0f);
                        break;
                }
            }
            foreach (BoothTarget target in Object.FindObjectsByType<BoothTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                TextMesh label = target.GetComponentInChildren<TextMesh>(true);
                label.font = config.BodyFont;
                label.fontStyle = FontStyle.Normal;
                label.GetComponent<MeshRenderer>().sharedMaterial = config.BodyFont.material;
            }
            PrefabUtility.SaveAsPrefabAsset(GameObject.Find("Booth decoration"), "Assets/Bullseye/Prefabs/BoothDecoration.prefab");
            string prefabPath = "Assets/Bullseye/Prefabs/BoothTarget.prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            TextMesh prefabLabel = prefab.GetComponentInChildren<TextMesh>(true);
            prefabLabel.font = config.BodyFont;
            prefabLabel.fontStyle = FontStyle.Normal;
            prefabLabel.GetComponent<MeshRenderer>().sharedMaterial = config.BodyFont.material;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static Sprite LoadSprite(string filename)
        {
            string path = Art + filename;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }

        private static void Dress(SpriteRenderer renderer, Sprite sprite, float width, float height)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        }
    }
}

