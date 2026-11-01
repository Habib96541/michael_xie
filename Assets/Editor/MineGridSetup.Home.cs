using System;
using System.IO;
using MineGrid;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static partial class MineGridSetup
{
    private static (GameObject Page, RectTransform Hud) BuildHome(Transform fit, GameUI ui)
    {
        var home = Page("Home", fit);
        Set(ui, "home", home);
        Icon("Home Logo", home.transform, .075f, .76f, .925f, .98f, _theme.logo);
        Text("Home Subtitle", home.transform, .12f, .70f, .88f, .76f, "Your next gem is waiting", 26);
        var heroPanel = Rect("Treasure Display", home.transform, .18f, .48f, .82f, .70f);
        Icon("Hero Left Coin", heroPanel, .02f, .05f, .29f, .54f, _theme.coin).rectTransform.localRotation = Quaternion.Euler(0, 0, -20);
        var mainGem = Icon("Hero Gem", heroPanel, .25f, .01f, .75f, .99f, _theme.gem);
        mainGem.gameObject.AddComponent<IdleFloat>();
        Icon("Hero Right Coin", heroPanel, .72f, .32f, .98f, .85f, _theme.coin).rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
        Link(Button("Play", home.transform, .10f, .40f, .90f, .495f, "Play Mines", _green), ui.Play);
        Link(Button("Shop", home.transform, .10f, .295f, .90f, .385f, "Treasure shop", new Color(.48f, .30f, .14f)), ui.Shop);
        var daily = Button("Daily", home.transform, .10f, .16f, .485f, .28f, "Daily treasure", new Color(.28f, .22f, .49f));
        Link(daily, ui.Wheel);
        Place(daily.GetComponentInChildren<TMP_Text>().rectTransform, .08f, .05f, .92f, .37f);
        Icon("Daily Coin", daily.transform, .31f, .40f, .69f, .97f, _theme.coin);
        Set(ui, "dailyLabel", daily.GetComponentInChildren<TMP_Text>());
        var progress = Button("Progression", home.transform, .515f, .16f, .90f, .28f, "Milestones", new Color(.18f, .29f, .43f));
        Link(progress, ui.Progression);
        Place(progress.GetComponentInChildren<TMP_Text>().rectTransform, .08f, .05f, .92f, .37f);
        Icon("Progress Gem", progress.transform, .31f, .40f, .69f, .97f, _theme.gem);
        Link(Button("Rules", home.transform, .10f, .075f, .485f, .145f, "How to play", _panel), ui.HowToPlay);
        Link(Button("Settings", home.transform, .515f, .075f, .90f, .145f, "Settings", _panel), ui.Settings);
        var hud = Card("Progress HUD", home.transform, .10f, .005f, .90f, .06f, _ink);
        Set(ui, "progression", Text("Progress", hud, .03f, .08f, .66f, .92f, "", 18, TextAlignmentOptions.Left));
        var claim = Button("Claim", hud, .68f, .14f, .98f, .86f, "Claim", _green);
        Set(ui, "claim", claim);
        return (home, hud);
    }
}
