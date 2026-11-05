using UnityEngine;

namespace HydraulicPress
{
    public enum CrushMaterial { Soft, Brittle, Metal, Elastic, Liquid, Plastic }

    [CreateAssetMenu(menuName = "Hydraulic Press/Crushable object")]
    public sealed class CrushableObjectDefinition : ScriptableObject
    {
        public string displayName;
        public CrushMaterial response;
        public GameObject prefab;
        public Color fragmentColor = Color.white;
        [Range(.2f, 1.4f)] public float height = .8f;
        [Range(.1f, 1)] public float resistance = .5f;
    }
}
