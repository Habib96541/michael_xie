using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared material palette. Materials are authored as assets under
/// Resources/Materials by the editor setup, and rebuilt procedurally if a
/// project is opened without them.
/// </summary>
public static class MaterialLibrary
{
    public static readonly string[] Keys =
    {
        "Wood", "WoodDark", "Gold", "Iron", "Stone", "Floor", "Wall",
        "GoldGlow", "PurpleGlow", "Dust", "Velvet"
    };

    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    public static Material Get(string key)
    {
        Material material;
        if (Cache.TryGetValue(key, out material) && material != null) return material;

        material = Resources.Load<Material>("Materials/" + key);
        if (material == null) material = BuildDefault(key);
        Cache[key] = material;
        return material;
    }

    public static Material BuildDefault(string key)
    {
        switch (key)
        {
            case "Wood":
                return Standard(new Color(0.40f, 0.23f, 0.12f), 0.05f, 0.34f, Color.black);
            case "WoodDark":
                return Standard(new Color(0.16f, 0.09f, 0.05f), 0.10f, 0.24f, Color.black);
            case "Gold":
                return Standard(new Color(0.82f, 0.56f, 0.16f), 0.78f, 0.62f, new Color(1f, 0.62f, 0.18f) * 0.04f);
            case "Iron":
                return Standard(new Color(0.16f, 0.15f, 0.20f), 0.80f, 0.55f, Color.black);
            case "Stone":
                return Standard(new Color(0.13f, 0.10f, 0.17f), 0.15f, 0.42f, Color.black);
            case "Floor":
                return Standard(new Color(0.055f, 0.043f, 0.075f), 0.35f, 0.64f, Color.black);
            case "Wall":
                return Standard(new Color(0.075f, 0.055f, 0.105f), 0.10f, 0.28f, Color.black);
            case "GoldGlow":
                return Standard(new Color(1f, 0.72f, 0.30f), 0f, 0.5f, new Color(1f, 0.64f, 0.22f) * 0.85f);
            case "PurpleGlow":
                return Standard(new Color(0.45f, 0.28f, 1f), 0f, 0.5f, new Color(0.44f, 0.26f, 1f) * 0.9f);
            case "Velvet":
                return Standard(new Color(0.30f, 0.06f, 0.13f), 0.02f, 0.20f, Color.black);
            case "Dust":
                return Unlit(new Color(1f, 0.88f, 0.72f, 0.55f));
            default:
                return Standard(new Color(1f, 0.2f, 0.8f), 0f, 0.5f, Color.black);
        }
    }

    public static Material Standard(Color color, float metallic, float smoothness, Color emission)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0.001f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return material;
    }

    public static Material Unlit(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { color = color };
    }
}
