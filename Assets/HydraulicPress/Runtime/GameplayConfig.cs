using UnityEngine;

namespace HydraulicPress
{
    [CreateAssetMenu(menuName = "Hydraulic Press/Gameplay configuration")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [Min(1)] public int basePoints = 100;
        [Range(0, .2f)] public float rewardMargin = .04f;
        [Range(.5f, 1.5f)] public float animationSpeed = 1;
        [Range(16, 64)] public int fragmentBudget = 40;
        public CrushableObjectDefinition[] objects;
        public UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset graphics;
        public UnityEngine.Rendering.Universal.UniversalRendererData rendererData;
    }
}
