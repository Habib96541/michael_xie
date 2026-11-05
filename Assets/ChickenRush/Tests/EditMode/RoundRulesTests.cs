using ChickenRush;
using NUnit.Framework;

public sealed class RoundRulesTests
{
    static RoundRules NewRound() => new RoundRules(10,new[]{1.1f,1.25f,1.45f,1.7f,2f,2.4f,3f,3.8f,5f,7f});
    static void Cross(RoundRules rules) {Assert.IsTrue(rules.Hop());Assert.IsTrue(rules.Land());}
    [Test] public void StartDeductsStakeExactlyOnce()
    {
        var r=NewRound();r.SetStake(3);Assert.IsTrue(r.Start());Assert.IsFalse(r.Start());Assert.AreEqual(7,r.Balance);
    }
    [Test] public void PreparingAndHoppingRejectCashOutAndExtraInputs()
    {
        var r=NewRound();r.Start();Assert.IsFalse(r.Hop());Assert.IsFalse(r.CashOut());r.Ready();
        Assert.IsTrue(r.Hop());Assert.IsFalse(r.Hop());Assert.IsFalse(r.CashOut());Assert.IsTrue(r.Land());Assert.AreEqual(1,r.Row);
    }
    [Test] public void CashOutCannotPayTwiceOrBeChangedByCollision()
    {
        var r=NewRound();r.Start();r.Ready();Cross(r);Assert.IsTrue(r.CashOut());int paid=r.Balance;
        Assert.IsFalse(r.CashOut());Assert.IsFalse(r.Hit());Assert.IsFalse(r.Hop());Assert.AreEqual(paid,r.Balance);
    }
    [Test] public void CollisionStopsTheHopAndNeverPays()
    {
        var r=NewRound();r.SetStake(4);r.Start();r.Ready();r.Hop();Assert.IsTrue(r.Hit());
        Assert.IsFalse(r.Land());Assert.IsFalse(r.CashOut());Assert.AreEqual(6,r.Balance);Assert.AreEqual(0,r.Row);
    }
    [Test] public void DecimalRewardMatchesDisplayedMultiplier()
    {
        var r=NewRound();r.SetStake(5);r.Start();r.Ready();for(int i=0;i<8;i++)Cross(r);
        Assert.AreEqual(19,r.PotentialReward);r.CashOut();Assert.AreEqual(19,r.Reward);Assert.AreEqual(24,r.Balance);
    }
    [Test] public void MaximumLaneCannotBeExceeded()
    {
        var r=NewRound();r.Start();r.Ready();for(int i=0;i<10;i++)Cross(r);Assert.IsFalse(r.Hop());Assert.AreEqual(10,r.Row);
    }
    [Test] public void NextRewardUsesTheSameIntegerRoundingAsCashOut()
    {
        var r=NewRound();r.SetStake(5);Assert.AreEqual(5,r.NextPotentialReward);r.Start();r.Ready();
        for(int i=0;i<8;i++){int next=r.NextPotentialReward;Cross(r);Assert.AreEqual(next,r.PotentialReward);}
    }
    [Test] public void RestartClearsPreviousResultAndProgress()
    {
        var r=NewRound();r.Start();r.Ready();Cross(r);r.CashOut();Assert.IsTrue(r.Start());Assert.AreEqual(0,r.Row);Assert.AreEqual(0,r.Reward);
    }
    [Test] public void FreeRefillOnlyAtZeroOutsideARound()
    {
        var r=NewRound();Assert.IsFalse(r.Refill());r.SetStake(10);r.Start();Assert.IsFalse(r.Refill());r.Ready();r.Hop();r.Hit();
        Assert.IsTrue(r.Refill());Assert.AreEqual(10,r.Balance);Assert.AreEqual(1,r.Stake);Assert.IsFalse(r.Refill());
    }
}
