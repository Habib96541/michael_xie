using System.Collections;
using NUnit.Framework;
using PocketBorough;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MatchFlowTests
{
    [UnityTest] public IEnumerator MatchCanPauseFinishAndReplayWithoutStaleState()
    {
        yield return SceneManager.LoadSceneAsync("PocketBorough");yield return null;
        var game=Object.FindFirstObjectByType<BoroughGame>();Assert.IsNotNull(game);Assert.AreEqual(8,game.Holes.Count);int total=game.World.objects.Count;
        game.StartMatch();Assert.AreEqual(MatchStage.Countdown,game.Stage);yield return new WaitForSecondsRealtime(3.3f);Assert.AreEqual(MatchStage.Playing,game.Stage);
        game.AutoPilot=true;yield return new WaitForSecondsRealtime(5);Assert.Greater(game.Player.Score,0);Assert.Greater(game.Player.Radius,BoroughRules.InitialRadius);Assert.IsTrue(game.Holes.Exists(h=>!h.IsPlayer&&h.Score>0));
        game.TogglePause();float time=game.TimeLeft;Vector3 position=game.Player.Position;yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(time,game.TimeLeft);Assert.AreEqual(position,game.Player.Position);
        game.Resume();game.TimeLeft=.15f;yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(MatchStage.Results,game.Stage);Assert.AreEqual(0,game.TimeLeft);Assert.GreaterOrEqual(game.Best,game.Player.Score);Assert.AreEqual(8,game.Rankings.Count);
        game.StartMatch();Assert.AreEqual(total,game.World.Remaining);Assert.AreEqual(0,game.World.falling.Count);foreach(var hole in game.Holes){Assert.AreEqual(0,hole.Score);Assert.AreEqual(0,hole.Consumed);Assert.AreEqual(BoroughRules.InitialRadius,hole.Radius,.001f);}Assert.AreEqual(120,game.TimeLeft);game.MainMenu();
    }
}
