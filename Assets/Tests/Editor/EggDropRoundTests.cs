using System;
using NUnit.Framework;

namespace EggDrop.Tests
{
    public sealed class EggDropRoundTests
    {
        [Test]
        public void FreshRoundRequiresLandingBeforeCollecting()
        {
            var round = new EggDropRound();
            Assert.That(round.Floor, Is.EqualTo(1));
            Assert.That(round.RoundPoints, Is.Zero);
            Assert.That(round.TryCollect(), Is.False);
            Assert.That(round.CanDrop, Is.True);
        }

        [Test]
        public void FallingRejectsDuplicateDropAndCollect()
        {
            var round = new EggDropRound();
            round.TryDrop();
            Assert.That(round.TryDrop(), Is.False);
            Assert.That(round.TryCollect(), Is.False);
        }

        [Test]
        public void SurvivingRaisesFloorRiskAndReward()
        {
            var round = new EggDropRound();
            float initialRisk = round.CrackChance;
            round.TryDrop();
            Assert.That(round.ResolveLanding(0.99f), Is.True);
            Assert.That(round.Floor, Is.EqualTo(2));
            Assert.That(round.CrackChance, Is.GreaterThan(initialRisk));
            Assert.That(round.RoundPoints, Is.EqualTo(132));
            Assert.That(round.NextMultiplier, Is.GreaterThan(round.Multiplier));
        }

        [Test]
        public void CollectionBanksPointsExactlyOnce()
        {
            var round = new EggDropRound();
            round.TryDrop();
            round.ResolveLanding(0.99f);
            Assert.That(round.TryCollect(), Is.True);
            Assert.That(round.BankedPoints, Is.EqualTo(132));
            Assert.That(round.TryCollect(), Is.False);
            Assert.That(round.TryDrop(), Is.False);
        }

        [Test]
        public void CrackLosesUncollectedPointsAndPreservesBank()
        {
            var round = new EggDropRound();
            round.TryDrop();
            round.ResolveLanding(0.99f);
            round.TryCollect();
            round.Restart();
            round.TryDrop();
            round.ResolveLanding(0.99f);
            round.TryDrop();
            Assert.That(round.ResolveLanding(0f), Is.False);
            Assert.That(round.State, Is.EqualTo(RoundState.Cracked));
            Assert.That(round.RoundPoints, Is.Zero);
            Assert.That(round.BankedPoints, Is.EqualTo(132));
            Assert.That(round.TryCollect(), Is.False);
            Assert.That(round.TryDrop(), Is.False);
        }

        [Test]
        public void RestartResetsRoundAndKeepsCollectedPoints()
        {
            var round = new EggDropRound();
            round.TryDrop();
            round.ResolveLanding(0.99f);
            round.TryCollect();
            round.Restart();
            Assert.That(round.Floor, Is.EqualTo(1));
            Assert.That(round.SuccessfulDrops, Is.Zero);
            Assert.That(round.RoundPoints, Is.Zero);
            Assert.That(round.BankedPoints, Is.EqualTo(132));
        }

        [Test]
        public void RestartCannotCancelAnActiveRound()
        {
            var round = new EggDropRound();
            round.TryDrop();
            round.Restart();
            Assert.That(round.State, Is.EqualTo(RoundState.Falling));
        }

        [Test]
        public void SummitRequiresCollectionAndDoesNotAllowThirteenthDrop()
        {
            var round = new EggDropRound();
            for (int floor = 1; floor <= EggDropRound.MaximumFloor; floor++)
            {
                Assert.That(round.Floor, Is.EqualTo(floor));
                Assert.That(round.CrackChance, Is.LessThan(1f));
                Assert.That(round.TryDrop(), Is.True);
                round.ResolveLanding(0.99f);
            }
            Assert.That(round.CanDrop, Is.False);
            Assert.That(round.CanCollect, Is.True);
            Assert.That(round.TryCollect(), Is.True);
            Assert.That(round.BankedPoints, Is.EqualTo(1408));
        }

        [Test]
        public void LandingWithoutDropIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new EggDropRound().ResolveLanding(0.5f));
        }

        [TestCase(-0.1f)]
        [TestCase(1f)]
        [TestCase(float.NaN)]
        public void InvalidRandomRollIsRejected(float roll)
        {
            var round = new EggDropRound();
            round.TryDrop();
            Assert.Throws<ArgumentOutOfRangeException>(() => round.ResolveLanding(roll));
        }

        [Test]
        public void ExactRiskBoundarySurvives()
        {
            var round = new EggDropRound();
            round.TryDrop();
            Assert.That(round.ResolveLanding(round.CrackChance), Is.True);
        }
    }
}
