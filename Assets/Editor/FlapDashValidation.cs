using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FlapDash.Editor
{
    public static class FlapDashValidation
    {
        [Serializable]
        private sealed class Report
        {
            public string status;
            public string unityVersion;
            public string completedUtc;
            public List<string> checks = new();
            public List<string> failures = new();
        }

        private static Report report;
        private static RoundController round;
        private static FlapPlayer player;
        private static PipeCourse course;
        private static Rigidbody2D playerBody;
        private static AudioSource flapAudio;
        private static float observedAudioTime;
        private static double observedAudioClock;
        private static Rigidbody2D movingPipe;
        private static Button button;
        private static RoundView roundView;
        private static TMP_Text staticHeading;
        private static string originalHeadingText;
        private static VisualEffectsSettings effects;
        private static PlayerFeedback playerVisuals;
        private static RoundFeedback roundVisuals;
        private static BackdropMotion backdrop;
        private static Vector3 observedBackdropPosition;
        private static Sprite observedBirdFrame;
        private static Keyboard keyboard;
        private static Mouse mouse;
        private static Touchscreen touchscreen;
        private static InputSettings previousInputSettings;
        private static InputSettings validationInputSettings;
        private static PipeGate testGate;
        private static GameObject collisionObstacle;
        private static int stage;
        private static int minimumFrame;
        private static float readyTime;
        private static float observedVelocity;
        private static float originalGravityScale;
        private static float observedPipeX;
        private static double deadline;
        private static bool oldOptionsEnabled;
        private static EnterPlayModeOptions oldOptions;
        private static bool stopping;

        [MenuItem("Flap Dash/Validate Playable Round")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before running validation.");
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save your scene edits before validation. Unsaved scenes will not be replaced.");

            report = new Report { unityVersion = Application.unityVersion };
            oldOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            oldOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Scenes/FlapDash.unity");
            stage = 0;
            stopping = false;
            deadline = EditorApplication.timeSinceStartup + 60d;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Step;
            Application.logMessageReceived += OnLog;
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
                Require(EditorApplication.timeSinceStartup < deadline, "Validation exceeded 60 seconds.");
                if (!EditorApplication.isPlaying || Time.frameCount < minimumFrame || Time.time < readyTime)
                    return;

                switch (stage)
                {
                    case 0:
                        round = Object.FindFirstObjectByType<RoundController>();
                        player = Object.FindFirstObjectByType<FlapPlayer>();
                        course = Object.FindFirstObjectByType<PipeCourse>();
                        button = Object.FindFirstObjectByType<Button>();
                        Require(round && player && course && button, "Scene references exist.");
                        Require(player.TryGetComponent(out playerBody), "Player has a Rigidbody2D.");
                        originalGravityScale = playerBody.gravityScale;
                        ValidateArtwork();
                        ValidateAudio();
                        PrepareOptionalTitleCheck();
                        Require(round.State == RoundState.Ready && round.Score == 0 && !playerBody.simulated,
                            "Ready state has zero score and no movement.");
                        Check("Ready scene: player stopped, score zero, timer 60.");
                        previousInputSettings = InputSystem.settings;
                        validationInputSettings = Object.Instantiate(previousInputSettings);
                        validationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        validationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings = validationInputSettings;
                        keyboard = InputSystem.AddDevice<Keyboard>();
                        mouse = InputSystem.AddDevice<Mouse>();
                        touchscreen = InputSystem.AddDevice<Touchscreen>();
                        button.onClick.Invoke();
                        Next(1);
                        break;
                    case 1:
                        Require(round.IsPlaying && playerBody.simulated, "Start button begins play.");
                        Require(playerBody.linearVelocity.y <= 0f, "Start does not queue a flap.");
                        Require(!flapAudio.isPlaying, "Start menu action does not chirp.");
                        Check("Ready and start menu actions do not play the flap sound.");
                        Check("Start button begins a round without an automatic flap.");
                        CheckOptionalTitle("Playing");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                        Next(2, 0.08f);
                        break;
                    case 2:
                        Require(playerBody.linearVelocity.y > 0f,
                            $"Space applies an upward flap. Keyboard enabled: {keyboard.enabled}; pressed: {keyboard.spaceKey.isPressed}; velocity: {playerBody.linearVelocity.y}.");
                        observedVelocity = playerBody.linearVelocity.y;
                        Require(flapAudio.isPlaying, "Keyboard flap starts the selected chirp.");
                        Next(3, 0.25f);
                        break;
                    case 3:
                        Require(playerBody.linearVelocity.y < observedVelocity - 1f, "Holding Space does not repeatedly flap.");
                        Check("Space press flaps once; holding it does not auto-flap.");
                        Require(!flapAudio.isPlaying, "Holding input does not repeat the short chirp.");
                        Check("Holding input does not loop or repeat the flap sound.");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Next(4);
                        break;
                    case 4:
                        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
                        Next(5, 0.08f);
                        break;
                    case 5:
                        Require(playerBody.linearVelocity.y > 0f, "Mouse press applies a flap.");
                        Require(flapAudio.isPlaying, "Mouse flap starts the same chirp source.");
                        observedAudioTime = flapAudio.time;
                        observedAudioClock = AudioSettings.dspTime;
                        Check("Mouse press reaches the same flap action.");
                        InputSystem.QueueStateEvent(mouse, new MouseState());
                        Next(6);
                        break;
                    case 6:
                        Require(flapAudio.isPlaying, "Rapid second input arrives while the previous chirp is active.");
                        InputSystem.QueueStateEvent(touchscreen, new TouchState
                        {
                            touchId = 1,
                            phase = UnityEngine.InputSystem.TouchPhase.Began,
                            position = new Vector2(5f, 5f),
                            pressure = 1f
                        });
                        Next(7, 0.08f);
                        break;
                    case 7:
                        Require(playerBody.linearVelocity.y > 0f, "Touch press applies a flap.");
                        Require(flapAudio.isPlaying, "Simulated touch starts the same chirp source.");
                        Require(flapAudio.time < observedAudioTime + AudioSettings.dspTime - observedAudioClock - 0.02d,
                            "Rapid flap restarts playback rather than continuing the earlier chirp.");
                        Check("Rapid consecutive flaps retrigger the single audio source.");
                        Check("Keyboard, mouse and simulated touch flaps start the selected chirp.");
                        Check("Simulated touchscreen press reaches the flap action.");
                        InputSystem.QueueStateEvent(touchscreen, new TouchState
                        {
                            touchId = 1,
                            phase = UnityEngine.InputSystem.TouchPhase.Ended,
                            position = new Vector2(5f, 5f)
                        });
                        playerBody.gravityScale = 0f;
                        playerBody.linearVelocity = Vector2.zero;
                        testGate = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PipeGate>("Assets/Prefabs/PipePair.prefab"));
                        testGate.name = "Validation Passage";
                        testGate.InitializeRound(round);
                        Require(testGate.TryGetComponent(out BoxCollider2D trigger), "Pipe has its score trigger.");
                        testGate.transform.position = player.transform.position - (Vector3)trigger.offset;
                        Physics2D.SyncTransforms();
                        Next(8, 0.08f);
                        break;
                    case 8:
                        Require(round.Score == 1, "Crossing the actual trigger awards one point.");
                        testGate.Pass(player);
                        Require(round.Score == 1, "The same pipe cannot score twice.");
                        Check("Physics passage scores one point; duplicate passage is ignored.");
                        Object.Destroy(testGate.gameObject);
                        Next(9, 1.4f);
                        break;
                    case 9:
                        Require(round.IsPlaying && course.ActivePipeCount > 0, "Authored course spawns pipes.");
                        Require(Object.FindFirstObjectByType<PipeGate>().TryGetComponent(out movingPipe), "Spawned pipe has its Rigidbody2D.");
                        observedPipeX = movingPipe.position.x;
                        Next(10, 0.1f);
                        break;
                    case 10:
                        Require(movingPipe.position.x < observedPipeX, "Pipe moves left while playing.");
                        Check("Authored course spawns and moves pipes during play.");
                        round.EndRound("Validation stop.");
                        Require(!flapAudio.isPlaying, "Ending a round stops flap audio.");
                        Check("Round end clears active flap audio.");
                        round.AddPoint();
                        Require(round.Score == 1 && !playerBody.simulated, "Results stops movement and score changes.");
                        observedPipeX = movingPipe.position.x;
                        observedBackdropPosition = backdrop.FirstLayer.position;
                        Next(11, 0.1f);
                        break;
                    case 11:
                        Require(Mathf.Approximately(movingPipe.position.x, observedPipeX), "Results freezes pipe movement.");
                        Require(backdrop.FirstLayer.position == observedBackdropPosition, "Results freezes optional backdrop motion.");
                        Check("Ending a round freezes the player, pipes, and score.");
                        CheckOptionalTitle("Results");
                        playerBody.gravityScale = originalGravityScale;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                        Next(12, 0.08f);
                        break;
                    case 12:
                        Require(round.IsPlaying && round.Score == 0 && round.RemainingTime > 59f && course.ActivePipeCount == 0,
                            "Restart resets score, time, and old pipes.");
                        Require(playerBody.linearVelocity.y <= 0f, "Restart has no stale flap input.");
                        Require(!flapAudio.isPlaying, "Restart menu action has no stale chirp.");
                        Check("Restart clears sound and does not produce a menu chirp.");
                        Check("Enter submits Play Again after pointer input; restart clears score, timer, pipes, and pending input.");
                        CheckOptionalTitle("Restart");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        collisionObstacle = new GameObject("Validation Obstacle");
                        collisionObstacle.tag = "Obstacle";
                        collisionObstacle.transform.position = player.transform.position;
                        collisionObstacle.AddComponent<BoxCollider2D>().size = Vector2.one;
                        Physics2D.SyncTransforms();
                        Next(13, 0.1f);
                        break;
                    case 13:
                        Require(round.State == RoundState.Results && !playerBody.simulated && round.RemainingTime > 0f,
                            "Obstacle collision ends the round before timeout.");
                        Check("Real Rigidbody2D obstacle collision ends the round.");
                        Object.Destroy(collisionObstacle);
                        SetDuration(0.15f);
                        button.onClick.Invoke();
                        Next(14, 0.35f);
                        break;
                    case 14:
                        Require(round.State == RoundState.Results && round.RemainingTime == 0f, "Timer expiry ends at zero.");
                        int scoreAtEnd = round.Score;
                        round.AddPoint();
                        Require(round.Score == scoreAtEnd, "Timeout result rejects later scoring.");
                        Check("Timer expiry ends once at zero and freezes the score.");
                        SetDuration(60f);
                        button.onClick.Invoke();
                        Next(15);
                        break;
                    case 15:
                        Require(round.IsPlaying && round.Score == 0 && round.RemainingTime > 59f,
                            "A timed-out round can restart cleanly.");
                        Check("Restart works after both collision and timeout.");
                        effects = Object.FindFirstObjectByType<VisualEffectsSettings>();
                        playerVisuals = player.GetComponent<PlayerFeedback>();
                        roundVisuals = Object.FindFirstObjectByType<RoundFeedback>();
                        playerBody.gravityScale = 0f;
                        playerBody.linearVelocity = Vector2.zero;
                        effects.SetIntensity(0f);
                        observedBackdropPosition = backdrop.FirstLayer.position;
                        playerVisuals.PlayFlap();
                        roundVisuals.PointScored();
                        Next(16, 0.12f);
                        break;
                    case 16:
                        Require(playerVisuals.Visual.localScale == Vector3.one && playerVisuals.ActiveParticles == 0 && roundVisuals.ActiveParticles == 0, "Zero intensity clears decorative motion and particles.");
                        Check("Zero visual intensity retains gameplay and suppresses decorative effects.");
                        Require(backdrop.FirstLayer.position == observedBackdropPosition, "Zero intensity stops cloud movement.");
                        observedBirdFrame = playerVisuals.CurrentFrame;
                        Check("Zero visual intensity freezes imported bird and backdrop animation.");
                        effects.SetIntensity(0.4f);
                        playerVisuals.PlayFlap();
                        Next(17, 0.08f);
                        break;
                    case 17:
                        Require(playerVisuals.Visual.localScale != Vector3.one && Mathf.Approximately(effects.Intensity, 0.4f), "Reduced intensity animates visual children.");
                        Require(player.transform.localScale == Vector3.one && playerBody.rotation == 0f && player.GetComponent<CircleCollider2D>().radius == 0.3f, "Feedback leaves physics root and collider unchanged.");
                        Check("Reduced intensity animates the visual child without changing collider size or root transform.");
                        Require(backdrop.FirstLayer.position != observedBackdropPosition, "Reduced intensity allows gentle backdrop motion.");
                        effects.SetIntensity(1f);
                        playerVisuals.PlayFlap();
                        roundVisuals.PointScored();
                        Next(18, 0.05f);
                        break;
                    case 18:
                        Require(playerVisuals.ActiveParticles > 0 && roundVisuals.ActiveParticles > 0, "Full intensity emits action-linked particles.");
                        Check("Full intensity emits flap and passage particles.");
                        Require(playerVisuals.CurrentFrame != observedBirdFrame, "Imported bird animation advances through its frames.");
                        Check("Imported bird frames and cloud layers animate when visual energy is enabled.");
                        round.EndRound("Effect cleanup test.");
                        round.RestartRound();
                        Next(19);
                        break;
                    case 19:
                        Require(round.IsPlaying && round.Score == 0 && playerVisuals.ActiveParticles == 0 && roundVisuals.ActiveParticles == 0, "Restart immediately clears transient effects.");
                        Require(effects.Intensity == 1f, "Intensity persists across rounds.");
                        Check("Restart clears transient particles immediately and retains chosen intensity.");
                        effects.SetIntensity(0f);
                        round.EndRound("Zero effects restart.");
                        round.RestartRound();
                        Next(20);
                        break;
                    case 20:
                        Require(round.IsPlaying && round.Score == 0 && effects.Intensity == 0f, "Round can restart immediately with effects disabled.");
                        Check("Zero-intensity restart preserves immediate round flow and scoring rules.");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                        Next(21, 0.08f);
                        break;
                    case 21:
                        Require(flapAudio.isPlaying, "Visual Energy does not mute the flap sound.");
                        Check("Flap sound remains independent of Visual Energy.");
                        round.EndRound("Audio cleanup.");
                        Require(!flapAudio.isPlaying, "Ending immediately stops an active chirp.");
                        Stop();
                        break;
                }
            }
            catch (Exception exception)
            {
                report.failures.Add(exception.Message);
                Stop();
            }
        }

        private static void Next(int nextStage, float delay = 0f)
        {
            stage = nextStage;
            minimumFrame = Time.frameCount + 3;
            readyTime = Time.time + delay;
        }

        private static void ValidateArtwork()
        {
            PlayerFeedback visuals = player.GetComponent<PlayerFeedback>();
            backdrop = Object.FindFirstObjectByType<BackdropMotion>();
            Require(visuals.FrameCount == 8 && visuals.CurrentFrame != null && backdrop != null, "Imported bird frames and backdrop exist.");
            var frameSettings = new SerializedObject(visuals);
            SerializedProperty frames = frameSettings.FindProperty("flightFrames");
            for (int index = 0; index < frames.arraySize; index++)
                Require(frames.GetArrayElementAtIndex(index).objectReferenceValue != null, "Every bird frame is assigned.");
            Image header = Object.FindFirstObjectByType<Canvas>().transform.Find("Header").GetComponent<Image>();
            Require(header.sprite != null && AssetDatabase.GetAssetPath(header.sprite).Contains("KenneyUI"), "UI uses the imported artwork.");
            Check("Imported bird frames, UI artwork and backdrop references are assigned.");
            RectTransform track = (RectTransform)header.transform.parent.Find("Effects Settings/Energy Slider");
            RectTransform fill = (RectTransform)track.Find("Slider Fill");
            Require(fill.rect.width <= track.rect.width + 0.01f && fill.rect.height <= track.rect.height + 0.01f,
                "Slider artwork stays within its track.");
            Check("Slider fill remains within the visible track bounds.");

            GameObject pipe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PipePair.prefab");
            foreach (string name in new[] { "Upper Pipe", "Lower Pipe", "Upper Rim", "Lower Rim" })
            {
                Transform section = pipe.transform.Find(name);
                BoxCollider2D collider = section.GetComponent<BoxCollider2D>();
                Vector2 expected = name.EndsWith("Pipe") ? new Vector2(1f, 10f) : new Vector2(1.2f, 0.28f);
                Require(Vector2.Scale(collider.size, section.localScale) == expected, "Pipe collision geometry stays unchanged.");
                Require(section.GetComponentInChildren<SpriteRenderer>().sprite.texture.name == "pipe", "Pipe visual uses imported texture.");
            }
            Check("Imported pipe artwork retains the existing body and rim collision dimensions.");
        }

        private static void PrepareOptionalTitleCheck()
        {
            roundView = Object.FindFirstObjectByType<RoundView>();
            Require(roundView != null, "Round View exists.");
            staticHeading = roundView.transform.Find("Header").GetComponentInChildren<TMP_Text>();
            Require(staticHeading != null, "Header has an active static heading.");
            originalHeadingText = staticHeading.text;
            var viewSettings = new SerializedObject(roundView);
            viewSettings.FindProperty("titleLabel").objectReferenceValue = null;
            viewSettings.ApplyModifiedPropertiesWithoutUndo();
            roundView.enabled = false;
            roundView.enabled = true;
            CheckOptionalTitle("Ready");
        }

        private static void CheckOptionalTitle(string state)
        {
            roundView.RefreshView();
            Require(staticHeading.text == originalHeadingText, "Round updates preserve the separate static heading.");
            Check($"{state} UI works without an optional round-panel title and preserves the static heading.");
        }

        private static void ValidateAudio()
        {
            Require(player.TryGetComponent(out flapAudio), "Player has an audio source.");
            Require(player.GetComponents<AudioSource>().Length == 1 && flapAudio.clip != null, "One source owns the selected clip.");
            Require(flapAudio.clip.loadState == AudioDataLoadState.Loaded && flapAudio.clip.length > 0f && flapAudio.clip.length < 0.3f,
                "The short chirp is loaded before the first flap.");
            Require(!flapAudio.playOnAwake && !flapAudio.loop && Mathf.Approximately(flapAudio.spatialBlend, 0f) && flapAudio.volume > 0f,
                "Flap sound is non-looping 2D audio without autoplay.");
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            Require(listeners.Length == 1 && listeners[0].isActiveAndEnabled, "Scene has one active listener.");
            Require(AudioListener.volume > 0f && !AudioListener.pause && !flapAudio.isPlaying, "Ready audio is available and silent.");
            Check("Selected chirp is loaded on one configured 2D source with one active listener.");
        }

        private static void SetDuration(float seconds)
        {
            SerializedObject settings = new(round);
            settings.FindProperty("roundDuration").floatValue = seconds;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Check(string description)
        {
            report.checks.Add(description);
        }

        private static void Require(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException(description);
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                report.failures.Add(message);
        }

        private static void Stop()
        {
            if (stopping)
                return;

            stopping = true;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
            if (previousInputSettings != null)
                InputSystem.settings = previousInputSettings;
            EditorApplication.ExitPlaymode();
        }

        private static void Finish()
        {
            EditorApplication.update -= Step;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Application.logMessageReceived -= OnLog;
            EditorSettings.enterPlayModeOptionsEnabled = oldOptionsEnabled;
            EditorSettings.enterPlayModeOptions = oldOptions;
            if (validationInputSettings != null)
                Object.DestroyImmediate(validationInputSettings);
            report.status = report.failures.Count == 0 ? "PASS" : "FAIL";
            report.completedUtc = DateTime.UtcNow.ToString("O");
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/FlapDashSmoke.json"));
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == "-flapReport") path = arguments[index + 1];

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log($"Flap Dash validation: {report.status}. Report: {path}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.failures.Count == 0 ? 0 : 1);
        }
    }
}
