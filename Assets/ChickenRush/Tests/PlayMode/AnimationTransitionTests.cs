#if UNITY_EDITOR
using System.Collections;
using ChickenRush;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AnimationTransitionTests
{
    GameObject chicken;
    ChickenAnimationController view;
    [SetUp] public void Setup()
    {
        chicken=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChickenRush/Prefabs/Chicken.prefab"));
        view=chicken.GetComponent<ChickenAnimationController>();
    }
    [TearDown] public void Cleanup() {Object.DestroyImmediate(chicken);}
    [UnityTest] public IEnumerator WalkHopLandAndIdle()
    {
        yield return null;Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
        view.Walk(true);yield return new WaitForSeconds(.12f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"));
        view.Jump(.6f);yield return new WaitForSeconds(.12f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Hop"));
        yield return new WaitForSeconds(.65f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
    }
    [UnityTest] public IEnumerator HitInterruptsHopAndHoldsUntilReset()
    {
        yield return null;view.Jump(.6f);yield return new WaitForSeconds(.15f);view.Lose();
        yield return new WaitForSeconds(.15f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
        view.Jump(.6f);view.Win();view.Walk(true);yield return new WaitForSeconds(.95f);
        Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
        view.ResetForRound();yield return new WaitForSeconds(.1f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
    }
    [UnityTest] public IEnumerator CelebrationCannotBeInterruptedByHopOrHit()
    {
        yield return null;view.Win();yield return new WaitForSeconds(.15f);
        view.Jump(.6f);view.Lose();yield return new WaitForSeconds(1);
        Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Celebrate"));
        view.ResetForRound();view.Walk(true);yield return new WaitForSeconds(.15f);
        Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"));
    }
    [UnityTest] public IEnumerator DangerReactsThenAllowsAResponsiveHop()
    {
        yield return null;view.ReactToDanger(true);yield return new WaitForSeconds(.12f);
        Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Danger"));
        view.Jump(.62f);yield return new WaitForSeconds(.12f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Hop"));
        view.Lose();view.ReactToDanger(false);yield return new WaitForSeconds(.12f);Assert.IsTrue(view.Animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
    }
}
#endif
