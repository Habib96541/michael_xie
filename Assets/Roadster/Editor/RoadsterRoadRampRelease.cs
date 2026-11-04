using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TeslaRoadster;
using Object = UnityEngine.Object;

static class RoadsterRoadRampRelease
{
    const string Phase = "Roadster.RoadRampRelease";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static RoadsterGame game;
    static RoadsterVehicle car;
    static Vector3 staged;
    static double deadline;
    static float time;
    static int step;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Roadster.unity");
        SessionState.SetInt(Phase, 1);
        Resume();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (SessionState.GetInt(Phase, 0) == 0)
            return;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        deadline = EditorApplication.timeSinceStartup + 90;
    }

    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, $"Timed out at step {step}");
            if (SessionState.GetInt(Phase, 0) == 2)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Build();
                return;
            }
            if (!EditorApplication.isPlaying)
                return;
            if (game == null)
            {
                game = Object.FindFirstObjectByType<RoadsterGame>();
                car = Object.FindFirstObjectByType<RoadsterVehicle>();
                if (game == null || car == null) return;
                Time.timeScale = 1.5f;
                var ramp = GameObject.Find("Launch platform");
                Check(ramp != null && Mathf.Abs(ramp.transform.position.x) < 0.001f, "Ramp is centered on the highway");
                Check(ramp.transform.forward.z < -0.99f, "Inverted exit points along the highway");
                Check(Mathf.Abs(car.transform.position.x) < 0.001f, "Car starts in center lane");
            }
            switch (step)
            {
                case 0:
                    if (State != "Staged") return;
                    staged = car.transform.position;
                    Check(staged.y > 6f && staged.z < -20f && Mathf.Abs(staged.x) < 0.001f, "Vertical hold above center lane");
                    Capture("road-ramp-staged.png");
                    time = Time.time;
                    step++;
                    break;
                case 1:
                    if (Time.time - time < 0.5f) return;
                    Check(State == "Staged" && Vector3.Distance(staged, car.transform.position) < 0.001f, "Car holds until Start");
                    Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "Start").onClick.Invoke();
                    Check(State == "Countdown", "Start begins countdown");
                    step++;
                    break;
                case 2:
                    if (State != "Launch" || Get<float>("rampElapsed") < 0.76f) return;
                    Check(car.transform.forward.z > 0.99f && car.transform.up.y < -0.90f, "Roadster leaves the ramp inverted and heading down the road");
                    Capture("road-ramp-release.png");
                    step++;
                    break;
                case 3:
                    if (State != "Launch" || Get<float>("rampElapsed") < 1.38f) return;
                    Capture("road-ramp-air.png");
                    step++;
                    break;
                case 4:
                    if (State == "Launch")
                    {
                        Vector3 center = car.transform.position + car.transform.up * 0.56f;
                        Vector3 view = Camera.main.WorldToViewportPoint(center);
                        Check(Mathf.Abs(center.x) < 0.001f, "Car stays over center lane during launch");
                        Check(view.z > 0 && view.x > 0.05f && view.x < 0.95f && view.y > 0.05f && view.y < 0.95f, "Launch stays in camera frame");
                        return;
                    }
                    if (State != "Driving") return;
                    Check(Mathf.Abs(car.transform.position.x) < 0.001f && Mathf.Abs(car.transform.position.z) < 0.001f
                        && car.transform.forward.z > 0.99f && car.transform.up.y > 0.99f, "Roadster lands upright in center lane");
                    Check(Mathf.Abs(Camera.main.aspect - 2.17f) < 0.01f, "2.17:1 game camera");
                    Debug.Log("ROAD_RAMP_PLAYMODE_PASS");
                    SessionState.SetInt(Phase, 2);
                    EditorApplication.ExitPlaymode();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Road ramp verification failed at step {step}. {ex}");
            SessionState.SetInt(Phase, 0);
            EditorApplication.Exit(1);
        }
    }

    static string State => Get<object>("state").ToString();
    static T Get<T>(string name) => (T)typeof(RoadsterGame).GetField(name, Private).GetValue(game);
    static void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }

    static void Build()
    {
        EditorApplication.update -= Tick;
        SessionState.SetInt(Phase, 0);
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var path = Path.Combine(Environment.GetEnvironmentVariable("ROADSTER_MARINE_TEST"), "RoadRampRelease", "Cloudflare-WebGL");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        BuildReport report;
        try
        {
            PlayerSettings.WebGL.decompressionFallback = true;
            report = BuildPipeline.BuildPlayer(scenes, path, BuildTarget.WebGL, BuildOptions.None);
        }
        finally
        {
            PlayerSettings.WebGL.decompressionFallback = fallback;
            AssetDatabase.SaveAssets();
        }
        Debug.Log($"ROAD_RAMP_WEBGL {report.summary.result} {report.summary.totalSize} bytes {path}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void Capture(string name)
    {
        var camera = Camera.main;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var design = (RectTransform)canvas.transform.Find("2.17:1 layout");
        var viewport = camera.rect;
        var anchors = design.anchorMin;
        var scale = design.localScale;
        var rt = new RenderTexture(1302, 600, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        camera.targetTexture = rt;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.aspect = 2.17f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        design.anchorMin = design.anchorMax = new Vector2(0.5f, 0.5f);
        design.localScale = Vector3.one;
        foreach (Transform child in canvas.transform)
            if (child.name == "Viewport matte") child.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("ROADSTER_MARINE_TEST"), name), tex.EncodeToPNG());
        RenderTexture.active = old;
        camera.targetTexture = null;
        camera.rect = viewport;
        camera.aspect = 2.17f;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        design.anchorMin = design.anchorMax = anchors;
        design.localScale = scale;
        foreach (Transform child in canvas.transform)
            if (child.name == "Viewport matte") child.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Object.Destroy(tex);
        Object.Destroy(rt);
    }
}

