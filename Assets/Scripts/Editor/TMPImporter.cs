using UnityEditor;
using UnityEngine;

namespace DiceGame.Editor
{
    [InitializeOnLoad]
    public static class TMPImporter
    {
        static TMPImporter()
        {
            ImportTMPResources();
        }

        [MenuItem("DiceGame/Import TMP Resources")]
        public static void ImportTMPResources()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                string packagePath = "Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Package Resources/TMP Essential Resources.unitypackage";
                if (!System.IO.File.Exists(packagePath))
                {
                    string[] matches = System.IO.Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", System.IO.SearchOption.AllDirectories);
                    if (matches.Length > 0)
                    {
                        packagePath = matches[0];
                    }
                }

                if (System.IO.File.Exists(packagePath))
                {
                    AssetDatabase.ImportPackage(packagePath, false);
                    Debug.Log("[DiceGame] Imported TMP Essential Resources successfully.");
                }
                else
                {
                    Debug.LogWarning("[DiceGame] TMP Essential Resources package not found.");
                }
            }
        }
    }
}

