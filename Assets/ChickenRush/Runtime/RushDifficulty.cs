using System;
using UnityEngine;

namespace ChickenRush
{
    [CreateAssetMenu(menuName = "Chicken Rush/Difficulty")]
    public sealed class RushDifficulty : ScriptableObject
    {
        public string displayName = "NORMAL";
        [Range(.3f, 1f)] public float hopSeconds = .42f;
        public float trafficSpeed = 2.3f;
        public float trafficSpacing = 5.5f;
        public float[] multipliers = {1.1f, 1.25f, 1.45f, 1.7f, 2f, 2.4f, 3f, 3.8f, 5f, 7f};
        [Tooltip("Global tuning for this difficulty; lane tiers apply progressively.")]
        public float speedScale=1;
        public TrafficTier[] tiers;
        [Tooltip("Seeded survival probability for each attempted hop; independent of input timing.")]
        public float[] successChances;
        public int riskVersion;

        public LaneTuning GetLane(int lane)
        {
            if(tiers==null || tiers.Length==0)throw new InvalidOperationException("Configure traffic tiers before starting the game.");
            int tier=0;
            for(int i=1;i<tiers.Length;i++)if(lane>=tiers[i].firstLane)tier=i;
            var t=tiers[tier];
            return new LaneTuning{lane=lane,tier=tier,vehicleCount=t.vehicleCount,
                speed=(t.speed+(lane-t.firstLane)*t.speedPerLane)*speedScale,
                minGap=t.minGapSeconds,maxGap=t.maxGapSeconds,
                guaranteedGap=Mathf.Max(hopSeconds+.26f,t.guaranteedGapSeconds),express=lane%5==0};
        }
    }

    [Serializable] public sealed class TrafficTier
    {
        public int firstLane=1,vehicleCount=3;
        public float speed=3.9f,speedPerLane=.18f;
        public float minGapSeconds=.65f,maxGapSeconds=1.25f,guaranteedGapSeconds=.9f;
    }
    public struct LaneTuning
    {
        public int lane,tier,vehicleCount;
        public float speed,minGap,maxGap,guaranteedGap;
        public bool express;
    }
}
