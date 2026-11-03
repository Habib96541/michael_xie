using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShatterLeap.Editor
{
    public static class PolishRevision
    {
        [MenuItem("ShatterLeap/Apply Sprint 1 Polish Lighting")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(SprintWorkflow.ScenePath);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.19f, 0.25f, 0.34f);
            RenderSettings.fogColor = new Color(0.022f, 0.04f, 0.065f);
            RenderSettings.fogDensity = 0.012f;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Soft key light")
                {
                    Light key = root.GetComponent<Light>();
                    key.intensity = 1.15f;
                    key.color = new Color(0.76f, 0.87f, 1);
                    key.shadows = LightShadows.None;
                    key.transform.rotation = Quaternion.Euler(48, -35, 0);
                }
                if (root.name != "Supplied arena (decorative)") { continue; }
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.name.ToLowerInvariant().Contains("ground_checkers"))
                    {
                        renderer.transform.position += Vector3.up * (-32 - renderer.bounds.center.y);
                    }
                    if (!renderer.enabled || !renderer.name.ToLowerInvariant().Contains("drap")) { continue; }
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        string path = "Assets/ShatterLeap/Materials/Polish curtain " + i + ".mat";
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (!material)
                        {
                            material = Object.Instantiate(materials[i]);
                            material.SetColor("_BaseColor", new Color(0.38f, 0.24f, 0.38f));
                            material.SetFloat("_Smoothness", 0.16f);
                            AssetDatabase.CreateAsset(material, path);
                        }
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                }
            }
            AddFill("Polish teal fill", Quaternion.Euler(25, 110, 0), new Color(0.38f, 0.80f, 0.79f), 0.55f);
            AddFill("Polish warm rim", Quaternion.Euler(30, 185, 0), new Color(1, 0.66f, 0.48f), 0.45f);
            Material glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShatterLeap/Materials/Teal glass.mat");
            glass.SetColor("_BaseColor", new Color(0.10f, 0.44f, 0.53f, 0.6f));
            glass.SetFloat("_Smoothness", 0.45f);
            glass.SetFloat("_SpecularHighlights", 0);
            glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(glass);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ShatterLeap/Config/Sprint1.asset");
            config.FallResultDelay = 2.6f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("SprintDoc/PolishRevision");
            File.WriteAllText("SprintDoc/PolishRevision/Lighting.txt", "Key: cool 1.15, teal vertex fill 0.55, warm rim 0.45; ambient slate; exponential-squared fog 0.012. No HDR, SSAO, deferred rendering or realtime shadows introduced. Scene-only curtain material overrides preserve originals.\n");
        }

        private static void AddFill(string name, Quaternion rotation, Color color, float intensity)
        {
            GameObject item = GameObject.Find(name);
            if (!item) { item = new GameObject(name, typeof(Light)); }
            Light light = item.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            item.transform.rotation = rotation;
        }
    }
}
