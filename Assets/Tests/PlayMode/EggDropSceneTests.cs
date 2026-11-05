using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EggDrop.Tests
{
    public sealed class EggDropSceneTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator PlayableSceneSupportsSurviveCollectCrackRestartAndAudio()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("EggDrop");
            while (!load.isDone) yield return null;
            yield return null;
            Assert.That(Application.isPlaying, Is.True);
            GameObject[] sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            TestContext.WriteLine("Loaded roots: " + string.Join(", ", sceneRoots.Select(item => item.name)));
            EggDropGame game = sceneRoots.SelectMany(item => item.GetComponentsInChildren<EggDropGame>(true)).Single();
            EggDropAudio audio = sceneRoots.SelectMany(item => item.GetComponentsInChildren<EggDropAudio>(true)).Single();
            EggBody egg = sceneRoots.SelectMany(item => item.GetComponentsInChildren<EggBody>(true)).Single();
            UIDocument document = sceneRoots.SelectMany(item => item.GetComponentsInChildren<UIDocument>(true)).Single();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("EggDrop"));
            Assert.That(game, Is.Not.Null, "Game controller is absent from loaded scene.");
            Assert.That(audio, Is.Not.Null, "Audio controller is absent from loaded scene.");
            Assert.That(egg, Is.Not.Null, "Egg is absent from loaded scene.");
            Assert.That(document != null, Is.True, "UI document is absent from loaded scene.");
            VisualElement root = document.rootVisualElement;
            Assert.That(root, Is.Not.Null, "UI document root is not initialized.");
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            AudioSource[] sources = audio.GetComponents<AudioSource>();
            Assert.That(sources, Has.Length.EqualTo(3));
            Assert.That(sources.Single(source => source.clip != null && source.clip.name == "Music").isPlaying, Is.True);
            Assert.That(root.Q<Button>("collect").enabledSelf, Is.False);
            game.StartRound();

            bool originallyMuted = audio.IsMuted;
            float originalMusic = audio.MusicVolume;
            float originalEffects = audio.EffectsVolume;
            Keyboard originalKeyboard = Keyboard.current;
            Keyboard testKeyboard = InputSystem.AddDevice<Keyboard>();
            testKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.M));
            yield return WaitFor(() => audio.IsMuted != originallyMuted);
            Assert.That(audio.IsMuted, Is.Not.EqualTo(originallyMuted), "The M shortcut did not toggle mute.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.M));
            yield return WaitFor(() => audio.IsMuted == originallyMuted);
            Assert.That(audio.IsMuted, Is.EqualTo(originallyMuted));
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            if (originallyMuted) game.ToggleMute();
            game.ToggleMute();
            Assert.That(sources.All(source => source.volume == 0), Is.True);
            Assert.That(root.Q<Button>("mute").text, Does.Contain("OFF"));
            game.ToggleMute();
            audio.SetMusicVolume(0.2f);
            audio.SetEffectsVolume(0.6f);
            Assert.That(sources.Single(source => source.clip != null && source.clip.name == "Music").volume, Is.EqualTo(0.09f).Within(0.001f));

            SetLandingRoll(false);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
            yield return WaitFor(() => root.Q<Label>("state").text == "IN FREE FALL");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            game.Drop();
            game.Collect();
            Assert.That(root.Q<Button>("drop").enabledSelf, Is.False);
            Assert.That(root.Q<Button>("collect").enabledSelf, Is.False);
            Assert.That(sources.Single(source => source.clip != null && source.clip.name == "Falling").isPlaying, Is.True);
            yield return WaitFor(() => root.Q<Button>("collect").enabledSelf);
            Assert.That(root.Q<Label>("points").text, Is.EqualTo("132"));
            Assert.That(root.Q<Label>("floor").text, Is.EqualTo("02"));
            Assert.That(sources.Single(source => source.clip != null && source.clip.name == "Falling").isPlaying, Is.False);
            Assert.That(egg.transform.position.y, Is.EqualTo(3.85f).Within(0.02f));
            Assert.That(root.Q("history").childCount, Is.EqualTo(12));

            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.C));
            yield return WaitFor(() => root.Q<Label>("bank").text == "132");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            game.Collect();
            Assert.That(root.Q<Label>("bank").text, Is.EqualTo("132"));
            Assert.That(root.Q<Label>("result-title").text, Is.EqualTo("Nicely played."));
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.R));
            yield return WaitFor(() => root.Q<Label>("points").text == "0");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            Assert.That(root.Q<Label>("points").text, Is.EqualTo("0"));
            Assert.That(root.Q<Label>("bank").text, Is.EqualTo("132"));

            SetLandingRoll(true);
            game.Drop();
            yield return WaitFor(() => root.Q<Label>("result-title").text == "Oh, shell.");
            Assert.That(root.Q<Label>("points").text, Is.EqualTo("0"));
            Assert.That(root.Q<Label>("bank").text, Is.EqualTo("132"));
            Assert.That(egg.transform.Find("Intact shell").gameObject.activeSelf, Is.False);
            Assert.That(egg.transform.Find("Fracture Effect/Yolk").gameObject.activeSelf, Is.True);
            game.ReturnToMenu();
            Assert.That(root.Q("main-menu").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("bank").text, Is.EqualTo("132"));
            Assert.That(egg.transform.Find("Intact shell").gameObject.activeSelf, Is.True);
            Assert.That(egg.transform.Find("Fracture Effect/Yolk").gameObject.activeSelf, Is.False);
            game.StartRound();
            Assert.That(root.Q<Button>("drop").enabledSelf, Is.True);
            audio.SetMusicVolume(originalMusic);
            audio.SetEffectsVolume(originalEffects);
            if (originallyMuted) game.ToggleMute();
            InputSystem.RemoveDevice(testKeyboard);
            if (originalKeyboard != null) originalKeyboard.MakeCurrent();
        }

        private static IEnumerator WaitFor(System.Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 7f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for gameplay or keyboard input.");
        }

        private static void SetLandingRoll(bool shouldCrack)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                Random.InitState(seed);
                Random.State state = Random.state;
                float roll = Random.value;
                if ((roll < 0.08f) != shouldCrack) continue;
                Random.state = state;
                return;
            }
            Assert.Fail("Could not select a deterministic landing roll.");
        }
    }
}
