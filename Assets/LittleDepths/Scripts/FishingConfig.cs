using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleDepths
{
    [Serializable]
    public class FishSpecies
    {
        public string id, displayName, rarity;
        public Sprite sprite;
        public Sprite[] swimFrames;
        public Vector2[] mouthPoints;
        public RuntimeAnimatorController swimController;
        public Color color = Color.white;
        public int value;
        public float minDepth, maxDepth = 100, weight = 1, size = 0.7f, speed = 0.4f;
    }

    [CreateAssetMenu(menuName = "Little Depths/Fishing balance")]
    public class FishingConfig : ScriptableObject
    {
        public int castsPerSession = 5;
        public int startingCapacity = 5;
        public float startingDepth = 8;
        public float depthPerUpgrade = 3;
        public int maximumDepthLevel = 20;
        public int maximumCapacityLevel = 20;
        public int maximumIncomeLevel = 15;
        public float descentMetersPerSecond = 2.6f;
        public const float ReelSpeedMultiplier=1.4f;
        public float ascentMetersPerSecond => descentMetersPerSecond*ReelSpeedMultiplier;
        public float metersToWorld = 0.65f;
        public float surfaceY = 1.1f;
        public float riverHalfWidth = 2.65f;
        public int offlineCapHours = 8;
        public List<FishSpecies> species = new List<FishSpecies>();
        public Sprite trashSprite;
        public Sprite hookSprite;
        public Sprite[] outfits, hats, hairstyles, boats;
        [Header("Complete character, worn hat and boat artwork")]
        public Sprite[] completeAnglers;
        public Vector2[] completeRodTips;
        public float[] completeHullWidths;
        public Vector2 hookMouthPoint=new Vector2(.217f,.533f);
        public Sprite lockSprite;
        public RuntimeAnimatorController anglerController;
        public Color[] hookColors = { new Color(0.1f, 0.25f, 0.33f), new Color(1, .68f, .22f), new Color(.94f,.4f,.58f), new Color(.35f,.8f,.73f), new Color(.6f,.48f,.85f), new Color(.9f,.52f,.3f) };

        public int Capacity(GameProgress p) => startingCapacity + p.capacityLevel;
        public float MaxDepth(GameProgress p) => startingDepth + p.depthLevel * depthPerUpgrade;
        public int UpgradeLimit(int index) => index == 0 ? maximumCapacityLevel : index == 1 ? maximumDepthLevel : index == 3 ? 10 : maximumIncomeLevel;
        public int UpgradeLevel(GameProgress p, int index) => index == 0 ? p.capacityLevel : index == 1 ? p.depthLevel : index == 3 ? p.speedLevel : p.incomeLevel;
        public int UpgradeCost(GameProgress p, int index)
        {
            int[] initial = { 80, 110, 65, 500 };
            return (int)(Math.Ceiling(initial[index] * Math.Pow(1.43, UpgradeLevel(p, index)) / 5) * 5);
        }
        public int IncomePerMinute(GameProgress p) => p.discovered.Count + p.incomeLevel * 5;
        public static int CompleteIndex(int outfit,int hair,int hat,int boat)
            => ((Mathf.Clamp(outfit,0,2)*3+Mathf.Clamp(hair,0,2))*4+Mathf.Clamp(hat,0,3))*3+Mathf.Clamp(boat,0,2);
        public Sprite CompleteSprite(int outfit,int hair,int hat,int boat)
            => completeAnglers[CompleteIndex(outfit,hair,hat,boat)];
    }
}
