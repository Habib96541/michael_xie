using System;
using System.IO;
using MineGrid;
using UnityEditor;
using UnityEngine;

public static partial class MineGridSetup
{
    private static CosmeticCatalog _catalog;
    private static void PrepareCatalog()
    {
        const string folder = Root + "/Shop";
        Directory.CreateDirectory(folder);
        Shader shader = Shader.Find("MineGrid/UI Gem Hue");
        if (!shader)
        {
            throw new InvalidOperationException("Cosmetic gem shader is missing.");
        }

        Material ruby = HueMaterial("Ruby", shader, .48f);
        Material emerald = HueMaterial("Emerald", shader, -.19f);
        Sprite frozen = Art("FrostedTile", 192, 192, (x, y) =>
        {
            if (!RoundPixel(x, y, 192, 192, 23))
            {
                return Color.clear;
            }

            Color color = Color.Lerp(new Color(.17f, .36f, .49f), new Color(.57f, .83f, .92f), y / 192f);
            if (x < 6 || x > 185 || y < 7 || y > 184)
            {
                color = new Color(.70f, .94f, 1);
            }

            if ((x + y) % 67 < 3)
            {
                color = Color.Lerp(color, Color.white, .5f);
            }

            return color;
        });
        Sprite obsidian = Art("ObsidianTile", 192, 192, (x, y) =>
        {
            if (!RoundPixel(x, y, 192, 192, 23))
            {
                return Color.clear;
            }

            bool border = x < 7 || x > 184 || y < 7 || y > 184;
            if (border)
            {
                return Color.Lerp(new Color(.55f, .27f, .08f), new Color(1, .84f, .39f), y / 192f);
            }

            if (Mathf.Abs(x - 96) + Mathf.Abs(y - 96) < 22)
            {
                return new Color(.85f, .62f, .22f);
            }

            return Color.Lerp(new Color(.055f, .045f, .09f), new Color(.19f, .15f, .24f), y / 192f);
        });
        Sprite midnight = BoardSwatch("MidnightPreview", new Color(.13f, .20f, .34f), new Color(.035f, .055f, .13f));
        Sprite frost = BoardSwatch("FrozenPreview", new Color(.40f, .76f, .88f), new Color(.09f, .24f, .34f));
        Sprite crystal = BoardSwatch("CrystalPreview", new Color(.55f, .28f, .77f), new Color(.15f, .08f, .28f));
        Sprite stars = Art("StarburstPreview", 128, 128, (x, y) =>
        {
            float dx = Mathf.Abs(x - 64), dy = Mathf.Abs(y - 64);
            return dx + dy < 36 || dx < 4 && dy < 56 || dy < 4 && dx < 56 ? new Color(.65f, .5f, 1) : Color.clear;
        });
        Sprite trail = Art("GoldenTrailPreview", 128, 128, (x, y) =>
        {
            for (int i = 0; i < 5; i++)
            {
                if (Mathf.Abs(x - 30 - i * 17) < 6 && Mathf.Abs(y - 25 - i * 18) < 13)
                {
                    return new Color(1, .77f, .24f);
                }
            }

            return Color.clear;
        });
        var items = new[]
        {
            Item("gem.cyan", "Classic Cyan", CosmeticCategory.Gem, 0, true, "The original brilliant cyan gem.", _theme.gem, null, Color.white, Color.white),
            Item("gem.ruby", "Ruby", CosmeticCategory.Gem, 500, false, "A fiery ruby with rose highlights.", _theme.gem, ruby, Color.white, Color.white),
            Item("gem.emerald", "Emerald", CosmeticCategory.Gem, 1000, false, "A vivid emerald-green treasure.", _theme.gem, emerald, Color.white, Color.white),
            Item("board.midnight", "Midnight", CosmeticCategory.Board, 0, true, "A deep indigo treasure vault.", midnight, null, new Color(.13f, .20f, .34f), new Color(.035f, .055f, .13f)),
            Item("board.frozen", "Frozen Vault", CosmeticCategory.Board, 2000, false, "An icy blue frame and cool stone well.", frost, null, new Color(.40f, .76f, .88f), new Color(.09f, .24f, .34f)),
            Item("board.crystal", "Crystal Cavern", CosmeticCategory.Board, 3000, false, "A violet crystal chamber for your gems.", crystal, null, new Color(.55f, .28f, .77f), new Color(.15f, .08f, .28f)),
            Item("tile.classic", "Classic", CosmeticCategory.Tile, 0, true, "The familiar beveled blue tile.", _theme.hidden, null, Color.white, Color.white),
            Item("tile.frosted", "Frosted", CosmeticCategory.Tile, 750, false, "Ice-blue tiles with frosty streaks.", frozen, null, Color.white, Color.white),
            Item("tile.obsidian", "Obsidian Gold", CosmeticCategory.Tile, 1500, false, "Dark stone, golden rims and diamond marks.", obsidian, null, Color.white, Color.white),
            Item("reveal.classic", "Classic Sparkle", CosmeticCategory.Reveal, 0, true, "A light ring of gem sparkles.", _theme.gem, null, new Color(.9f, 1, .6f), Color.white, RevealStyle.Classic),
            Item("reveal.stars", "Starburst", CosmeticCategory.Reveal, 1000, false, "A wide violet burst of diamond sparks.", stars, null, new Color(.75f, .55f, 1), Color.white, RevealStyle.Starburst),
            Item("reveal.gold", "Golden Trail", CosmeticCategory.Reveal, 2500, false, "A flowing trail of golden treasure dust.", trail, null, new Color(1, .78f, .23f), Color.white, RevealStyle.GoldenTrail)
        };
        _catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(folder + "/Catalog.asset");
        if (!_catalog)
        {
            _catalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
            AssetDatabase.CreateAsset(_catalog, folder + "/Catalog.asset");
            SetObjects(_catalog, "items", items);
        }

        _catalog.Validate();
        AssetDatabase.SaveAssets();
    }

