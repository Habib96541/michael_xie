using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HydraulicPress
{
    public enum NavigationState { MainMenu, Playing, Paused }
    public sealed class GameManager : MonoBehaviour
    {
        public GameplayConfig config;
        public HydraulicPressController press;
        public PhysicsEffectManager effects;
        public AudioManager audioManager;
        public CameraController cameraRig;
        public UIManager hud;
        public Transform objectAnchor;
        private CrushableObject[] pool;
        private CrushableObject current;
        private System.Random seeds;
        private bool presentationBusy;
        private bool focused = true;
        private bool settingsOpen;
        private Coroutine autoRestart;
        public NavigationState Navigation { get; private set; } = NavigationState.MainMenu;
        public RoundManager Round { get; private set; }
        public SaveData Save { get; private set; }
        public bool CanCrush => Navigation == NavigationState.Playing && focused && !settingsOpen && !presentationBusy && Round != null && Round.CanCrush;
        public bool CanCollect => Navigation == NavigationState.Playing && focused && !settingsOpen && !presentationBusy && Round != null && Round.CanCollect;
        public bool Busy => presentationBusy;
        public bool SettingsOpen => settingsOpen;
        public string CurrentName => current == null ? "" : current.Definition.displayName;
        public event Action Updated;

        private void Start()
        {
            if (config == null || config.objects == null || config.objects.Length != 20) { Debug.LogError("Hydraulic Press requires exactly twenty valid object definitions."); enabled = false; return; }
            Save = SaveManager.Load(); seeds = new System.Random(unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks));
            audioManager.Initialize(Save); effects.Initialize(config.fragmentBudget); cameraRig.Initialize(Save);
            press.Initialize(audioManager, effects, cameraRig, config.animationSpeed);
            pool = new CrushableObject[20];
            for (int i = 0; i < pool.Length; i++)
            {
                var def = config.objects[i];
                if (def == null || def.prefab == null) { Debug.LogError("Invalid object definition at " + i); enabled = false; return; }
                GameObject go = Instantiate(def.prefab, objectAnchor); pool[i] = go.AddComponent<CrushableObject>(); pool[i].Initialize(def); go.SetActive(false);
            }
            ApplyGraphics(); hud.Initialize(this);
            current = pool[0]; current.ResetObject(); current.gameObject.SetActive(true);
            ApplyPause(); Updated?.Invoke();
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (settingsOpen) SetSettings(false);
                else if (Navigation == NavigationState.Paused) PlayOrResume();
                else if (Navigation == NavigationState.Playing) PauseGame();
                return;
            }
            if (settingsOpen) return;
            if (Navigation != NavigationState.Playing)
            {
                if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) PlayOrResume();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !settingsOpen) Crush();
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame && !settingsOpen) Collect();
        }
        public void Crush()
        {
            if (!CanCrush) return;
            audioManager.Unlock();
            if (!Round.TryBeginCrush(out bool danger)) return;
            presentationBusy = true; Updated?.Invoke(); StartCoroutine(CrushSequence(danger));
        }
        private IEnumerator CrushSequence(bool danger)
        {
            yield return press.Crush(current, danger, null);
            Round.ResolveCrush();
            if (Round.State == RoundState.Failed)
            {
                SaveManager.Write(Save); yield return new WaitForSeconds(.25f);
                presentationBusy = false; hud.ShowResult(false, Round.Multiplier, 0); Updated?.Invoke(); yield break;
            }
            Save.totalCrushed = SaveManager.AddSaturated(Save.totalCrushed, 1);
            SaveManager.Write(Save); hud.PopMultiplier(); audioManager.Play(audioManager.multiplier, .35f, Mathf.Min(1.4f, 1 + Round.Crushed * .025f));
            if (Round.State == RoundState.Perfect)
            {
                Bank(Round.Reward); presentationBusy = false; hud.ShowResult(true, Round.Multiplier, Round.Reward, true); ScheduleRound(); Updated?.Invoke(); yield break;
            }
            current.gameObject.SetActive(false); SpawnCurrent();
            yield return SpawnAnimation(); presentationBusy = false; Updated?.Invoke();
        }
        public void Collect()
        {
            if (!CanCollect) return;
            audioManager.Unlock();
            if (!Round.TryCollect(out long points)) return;
            Bank(points); audioManager.Play(audioManager.collect, .7f);
            hud.ShowResult(true, Round.Multiplier, points); ScheduleRound(); Updated?.Invoke();
        }
        private void Bank(long points)
        {
            Save.bank = SaveManager.AddSaturated(Save.bank, points); Save.bestRound = Math.Max(Save.bestRound, points);
            Save.bestMultiplier = Math.Max(Save.bestMultiplier, Round.Multiplier); SaveManager.Write(Save);
        }
        private void ScheduleRound() { autoRestart = StartCoroutine(NextRound()); }
        private IEnumerator NextRound() { yield return new WaitForSeconds(2.5f); autoRestart = null; NewRound(); }
        public void NewRound()
        {
            if (Navigation != NavigationState.Playing || presentationBusy || settingsOpen || config == null || pool == null) return;
            if (autoRestart != null) { StopCoroutine(autoRestart); autoRestart = null; }
            if (Round != null) Round.Changed -= OnRoundChanged;
            effects.Clear(); press.Restore(); foreach (var item in pool) item.gameObject.SetActive(false);
            Round = new RoundManager(seeds.Next(), config.basePoints, config.rewardMargin); Round.Changed += OnRoundChanged;
            hud.HideResult(); SpawnCurrent(); presentationBusy = true; StartCoroutine(InitialSpawn()); Updated?.Invoke();
        }
        private IEnumerator InitialSpawn() { yield return SpawnAnimation(); presentationBusy = false; Updated?.Invoke(); }
        private void SpawnCurrent()
        {
            current = pool[Round.CurrentObject]; current.ResetObject(); current.gameObject.SetActive(true); current.transform.localPosition = Vector3.zero;
        }
        private IEnumerator SpawnAnimation()
        {
            for (float t = 0; t < .25f; t += Time.deltaTime * config.animationSpeed)
            {
                float p = Mathf.Clamp01(t / .25f); float ease = 1 - Mathf.Pow(1 - p, 3);
                current.transform.localPosition = new Vector3(Mathf.Lerp(1.6f, 0, ease), Mathf.Sin(p * Mathf.PI) * .12f, 0);
                current.transform.localScale = Vector3.one * Mathf.Lerp(.7f, 1, ease); yield return null;
            }
            current.transform.localPosition = Vector3.zero; current.transform.localScale = Vector3.one;
        }
        private void OnRoundChanged() => Updated?.Invoke();
        public void PlayOrResume()
        {
            if (Save == null || settingsOpen || Navigation == NavigationState.Playing) return;
            bool fromMain = Navigation == NavigationState.MainMenu;
            Navigation = NavigationState.Playing;
            ApplyPause();
            if (Round == null || (fromMain && Round.State != RoundState.Ready && Round.State != RoundState.Crushing)) NewRound();
            Updated?.Invoke();
        }
        public void PauseGame()
        {
            if (Navigation != NavigationState.Playing || Save == null) return;
            Navigation = NavigationState.Paused; ApplyPause(); Updated?.Invoke();
        }
        public void ShowMainMenu()
        {
            if (Save == null) return;
            settingsOpen = false; hud.SetSettings(false);
            Navigation = NavigationState.MainMenu; SaveManager.Write(Save); ApplyPause(); Updated?.Invoke();
        }
        public void SetSettings(bool open)
        {
            if (Save == null || (open && presentationBusy && Navigation == NavigationState.Playing)) return;
            settingsOpen = open; hud.SetSettings(open); ApplyPause(); Updated?.Invoke();
            if (!open) SaveManager.Write(Save);
        }
        public void SettingsChanged() { audioManager.ApplyVolumes(); ApplyGraphics(); SaveManager.Write(Save); }
        private void ApplyGraphics()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1;
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = Save.lowGraphics ? 2 : 1;
#else
            Application.targetFrameRate = Save.lowGraphics ? 30 : 60;
