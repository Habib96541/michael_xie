using System;
using System.IO;
using MineGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MineGridSmoke
{
    public static T Field<T>(GameUI ui, string name)
    {
        object value = name switch
        {
            "round" => ui.CurrentRound,
            "saves" => ui.Persistence,
            "start" => ui.StartControl,
            "spin" => ui.SpinControl,
            "finish" => ui.FinishControl,
            "mineSlider" => ui.MineSelector,
            "tiles" => ui.TileViews,
            "popup" => ui.PopupObject,
            "popupAction" => ui.PopupPrimary,
            "popupSecondary" => ui.PopupSecondary,
            "home" => ui.HomePage,
            "progressionPage" => ui.ProgressionPage,
            "settingsControls" => ui.SettingsControls,
            "reduced" => ui.ReducedToggle,
            "mute" => ui.MuteToggle,
            "title" => ui.TitlePage,
            _ => throw new ArgumentException(name)};
        return (T)value;
    }

    private static void Call(GameUI ui, string name, params object[] args)
    {
        ui.FinishControl.onClick.Invoke();
    }

    [MenuItem("MineGrid/Run Play Mode Smoke Test")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new Exception("Enter Play Mode first.");
        }

        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        if (!ui)
        {
            throw new Exception("Open MineGrid scene.");
        }

        int checks = 0;
        Action<bool, string> check = (ok, msg) =>
        {
            if (!ok)
            {
                throw new Exception(msg);
            }

            checks++;
        };
        var save = Field<SaveService>(ui, "saves");
        string backup = PlayerPrefs.GetString(SaveService.Key);
        var original = JsonUtility.ToJson(save.Data);
        try
        {
            save.Data.reducedEffects = true;
            ui.ClosePopup();
            ui.Play();
            check(Field<Round>(ui, "round").State == RoundState.Setup, "setup");
            var start = Field<Button>(ui, "start");
            var finish = Field<Button>(ui, "finish");
            var slider = Field<Slider>(ui, "mineSlider");
            var tiles = Field<TileView[]>(ui, "tiles");
            slider.value = 3;
            start.onClick.Invoke();
            var r = Field<Round>(ui, "round");
            check(r.State == RoundState.Playing && !slider.interactable, "selection locked");
            finish.onClick.Invoke();
            check(r.State == RoundState.Finished && r.Banked == 0, "zero finish");
            check(Field<GameObject>(ui, "popup").activeSelf, "result popup");
            ui.Again();
            start.onClick.Invoke();
            r = Field<Round>(ui, "round");
            int safe = 0;
            while (r.Board.IsMine(safe))
            {
                safe++;
            }

            ui.Home();
            check(Field<GameObject>(ui, "popup").activeSelf && r.State == RoundState.Playing, "leave confirmation");
            tiles[safe].GetComponent<Button>().onClick.Invoke();
            check(r.Board.SafeRevealed == 0, "popup blocks callbacks");
            Field<Button>(ui, "popupAction").onClick.Invoke();
            check(!Field<GameObject>(ui, "popup").activeSelf && r.State == RoundState.Playing, "cancel resumes");
            tiles[safe].GetComponent<Button>().onClick.Invoke();
            tiles[safe].GetComponent<Button>().onClick.Invoke();
            check(r.Board.SafeRevealed == 1 && r.Score == 30, "repeat callback rejected");
            finish.onClick.Invoke();
            check(r.Banked == 30, "finish banks");
            int coins = save.Data.coins;
            Call(ui, "EndRound");
            check(save.Data.coins == coins, "duplicate completion no reward");
            ui.Again();
            start.onClick.Invoke();
            r = Field<Round>(ui, "round");
            ui.Home();
            Field<Button>(ui, "popupSecondary").onClick.Invoke();
            check(r.State == RoundState.Lost && Field<GameObject>(ui, "home").activeSelf, "confirm forfeits and leaves");
            ui.Play();
            start.onClick.Invoke();
            r = Field<Round>(ui, "round");
            int mine = 0;
            while (!r.Board.IsMine(mine))
            {
                mine++;
            }

            tiles[mine].GetComponent<Button>().onClick.Invoke();
            check(r.State == RoundState.Lost && r.Banked == 0, "mine loss");
            tiles[safe].GetComponent<Button>().onClick.Invoke();
            check(r.State == RoundState.Lost, "post round input");
            ui.Again();
            slider.value = 24;
            start.onClick.Invoke();
            r = Field<Round>(ui, "round");
            for (int i = 0; i < 25; i++)
            {
                if (!r.Board.IsMine(i))
                {
                    tiles[i].GetComponent<Button>().onClick.Invoke();
                }
            }

            check(r.State == RoundState.Won && r.Banked == 240, "automatic win");
            ui.Again();
            ui.Home();
            ui.Progression();
            check(Field<GameObject>(ui, "progressionPage").activeSelf, "milestone panel");
            ui.Home();
            ui.Settings();
            check(Field<GameObject>(ui, "settingsControls").activeSelf, "settings controls");
            Field<Toggle>(ui, "reduced").isOn = false;
            Field<Toggle>(ui, "reduced").isOn = true;
            check(save.Data.reducedEffects, "effects toggle");
            var mute = Field<Toggle>(ui, "mute");
            mute.isOn = !mute.isOn;
            check(ui.GetComponent<AudioSource>().mute == mute.isOn && save.Data.muted == mute.isOn, "mute setting and audio source");
            ui.ClosePopup();
            ui.Home();
            File.WriteAllText("MineGrid-playmode-verification.txt", $"PASS: {checks} Sprint 4 wired UI checks. Setup; selection locking; zero finish; popup; leave/cancel/forfeit; popup callback blocking; duplicate reveal; direct banking; duplicate completion; loss; post-result input; auto-win; milestones; settings and reduced effects.\n");
            Debug.Log($"MineGrid Play Mode smoke passed: {checks} checks");
        }
        finally
        {
            PlayerPrefs.SetString(SaveService.Key, backup);
            PlayerPrefs.Save();
            JsonUtility.FromJsonOverwrite(original, save.Data);
            ui.Audio.Mute(save.Data.muted);
            Field<Toggle>(ui, "mute").SetIsOnWithoutNotify(save.Data.muted);
            Field<Toggle>(ui, "reduced").SetIsOnWithoutNotify(save.Data.reducedEffects);
            ui.ClosePopup();
            ui.Home();
        }
    }
}
