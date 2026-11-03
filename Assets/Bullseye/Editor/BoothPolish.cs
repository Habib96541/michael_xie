using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BullseyeBlitz.Editor
{
    public static class BoothPolish
    {
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Bullseye/Scenes/BullseyeBlitz.unity");
            Sprite white = AssetDatabase.LoadAllAssetsAtPath("Assets/Bullseye/Art/White.png").OfType<Sprite>().First();
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.name == "Shelf front" && renderer.GetComponent<BoxCollider2D>() == null)
                {
                    renderer.gameObject.layer = 9;
                    renderer.gameObject.AddComponent<BoxCollider2D>();
                }
                if (renderer.GetComponent<BoothTarget>() != null)
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    TextMesh label = renderer.GetComponentInChildren<TextMesh>(true);
                    label.characterSize = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y) * .055f;
                    label.fontStyle = FontStyle.Bold;
                }
            }
            if (GameObject.Find("Booth decoration") == null)
            {
                GameObject decoration = new GameObject("Booth decoration");
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bullseye/Art/UnlitSprites.mat");
                for (int row = 0; row < 3; row++)
                {
                    GameObject maskObject = new GameObject("Shelf reveal mask " + row);
                    maskObject.transform.SetParent(decoration.transform);
                    maskObject.transform.position = new Vector3(0, 2.25f - row * 2.05f + .22f, 0);
                    maskObject.transform.localScale = new Vector3(12, 1.5f, 1);
                    SpriteMask mask = maskObject.AddComponent<SpriteMask>(); mask.sprite = white;
                    mask.isCustomRangeActive = true;
                    mask.frontSortingLayerID = SortingLayer.NameToID("Targets");
                    mask.frontSortingOrder = row * 10 + 2;
                    mask.backSortingLayerID = SortingLayer.NameToID("Targets");
                    mask.backSortingOrder = row * 10 - 1;
                }
                for (int i = 0; i < 25; i++)
                {
                    SpriteRenderer light = new GameObject("Marquee diamond").AddComponent<SpriteRenderer>();
                    light.transform.SetParent(decoration.transform);
                    light.sprite = white; light.sharedMaterial = material;
                    light.color = i % 2 == 0 ? new Color(1, .86f, .4f) : new Color(1, .55f, .28f);
                    light.transform.position = new Vector3(-5.5f + i * .46f, 4.4f, 0);
                    light.transform.localScale = new Vector3(.13f, .13f, 1);
                    light.transform.rotation = Quaternion.Euler(0, 0, 45);
                    light.sortingLayerName = "Backdrop"; light.sortingOrder = 6;
                }
                PrefabUtility.SaveAsPrefabAsset(decoration, "Assets/Bullseye/Prefabs/BoothDecoration.prefab");
            }
            foreach (SpriteMask mask in Object.FindObjectsByType<SpriteMask>(FindObjectsSortMode.None))
            {
                int row = Mathf.RoundToInt((2.47f - mask.transform.position.y) / 2.05f);
                mask.frontSortingLayerID = SortingLayer.NameToID("Targets");
                mask.frontSortingOrder = row * 10 + 2;
                mask.backSortingLayerID = SortingLayer.NameToID("Targets");
                mask.backSortingOrder = row * 10 - 1;
            }
            PrefabUtility.SaveAsPrefabAsset(GameObject.Find("Booth decoration"), "Assets/Bullseye/Prefabs/BoothDecoration.prefab");
            string targetPath = "Assets/Bullseye/Prefabs/BoothTarget.prefab";
            GameObject targetPrefab = PrefabUtility.LoadPrefabContents(targetPath);
            targetPrefab.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            targetPrefab.GetComponentInChildren<TextMesh>(true).characterSize = targetPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size.y * .055f;
            targetPrefab.GetComponentInChildren<TextMesh>(true).fontStyle = FontStyle.Bold;
            PrefabUtility.SaveAsPrefabAsset(targetPrefab, targetPath);
            PrefabUtility.UnloadPrefabContents(targetPrefab);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
