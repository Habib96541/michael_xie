using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FlapDash.Editor
{
    public static class FlapDashPreview
    {
        private static Camera camera;
        private static Canvas canvas;
        private static RoundController round;
        private static RoundView view;
        private static RenderTexture target;
        private static RenderTexture previousTarget;
        private static RenderMode previousCanvasMode;
        private static Camera previousCanvasCamera;
        private static float previousPlaneDistance;
        private static int previousSortingOrder;
        private static bool previousOverrideSorting;
        private static float previousAspect;
        private static float previousTimeScale;
        private static bool previousOptionsEnabled;
        private static EnterPlayModeOptions previousOptions;
        private static string outputDirectory;
        private static int stage;
        private static int animationFrame;
        private static double readyAt;
        private static int minimumFrame;
        private static double deadline;
        private static bool stopping;
        private static bool failed;

        [MenuItem("Flap Dash/Capture Scene Previews")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before capturing previews.");

            outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/Preview"));
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == "-flapPreviewDirectory") outputDirectory = Path.GetFullPath(arguments[index + 1]);
            Directory.CreateDirectory(outputDirectory);
            previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousOptions = EditorSettings.enterPlayModeOptions;
            previousTimeScale = Time.timeScale;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Scenes/FlapDash.unity");
            stage = 0;
            animationFrame = 0;
            readyAt = 0d;
            stopping = false;
            failed = false;
            deadline = EditorApplication.timeSinceStartup + 60d;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Step;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                minimumFrame = Time.frameCount + 3;
            else if (state == PlayModeStateChange.EnteredEditMode && stopping)
                Finish();
        }

        private static void Step()
        {
            if (stopping)
                return;

            try
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException("Scene preview exceeded 60 seconds.");
                EditorApplication.QueuePlayerLoopUpdate();
                if (!EditorApplication.isPlaying || Time.frameCount < minimumFrame || EditorApplication.timeSinceStartup < readyAt)
                    return;

                switch (stage)
                {
                    case 0:
                        PrepareCamera();
                        Next();
                        break;
                    case 1:
                        WriteFrame("FlapDash_Ready.png");
                        round.StartRound();
                        PipeGate prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PipePair.prefab").GetComponent<PipeGate>();
                        PipeGate pipe = Object.Instantiate(prefab, new Vector3(2f, 0f, 0f), Quaternion.identity);
                        pipe.InitializeRound(round);
                        Next();
                        break;
                    case 2:
                        Object.FindFirstObjectByType<PlayerFeedback>().PlayFlap();
                        round.AddPoint();
                        Next(0.02f);
                        break;
                    case 3:
                        WriteFrame($"FlapDash_Animation_{animationFrame:00}.png");
                        if (animationFrame == 3) WriteFrame("FlapDash_Playing.png");
                        animationFrame++;
                        if (animationFrame < 24)
                        {
                            minimumFrame = Time.frameCount + 1;
                            readyAt = EditorApplication.timeSinceStartup + 1d / 30d;
                            break;
                        }
                        round.EndRound("You hit an obstacle.");
                        Next();
                        break;
                    case 4:
                        WriteFrame("FlapDash_Results.png");
                        Object.FindFirstObjectByType<VisualEffectsSettings>().SetIntensity(0f);
                        Next();
                        break;
                    case 5:
                        WriteFrame("FlapDash_NoEffects.png");
                        Stop();
                        break;
                }
            }
            catch (Exception exception)
            {
                failed = true;
                Debug.LogException(exception);
                Stop();
            }
        }

        private static void PrepareCamera()
        {
            camera = Camera.main;
            canvas = Object.FindFirstObjectByType<Canvas>();
            round = Object.FindFirstObjectByType<RoundController>();
            view = Object.FindFirstObjectByType<RoundView>();
            if (camera == null || canvas == null || round == null || view == null)
                throw new InvalidOperationException("The Flap Dash scene is missing preview references.");

            previousTarget = camera.targetTexture;
            previousAspect = camera.aspect;
            previousCanvasMode = canvas.renderMode;
            previousCanvasCamera = canvas.worldCamera;
            previousPlaneDistance = canvas.planeDistance;
            previousSortingOrder = canvas.sortingOrder;
            previousOverrideSorting = canvas.overrideSorting;
            target = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            camera.aspect = 540f / 960f;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            Time.timeScale = 0f;
        }

        private static void WriteFrame(string fileName)
        {
            view.RefreshView();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("The active render pipeline does not support a single-camera render request.");
            RenderPipeline.SubmitRenderRequest(camera, request);

            RenderTexture previousActive = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(outputDirectory, fileName), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(pixels);
            }
            Debug.Log($"FLAP_PREVIEW_FRAME: {Path.Combine(outputDirectory, fileName)}");
        }

        private static void Next(float delay = 0.35f)
        {
            stage++;
            readyAt = EditorApplication.timeSinceStartup + delay;
            minimumFrame = Time.frameCount + 2;
        }

        private static void Stop()
        {
            stopping = true;
            RestoreRenderState();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            else
                Finish();
        }

        private static void RestoreRenderState()
        {
            if (canvas != null)
            {
                canvas.renderMode = previousCanvasMode;
                canvas.worldCamera = previousCanvasCamera;
                canvas.planeDistance = previousPlaneDistance;
                canvas.overrideSorting = previousOverrideSorting;
                canvas.sortingOrder = previousSortingOrder;
            }
            if (camera != null)
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
            }
            if (target != null)
            {
                target.Release();
                Object.DestroyImmediate(target);
                target = null;
            }
            Time.timeScale = previousTimeScale;
        }

        private static void Finish()
        {
            EditorApplication.update -= Step;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousOptions;
            Debug.Log(failed ? "FLAP_PREVIEW_FAIL" : $"FLAP_PREVIEW_PASS: {outputDirectory}");
            if (Application.isBatchMode)
                EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
