using UnityEngine;
using UnityEngine.UIElements;

namespace EggDrop
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class EggDropUI : MonoBehaviour
    {
        [SerializeField] private EggDropGame game;
        [SerializeField] private EggDropAudio gameAudio;

        private VisualElement root;
        private Label floorLabel;
        private Label multiplierLabel;
        private Label pointsLabel;
        private Label bankLabel;
        private Label chanceLabel;
        private Label nextLabel;
        private Label statusLabel;
        private Label stateLabel;
        private Label resultTitle;
        private Label resultDescription;
        private Button dropButton;
        private Button collectButton;
        private Button restartButton;
        private Button muteButton;
        private VisualElement riskFill;
        private VisualElement resultPanel;
        private VisualElement history;
        private Slider musicSlider;
        private Slider effectsSlider;
        private Label[] floorLabels;
        private VisualElement menu;
        private VisualElement menuHome;
        private VisualElement menuSettings;
        private VisualElement screen;
        private Button playButton;
        private Button settingsButton;
        private Button settingsBackButton;
        private Button menuButton;
        private Button quitButton;
        private Button menuMuteButton;
        private Slider menuMusicSlider;
        private Slider menuEffectsSlider;

        private void Awake()
        {
            TryGetComponent(out UIDocument document);
            root = document.rootVisualElement;
            floorLabel = root.Q<Label>("floor");
            multiplierLabel = root.Q<Label>("multiplier");
            pointsLabel = root.Q<Label>("points");
            bankLabel = root.Q<Label>("bank");
            chanceLabel = root.Q<Label>("chance");
            nextLabel = root.Q<Label>("next");
            statusLabel = root.Q<Label>("status");
            stateLabel = root.Q<Label>("state");
            resultTitle = root.Q<Label>("result-title");
            resultDescription = root.Q<Label>("result-description");
            dropButton = root.Q<Button>("drop");
            collectButton = root.Q<Button>("collect");
            restartButton = root.Q<Button>("restart");
            muteButton = root.Q<Button>("mute");
            riskFill = root.Q("risk-fill");
            resultPanel = root.Q("result");
            history = root.Q("history");
            musicSlider = root.Q<Slider>("music");
            effectsSlider = root.Q<Slider>("effects");
            screen = root.Q("screen");
            menu = root.Q("main-menu");
            menuHome = root.Q("menu-home");
            menuSettings = root.Q("menu-settings");
            playButton = root.Q<Button>("play");
            settingsButton = root.Q<Button>("settings");
            settingsBackButton = root.Q<Button>("settings-back");
            menuButton = root.Q<Button>("return-menu");
            quitButton = root.Q<Button>("quit");
            menuMuteButton = root.Q<Button>("menu-mute");
            menuMusicSlider = root.Q<Slider>("menu-music");
            menuEffectsSlider = root.Q<Slider>("menu-effects");
            quitButton.SetEnabled(!Application.isEditor);
            if (Application.isEditor) quitButton.text = "QUIT (BUILD ONLY)";
            floorLabels = new Label[EggDropRound.MaximumFloor];
            for (int index = 0; index < floorLabels.Length; index++)
            {
                Label floor = new Label((index + 1).ToString("00"));
                floor.AddToClassList("floor-step");
                history.Add(floor);
                floorLabels[index] = floor;
            }
            root.RegisterCallback<GeometryChangedEvent>(HandleResize);
        }

        private void OnEnable()
        {
            dropButton.clicked += game.Drop;
            collectButton.clicked += game.Collect;
            restartButton.clicked += game.Restart;
            muteButton.clicked += game.ToggleMute;
            musicSlider.RegisterValueChangedCallback(HandleMusicChanged);
            effectsSlider.RegisterValueChangedCallback(HandleEffectsChanged);
            playButton.clicked += game.StartRound;
            menuButton.clicked += game.ReturnToMenu;
            quitButton.clicked += game.Quit;
            settingsButton.clicked += OpenSettings;
            settingsBackButton.clicked += CloseSettings;
            menuMuteButton.clicked += game.ToggleMute;
            menuMusicSlider.RegisterValueChangedCallback(HandleMusicChanged);
            menuEffectsSlider.RegisterValueChangedCallback(HandleEffectsChanged);
        }

        private void OnDisable()
        {
            dropButton.clicked -= game.Drop;
            collectButton.clicked -= game.Collect;
            restartButton.clicked -= game.Restart;
            muteButton.clicked -= game.ToggleMute;
            musicSlider.UnregisterValueChangedCallback(HandleMusicChanged);
            effectsSlider.UnregisterValueChangedCallback(HandleEffectsChanged);
            playButton.clicked -= game.StartRound;
            menuButton.clicked -= game.ReturnToMenu;
            quitButton.clicked -= game.Quit;
            settingsButton.clicked -= OpenSettings;
            settingsBackButton.clicked -= CloseSettings;
            menuMuteButton.clicked -= game.ToggleMute;
            menuMusicSlider.UnregisterValueChangedCallback(HandleMusicChanged);
            menuEffectsSlider.UnregisterValueChangedCallback(HandleEffectsChanged);
        }

        private void Start() => RefreshAudio();

        public void RefreshAudio()
        {
            muteButton.text = gameAudio.IsMuted ? "SOUND OFF  [M]" : "SOUND ON  [M]";
            musicSlider.SetValueWithoutNotify(gameAudio.MusicVolume);
            effectsSlider.SetValueWithoutNotify(gameAudio.EffectsVolume);
            menuMuteButton.text = gameAudio.IsMuted ? "SOUND OFF  [M]" : "SOUND ON  [M]";
            menuMusicSlider.SetValueWithoutNotify(gameAudio.MusicVolume);
            menuEffectsSlider.SetValueWithoutNotify(gameAudio.EffectsVolume);
        }

        public void ShowMenu(bool visible)
        {
            menu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            screen.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            SetSettingsVisible(false);
            RefreshAudio();
            root.focusController?.focusedElement?.Blur();
        }

        private void OpenSettings()
        {
            SetSettingsVisible(true);
            gameAudio.Click();
        }

        private void CloseSettings()
        {
            SetSettingsVisible(false);
            gameAudio.Click();
        }

        private void SetSettingsVisible(bool visible)
        {
            menuHome.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            menuSettings.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Render(EggDropRound round, bool isAnimating, string status)
        {
            bool cracked = round.State == RoundState.Cracked;
            bool collected = round.State == RoundState.Collected;
            bool ended = cracked || collected;
            root.EnableInClassList("round-ended", ended);
            floorLabel.text = round.Floor.ToString("00");
            multiplierLabel.text = round.Multiplier.ToString("0.00") + "x";
            pointsLabel.text = cracked ? "0" : round.RoundPoints.ToString("N0");
            bankLabel.text = round.BankedPoints.ToString("N0");
            chanceLabel.text = round.CrackChance.ToString("P1");
            nextLabel.text = round.SuccessfulDrops < EggDropRound.MaximumFloor
                ? "Next landing  " + round.NextMultiplier.ToString("0.00") + "x" : "12 / 12 FLOORS CLEARED";
            statusLabel.text = status;
            stateLabel.text = cracked ? "SHELL SHOCKED" : collected ? "POINTS COLLECTED"
                : round.State == RoundState.Falling ? "IN FREE FALL" : isAnimating ? "NICE LANDING" : "READY WHEN YOU ARE";
            riskFill.style.width = Length.Percent(round.CrackChance * 100f);
            riskFill.EnableInClassList("high-risk", round.CrackChance >= 0.4f);
            dropButton.SetEnabled(round.CanDrop && !isAnimating);
            collectButton.SetEnabled(round.CanCollect && !isAnimating);
            dropButton.text = round.State == RoundState.Falling ? "FALLING..." : isAnimating ? "GOING UP..." : "DROP EGG   [SPACE]";
            collectButton.text = "COLLECT " + round.RoundPoints.ToString("N0") + "   [C]";
            resultPanel.style.display = ended ? DisplayStyle.Flex : DisplayStyle.None;
            resultPanel.EnableInClassList("cracked", cracked);
            resultTitle.text = cracked ? "Oh, shell." : "Nicely played.";
            resultDescription.text = cracked ? "The egg cracked on floor " + round.Floor + ". Round points lost. Your bank is safe."
                : "+" + round.RoundPoints.ToString("N0") + " points safely collected.";
            for (int index = 0; index < floorLabels.Length; index++)
            {
                floorLabels[index].EnableInClassList("cleared", index < round.SuccessfulDrops);
                floorLabels[index].EnableInClassList("current", !ended && index == round.Floor - 1 && round.CanDrop);
                floorLabels[index].EnableInClassList("failed", cracked && index == round.Floor - 1);
            }
        }

        private void HandleResize(GeometryChangedEvent change)
        {
            root.EnableInClassList("compact", change.newRect.width < 1000f);
            root.EnableInClassList("portrait", change.newRect.width < change.newRect.height);
        }
        private void HandleMusicChanged(ChangeEvent<float> change)
        {
            gameAudio.SetMusicVolume(change.newValue);
            RefreshAudio();
        }

        private void HandleEffectsChanged(ChangeEvent<float> change)
        {
            gameAudio.SetEffectsVolume(change.newValue);
            RefreshAudio();
        }
    }
}
