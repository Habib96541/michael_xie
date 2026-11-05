using UnityEngine;

namespace CyberScratch.UI
{
    /// <summary>
    /// Loads the painted CyberScratch board sprites from Assets/Sprites/Gama_Scene.
    /// Scene references are serialized after the editor build, so play mode does not depend on this loader.
    /// </summary>
    public static class GameSceneSprites
    {
        private const string Folder = "Assets/Sprites/Gama_Scene/";

        public static Sprite Get(string fileName)
        {
#if UNITY_EDITOR
            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(Folder + fileName);
            Sprite fallback = null;
            if (assets == null) return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    fallback = sprite;
                    if (sprite.name.EndsWith("_0")) return sprite;
                }
            }

            return fallback;
#else
            return null;
#endif
        }

        public static Texture2D GetTexture(string fileName)
        {
            Sprite sprite = Get(fileName);
            return sprite != null ? sprite.texture : null;
        }
    }
}
