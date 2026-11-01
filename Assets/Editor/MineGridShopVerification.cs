using System;
using System.IO;
using MineGrid;
using UnityEditor;
using UnityEngine;

public static class MineGridShopVerification
{
    [MenuItem("MineGrid/Verify Cosmetic Shop")]
    public static void Run()
    {
        CosmeticCatalog catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/MineGrid/Shop/Catalog.asset");
        catalog.Validate();
        string[] keys =
        {
            SaveService.Key,
            "MineGrid.Save.v2",
            "MineGrid.Save.v1"
        };
        var backups = new string[keys.Length];
        var existed = new bool[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(keys[i]);
            backups[i] = PlayerPrefs.GetString(keys[i]);
            PlayerPrefs.DeleteKey(keys[i]);
        }

        int checks = 0;
        Action<bool, string> check = (valid, message) =>
        {
            if (!valid)
            {
                throw new Exception(message);
            }

            checks++;
        };
        try
        {
            var saves = new SaveService();
            var service = new CosmeticService(saves, catalog);
            check(saves.Data.ownedCosmetics.Count == 4, "four defaults");
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                check(service.Equipped(category).IsDefault, "default equipment");
            }

            check(service.Purchase("missing") == CosmeticResult.InvalidItem, "invalid item");
            check(service.Equip(CosmeticCategory.Gem, "gem.ruby") == CosmeticResult.NotOwned, "unowned equipment rejected");
            check(service.Equip(CosmeticCategory.Tile, "gem.cyan") == CosmeticResult.CategoryMismatch, "category mismatch");
            string before = JsonUtility.ToJson(saves.Data);
            check(service.Purchase("gem.ruby") == CosmeticResult.InsufficientCoins && JsonUtility.ToJson(saves.Data) == before, "insufficient unchanged");
            saves.Data.coins = 1500;
            saves.Data.totalPoints = 777;
            saves.Save();
            check(service.Purchase("gem.ruby") == CosmeticResult.Success && saves.Data.coins == 1000, "exact purchase");
            for (int i = 0; i < 10; i++)
            {
                check(service.Purchase("gem.ruby") == CosmeticResult.AlreadyOwned && saves.Data.coins == 1000, "rapid duplicate purchase");
            }

            check(service.Equipped(CosmeticCategory.Gem).Id == "gem.cyan", "purchase does not equip");
            check(service.Equip(CosmeticCategory.Gem, "gem.ruby") == CosmeticResult.Success && saves.Data.coins == 1000, "equip free");
            var loaded = new SaveService();
            var reload = new CosmeticService(loaded, catalog);
            check(reload.Owns("gem.ruby") && reload.Equipped(CosmeticCategory.Gem).Id == "gem.ruby", "reload ownership equipment");
            check(loaded.Data.totalPoints == 777, "purchases do not change progression");
            reload.RestoreDefaults();
            check(reload.Owns("gem.ruby") && reload.Equipped(CosmeticCategory.Gem).IsDefault, "defaults keep purchases");
            loaded.Data.equippedCosmetics[0].id = "removed.gem";
            loaded.Data.equippedCosmetics.Add(null);
            loaded.Save();
            reload = new CosmeticService(new SaveService(), catalog);
            check(reload.Equipped(CosmeticCategory.Gem).IsDefault, "removed ID fallback");
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var round = new Round(config);
            round.Start(3, new System.Random(91));
            int safe = 0;
            while (round.Board.IsMine(safe))
            {
                safe++;
            }

            round.Reveal(safe);
            round.Finish();
            check(round.Banked == 30, "cosmetics preserve scoring");
            UnityEngine.Object.DestroyImmediate(config);
            PlayerPrefs.DeleteKey(SaveService.Key);
            PlayerPrefs.SetString("MineGrid.Save.v2", "{\"version\":2,\"coins\":321,\"totalPoints\":1234,\"rounds\":7,\"selectedMines\":4,\"muted\":true,\"reducedEffects\":true,\"nextSpin\":500000,\"pendingCoins\":25,\"claimed\":[0]}");
            var migrated = new SaveService();
            var migratedShop = new CosmeticService(migrated, catalog);
            check(migrated.Data.version == 3 && migrated.Data.coins == 321 && migrated.Data.totalPoints == 1234, "v2 currency progression migration");
            check(migrated.Data.rounds == 7 && migrated.Data.selectedMines == 4 && migrated.Data.muted && migrated.Data.reducedEffects, "v2 settings migration");
            check(migrated.Data.nextSpin == 500000 && migrated.Data.pendingCoins == 25 && migrated.Data.claimed.Contains(0), "v2 rewards migration");
            check(migrated.Data.ownedCosmetics.Count == 4, "migration defaults granted");
            var invalid = ScriptableObject.CreateInstance<CosmeticCatalog>();
            var so = new SerializedObject(invalid);
            var items = so.FindProperty("_items");
            items.arraySize = 2;
            items.GetArrayElementAtIndex(0).objectReferenceValue = catalog.Items[0];
            items.GetArrayElementAtIndex(1).objectReferenceValue = catalog.Items[0];
            so.ApplyModifiedPropertiesWithoutUndo();
            bool rejected = false;
            try
            {
                invalid.Validate();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            check(rejected, "duplicate catalog rejected");
            UnityEngine.Object.DestroyImmediate(invalid);
            var brokenItem = UnityEngine.Object.Instantiate(catalog.Items[1]);
            var itemData = new SerializedObject(brokenItem);
            itemData.FindProperty("_price").intValue = -10;
            itemData.ApplyModifiedPropertiesWithoutUndo();
            var brokenCatalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
            var catalogData = new SerializedObject(brokenCatalog);
            var entries = catalogData.FindProperty("_items");
            entries.arraySize = catalog.Items.Count;
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                entries.GetArrayElementAtIndex(i).objectReferenceValue = i == 1 ? brokenItem : catalog.Items[i];
            }

            catalogData.ApplyModifiedPropertiesWithoutUndo();
            rejected = false;
            try
            {
                brokenCatalog.Validate();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            check(rejected, "negative price rejected");
            itemData.FindProperty("_price").intValue = 500;
            itemData.FindProperty("_artwork").objectReferenceValue = null;
            itemData.ApplyModifiedPropertiesWithoutUndo();
            rejected = false;
            try
            {
                brokenCatalog.Validate();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            check(rejected, "missing artwork rejected");
            UnityEngine.Object.DestroyImmediate(brokenItem);
            UnityEngine.Object.DestroyImmediate(brokenCatalog);
            File.WriteAllText("MineGrid-shop-verification.txt", $"PASS: {checks} shop rule checks. Defaults, affordability, exact spend, duplicate/rapid purchases, free equipment, category and ownership validation, reload, restore defaults, removed IDs, scoring, v2 migration, invalid catalog. Saves restored.\n");
            Debug.Log($"MineGrid shop rules passed: {checks} checks.");
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (existed[i])
                {
                    PlayerPrefs.SetString(keys[i], backups[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(keys[i]);
                }
            }

            PlayerPrefs.Save();
        }
    }
}
