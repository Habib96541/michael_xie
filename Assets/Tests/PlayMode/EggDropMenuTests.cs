using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EggDrop.Tests
{
    public sealed class EggDropMenuTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator PortraitMenuReservesWrappedTextHeightOnStartup()
        {
            yield return SceneManager.LoadSceneAsync("EggDrop");
            yield return null;
            PanelSettings settings = Object.FindFirstObjectByType<UIDocument>().panelSettings;
            RenderTexture previousTarget = settings.targetTexture;
            RenderTexture target = new RenderTexture(1080, 1920, 24);
            try
            {
                target.Create();
                settings.targetTexture = target;
                yield return SceneManager.LoadSceneAsync("EggDrop");
                for (int frame = 0; frame < 8; frame++) yield return null;
                VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
                SavePreview(target, "Startup_1080x1920");
                AssertStoryLayout(root, "Initial portrait layout");
            }
            finally
            {
                settings.targetTexture = previousTarget;
                target.Release();
                Object.Destroy(target);
            }
        }

        [UnityTest]
        public IEnumerator MenuBlocksGameplayAndSettingsPersistIntoPlayAndReload()
        {
            yield return SceneManager.LoadSceneAsync("EggDrop");
            yield return null;
            EggDropGame game = Object.FindFirstObjectByType<EggDropGame>();
            EggDropAudio audio = Object.FindFirstObjectByType<EggDropAudio>();
            EggBody egg = Object.FindFirstObjectByType<EggBody>();
            VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            float originalMusic = audio.MusicVolume;
            float originalEffects = audio.EffectsVolume;
            bool originallyMuted = audio.IsMuted;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                AssertMenuVisible(root);
                Assert.That(root.Q<Button>("quit").enabledSelf, Is.False, "Quit must not close the editor.");
                yield return CaptureMenu("Home", root);
                Vector3 eggPosition = egg.transform.position;
                Press(keyboard.spaceKey);
                Press(keyboard.cKey);
                Press(keyboard.rKey);
                game.Drop();
                game.Collect();
                game.Restart();
                yield return new WaitForSeconds(0.2f);
                Release(keyboard.spaceKey);
                Release(keyboard.cKey);
                Release(keyboard.rKey);
                AssertMenuVisible(root);
                Assert.That(egg.transform.position, Is.EqualTo(eggPosition));
                Assert.That(egg.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(root.Q<Label>("points").text, Is.EqualTo("0"));

                Submit(root.Q<Button>("settings"));
                Assert.That(root.Q("menu-settings").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(root.Q("menu-home").style.display.value, Is.EqualTo(DisplayStyle.None));
                root.Q<Slider>("menu-music").value = 0.24f;
                root.Q<Slider>("menu-effects").value = 0.65f;
                Assert.That(audio.MusicVolume, Is.EqualTo(0.24f).Within(0.001f));
                Assert.That(audio.EffectsVolume, Is.EqualTo(0.65f).Within(0.001f));
                Submit(root.Q<Button>("menu-mute"));
                Assert.That(audio.IsMuted, Is.Not.EqualTo(originallyMuted));
                Assert.That(root.Q<Button>("mute").text, Is.EqualTo(root.Q<Button>("menu-mute").text));
                Press(keyboard.mKey);
                yield return null;
                Release(keyboard.mKey);
                Assert.That(audio.IsMuted, Is.EqualTo(originallyMuted));
                yield return CaptureMenu("Settings", root);
                Submit(root.Q<Button>("settings-back"));
                Assert.That(root.Q("menu-home").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Submit(root.Q<Button>("play"));
                yield return null;
                Assert.That(root.Q("main-menu").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(root.Q("screen").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(root.Q<Label>("floor").text, Is.EqualTo("01"));
                Assert.That(root.Q<Label>("state").text, Is.EqualTo("READY WHEN YOU ARE"));
                Assert.That(root.Q<Slider>("music").value, Is.EqualTo(0.24f).Within(0.001f));
                Assert.That(root.Q<Slider>("effects").value, Is.EqualTo(0.65f).Within(0.001f));
                game.ReturnToMenu();
                Assert.That(root.Q("main-menu").style.display.value, Is.EqualTo(DisplayStyle.None), "An unfinished round cannot be abandoned through the result menu action.");

                yield return SceneManager.LoadSceneAsync("EggDrop");
                yield return null;
                audio = Object.FindFirstObjectByType<EggDropAudio>();
                root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
                AssertMenuVisible(root);
                Assert.That(audio.MusicVolume, Is.EqualTo(0.24f).Within(0.001f));
                Assert.That(audio.EffectsVolume, Is.EqualTo(0.65f).Within(0.001f));
                Assert.That(audio.IsMuted, Is.EqualTo(originallyMuted));
            }
            finally
            {
                if (audio != null)
                {
                    audio.SetMusicVolume(originalMusic);
                    audio.SetEffectsVolume(originalEffects);
                    if (audio.IsMuted != originallyMuted) audio.ToggleMute();
                }
            }
        }

        private static void AssertMenuVisible(VisualElement root)
        {
            Assert.That(root.Q("main-menu").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q("screen").style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        private static void Submit(Button button)
        {
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private static IEnumerator CaptureMenu(string screen, VisualElement root)
        {
            PanelSettings settings = Object.FindFirstObjectByType<UIDocument>().panelSettings;
            RenderTexture previousTarget = settings.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            foreach (Vector2Int size in new[]
            {
                new Vector2Int(1200, 800), new Vector2Int(800, 1200),
                new Vector2Int(540, 960), new Vector2Int(1080, 1920), new Vector2Int(320, 640)
            })
            {
                RenderTexture target = new RenderTexture(size.x, size.y, 24);
                try
                {
                    target.Create();
                    settings.targetTexture = target;
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    string action = screen == "Home" ? "play" : "settings-back";
                    Rect bounds = root.Q<Button>(action).worldBound;
                    Assert.That(bounds.width, Is.GreaterThan(100));
                    Assert.That(root.worldBound.Contains(bounds.center), Is.True, "The menu action is outside the viewport.");
                    SavePreview(target, screen + "_" + size.x + "x" + size.y);
                    AssertStoryLayout(root, screen + " at " + size);
                }
                finally
                {
                    settings.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    target.Release();
                    Object.Destroy(target);
                }
            }
            for (int frame = 0; frame < 3; frame++) yield return null;
        }

        private static void SavePreview(RenderTexture target, string name)
        {
            string folder = System.Environment.GetEnvironmentVariable("EGGDROP_MENU_PREVIEW");
            if (string.IsNullOrEmpty(folder)) return;
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "Sprint03_" + name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(image);
            }
        }

        private static void AssertStoryLayout(VisualElement root, string viewport)
        {
            VisualElement story = root.Q(className: "menu-story");
            Label eyebrow = story.Q<Label>(className: "eyebrow");
            Label title = story.Q<Label>(className: "menu-title");
            Label description = story.Q<Label>(className: "menu-description");
            float requiredHeight = title.MeasureTextSize(title.text, title.contentRect.width,
                VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;
            Assert.That(title.contentRect.height, Is.GreaterThanOrEqualTo(requiredHeight - 1f),
                viewport + ": The title does not reserve enough height for its wrapped text.");
            Assert.That(title.worldBound.yMin - eyebrow.worldBound.yMax, Is.GreaterThanOrEqualTo(10f),
                viewport + ": The title overlaps the eyebrow.");
            Assert.That(description.worldBound.yMin - title.worldBound.yMax, Is.GreaterThanOrEqualTo(12f),
                viewport + ": The subtitle overlaps the title.");
            if (root.ClassListContains("portrait"))
            {
                Assert.That(root.Q(className: "menu-card").worldBound.yMin - description.worldBound.yMax,
                    Is.GreaterThanOrEqualTo(20f), viewport + ": The menu card overlaps the story.");
            }
        }
    }
}