#endif
            QualitySettings.shadows = Save.lowGraphics ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.shadowDistance = Save.lowGraphics ? 0 : 22;
            // Keep native resolution in both modes; reduce effects and cadence to save power.
            if (config.graphics != null) { config.graphics.renderScale = 1; config.graphics.msaaSampleCount = Save.lowGraphics ? 2 : 4; config.graphics.shadowDistance = Save.lowGraphics ? 0 : 22; }
            if (config.rendererData != null) foreach(var feature in config.rendererData.rendererFeatures) if(feature != null) feature.SetActive(!Save.lowGraphics);
            effects.LowPower = Save.lowGraphics;
        }
        private void OnApplicationFocus(bool focus) { focused = focus; ApplyPause(); Updated?.Invoke(); }
        private void OnApplicationPause(bool pause) { focused = !pause; ApplyPause(); }
        private void ApplyPause() { bool paused = !focused || settingsOpen || Navigation != NavigationState.Playing; Time.timeScale = paused ? 0 : 1; AudioListener.pause = paused; }
        private void OnApplicationQuit() { if (Save != null) SaveManager.Write(Save); }
        private void OnDestroy() { if (Round != null) Round.Changed -= OnRoundChanged; Time.timeScale = 1; AudioListener.pause = false; UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1; }
    }
}
