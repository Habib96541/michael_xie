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
    private static (GameObject Page, Button Start) BuildGameplay(Transform fit, GameUI ui)
    {
        var game = Page("Mines", fit);
        Set(ui, "game", game);
        var back = Button("Back", game.transform, .055f, .905f, .25f, .977f, "Home", _panel);
        Link(back, ui.Home);
        Set(ui, "back", back);
        var bank = Card("Bankable Capsule", game.transform, .28f, .905f, .945f, .977f, _panel);
        Text("Bankable Caption", bank, .04f, .10f, .48f, .90f, "Bankable", 20, TextAlignmentOptions.Left);
        Icon("Bankable Coin", bank, .51f, .12f, .65f, .88f, _theme.coin);
        Set(ui, "scoreLabel", Text("Round Score", bank, .66f, .10f, .94f, .90f, "0", 31, TextAlignmentOptions.Right, true));
        var selector = Card("Risk Selector", game.transform, .055f, .785f, .945f, .89f, _panel);
        Icon("Mine Icon", selector, .025f, .26f, .15f, .96f, _theme.mine);
        Set(ui, "mineLabel", Text("Mine Selection", selector, .17f, .47f, .94f, .97f, "", 23, TextAlignmentOptions.Left));
        Set(ui, "mineSlider", Slider(selector, .08f, .04f, .92f, .42f));
        var frame = Card("Recessed Board Frame", game.transform, .04f, .245f, .96f, .765f, new Color(.13f, .20f, .34f));
        var inner = Card("Board Well", frame, .018f, .025f, .982f, .975f, new Color(.035f, .055f, .13f));
        var board = Rect("Board", inner, .025f, .025f, .975f, .975f);
        Set(ui, "boardTransform", board);
        var grid = board.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
        board.gameObject.AddComponent<ResponsiveGrid>();
        var prefab = CreateTile();
        var tiles = new TileView[25];
        for (int i = 0; i < 25; i++)
        {
            tiles[i] = (TileView)PrefabUtility.InstantiatePrefab(prefab, board);
            tiles[i].name = $"Tile {i:00}";
        }

        SetObjects(ui, "tiles", tiles);
        var applicator = ui.gameObject.AddComponent<CosmeticApplicator>();
        Set(ui, "cosmeticApplicator", applicator);
        Set(applicator, "frame", frame.GetComponent<Image>());
        Set(applicator, "well", inner.GetComponent<Image>());
        SetObjects(applicator, "tiles", tiles);
        var reward = Card("Next Reward", game.transform, .20f, .185f, .80f, .235f, _panel);
        Icon("Next Coin", reward, .04f, .08f, .16f, .92f, _theme.coin);
        Set(ui, "nextReward", Text("Next Reward Text", reward, .18f, .08f, .96f, .92f, "", 21));
        Set(ui, "status", Text("Status", game.transform, .055f, .13f, .945f, .18f, "", 21));
        var start = Button("Start", game.transform, .08f, .015f, .92f, .12f, "Start game", _green);
        Set(ui, "start", start);
        var finish = Button("Finish", game.transform, .08f, .015f, .92f, .12f, "Finish game", new Color(.13f, .63f, .75f));
        Icon("Finish Coin", finish.transform, .06f, .18f, .18f, .82f, _theme.coin);
        Set(ui, "finish", finish);
        return (game, start);
    }
}
