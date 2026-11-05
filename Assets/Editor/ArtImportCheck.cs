using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace com.javierquevedo.Editor
{
    /// <summary>
    /// Forces a re-import of the authored art and reports anything that would not load at runtime.
    /// Run from the CyberOrbs menu, or headless:
    /// Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod com.javierquevedo.Editor.ArtImportCheck.Run
    /// </summary>
    public static class ArtImportCheck
    {
        private const string SpriteFolder = "Assets/Resources/Art/Sprites";
        private const string ReportPath = "Logs/art_check.txt";

        private static readonly string[] Required =
        {
            "orb_fire", "orb_void", "orb_circuit", "orb_storm", "orb_solar", "orb_ice",
            "turret_base", "turret_barrel", "chamber_ring", "aim_dot",
            "vfx_ring", "vfx_spark", "vfx_shard",
            "hud_panel", "rank_badge", "battery_full", "battery_empty",
            "button_green", "button_cyan", "button_yellow", "button_magenta", "button_blue",
            "pill_cyan", "pill_blue", "pill_green",
            "icon_restart", "icon_sound", "icon_muted", "icon_pause", "icon_play", "icon_help", "icon_swap",
            "colon_cyan", "digit_cyan_0", "digit_amber_5", "digit_red_9"
        };

        [MenuItem("CyberOrbs/Verify Art Import")]
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            StringBuilder report = new StringBuilder();
            int missing = 0;

            foreach (string name in Required)
            {
                string path = SpriteFolder + "/" + name + ".png";
                Texture2D tex = Resources.Load<Texture2D>("Art/Sprites/" + name);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (tex == null || importer == null)
                {
                    missing++;
                    report.AppendLine("MISSING  " + name + (importer == null ? "  (no importer)" : "  (Resources.Load returned null)"));
                    continue;
                }

                report.AppendLine(string.Format("ok       {0,-14} {1}x{2}  type={3} mips={4} ppu={5} border={6}",
                    name, tex.width, tex.height, importer.textureType, importer.mipmapEnabled,
                    importer.spritePixelsPerUnit, importer.spriteBorder));
            }

            foreach (string font in new[] { "Rajdhani-Bold", "Rajdhani-SemiBold" })
            {
                Font f = Resources.Load<Font>("Fonts/" + font);
                if (f == null) { missing++; report.AppendLine("MISSING  font " + font); }
                else report.AppendLine("ok       font " + font);
            }

            report.AppendLine();
            report.AppendLine(missing == 0 ? "ALL ART LOADS" : missing + " ASSET(S) FAILED TO LOAD");

            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[ArtImportCheck]\n" + report);
        }
    }
}
