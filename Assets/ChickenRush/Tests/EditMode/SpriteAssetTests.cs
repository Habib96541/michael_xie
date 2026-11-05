using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SpriteAssetTests
{
    [Test] public void BlinkHoldsForOneHundredTwentyMilliseconds()
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ChickenRush/Animations/Idle.anim");
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
        var frames=AnimationUtility.GetObjectReferenceCurve(clip,binding);
        int blink=System.Array.FindIndex(frames,k=>k.value.name=="IdleBlink");
        Assert.GreaterOrEqual(blink,0);Assert.That(frames[blink+1].time-frames[blink].time,Is.EqualTo(.12f).Within(.001f));
    }
    [Test] public void EveryExtractedSpriteHasTheSameFootPivotAndNoDownscaling()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/ChickenRush/Art/Chicken"}))
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.AreEqual(new Vector2(192,192),sprite.rect.size,sprite.name);
            Assert.AreEqual(new Vector2(96,18),sprite.pivot,sprite.name);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            Assert.IsFalse(importer.mipmapEnabled,sprite.name);Assert.AreEqual(FilterMode.Bilinear,importer.filterMode);
        }
    }
}
