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
    private static GameObject Page(string name, Transform parent) => Rect(name, parent, 0, .045f, 1, .918f).gameObject;
    private static RectTransform Card(string name, Transform parent, float x0, float y0, float x1, float y1, Color color)
    {
        var image = Image(name, parent, x0, y0, x1, y1, color);
        var shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, .01f, .045f, .75f);
        shadow.effectDistance = new Vector2(0, -6);
        return image.rectTransform;
    }

    private static void Place(RectTransform r, float x0, float y0, float x1, float y1)
    {
        r.anchorMin = new Vector2(x0, y0);
        r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        Place(r, x0, y0, x1, y1);
        return r;
    }

    private static Image Image(string name, Transform parent, float x0, float y0, float x1, float y1, Color color)
    {
        var r = Rect(name, parent, x0, y0, x1, y1);
        var image = r.gameObject.AddComponent<Image>();
        image.sprite = _rounded;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static Image Icon(string name, Transform parent, float x0, float y0, float x1, float y1, Sprite sprite)
    {
        var image = Image(name, parent, x0, y0, x1, y1, Color.white);
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, float x0, float y0, float x1, float y1, string content, int size, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool display = false)
    {
        var r = Rect(name, parent, x0, y0, x1, y1);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = display ? _theme.displayFont : _theme.bodyFont;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyles.Normal;
        t.alignment = alignment;
        t.color = new Color(.91f, .96f, 1);
        t.raycastTarget = false;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * .78f;
        t.fontSizeMax = size;
        if (display)
        {
            var shadow = r.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.025f, .04f, .10f, .6f);
            shadow.effectDistance = new Vector2(0, -2);
        }

        return t;
    }

    private static Button Button(string name, Transform parent, float x0, float y0, float x1, float y1, string label, Color color)
    {
        var r = Card(name, parent, x0, y0, x1, y1, color);
        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = r.GetComponent<Image>();
        var colors = b.colors;
        colors.highlightedColor = new Color(1, 1, .93f);
        colors.pressedColor = new Color(.70f, .80f, .87f);
        colors.disabledColor = new Color(.42f, .50f, .60f, .75f);
        b.colors = colors;
        r.gameObject.AddComponent<ButtonFeedback>();
        Text("Label", r, .06f, .08f, .94f, .94f, label, 34, TextAlignmentOptions.Center, true);
        return b;
    }

    private static Toggle Toggle(string name, Transform parent, float x0, float y0, float x1, float y1, string label)
    {
        var r = Rect(name, parent, x0, y0, x1, y1);
        var t = r.gameObject.AddComponent<Toggle>();
        var box = Image("Toggle Box", r, 0, .15f, .13f, .85f, new Color(.04f, .07f, .15f));
        var check = Image("Enabled Mark", box.transform, .18f, .18f, .82f, .82f, _cyan);
        t.targetGraphic = box;
        t.graphic = check;
        Text("Label", r, .18f, .02f, 1, .98f, label, 22, TextAlignmentOptions.Left);
        return t;
    }

    private static Slider Slider(Transform parent, float x0, float y0, float x1, float y1)
    {
        var r = Rect("Mine Slider", parent, x0, y0, x1, y1);
        var s = r.gameObject.AddComponent<Slider>();
        Image("Track", r, 0, .35f, 1, .65f, _ink);
        var area = Rect("Handle Area", r, .035f, 0, .965f, 1);
        var handle = Image("Handle", area, 0, .02f, 0, .98f, _gold);
        handle.rectTransform.sizeDelta = new Vector2(48, 0);
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.minValue = 1;
        s.maxValue = 24;
        s.wholeNumbers = true;
        s.value = 3;
        return s;
    }

    private static TileView CreateTile()
    {
        var r = Rect("Mine Tile", null, 0, 0, 1, 1);
        var face = r.gameObject.AddComponent<Image>();
        face.sprite = _theme.hidden;
        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = face;
        var icon = Icon("Revealed Treasure", r, .13f, .13f, .87f, .91f, _theme.gem);
        icon.gameObject.SetActive(false);
        var label = Text("Tile Symbol", r, .05f, .01f, .95f, .26f, "", 16, TextAlignmentOptions.Center, true);
        r.gameObject.AddComponent<ButtonFeedback>();
        var view = r.gameObject.AddComponent<TileView>();
        Set(view, "button", b);
        Set(view, "face", face);
        Set(view, "label", label);
        Set(view, "hidden", _theme.hidden);
        Set(view, "gem", _theme.safeTile);
        Set(view, "mine", _theme.mineTile);
        Set(view, "treasure", icon);
        Set(view, "gemIcon", _theme.gem);
        Set(view, "mineIcon", _theme.mine);
        var prefab = PrefabUtility.SaveAsPrefabAsset(r.gameObject, Root + "/Prefabs/MineTile.prefab");
        UnityEngine.Object.DestroyImmediate(r.gameObject);
        return prefab.GetComponent<TileView>();
    }
}
