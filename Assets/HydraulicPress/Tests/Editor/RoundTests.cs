using System;
using System.Linq;
using NUnit.Framework;

namespace HydraulicPress.Tests
{
    public sealed class RoundTests
    {
        [Test]
        public void TenThousandRoundsHaveTwentyUniqueObjectsAndExactlyThreeHazards()
        {
            var counts = new int[20];
            for (int seed = 0; seed < 10000; seed++)
            {
                var round = new RoundManager(seed); var hazards = round.CopyDangerPositions(); var objects = round.CopyObjectOrder();
                Assert.That(hazards.Length, Is.EqualTo(20)); Assert.That(hazards.Count(x => x), Is.EqualTo(3));
                Assert.That(objects.OrderBy(x => x).ToArray(), Is.EqualTo(Enumerable.Range(0,20).ToArray()));
                for (int i=0;i<20;i++) if(hazards[i]) counts[i]++;
            }
            foreach(int count in counts) Assert.That(count, Is.InRange(1300,1700), "Position frequency inconsistent with uniform shuffling");
        }
        [Test]
        public void SeedReproducesBothSequences()
        {
            var a=new RoundManager(413);var b=new RoundManager(413);
            Assert.That(a.CopyDangerPositions(),Is.EqualTo(b.CopyDangerPositions()));Assert.That(a.CopyObjectOrder(),Is.EqualTo(b.CopyObjectOrder()));
        }
        [Test]
        public void ActionsCannotMutateCommittedSequence()
        {
            var round=new RoundManager(SafeFirstSeed());var before=round.CopyDangerPositions();
            round.TryBeginCrush(out _);round.TryCollect(out _);round.TryBeginCrush(out _);round.ResolveCrush();
            Assert.That(round.CopyDangerPositions(),Is.EqualTo(before));
        }
        [Test]
        public void DoubleCrushAndMidAnimationCashoutAreRejected()
        {
            var round=new RoundManager(SafeFirstSeed());Assert.That(round.TryBeginCrush(out _),Is.True);
            for(int i=0;i<30;i++){Assert.That(round.TryBeginCrush(out _),Is.False);Assert.That(round.TryCollect(out _),Is.False);}
            Assert.That(round.Crushed,Is.Zero);Assert.That(round.ResolveCrush(),Is.True);Assert.That(round.ResolveCrush(),Is.False);Assert.That(round.Crushed,Is.EqualTo(1));
        }
        [Test]
        public void FirstSafeCashoutBanksExactly112PointsOnce()
        {
            var round=new RoundManager(SafeFirstSeed());Assert.That(round.TryCollect(out _),Is.False);round.TryBeginCrush(out _);round.ResolveCrush();
            Assert.That(round.Multiplier,Is.EqualTo(.96/.85).Within(1e-10));Assert.That(round.TryCollect(out long points),Is.True);Assert.That(points,Is.EqualTo(112));
            Assert.That(round.TryCollect(out _),Is.False);Assert.That(round.TryBeginCrush(out _),Is.False);
        }
        [Test]
        public void FailureLosesUnbankedRewardAndLocksRound()
        {
            var round=new RoundManager(11);
            while(round.CanCrush){round.TryBeginCrush(out _);round.ResolveCrush();}
            Assert.That(round.State,Is.EqualTo(RoundState.Failed));Assert.That(round.TryCollect(out long reward),Is.False);Assert.That(reward,Is.Zero);
        }
        [Test]
        public void SeventeenSuccessesAwardPerfectAndStopBeforeCertainFailure()
        {
            int seed=0;for(;seed<500000;seed++){var hazards=new RoundManager(seed).CopyDangerPositions();if(!hazards.Take(17).Any(x=>x))break;}
            Assert.That(seed,Is.LessThan(500000));var round=new RoundManager(seed);
            for(int i=0;i<17;i++){Assert.That(round.TryBeginCrush(out bool danger),Is.True);Assert.That(danger,Is.False);round.ResolveCrush();}
            Assert.That(round.State,Is.EqualTo(RoundState.Perfect));Assert.That(round.Multiplier,Is.EqualTo(1094.4).Within(1e-7));Assert.That(round.Reward,Is.EqualTo(109440));Assert.That(round.CanCrush,Is.False);
        }
        [TestCase(0,1)] [TestCase(1,.85)] [TestCase(2,.7157894736842105)] [TestCase(17,1d/1140)]
        public void SurvivalMatchesCombinatorics(int k,double expected)=>Assert.That(RoundManager.SurvivalAt(k),Is.EqualTo(expected).Within(1e-12));
        [Test]
        public void MultiplierIncreasesAtEverySuccess(){double last=1;for(int k=1;k<=17;k++){double next=RoundManager.MultiplierAt(k);Assert.That(next,Is.GreaterThan(last));last=next;}}
        [TestCase(0)] [TestCase(-5)] [TestCase(1000001)]
        public void InvalidRewardConfigIsRejected(int points)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new RoundManager(0,points));
        [TestCase("")] [TestCase("garbage")] [TestCase("{broken")] [TestCase("{\"version\":1,\"bank\":-1}")]
        public void CorruptedSaveIsRejected(string json)=>Assert.That(SaveManager.TryParse(json,out _),Is.False);
        [Test]
        public void SaveRoundTripPreservesProgressAndSettings()
        {
            var original=new SaveData{bank=123456,totalCrushed=17,bestRound=1000,bestMultiplier=10,sound=.4f,music=.1f,shake=false};
            Assert.That(SaveManager.TryParse(UnityEngine.JsonUtility.ToJson(original),out var restored),Is.True);
            Assert.That(restored.bank,Is.EqualTo(123456));Assert.That(restored.totalCrushed,Is.EqualTo(17));Assert.That(restored.shake,Is.False);Assert.That(restored.sound,Is.EqualTo(.4f));
        }
        [Test]
        public void LifetimeScoreCannotOverflow()=>Assert.That(SaveManager.AddSaturated(long.MaxValue-5,100),Is.EqualTo(long.MaxValue));
        private static int SafeFirstSeed(){for(int i=0;i<100;i++)if(!new RoundManager(i).CopyDangerPositions()[0])return i;throw new Exception("No safe seed");}
    }
}
