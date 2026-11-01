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
    private static (Image Overlay, RectTransform Panel, RectTransform Particles) BuildPopup(Transform fit, GameUI ui)
    {
        var popup = Image("Popup Overlay", fit, 0, 0, 1, 1, new Color(.015f, .025f, .08f, .90f));
        popup.sprite = null;
        Set(ui, "popup", popup.gameObject);
        var panel = Card("Popup Card", popup.transform, .075f, .24f, .925f, .80f, _panel);
        Set(ui, "popupPanel", panel);
        var rim = panel.gameObject.AddComponent<Outline>();
        rim.effectColor = new Color(.37f, .67f, .83f);
        rim.effectDistance = new Vector2(2, -2);
        Set(ui, "popupTitle", Text("Outcome", panel, .04f, .80f, .96f, .96f, "", 40, TextAlignmentOptions.Center, true));
        Set(ui, "popupEmblem", Icon("Outcome Emblem", panel, .36f, .59f, .64f, .80f, _theme.coin));
        Set(ui, "popupBody", Text("Details", panel, .065f, .32f, .935f, .59f, "", 24));
        Set(ui, "popupAction", Button("Popup Action", panel, .09f, .17f, .91f, .315f, "Continue", _green));
        Set(ui, "popupSecondary", Button("Popup Secondary", panel, .09f, .018f, .91f, .15f, "Home", new Color(.17f, .23f, .37f)));
        var settings = Rect("Settings Controls", panel, .08f, .30f, .92f, .59f);
        Set(ui, "settingsControls", settings.gameObject);
        Set(ui, "mute", Toggle("Mute", settings, .04f, .54f, .96f, .98f, "Mute sound"));
        Set(ui, "reduced", Toggle("Reduced Effects", settings, .04f, .02f, .96f, .46f, "Reduce motion & effects"));
        var particles = Rect("Celebration", fit, 0, 0, 1, 1);
        Set(ui, "particles", particles);
        return (popup, panel, particles);
    }
}
