using System;
using System.IO;
using System.Text.RegularExpressions;
using MineGrid;
using TMPro;
using UnityEngine;

public static class MineGridVisualAudit
{
    public static int Inspect(GameUI ui)
    {
        int count = 0;
        foreach (var t in ui.GetComponentsInChildren<TMP_Text>())
        {
            if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text))
            {
                continue;
            }

            t.ForceMeshUpdate();
            if (t.isTextOverflowing)
            {
                throw new Exception("Text overflow: " + t.transform.parent.name + "/" + t.name + " at " + Screen.width + "x" + Screen.height);
            }

            string text = Regex.Replace(t.text, "<.*?>", "");
            foreach (char c in text)
            {
                if (!char.IsControl(c) && !t.font.HasCharacter(c, true, true))
                {
                    throw new Exception("Missing glyph " + (int)c + " in " + t.name);
                }
            }

            count++;
        }

        return count;
    }

    public static int CheckAssets()
    {
        int count = 0;
        foreach (string file in new[]
        {
            "MineGridLogo",
            "GoldCoin",
            "CyanGem",
            "NavyMine"
        }

        )
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes("Assets/MineGrid/Art/Brand/" + file + ".png"));
            bool clear = false, opaque = false;
            foreach (var pixel in tex.GetPixels32())
            {
                if (pixel.a == 0)
                {
                    clear = true;
                }

                if (pixel.a == 255)
                {
                    opaque = true;
                }
            }

            UnityEngine.Object.Destroy(tex);
            if (!clear || !opaque)
            {
                throw new Exception("Artwork alpha invalid: " + file);
            }

            count++;
        }

        return count;
    }
}
