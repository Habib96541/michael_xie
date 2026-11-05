using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HydraulicPress.PlayTests
{
    public sealed class GameplayTests
    {
        private GameManager game;
        private string saveBefore;
        private string backupBefore;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            saveBefore=PlayerPrefs.GetString("hydraulic-press.save.v1","");backupBefore=PlayerPrefs.GetString("hydraulic-press.save.v1.backup","");
            SceneManager.LoadScene("HydraulicPress");yield return null;
            game=Object.FindFirstObjectByType<GameManager>();Assert.That(game,Is.Not.Null);
            Assert.That(game.Navigation, Is.EqualTo(NavigationState.MainMenu));
            Assert.That(game.Round, Is.Null); Assert.That(Time.timeScale, Is.Zero);
            game.Crush(); game.Collect(); Assert.That(game.Round, Is.Null);
            game.PlayOrResume();
            yield return Ready();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            PlayerPrefs.SetString("hydraulic-press.save.v1",saveBefore);PlayerPrefs.SetString("hydraulic-press.save.v1.backup",backupBefore);PlayerPrefs.Save();
            Time.timeScale=1;yield return null;
        }
        [UnityTest]
        public IEnumerator RapidActionsAreLockedAndCashoutCreditsOnce()
        {
            for(int i=0;game.Round.CopyDangerPositions()[0]&&i<100;i++){game.NewRound();yield return Ready();}
            Assert.That(game.Round.CopyDangerPositions()[0],Is.False);
            long bank=game.Save.bank;var round=game.Round;game.Crush();
            for(int i=0;i<30;i++){game.Crush();game.Collect();game.NewRound();}
            Assert.That(game.Round,Is.SameAs(round));Assert.That(game.Round.State,Is.EqualTo(RoundState.Crushing));Assert.That(game.Save.bank,Is.EqualTo(bank));
            yield return Ready();Assert.That(game.Round.Crushed,Is.EqualTo(1));long reward=game.Round.Reward;
            game.Collect();for(int i=0;i<20;i++)game.Collect();Assert.That(game.Save.bank,Is.EqualTo(bank+reward));Assert.That(game.hud.ResultVisible,Is.True);
            yield return new WaitForSeconds(2.8f);yield return Ready();Assert.That(game.Round.Crushed,Is.Zero);
        }
        [UnityTest]
        public IEnumerator FailureRestoresMachineAndReusesBoundedPoolsAcrossTwelveRounds()
        {
            int bodies=Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            for(int round=0;round<12;round++)
            {
                // Choose a naturally shuffled round with a first-position hazard. No outcome is edited.
                for(int i=0;!game.Round.CopyDangerPositions()[0]&&i<100;i++){game.NewRound();yield return Ready();}
                Assert.That(game.Round.CopyDangerPositions()[0],Is.True);long bank=game.Save.bank;
                game.Crush();yield return Ready();Assert.That(game.Round.State,Is.EqualTo(RoundState.Failed));Assert.That(game.Save.bank,Is.EqualTo(bank));
                Assert.That(game.press.plate.localRotation,Is.Not.EqualTo(Quaternion.identity));
                game.NewRound();yield return Ready();Assert.That(game.press.plate.localRotation,Is.EqualTo(Quaternion.identity));Assert.That(game.effects.ActiveFragments,Is.Zero);
                Assert.That(game.objectAnchor.childCount,Is.EqualTo(20));
                Assert.That(Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(bodies));
            }
        }
        [UnityTest]
        public IEnumerator PauseFreezesAnActivePressAndMenusPreserveItsCommittedRound()
        {
            var round=game.Round; var hazards=round.CopyDangerPositions(); var bank=game.Save.bank;
            game.Crush(); yield return new WaitForSeconds(.12f);
            game.PauseGame(); Assert.That(game.Navigation,Is.EqualTo(NavigationState.Paused));
            Assert.That(Time.timeScale,Is.Zero); Assert.That(AudioListener.pause,Is.True);
            var plate=game.press.plate.localPosition;
            game.Crush(); game.Collect(); game.NewRound();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(game.press.plate.localPosition,Is.EqualTo(plate));
            Assert.That(round.State,Is.EqualTo(RoundState.Crushing));
            game.SetSettings(true); Assert.That(game.SettingsOpen,Is.True);
            game.SetSettings(false); Assert.That(Time.timeScale,Is.Zero);
            game.ShowMainMenu(); Assert.That(game.Navigation,Is.EqualTo(NavigationState.MainMenu));
            Assert.That(game.Round,Is.SameAs(round)); Assert.That(game.Save.bank,Is.EqualTo(bank));
            game.PlayOrResume(); Assert.That(Time.timeScale,Is.EqualTo(1)); Assert.That(AudioListener.pause,Is.False);
            Assert.That(game.Round,Is.SameAs(round)); CollectionAssert.AreEqual(hazards,round.CopyDangerPositions());
            yield return Ready();
            Assert.That(round.State,Is.Not.EqualTo(RoundState.Crushing)); Assert.That(round.Crushed,Is.EqualTo(hazards[0]?0:1));
        }
        [UnityTest]
        public IEnumerator EveryObjectCanDeformAndResetWithoutAllocatingNewMeshes()
        {
            var items=game.objectAnchor.GetComponentsInChildren<CrushableObject>(true);Assert.That(items.Length,Is.EqualTo(20));
            foreach(var item in items){item.Deform(.8f);Assert.That(item.transform.localScale.y,Is.LessThan(.4f));item.ResetObject();Assert.That(item.transform.localScale,Is.EqualTo(Vector3.one));}
            game.SetSettings(true);Assert.That(Time.timeScale,Is.Zero);Assert.That(game.CanCrush,Is.False);game.SetSettings(false);Assert.That(Time.timeScale,Is.EqualTo(1));
            yield return null;
        }
        private IEnumerator Ready()
        {
            float deadline=Time.realtimeSinceStartup+12;
            do{yield return null;if(Time.realtimeSinceStartup>deadline)Assert.Fail($"Presentation did not finish within twelve seconds (timeScale={Time.timeScale}, navigation={game?.Navigation}, state={game?.Round?.State})");}while(game!=null&&game.Busy);
        }
    }
}
