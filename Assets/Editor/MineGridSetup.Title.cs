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
    private static GameObject BuildTitle(Transform fit, GameUI ui)
    {
        var title = Page("Title", fit);
        Set(ui, "title", title);
        Icon("Title Logo", title.transform, .07f, .64f, .93f, .92f, _theme.logo);
        var hero = Icon("Title Gem", title.transform, .29f, .32f, .71f, .63f, _theme.gem);
        hero.gameObject.AddComponent<IdleFloat>();
        Text("Title Tagline", title.transform, .12f, .23f, .88f, .32f, "Find your fortune.", 34, TextAlignmentOptions.Center, true);
        Link(Button("Enter", title.transform, .11f, .085f, .89f, .205f, "Let's play", _green), ui.Home);
        Text("Title Hint", title.transform, .12f, .025f, .88f, .075f, "25 tiles. One brilliant adventure.", 21);
        return title;
    }
}