    private static Sprite BoardSwatch(string name, Color frame, Color well)
    {
        return Art(name, 128, 128, (x, y) =>
        {
            if (!RoundPixel(x, y, 128, 128, 15))
            {
                return Color.clear;
            }

            if (x < 10 || x > 117 || y < 10 || y > 117)
            {
                return frame;
            }

            if (x % 33 < 25 && y % 33 < 25)
            {
                return Color.Lerp(frame, Color.white, .15f);
            }

            return well;
        });
    }

    private static Material HueMaterial(string name, Shader shader, float offset)
    {
        string path = Root + "/Shop/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader);
            material.SetFloat("_HueOffset", offset);
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static CosmeticItem Item(string id, string name, CosmeticCategory category, int price, bool isDefault, string description, Sprite artwork, Material material, Color primary, Color secondary, RevealStyle style = RevealStyle.Classic)
    {
        string path = Root + "/Shop/" + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<CosmeticItem>(path);
        if (item)
        {
            return item;
        }

        item = ScriptableObject.CreateInstance<CosmeticItem>();
        AssetDatabase.CreateAsset(item, path);
        var serialized = new SerializedObject(item);
        serialized.FindProperty("_id").stringValue = id;
        serialized.FindProperty("_displayName").stringValue = name;
        serialized.FindProperty("_description").stringValue = description;
        serialized.FindProperty("_category").enumValueIndex = (int)category;
        serialized.FindProperty("_price").intValue = price;
        serialized.FindProperty("_isDefault").boolValue = isDefault;
        serialized.FindProperty("_artwork").objectReferenceValue = artwork;
        serialized.FindProperty("_material").objectReferenceValue = material;
        serialized.FindProperty("_primary").colorValue = primary;
        serialized.FindProperty("_secondary").colorValue = secondary;
        serialized.FindProperty("_revealStyle").enumValueIndex = (int)style;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }
}
