#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash.EditorTools
{
    /// <summary>Checks real menu transforms at phone, tablet, desktop and rotated render sizes.</summary>
    public static class MobileLayoutChecks
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var app = new GameObject("Layout fixture").AddComponent<NeonDashApp>();
            var store = new RecordStore();
            store.Load();
            SetField(app, "store", store);
            SetField(app, "seed", store.LastSeed);
            SetField(app, "<Store>k__BackingField", store);
            SetField(app, "<Audio>k__BackingField", app.gameObject.AddComponent<AudioDirector>());

            var camera = new GameObject("Layout Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.035f, 0.12f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            RectTransform safe = UiKit.CreateCanvas("Layout Canvas", 10);
            Canvas canvas = safe.GetComponentInParent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            ResponsiveCanvas responsive = canvas.GetComponent<ResponsiveCanvas>();
            UiScreen[] screens = {
                UiScreen.Create<UiLobby>(app, safe, "Lobby"),
                UiScreen.Create<UiHud>(app, safe, "HUD"),
                UiScreen.Create<UiResult>(app, safe, "Results"),
                UiScreen.Create<UiRecords>(app, safe, "Records"),
                UiScreen.Create<UiPause>(app, safe, "Pause")
            };
            Vector2Int[] sizes = {
                new Vector2Int(320, 480), new Vector2Int(360, 680),
                new Vector2Int(390, 720), new Vector2Int(430, 800),
                new Vector2Int(540, 1020), new Vector2Int(1080, 2040),
                new Vector2Int(680, 360), new Vector2Int(844, 390),
                new Vector2Int(768, 1024), new Vector2Int(1280, 720),
                new Vector2Int(360, 680) // Return to portrait after rotation.
            };
            int checkedControls = 0;
            Directory.CreateDirectory("Deliverables/MobileChecks");
            foreach (Vector2Int size in sizes)
            {
                var target = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = target;
                camera.Render();
                Canvas.ForceUpdateCanvases();
                responsive.Apply();
                Canvas.ForceUpdateCanvases();
                responsive.Apply();
                Canvas.ForceUpdateCanvases();
                foreach (UiScreen screen in screens)
                {
                    screen.gameObject.SetActive(true);
                    if (screen is UiLobby || screen is UiHud)
                        screen.SendMessage("RefreshLayout", SendMessageOptions.RequireReceiver);
                    screen.GetComponent<CanvasGroup>().alpha = 1f;
                    Canvas.ForceUpdateCanvases();
                    foreach (Selectable control in screen.GetComponentsInChildren<Selectable>(true))
                    {
                        AssertInside((RectTransform)control.transform, safe, size);
                        checkedControls++;
                    }
                    foreach (RectTransform rect in screen.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (rect.name == "Geometry Dash Logo" || rect.name == "Launch console"
                            || rect.name == "Run report" || rect.name == "Record archive" || rect.name == "Pause console")
                            AssertInside(rect, safe, size);
                    }
                    if (screen is UiLobby && size == new Vector2Int(360, 680))
                        Capture(camera, target, "Deliverables/MobileChecks/portrait-360x680.png");
                    if (screen is UiLobby && size == new Vector2Int(844, 390))
                        Capture(camera, target, "Deliverables/MobileChecks/landscape-844x390.png");
                    screen.gameObject.SetActive(false);
                }
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                Debug.Log("MOBILE LAYOUT PASS " + size.x + "x" + size.y);
            }
            Debug.Log("MOBILE LAYOUT PASS: " + checkedControls + " control bounds across " + sizes.Length + " render sizes; all five screens fit.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }

        static void AssertInside(RectTransform item, RectTransform safe, Vector2Int size)
        {
            var corners = new Vector3[4];
            item.GetWorldCorners(corners);
            Rect bounds = safe.rect;
            foreach (Vector3 corner in corners)
            {
                Vector3 point = safe.InverseTransformPoint(corner);
                if (point.x < bounds.xMin - 1f || point.x > bounds.xMax + 1f
                    || point.y < bounds.yMin - 1f || point.y > bounds.yMax + 1f)
                    throw new Exception("Mobile clipping at " + size + ": " + item.name + " at " + point + " outside " + bounds
                        + "; screen " + ((RectTransform)item.GetComponentInParent<UiScreen>().transform).rect
                        + "; scale " + item.GetComponentInParent<UiScreen>().transform.localScale);
            }
        }

        static void Capture(Camera camera, RenderTexture target, string path)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = previous;
        }
    }
}
#endif
