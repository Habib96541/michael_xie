#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberScratch.Editor
{
    public static class CyberArtGenerator
    {
        private const string SpritesPath = "Assets/CyberScratch/Sprites";

        [MenuItem("CyberScratch/Generate Art Assets", false, 1)]
        public static void GenerateAllAssets()
        {
            VectorArtRenderer.GenerateAllVectorAssets();
        }
    }
}
#endif
