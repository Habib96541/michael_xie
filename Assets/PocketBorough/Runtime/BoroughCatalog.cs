using UnityEngine;

namespace PocketBorough
{
    [System.Serializable] public class ModelDefinition
    {
        public string title;
        public GameObject prefab;
        public float footprint, mass;
        public int points;
        public int category;
    }
    public class BoroughCatalog : ScriptableObject
    {
        public ModelDefinition[] models;
        public Material cityMaterial, groundMaterial, bowlMaterial, rimMaterial;
        public Mesh bowlMesh, rimMesh;
        public TMPro.TMP_FontAsset font;
        public Sprite panelSprite;
        public AudioClip[] sounds;
        public AudioClip music;
        public UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset desktopPipeline,mobilePipeline;
    }
    public static class BoroughRules
    {
        public const float Duration=120, ArenaLimit=55, InitialRadius=.88f;
        public static float Radius(float mass)=>Mathf.Min(11.5f,Mathf.Sqrt(InitialRadius*InitialRadius+Mathf.Max(0,mass)*.022f));
        public static bool Fits(float radius,float footprint)=>radius>=footprint*1.08f;
        public static bool CanEatHole(float radius,float other)=>radius>other*1.32f;
        public static int Level(float radius)=>Mathf.Clamp(1+Mathf.FloorToInt((radius-InitialRadius)/.8f),1,14);
    }
}
