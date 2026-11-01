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
    private static GameObject BuildProgression(GameConfig config, Transform fit, GameUI ui)
    {
        var progressionPage = Page("Milestones", fit);
        Set(ui, "progressionPage", progressionPage);
        Text("Milestone Heading", progressionPage.transform, .06f, .88f, .94f, .98f, "Treasure trail", 43, TextAlignmentOptions.Center, true);
        Text("Milestone Help", progressionPage.transform, .06f, .81f, .94f, .88f, "Bank coins. Reach milestones. Get rewarded.", 21);
        var labels = new TMP_Text[config.milestonePoints.Length];
        var buttons = new Button[labels.Length];
        var bars = new Image[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            float top = .79f - i * .138f;
            var row = Card("Milestone " + i, progressionPage.transform, .055f, top - .123f, .945f, top, _panel);
            Icon("Milestone Coin", row, .025f, .25f, .16f, .90f, _theme.coin);
            labels[i] = Text("Milestone Details", row, .18f, .30f, .68f, .94f, "", 22, TextAlignmentOptions.Left);
            var track = Image("Progress Track", row, .18f, .13f, .65f, .20f, _ink);
            var fill = Image("Progress Fill", track.transform, 0, 0, 1, 1, _cyan);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bars[i] = fill;
            buttons[i] = Button("Claim " + i, row, .70f, .16f, .97f, .84f, "Claim", _green);
        }

        SetObjects(ui, "milestoneLabels", labels);
        SetObjects(ui, "milestoneButtons", buttons);
        SetObjects(ui, "milestoneFills", bars);
        Link(Button("Milestone Home", progressionPage.transform, .23f, .025f, .77f, .095f, "Home", _panel), ui.Home);
        return progressionPage;
    }
}
