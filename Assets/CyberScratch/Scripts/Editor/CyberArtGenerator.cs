#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberScratch.Editor
{
    [InitializeOnLoad]
    public static class CyberArtGenerator
    {
        private const string SpritesPath = "Assets/CyberScratch/Sprites";

        static CyberArtGenerator()
        {
            EditorApplication.delayCall += CheckAndGenerate;
        }

        private static void CheckAndGenerate()
        {
            // If any key high-res asset is missing or old, generate all
            string bronzeGearPath = Path.Combine(SpritesPath, "Symbol_BronzeGear.png");
            if (!File.Exists(bronzeGearPath) || new FileInfo(bronzeGearPath).Length < 35000)
            {
                GenerateAllAssets();
            }
        }

        [MenuItem("CyberScratch/Generate Cyber Art Assets", false, 1)]
        public static void GenerateAllAssets()
        {
            HumanVectorArtRenderer.GenerateAllVectorAssets();
        }
    }
}
#endif
