using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Watermelon;

public static class WatermelonPlayModeCheck
{
    private static bool running;

    [MenuItem("Watermelon/Check Round Flow (Play Mode)")]
    public static async void CheckRoundFlow()
    {
        if (!Application.isPlaying || running)
        {
            Debug.Log("Enter Play Mode in the Watermelon scene before running the round check.");
            return;
        }

        WatermelonGame game = UnityEngine.Object.FindFirstObjectByType<WatermelonGame>();
        if (game == null)
            return;

        running = true;
        float originalTimeScale = Time.timeScale;
        UnityEngine.Random.State originalRandomState = UnityEngine.Random.state;
        string result;
        try
        {
            Time.timeScale = 8f;
            UnityEngine.Random.InitState(20261102);
            var gameData = new SerializedObject(game);
            int minimum = gameData.FindProperty("minimumBurstCount").intValue;
            int maximum = gameData.FindProperty("maximumBurstCount").intValue;
            float increment = gameData.FindProperty("multiplierPerBand").floatValue;
            float interval = gameData.FindProperty("bandInterval").floatValue;
            var visual = (WatermelonVisual)gameData.FindProperty("visual").objectReferenceValue;
            var visualData = new SerializedObject(visual);
            var whole = (Transform)visualData.FindProperty("wholeWatermelon").objectReferenceValue;
            var bands = (Transform)visualData.FindProperty("bandsRoot").objectReferenceValue;
            var fragments = (Transform)visualData.FindProperty("fragmentsRoot").objectReferenceValue;
            Rigidbody[] bodies = fragments.GetComponentsInChildren<Rigidbody>(true);
            var ui = (WatermelonUI)gameData.FindProperty("userInterface").objectReferenceValue;
            var uiData = new SerializedObject(ui);
            var addButton = (Button)uiData.FindProperty("addBandButton").objectReferenceValue;
            var restartButton = (Button)uiData.FindProperty("restartButton").objectReferenceValue;
            var multiplier = (Text)uiData.FindProperty("multiplier").objectReferenceValue;
            var playButton = (Button)uiData.FindProperty("playButton").objectReferenceValue;
            var menuButton = (Button)uiData.FindProperty("menuButton").objectReferenceValue;
            var menuRoot = (GameObject)uiData.FindProperty("menuRoot").objectReferenceValue;
            var gameplayRoot = (GameObject)uiData.FindProperty("gameplayRoot").objectReferenceValue;
            game.TryGetComponent(out AudioSource audioSource);
            var burstCounts = new List<int>();

            for (int round = 0; round < 12; round++)
            {
                playButton.onClick.Invoke();
                await Awaitable.NextFrameAsync();
                Require(game.IsPlaying && gameplayRoot.activeSelf && !menuRoot.activeSelf, "Play must show gameplay and enable band input.");
                Require(game.BandCount == 0 && Mathf.Approximately(game.Multiplier, 1f), "Restart must reset count and multiplier.");
                Require(bands.childCount == 0, "Restart must remove all previous bands.");
                Require(whole.gameObject.activeSelf && !fragments.gameObject.activeSelf, "Restart must restore the whole watermelon.");
                Require(addButton.gameObject.activeSelf && !restartButton.gameObject.activeSelf, "Restart must restore the Add Band button.");
                foreach (Rigidbody body in bodies)
                    Require(body.isKinematic, "Restart must stop fragment physics.");

                while (!game.HasBurst)
                {
                    int previousCount = game.BandCount;
                    addButton.onClick.Invoke();
                    Require(game.BandCount == previousCount + 1, "An accepted press must add exactly one band.");
                    Require(Mathf.Approximately(game.Multiplier, 1f + game.BandCount * increment), "The multiplier must match the band count.");
                    Require(multiplier.text == $"{game.Multiplier:0.00}x", "The multiplier display must match the round.");
                    Require(bands.childCount == game.BandCount, "Each counted band must have a visible instance.");
                    addButton.onClick.Invoke();
                    Require(game.BandCount == previousCount + 1, "The cooldown must reject an immediate second press.");
                    Require(game.BandCount <= maximum, "The round must burst by the configured maximum.");
                    await Awaitable.WaitForSecondsAsync(interval + 0.03f);
                }

                Require(game.BandCount >= minimum, "The round must survive until the configured minimum.");
                Require(!whole.gameObject.activeSelf, "A burst must hide the whole watermelon.");
                Require(fragments.gameObject.activeSelf, "A burst must show fragments.");
                Require(!addButton.gameObject.activeSelf && restartButton.gameObject.activeSelf, "A burst must replace Add Band with Try Again.");
                foreach (Rigidbody body in bodies)
                    Require(!body.isKinematic, "A burst must activate fragment physics.");
                int finalCount = game.BandCount;
                game.AddBand();
                Require(game.BandCount == finalCount, "A finished round must reject extra bands.");
                burstCounts.Add(finalCount);
                restartButton.onClick.Invoke();
                Require(game.BandCount == 0 && !game.HasBurst, "The restart button must begin a fresh round.");
                menuButton.onClick.Invoke();
                Require(!game.IsPlaying && menuRoot.activeSelf && !gameplayRoot.activeSelf, "Menu must hide gameplay and end the round.");
                game.AddBand();
                Require(game.BandCount == 0 && !game.CanAddBand, "The menu must block band input.");
                Require(!audioSource.isPlaying, "Returning to the menu must stop round sounds.");
                Require(whole.gameObject.activeSelf && !fragments.gameObject.activeSelf, "The menu must restore the whole watermelon.");
                await Awaitable.NextFrameAsync();
                Require(bands.childCount == 0, "The menu must clear the previous bands.");
            }

            Require(new HashSet<int>(burstCounts).Count > 1, "Fresh rounds must vary their hidden burst count.");
            result = $"PASS: 12 rounds. Burst counts: {string.Join(", ", burstCounts)}. Verified Play and Menu navigation, menu input lock and sound cleanup, button callbacks, multiplier, band instances, cooldown, burst limits, fragment physics, terminal input lock, and restart cleanup.";
            Debug.Log(result);
        }
        catch (Exception exception)
        {
            result = $"FAIL: {exception}";
            Debug.LogError(result);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            UnityEngine.Random.state = originalRandomState;
            if (game != null && Application.isPlaying)
                game.ReturnToMenu();
            running = false;
        }
        File.WriteAllText("Temp/WatermelonPlayModeCheck.txt", result);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
