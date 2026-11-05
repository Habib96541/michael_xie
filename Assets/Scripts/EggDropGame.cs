using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EggDrop
{
    public sealed class EggDropGame : MonoBehaviour
    {
        [SerializeField] private EggBody egg;
        [SerializeField] private EggDropUI gameInterface;
        [SerializeField] private EggDropAudio gameAudio;
        [SerializeField] private EggDropStage stage;

        private readonly EggDropRound round = new EggDropRound();
        private InputActionMap shortcuts;
        private bool dropPressed;
        private bool collectPressed;
        private bool restartPressed;
        private bool mutePressed;
        private bool isAnimating;
        private bool isInMenu = true;
        private string status = "A little courage. A lot of gravity.";

        private void Awake()
        {
            shortcuts = new InputActionMap("Egg Drop");
            shortcuts.AddAction("Drop", InputActionType.Button, "<Keyboard>/space").performed += context => dropPressed = true;
            shortcuts.AddAction("Collect", InputActionType.Button, "<Keyboard>/c").performed += context => collectPressed = true;
            shortcuts.AddAction("Restart", InputActionType.Button, "<Keyboard>/r").performed += context => restartPressed = true;
            shortcuts.AddAction("Mute", InputActionType.Button, "<Keyboard>/m").performed += context => mutePressed = true;
        }

        private void OnEnable()
        {
            egg.Landed += HandleLanding;
            shortcuts.Enable();
        }

        private void OnDisable()
        {
            egg.Landed -= HandleLanding;
            shortcuts.Disable();
            dropPressed = collectPressed = restartPressed = mutePressed = false;
        }

        private void OnDestroy() => shortcuts.Dispose();

        private void Start()
        {
            egg.Prepare(EggDropStage.HeightFor(1));
            RefreshInterface();
            gameInterface.ShowMenu(true);
        }

        private void Update()
        {
            if (dropPressed) Drop();
            if (collectPressed) Collect();
            if (restartPressed) Restart();
            if (mutePressed) ToggleMute();
            dropPressed = collectPressed = restartPressed = mutePressed = false;
        }

        public void Drop()
        {
            if (isInMenu || isAnimating || !round.TryDrop()) return;
            status = "Hold your breath...";
            egg.RequestDrop();
            gameAudio.Drop(round.CrackChance);
            RefreshInterface();
        }

        public void Collect()
        {
            if (isInMenu || isAnimating || !round.TryCollect()) return;
            status = "Good call. Those points are yours.";
            gameAudio.Collect();
            RefreshInterface();
        }

        public void Restart()
        {
            if (isInMenu) return;
            if (round.State != RoundState.Collected && round.State != RoundState.Cracked) return;
            ResetRound();
            gameAudio.Click();
        }

        public void StartRound()
        {
            if (!isInMenu) return;
            isInMenu = false;
            dropPressed = collectPressed = restartPressed = false;
            ResetRound();
            gameInterface.ShowMenu(false);
            gameAudio.Click();
        }

        public void ReturnToMenu()
        {
            if (isInMenu || (round.State != RoundState.Collected && round.State != RoundState.Cracked)) return;
            isInMenu = true;
            dropPressed = collectPressed = restartPressed = false;
            ResetRound();
            gameInterface.ShowMenu(true);
            gameAudio.Click();
        }

        public void Quit()
        {
            if (!isInMenu || Application.isEditor) return;
            Application.Quit();
        }

        private void ResetRound()
        {
            StopAllCoroutines();
            isAnimating = false;
            round.Restart();
            status = "Fresh egg. Fresh start.";
            egg.Prepare(EggDropStage.HeightFor(1));
            stage.ShowFloor(1, 0);
            RefreshInterface();
        }

        public void ToggleMute()
        {
            gameAudio.ToggleMute();
            gameInterface.RefreshAudio();
        }

        private void HandleLanding()
        {
            bool survived = round.ResolveLanding(Random.value * 0.9999999f);
            gameAudio.Land(survived);
            stage.Impact(!survived);
            if (!survived)
            {
                status = "That was one floor too far.";
                egg.Crack();
                RefreshInterface();
                return;
            }
            StartCoroutine(PrepareNextDrop());
        }

        private IEnumerator PrepareNextDrop()
        {
            isAnimating = true;
            status = "Still in one piece!";
            RefreshInterface();
            yield return egg.Squash();
            gameAudio.Success();
            yield return new WaitForSeconds(0.3f);
            stage.ShowFloor(round.Floor, round.SuccessfulDrops);
            if (round.CanDrop) yield return egg.LiftTo(EggDropStage.HeightFor(round.Floor));
            isAnimating = false;
            status = round.CanDrop ? "Take the points, or trust the shell?" : "All 12 floors. Collect your reward!";
            RefreshInterface();
        }

        private void RefreshInterface() => gameInterface.Render(round, isAnimating, status);
    }
}
