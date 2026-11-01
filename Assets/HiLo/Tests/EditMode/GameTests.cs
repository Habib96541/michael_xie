using System;
using System.Collections.Generic;
using System.Numerics;
using HiLo.Core;
using NUnit.Framework;
using UnityEngine;

public sealed class GameTests
{
    GameSession Start(int rank=8)
    {
        var deck=new CardDeckManager(81);var game=new GameSession(new ScientificNumber(200),10,.04,deck);game.Begin();
        // Walk deterministic seeds until the requested current card appears. Never changes the live engine.
        for(int seed=0;game.Current.Rank!=rank;seed++){deck=new CardDeckManager(seed);game=new GameSession(new ScientificNumber(200),10,.04,deck);game.Begin();}
        return game;
    }
    [Test] public void DeckContainsExactly52UniqueCards()
    { var deck=new CardDeckManager(5);var seen=new HashSet<string>();for(int i=0;i<52;i++)seen.Add(deck.Draw().AssetName);Assert.AreEqual(52,seen.Count);Assert.AreEqual(0,deck.Remaining); }
    [Test] public void ExhaustionReshufflesSeamlessly()
    {var deck=new CardDeckManager(5);for(int i=0;i<52;i++)deck.Draw();var c=deck.Draw();Assert.AreEqual(2,deck.Shuffles);Assert.AreEqual(51,deck.Remaining);Assert.That(c.Rank,Is.InRange(2,14));}
    [Test] public void ShuffleIsDeterministicForTestSeedAndVariesAcrossSeeds()
    {var a=new CardDeckManager(8);var b=new CardDeckManager(8);var c=new CardDeckManager(9);bool different=false;for(int i=0;i<52;i++){string x=a.Draw().AssetName;Assert.AreEqual(x,b.Draw().AssetName);different|=x!=c.Draw().AssetName;}Assert.IsTrue(different);}
    [Test] public void RemainingDeckOddsIncludePush()
    {var deck=new CardDeckManager(1);deck.SetTestSequence(new Card(9,0),new Card(7,0),new Card(8,0),new Card(9,1));var odds=deck.GetOdds(new Card(8,1));Assert.AreEqual(2,odds.Higher);Assert.AreEqual(1,odds.Lower);Assert.AreEqual(1,odds.Equal);Assert.AreEqual(.5,odds.Probability(true));}
    [TestCase(true,9)] [TestCase(false,7)] public void CorrectPredictionsIncreaseStreakAndMultiplier(bool higher,int next)
    {var g=Start();g.Deck.SetTestSequence(new Card(next,0),new Card(6,1),new Card(12,2));Assert.IsTrue(g.Predict(higher));Assert.AreEqual(Outcome.Correct,g.LastOutcome);Assert.AreEqual(BigInteger.One,g.Streak);Assert.Greater(g.Multiplier.CompareTo(ScientificNumber.One),0);g.CompleteReveal();Assert.AreEqual(RoundState.Decision,g.State);}
    [Test] public void EqualCardsPushWithoutChangingMultiplier()
    {var g=Start();g.Deck.SetTestSequence(new Card(8,0),new Card(10,0));Assert.IsTrue(g.Predict(true));Assert.AreEqual(Outcome.Push,g.LastOutcome);Assert.AreEqual(BigInteger.Zero,g.Streak);Assert.AreEqual(0,g.Multiplier.CompareTo(ScientificNumber.One));g.CompleteReveal();Assert.AreEqual(RoundState.Decision,g.State);}
    [Test] public void WrongPredictionEndsRoundAndForfeitsPotential()
    {var g=Start();g.Deck.SetTestSequence(new Card(3,0),new Card(12,0));g.Predict(true);g.CompleteReveal();Assert.AreEqual(Outcome.Wrong,g.LastOutcome);Assert.AreEqual(RoundState.Ended,g.State);Assert.AreEqual("190",g.Balance.Format());Assert.IsFalse(g.CashOut());}
    [Test] public void AceIsHighAndImpossibleChoiceIsRejected()
    {var g=Start(14);Assert.AreEqual(0,g.Odds.Higher);Assert.IsFalse(g.Predict(true));g.Deck.SetTestSequence(new Card(13,0));Assert.IsTrue(g.Predict(false));Assert.AreEqual(Outcome.Correct,g.LastOutcome);}
    [Test] public void RapidClicksAndCashoutDuringRevealAreRejected()
    {var g=Start();g.Deck.SetTestSequence(new Card(9,0),new Card(10,0));Assert.IsTrue(g.Predict(true));Assert.IsFalse(g.Predict(true));Assert.IsFalse(g.CashOut());Assert.IsFalse(g.Begin());Assert.AreEqual(1,g.Deck.Remaining);}
    [Test] public void CashOutReturnsPotentialExactlyOnce()
    {var g=Start();Assert.IsTrue(g.CashOut());Assert.AreEqual("200",g.Balance.Format());Assert.IsFalse(g.CashOut());Assert.AreEqual("200",g.Balance.Format());}
    [Test] public void TimeoutEndsWithoutChoosingOrPaying()
    {var g=Start();int n=g.Deck.Remaining;Assert.IsTrue(g.Timeout());Assert.AreEqual(Outcome.Timeout,g.LastOutcome);Assert.AreEqual(n,g.Deck.Remaining);Assert.AreEqual("190",g.Balance.Format());}
    [Test] public void RestartResetsRoundButKeepsBalance()
    {var g=Start();g.Timeout();Assert.IsTrue(g.Begin());Assert.AreEqual("180",g.Balance.Format());Assert.AreEqual(0,g.Multiplier.CompareTo(ScientificNumber.One));Assert.AreEqual(BigInteger.Zero,g.Streak);}
    [Test] public void FreeRefillPreventsPermanentBankruptcy()
    {var g=new GameSession(new ScientificNumber(10));g.Begin();Assert.IsFalse(g.Refill(100));g.Timeout();Assert.IsFalse(g.CanAfford);Assert.IsTrue(g.Refill(100));Assert.IsTrue(g.Begin());Assert.IsFalse(g.Refill(100));}
    [Test] public void StakeDepletionDoesNotLoseARoundToFloatingPointDrift()
    {var g=new GameSession(new ScientificNumber(200));for(int i=0;i<20;i++){Assert.IsTrue(g.Begin(),"Round "+i);g.Timeout();}Assert.AreEqual("0",g.Balance.Format());Assert.IsFalse(g.CanAfford);}
    [Test] public void RiskierChoicesPayMoreAndEveryCorrectChoiceGrows()
    {var odds=new Odds(4,44,3);Assert.Greater(odds.Factor(true,.04),odds.Factor(false,.04));Assert.AreEqual(1.01,new Odds(4,0,3).Factor(true,.04));Assert.AreEqual(1.02,new Odds(4,0,3).Factor(true,.04,.02));}
    [Test] public void AllEqualRemainderCanPushThroughToFreshDeck()
    {var g=Start();g.Deck.SetTestSequence(new Card(8,0),new Card(8,1));Assert.IsTrue(g.Predict(true));g.CompleteReveal();Assert.IsTrue(g.Predict(false));g.CompleteReveal();Assert.AreEqual(Outcome.Push,g.LastOutcome);Assert.AreEqual(52,g.Deck.Remaining);Assert.AreEqual(RoundState.Decision,g.State);Assert.AreEqual(BigInteger.Zero,g.Streak);Assert.AreEqual(0,g.Multiplier.CompareTo(ScientificNumber.One));}
    [Test] public void ExpectedValueIncludingPushMatchesConfiguredEdge()
    {var odds=new Odds(12,34,5);double factor=odds.Factor(true,.04);double expected=(12*factor+5d)/51;Assert.That(expected,Is.EqualTo(1-.04*34/51).Within(1e-12));}
    [Test] public void HugeNumbersStayFiniteAndRoundTrip()
    {var n=ScientificNumber.One;for(int i=0;i<100000;i++)n=n.Multiply(12.52);Assert.IsFalse(double.IsInfinity(n.Mantissa));Assert.Greater(n.Exponent,new BigInteger(100000));Assert.AreEqual(0,n.CompareTo(ScientificNumber.Decode(n.Encode())));StringAssert.Contains("e",n.Format());}
    [Test] public void ArbitraryExponentDoesNotOverflow()
    {var e=BigInteger.Pow(10,30);var n=new ScientificNumber(9,e).Multiply(2);Assert.AreEqual(e+1,n.Exponent);Assert.That(n.Mantissa,Is.EqualTo(1.8).Within(1e-12));}
    [Test] public void ScientificBalanceAdditionSubtractionAndFormatting()
    {var n=new ScientificNumber(1234);Assert.AreEqual("1.23K",n.Format());Assert.That(n.Subtract(new ScientificNumber(10)).Mantissa,Is.EqualTo(1.224).Within(1e-12));Assert.AreEqual("200",new ScientificNumber(190).Add(new ScientificNumber(10)).Format());}
    [Test] public void StagesTightenAndEndlessTimerHasMinimum()
    {var c=ScriptableObject.CreateInstance<HiLo.GameConfig>();Assert.AreEqual(0,c.DecisionSeconds(4));Assert.AreEqual(16,c.DecisionSeconds(5));Assert.AreEqual(11,c.DecisionSeconds(10));Assert.AreEqual(8,c.DecisionSeconds(20));Assert.AreEqual(7,c.DecisionSeconds(40));Assert.AreEqual(c.minimumDecisionSeconds,c.DecisionSeconds(BigInteger.Pow(10,100)));UnityEngine.Object.DestroyImmediate(c);}
    [Test] public void LongSessionHasNoStreakCapAndReshuffles()
    {var g=Start(2);for(int i=0;i<10000;i++){int next=g.Current.Rank==2?14:2;g.Deck.SetTestSequence(new Card(next,0));Assert.IsTrue(g.Predict(next>g.Current.Rank));g.CompleteReveal();}Assert.AreEqual(new BigInteger(10000),g.Streak);Assert.AreEqual(RoundState.Decision,g.State);Assert.Greater(g.Deck.Shuffles,10000);}
}
