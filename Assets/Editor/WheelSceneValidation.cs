using System;
using System.IO;
using System.Reflection;
using SpinTheWheel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[InitializeOnLoad]
public static class WheelSceneValidation
{
    private const string RunningKey = "WheelSprintValidation";
    private const string ReportPath = "Temp/Sprint_01_validation.txt";
    private static WheelController controller;
    private static WheelMenuController menu;
    private static RectTransform wheel;
    private static Button button;
    private static TMP_Text result;
    private static TMP_Text[] labels;
    private static float[] multipliers;
    private static int current;
    private static int completed;
    private static double started;
    private static bool waiting;
    private static Random.State previousRandomState;

    static WheelSceneValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Spin the Wheel/Run Sprint Validation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode first.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene("Assets/Scenes/SpinTheWheel.unity");
        SessionState.SetBool(RunningKey, true);
        File.WriteAllText(ReportPath, "Running Play mode validation.\n");
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(RunningKey, false))
            return;
        if (change != PlayModeStateChange.EnteredPlayMode)
            return;

        try
        {
            previousRandomState = Random.state;
            menu = Object.FindFirstObjectByType<WheelMenuController>();
            if (menu != null)
            {
                Require(menu.IsMenuOpen, "Scene starts at the main menu");
                MenuButton("playButton").onClick.Invoke();
                Require(!menu.IsMenuOpen, "Play button opens the game");
            }
            controller = Object.FindFirstObjectByType<WheelController>();
            Require(controller != null && controller.enabled, "Controller initialized");
            wheel = Field<RectTransform>("wheel");
            button = Field<Button>("spinButton");
            result = Field<TMP_Text>("resultLabel");
            labels = Field<TMP_Text[]>("segmentLabels");
            multipliers = Field<float[]>("multipliers");
            Require(labels.Length == 20 && multipliers.Length == 20, "20 labeled multipliers");
            for (int index = 0; index < labels.Length; index++)
                Require(labels[index].text == WheelController.FormatMultiplier(multipliers[index]), "Label " + index);

            InputSystemUIInputModule module = Object.FindFirstObjectByType<InputSystemUIInputModule>();
            Require(module != null && module.point.action != null && module.leftClick.action != null &&
                module.submit.action != null, "New Input System references survive scene reload");

            typeof(WheelController).GetField("spinDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 0.1f);
            completed = 0;
            current = 0;
            waiting = false;
            EditorApplication.update += Tick;
        }
        catch (Exception exception)
        {
            Finish("FAIL: " + exception);
        }
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Play mode ended before validation finished.");
            if (!waiting)
            {
                if (current >= 40)
                {
                    Finish("PASS: 40 spins completed, all 20 outcomes verified twice.\n" +
                        "PASS: labels and reported multipliers match the segment at the fixed pointer.\n" +
                        "PASS: the winning label is upright at the fixed pointer for every outcome.\n" +
                        "PASS: repeated requests during a spin do not change its selected outcome.\n" +
                        "PASS: button locks while spinning and unlocks after each result.\n" +
                        "PASS: spin completes with Time.timeScale = 0 using unscaled time.\n" +
                        "PASS: New Input System action references survive saving and reloading.\n" +
                        (menu == null ? "" : "PASS: menu startup, Play, return, preserved results, and navigation guard verified.\n"));
                    return;
                }

                int target = current % 20;
                int seed = 0;
                do { Random.InitState(seed++); } while (Random.Range(0, 20) != target);
                Random.InitState(seed - 1);
                Time.timeScale = current >= 20 ? 0f : 1f;
                button.onClick.Invoke();
                Require(controller.IsSpinning && !button.interactable, "Spin locks button");
                Require(result.text == "--", "Result hidden during animation");
                for (int attempt = 0; attempt < 10; attempt++)
                    controller.Spin();
                if (menu != null)
                {
                    menu.ShowMenu();
                    Require(!menu.IsMenuOpen && controller.IsSpinning, "Menu cannot interrupt a spin");
                }
                started = EditorApplication.timeSinceStartup;
                waiting = true;
                return;
            }

            if (controller.IsSpinning)
            {
                Require(EditorApplication.timeSinceStartup - started < 10f, "Spin finished before timeout");
                return;
            }

            int expected = current % 20;
            Require(controller.SelectedIndex == expected, "Chosen segment preserved through repeated clicks");
            Require(button.interactable, "Button unlocked");
            Require(Mathf.Approximately(controller.ResultMultiplier, multipliers[expected]), "Award matches selected multiplier");
            Require(result.text == labels[expected].text, "Result matches wheel label");
            Vector3 labelDirection = labels[expected].transform.position - wheel.position;
            Require(Mathf.Abs(Vector3.SignedAngle(Vector3.up, labelDirection, Vector3.forward)) < 0.02f,
                "Selected label is physically under the fixed pointer");
            Require(Vector3.Dot(labels[expected].transform.up, Vector3.up) > 0.9999f,
                "Selected label is upright at the fixed pointer");
            Require(wheel.localEulerAngles.z >= 0f && wheel.localEulerAngles.z < 360f, "Rotation normalized");
            if (menu != null)
            {
                MenuButton("menuButton").onClick.Invoke();
                Require(menu.IsMenuOpen && !controller.gameObject.activeInHierarchy, "Menu button hides the game");
                MenuButton("playButton").onClick.Invoke();
                Require(!menu.IsMenuOpen && controller.gameObject.activeInHierarchy, "Play restores the game");
                Require(controller.SelectedIndex == expected && result.text == labels[expected].text,
                    "Returning to the game preserves the last result");
            }
            completed++;
            current++;
            waiting = false;
        }
        catch (Exception exception)
        {
            Finish("FAIL after " + completed + " spins: " + exception);
        }
    }

    private static T Field<T>(string name)
    {
        return (T)typeof(WheelController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
    }

    private static Button MenuButton(string name)
    {
        return (Button)typeof(WheelMenuController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Finish(string report)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(RunningKey, false);
        Time.timeScale = 1f;
        Random.state = previousRandomState;
        File.WriteAllText(ReportPath, report);
        Debug.Log(report);
        EditorApplication.ExitPlaymode();
    }
}
