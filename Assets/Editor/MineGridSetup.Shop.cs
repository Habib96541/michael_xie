using MineGrid;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class MineGridSetup
{
    private static GameObject BuildShop(Transform fit, GameUI ui)
    {
        var page = Page("Cosmetic Shop", fit);
        var view = page.AddComponent<ShopView>();
        Set(ui, "shopView", view);
        Set(ui, "shopPage", page);
        Set(ui, "catalog", _catalog);
        Text("Shop Heading", page.transform, .06f, .90f, .94f, .98f, "Treasure shop", 40, TextAlignmentOptions.Center, true);
        var tabs = new Button[4];
        string[] names =
        {
            "Gem skins",
            "Board themes",
            "Tile designs",
            "Reveal effects"
        };
        for (int i = 0; i < 4; i++)
        {
            float left = i % 2 == 0 ? .055f : .51f;
            float bottom = i < 2 ? .82f : .73f;
            tabs[i] = Button(names[i], page.transform, left, bottom, left + .435f, bottom + .08f, names[i], _panel);
        }

        SetObjects(view, "categories", tabs);
        var cards = new ShopItemCard[3];
        for (int i = 0; i < 3; i++)
        {
            float top = .727f - i * .135f;
            var button = Button("Shop Item " + i, page.transform, .055f, top - .12f, .945f, top, "", _panel);
            var card = button.gameObject.AddComponent<ShopItemCard>();
            cards[i] = card;
            Set(card, "button", button);
            Set(card, "background", button.GetComponent<Image>());
            Set(card, "artwork", Icon("Item Artwork", button.transform, .025f, .12f, .20f, .90f, _theme.gem));
            Set(card, "name", Text("Item Name", button.transform, .23f, .46f, .73f, .93f, "", 27, TextAlignmentOptions.Left, true));
            Set(card, "price", Text("Item Price", button.transform, .23f, .08f, .72f, .44f, "", 21, TextAlignmentOptions.Left));
            Set(card, "state", Text("Item State", button.transform, .74f, .22f, .98f, .76f, "", 20));
        }

        SetObjects(view, "cards", cards);
        PrefabUtility.SaveAsPrefabAsset(cards[0].gameObject, Root + "/Prefabs/ShopItemCard.prefab");
        var panel = Card("Cosmetic Preview", page.transform, .055f, .125f, .945f, .315f, _panel);
        var preview = panel.gameObject.AddComponent<CosmeticPreview>();
        Set(view, "preview", preview);
        var frame = Card("Preview Board", panel, .035f, .46f, .36f, .97f, new Color(.13f, .20f, .34f));
        Set(preview, "frame", frame.GetComponent<Image>());
        var sampleTiles = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float left = .06f + (i % 2) * .47f, bottom = .07f + (i / 2) * .46f;
            sampleTiles[i] = Icon("Preview Tile " + i, frame, left, bottom, left + .40f, bottom + .40f, _theme.hidden);
        }

        SetObjects(preview, "tiles", sampleTiles);
        var previewGem = Icon("Preview Gem", sampleTiles[3].transform, .1f, .1f, .9f, .9f, _theme.gem);
        Set(preview, "gem", previewGem);
        Set(preview, "effect", previewGem.gameObject.AddComponent<RevealEffectPlayer>());
        Set(view, "details", Text("Preview Details", panel, .40f, .43f, .97f, .97f, "", 20, TextAlignmentOptions.Left));
        var action = Button("Buy Or Equip", panel, .40f, .02f, .97f, .44f, "Buy", _green);
        Set(view, "action", action);
        Set(view, "actionLabel", action.GetComponentInChildren<TMP_Text>());
        var previewButton = Button("Preview Effect", panel, .025f, .02f, .365f, .44f, "Preview", _panel);
        Set(view, "previewEffect", previewButton);
        Text("Preview Hint", page.transform, .06f, .095f, .94f, .12f, "Preview only • buying does not equip", 17);
        Link(Button("Shop Home", page.transform, .055f, .008f, .40f, .09f, "Home", _panel), ui.Home);
        Set(view, "defaults", Button("Restore Defaults", page.transform, .43f, .008f, .945f, .09f, "Restore defaults", _panel));
        // The fitted portrait area is narrower on landscape displays.
        foreach (TMP_Text label in page.GetComponentsInChildren<TMP_Text>())
        {
            label.fontSizeMin *= .70f;
        }

        return page;
    }
}
