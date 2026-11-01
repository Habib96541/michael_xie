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
    private static GameObject BuildWheel(GameConfig config, Transform fit, GameUI ui)
    {
        var wheelPage = Page("Daily Rewards", fit);
        Set(ui, "wheelPage", wheelPage);
        Text("Wheel Title", wheelPage.transform, .06f, .865f, .94f, .98f, "Daily treasure", 43, TextAlignmentOptions.Center, true);
        Text("Wheel Help", wheelPage.transform, .08f, .80f, .92f, .865f, "A free spin. A little golden luck.", 22);
        var wheelArea = Rect("Wheel Area", wheelPage.transform, .09f, .30f, .91f, .79f);
        var wheel = Icon("Wheel", wheelArea, 0, 0, 1, 1, _theme.wheel);
        var wf = wheel.gameObject.AddComponent<AspectRatioFitter>();
        wf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        wf.aspectRatio = 1;
        Set(ui, "wheel", wheel.rectTransform);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60 * Mathf.Deg2Rad, x = .5f + Mathf.Sin(a) * .30f, y = .5f + Mathf.Cos(a) * .30f;
            var group = Rect("Reward " + i, wheel.transform, x - .09f, y - .115f, x + .09f, y + .115f);
            group.localRotation = Quaternion.Euler(0, 0, -i * 60);
            Icon("Reward Coin", group, .14f, .36f, .86f, .97f, _theme.coin);
            Text("Reward Amount", group, .02f, .01f, .98f, .40f, config.wheelCoins[i].ToString(), 29, TextAlignmentOptions.Center, true);
        }

        var hub = Card("Wheel Hub", wheel.transform, .395f, .395f, .605f, .605f, _panel);
        Icon("Hub Gem", hub, .08f, .08f, .92f, .92f, _theme.gem);
        var pointer = Icon("Pointer", wheelArea, .44f, .90f, .56f, 1.07f, _theme.gem);
        pointer.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
        Set(ui, "cooldown", Text("Cooldown", wheelPage.transform, .06f, .23f, .94f, .29f, "", 22));
        Set(ui, "spin", Button("Spin", wheelPage.transform, .10f, .11f, .90f, .215f, "Spin free", _green));
        Link(Button("Wheel Home", wheelPage.transform, .25f, .02f, .75f, .085f, "Home", _panel), ui.Home);
        return wheelPage;
    }
}
