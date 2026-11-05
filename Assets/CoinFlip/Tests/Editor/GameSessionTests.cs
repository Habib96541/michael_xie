using NUnit.Framework;
using UnityEngine;

namespace CoinFlip.Tests
{
    public class GameSessionTests
    {
        GameSession Session(CoinSide result, SaveData save=null)=>new GameSession(save??new SaveData(),()=>result);
        [Test] public void NewGameStartsWithTenAndRequiresSelection()
        {var s=Session(CoinSide.Heads);Assert.AreEqual(10,s.Data.balance);Assert.False(s.TryBegin(out _));Assert.AreEqual(RoundState.Idle,s.State);}
        [TestCase(CoinSide.Heads)] [TestCase(CoinSide.Tails)]
        public void WinningRoundDeductsOnceAndPaysTotalReturnOnce(CoinSide side)
        {
            var s=Session(side);s.Select(side);s.SetWager(3);Assert.True(s.TryBegin(out var r));Assert.AreEqual(7,s.Data.balance);
            Assert.False(s.TryBegin(out _));Assert.False(s.Select(side==CoinSide.Heads?CoinSide.Tails:CoinSide.Heads));Assert.False(s.SetWager(1));
            Assert.True(s.Complete(r));Assert.AreEqual(13,s.Data.balance);Assert.False(s.Complete(r));Assert.AreEqual(13,s.Data.balance);Assert.AreEqual(1,s.Data.streak);
        }
        [Test] public void LosingAllCoinsAllowsFreshStart()
        {var s=Session(CoinSide.Tails);s.Select(CoinSide.Heads);s.SetWager(10);s.TryBegin(out var r);s.Complete(r);Assert.AreEqual(0,s.Data.balance);Assert.False(s.CanFlip);Assert.True(s.Reset());Assert.AreEqual(10,s.Data.balance);Assert.AreEqual(1,s.Data.wager);Assert.True(s.CanFlip);}
        [Test] public void InvalidAndUnaffordableWagersNeverDraw()
        {
            int draws=0;var s=new GameSession(new SaveData(),()=>{draws++;return CoinSide.Heads;});s.Select(CoinSide.Heads);
            Assert.False(s.SetWager(0));Assert.False(s.SetWager(-10));Assert.True(s.SetWager(11));Assert.False(s.TryBegin(out _));Assert.AreEqual(0,draws);Assert.AreEqual(10,s.Data.balance);
        }
        [Test] public void OutcomeIsDrawnOnceAndLocked()
        {int draws=0;var s=new GameSession(new SaveData(),()=>{draws++;return CoinSide.Tails;});s.Select(CoinSide.Heads);s.TryBegin(out var r);for(int i=0;i<100;i++)s.TryBegin(out _);Assert.AreEqual(1,draws);Assert.AreEqual(CoinSide.Tails,r.Outcome);s.Complete(r);Assert.AreEqual(9,s.Data.balance);}
        [TestCase(true)] [TestCase(false)] public void ReloadDuringFlipSettlesStoredRoundOnce(bool won)
        {
            var s=Session(won?CoinSide.Heads:CoinSide.Tails);s.Select(CoinSide.Heads);s.SetWager(4);s.TryBegin(out _);
            var save=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s.Data));int draws=0;
            var loaded=new GameSession(save,()=>{draws++;return CoinSide.Heads;});Assert.AreEqual(won?14:6,loaded.Data.balance);Assert.AreEqual(0,draws);Assert.AreEqual(-1,loaded.Data.pendingResult);
            var twice=Session(CoinSide.Heads,JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(loaded.Data)));Assert.AreEqual(loaded.Data.balance,twice.Data.balance);Assert.AreEqual(1,twice.Data.history.Count);
        }
        [Test] public void ForgedOrOldSettlementIsRejected()
        {var s=Session(CoinSide.Heads);s.Select(CoinSide.Heads);s.TryBegin(out var r);Assert.False(s.Complete(new Round(r.Id,99,r.Choice,r.Outcome)));Assert.False(s.Complete(new Round(r.Id+1,r.Wager,r.Choice,r.Outcome)));Assert.AreEqual(9,s.Data.balance);Assert.True(s.Complete(r));}
        [Test] public void CorruptedSaveFallsBackGracefully()
        {var bad=new SaveData{balance=-4};Assert.False(bad.IsValid());Assert.AreEqual(10,Session(CoinSide.Heads,bad).Data.balance);bad=new SaveData{pendingResult=1,pendingWager=0};Assert.False(bad.IsValid());bad=new SaveData{sfx=float.NaN};Assert.False(bad.IsValid());}
        [TestCase("{garbage")] [TestCase("{\"version\":1,\"balance\":-10}")]
        public void CorruptPlayerPrefsJsonLoadsDefaultWithoutThrowing(string json)
        {
            bool existed=PlayerPrefs.HasKey(SaveManager.Key);string original=PlayerPrefs.GetString(SaveManager.Key,"");
            try{PlayerPrefs.SetString(SaveManager.Key,json);Assert.AreEqual(10,SaveManager.Load().balance);}
            finally{if(existed)PlayerPrefs.SetString(SaveManager.Key,original);else PlayerPrefs.DeleteKey(SaveManager.Key);PlayerPrefs.Save();}
        }
        [Test] public void HistoryIsBoundedAndBestStreakSurvivesReset()
        {var s=Session(CoinSide.Heads);s.Select(CoinSide.Heads);for(int i=0;i<20;i++){s.TryBegin(out var r);s.Complete(r);s.Ready();}Assert.AreEqual(8,s.Data.history.Count);Assert.AreEqual(20,s.Data.bestStreak);s.Reset();Assert.AreEqual(20,s.Data.bestStreak);Assert.AreEqual(0,s.Data.streak);}
        [Test] public void RepeatedRandomRoundsConserveCurrency()
        {
            var rng=new System.Random(803);var s=new GameSession(new SaveData{balance=10000},()=>(CoinSide)rng.Next(2));int expected=10000;
            for(int i=0;i<1000;i++){s.Select((CoinSide)(i%2));s.SetWager(i%13+1);Assert.True(s.TryBegin(out var r));expected+=r.Won?r.Wager:-r.Wager;s.Complete(r);Assert.AreEqual(expected,s.Data.balance);Assert.GreaterOrEqual(s.Data.balance,0);s.Ready();}
        }
    }
}
