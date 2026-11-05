using System;
using CyberWhack.Audio;
using CyberWhack.Art;
using CyberWhack.Data;
using CyberWhack.Deterministic;
using CyberWhack.UI;
using CyberWhack.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CyberWhack.Gameplay
{
    public enum GameState
    {
        MainMenu,
        ReadyCountdown,
        Playing,
        Paused,
        GameOver
    }

    public class CyberGameDirector : MonoBehaviour
    {
        public static CyberGameDirector Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig config;
        [SerializeField] private HatchNode[] hatchNodes;
        [SerializeField] private CyberHUD hud;

        [Header("Deterministic Match Seed")]
        [SerializeField] private bool useRandomSeedAtStart = true;
        [SerializeField] private int initialSeed = 429182;

        public GameState CurrentState => _state;
        public int ActiveMatchSeed => _seed;
        public int CurrentScore => _score;
        public int CurrentStreak => _streak;
        public int CurrentMultiplier => _multiplier;

        private GameState _state = GameState.MainMenu;
        private GameState _resumeState = GameState.Playing;
        private SeededRandom _prng;
        private int _seed;

        public struct ScheduledTargetSpawn
        {
            public float Timestamp;
            public int NodeIndex;
            public TargetType TargetType;
            public float Duration;
            public int RequiredHits;
        }

        private const int MaxScheduledSpawns = 200;
        private readonly ScheduledTargetSpawn[] _matchSchedule = new ScheduledTargetSpawn[MaxScheduledSpawns];
        private int _totalScheduledSpawns;
        private int _scheduleIndex;

        // Session metrics
        private float _sessionTimer;
        private float _countdownTimer;
        private int _lastCountdownInt = int.MinValue;
        private int _score;
        private int _streak;
        private int _maxStreak;
        private int _multiplier = 1;
        private int _totalHits;
        private int _emptyTaps;
        private int _empDetonated;
        private int _highScore;

        private const string PrefsHighScoreKey = "CyberWhack_HighScore";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
            }

            if (CyberAudioManager.Instance == null)
            {
                GameObject audioObj = new GameObject("CyberAudioManager", typeof(CyberAudioManager));
            }

            if (hud == null)
            {
                hud = FindFirstObjectByType<CyberHUD>();
            }

            if (hatchNodes == null || hatchNodes.Length == 0)
            {
                hatchNodes = FindObjectsByType<HatchNode>(FindObjectsSortMode.None);
                Array.Sort(hatchNodes, (a, b) => a.NodeIndex.CompareTo(b.NodeIndex));
            }

            if (useRandomSeedAtStart)
            {
                initialSeed = UnityEngine.Random.Range(100000, 999999);
            }
            _seed = initialSeed;
            _prng = new SeededRandom(_seed);
            _highScore = PlayerPrefs.GetInt(PrefsHighScoreKey, 0);

            // Self-healing runtime UI hierarchy construction if not pre-baked
            if (hud == null || hatchNodes == null || hatchNodes.Length < 9)
            {
                BuildRuntimeHierarchy();
            }

            // Presentation is intentionally independent from the deterministic
            // game loop. Apply the Moonlit Burrow theme to authored and
            // self-healed hierarchies alike.
            NeonBurrowTheme.Apply(hud, hatchNodes);
            SetHatchGridVisible(false);

            HookEvents();
        }

        private void Start()
        {
            ReturnToMainMenu();
        }

        private void HookEvents()
        {
            if (hatchNodes != null)
            {
                for (int i = 0; i < hatchNodes.Length; i++)
                {
                    if (hatchNodes[i] != null)
                    {
                        hatchNodes[i].SetNodeIndex(i);
                        hatchNodes[i].OnTargetStruck += HandleTargetStruck;
                        hatchNodes[i].OnEmptyHatchTapped += HandleEmptyHatchTapped;
                    }
                }
            }

            if (hud != null)
            {
                hud.OnStartGameClicked += HandleStartGameClicked;
                hud.OnRestartSameSeedClicked += HandleRestartSameSeedClicked;
                hud.OnRestartNewSeedClicked += HandleRestartNewSeedClicked;
                hud.OnReturnToMenuClicked += HandleReturnToMenuClicked;
                hud.OnSeedRandomizeClicked += HandleSeedRandomizeClicked;
                hud.OnSeedChanged += HandleSeedChanged;
                hud.OnPauseClicked += HandlePauseClicked;
                hud.OnResumeClicked += HandleResumeClicked;
            }
        }

        private void UnhookEvents()
        {
            if (hatchNodes != null)
            {
                for (int i = 0; i < hatchNodes.Length; i++)
                {
                    if (hatchNodes[i] != null)
                    {
                        hatchNodes[i].OnTargetStruck -= HandleTargetStruck;
                        hatchNodes[i].OnEmptyHatchTapped -= HandleEmptyHatchTapped;
                    }
                }
            }

            if (hud != null)
            {
                hud.OnStartGameClicked -= HandleStartGameClicked;
                hud.OnRestartSameSeedClicked -= HandleRestartSameSeedClicked;
                hud.OnRestartNewSeedClicked -= HandleRestartNewSeedClicked;
                hud.OnReturnToMenuClicked -= HandleReturnToMenuClicked;
                hud.OnSeedRandomizeClicked -= HandleSeedRandomizeClicked;
                hud.OnSeedChanged -= HandleSeedChanged;
                hud.OnPauseClicked -= HandlePauseClicked;
                hud.OnResumeClicked -= HandleResumeClicked;
            }
        }

        private void OnDestroy()
        {
            UnhookEvents();
            ArcadeFeel.CancelHitStop();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ReturnToMainMenu()
        {
            _state = GameState.MainMenu;
            ArcadeFeel.CancelHitStop();
            RetractAllHatches();
            SetHatchGridVisible(false);

            if (hud != null)
            {
                hud.ShowMainMenu(_highScore, _seed);
            }
        }

        private void SetHatchGridVisible(bool visible)
        {
            if (hatchNodes != null && hatchNodes.Length > 0 && hatchNodes[0] != null)
            {
                Transform parent = hatchNodes[0].transform.parent;
                if (parent != null && (parent.name.Contains("Grid") || parent.name.Contains("Container")))
                {
                    parent.gameObject.SetActive(visible);
                    return;
                }
                for (int i = 0; i < hatchNodes.Length; i++)
                {
                    if (hatchNodes[i] != null)
                    {
                        hatchNodes[i].gameObject.SetActive(visible);
                    }
                }
            }
        }

        private void RetractAllHatches()
        {
            if (hatchNodes != null)
            {
                for (int i = 0; i < hatchNodes.Length; i++)
                {
                    if (hatchNodes[i] != null)
                    {
                        hatchNodes[i].ResetToSubterranean();
                    }
                }
            }
        }

        public void StartMatch(int matchSeed)
        {
            SetHatchGridVisible(true);
            _seed = matchSeed;
            _prng.Init(_seed);

            _score = 0;
            _streak = 0;
            _maxStreak = 0;
            _multiplier = 1;
            _totalHits = 0;
            _emptyTaps = 0;
            _empDetonated = 0;

            _sessionTimer = (config != null) ? config.RoundDuration : 60.0f;
            _countdownTimer = (config != null) ? config.CountdownDuration : 3.0f;
            _lastCountdownInt = int.MinValue;

            GenerateMatchSchedule();

            _state = GameState.ReadyCountdown;

            if (hud != null)
            {
                hud.TransitionToGame();
                hud.SetMatchSeed(_seed);
                hud.UpdateScore(_score);
                hud.UpdateMultiplier(_multiplier, _streak, GetNextTierStreakTarget());
                hud.UpdateTimer(_sessionTimer, (config != null) ? config.RoundDuration : 60.0f);
            }
        }

        private void GenerateMatchSchedule()
        {
            if (config == null) return;

            _totalScheduledSpawns = 0;
            _scheduleIndex = 0;

            float[] nodeAvailableAt = new float[9];
            for (int i = 0; i < 9; i++) nodeAvailableAt[i] = 0f;

            float roundDur = config.RoundDuration;
            float simTime = 0.5f;

            while (simTime < (roundDur - 1.0f) && _totalScheduledSpawns < _matchSchedule.Length)
            {
                float progress = simTime / roundDur;
                float baseInterval = Mathf.Lerp(config.InitialSpawnInterval, config.MinSpawnInterval, progress);
                float jitter = _prng.NextFloat(-0.04f, 0.04f);
                float nextInterval = Mathf.Max(0.28f, baseInterval + jitter);

                // Find available nodes at simTime
                int startIdx = _prng.NextInt(0, 9);
                int chosenNode = -1;
                for (int i = 0; i < 9; i++)
                {
                    int checkIdx = (startIdx + i) % 9;
                    if (nodeAvailableAt[checkIdx] <= simTime)
                    {
                        chosenNode = checkIdx;
                        break;
                    }
                }

                if (chosenNode != -1)
                {
                    float roll = _prng.NextFloat();
                    TargetType targetType;
                    if (roll < 0.52f)
                    {
                        targetType = TargetType.StandardCyberMole;
                    }
                    else if (roll < 0.72f)
                    {
                        targetType = TargetType.OverclockedGoldMole;
                    }
                    else if (roll < 0.88f)
                    {
                        targetType = TargetType.ArmoredTankMole;
                    }
                    else
                    {
                        targetType = TargetType.EmpDecoyMine;
                    }

                    float duration = config.GetActiveDuration(targetType);
                    int requiredHits = config.GetHitsRequired(targetType);

                    _matchSchedule[_totalScheduledSpawns++] = new ScheduledTargetSpawn
                    {
                        Timestamp = simTime,
                        NodeIndex = chosenNode,
                        TargetType = targetType,
                        Duration = duration,
                        RequiredHits = requiredHits
                    };

                    nodeAvailableAt[chosenNode] = simTime + duration + 0.16f; // Duration + slide transitions
                }

                simTime += nextInterval;
            }
        }

        private void Update()
        {
            HandlePauseHotkey();
            if (_state == GameState.Paused) return;

            float dt = Time.deltaTime;

            if (_state == GameState.ReadyCountdown)
            {
                _countdownTimer -= dt;
                int countdownInt = Mathf.CeilToInt(_countdownTimer);
                if (countdownInt != _lastCountdownInt)
                {
                    _lastCountdownInt = countdownInt;
                    if (countdownInt > 0)
                    {
                        CyberAudioManager.Instance?.PlayCountdownTick(countdownInt);
                    }
                }

                if (hud != null)
                {
                    hud.ShowReadyCountdown(countdownInt);
                }

                if (_countdownTimer <= 0.0f)
                {
                    _state = GameState.Playing;
                    CyberAudioManager.Instance?.PlayRoundStart();
                    if (hud != null)
                    {
                        hud.HideReadyCountdown();
                    }
                }
                return;
            }

            if (_state == GameState.Playing)
            {
                _sessionTimer -= dt;
                float totalDur = (config != null) ? config.RoundDuration : 60.0f;
                if (hud != null)
                {
                    hud.UpdateTimer(_sessionTimer, totalDur);
                }

                if (_sessionTimer <= 0.0f)
                {
                    EndMatch();
                    return;
                }

                // Deterministic timeline dispatch (zero allocation, locked 60 FPS)
                float sessionElapsed = totalDur - _sessionTimer;
                while (_scheduleIndex < _totalScheduledSpawns && _matchSchedule[_scheduleIndex].Timestamp <= sessionElapsed)
                {
                    var spawn = _matchSchedule[_scheduleIndex];
                    if (hatchNodes != null && spawn.NodeIndex >= 0 && spawn.NodeIndex < hatchNodes.Length && hatchNodes[spawn.NodeIndex] != null)
                    {
                        hatchNodes[spawn.NodeIndex].SpawnTarget(spawn.TargetType, spawn.Duration, spawn.RequiredHits);
                    }
                    _scheduleIndex++;
                }
            }
        }

        private void HandleTargetStruck(HatchNode node, TargetType type, bool isDestroyed)
        {
            if (_state != GameState.Playing) return;

            if (type == TargetType.EmpDecoyMine)
            {
                int penalty = (config != null) ? config.EmpScorePenalty : 200;
                _score = Mathf.Max(0, _score - penalty);
                _streak = 0;
                _multiplier = 1;
                _empDetonated++;

                if (hud != null)
                {
                    hud.UpdateScore(_score);
                    hud.UpdateMultiplier(_multiplier, _streak, GetNextTierStreakTarget());
                }
                return;
            }

            if (!isDestroyed)
            {
                return;
            }

            _totalHits++;
            _streak++;
            if (_streak > _maxStreak) _maxStreak = _streak;

            int prevMultiplier = _multiplier;
            _multiplier = (config != null) ? config.CalculateMultiplier(_streak) : 1;

            if (_multiplier > prevMultiplier)
            {
                CyberAudioManager.Instance.PlayComboTierUp();
                ArcadeFeel.Impact(0.55f, 0.28f);
                ArcadeFeel.HitStop(0.05f, 0.05f);
                if (EffectPoolManager.Instance != null)
                {
                    Vector2 nodePos = EffectPoolManager.Instance.FromWorld(node.transform.position);
                    EffectPoolManager.Instance.SpawnCallout(nodePos + new Vector2(0f, 48f), $"x{_multiplier} COMBO", new Color(1f, 0.95f, 0.7f, 1f));
                }
            }

            int basePoints = (config != null) ? config.GetBaseScore(type) : 100;
            int pointsEarned = basePoints * _multiplier;
            _score += pointsEarned;

            Vector2 calloutPos = EffectPoolManager.Instance != null
                ? EffectPoolManager.Instance.FromWorld(node.transform.position) + new Vector2(0f, 36f)
                : Vector2.zero;
            Color calloutCol = type == TargetType.OverclockedGoldMole
                ? new Color(1f, 0.86f, 0.25f, 1f)
                : new Color(1f, 0.97f, 0.82f, 1f);
            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.SpawnCallout(calloutPos, $"+{pointsEarned}", calloutCol);
            }

            if (hud != null)
            {
                hud.UpdateScore(_score);
                hud.UpdateMultiplier(_multiplier, _streak, GetNextTierStreakTarget());
            }
        }

        private void HandleEmptyHatchTapped(HatchNode node)
        {
            if (_state != GameState.Playing) return;

            _emptyTaps++;
            _streak = 0;
            _multiplier = 1;

            if (hud != null)
            {
                hud.UpdateMultiplier(_multiplier, _streak, GetNextTierStreakTarget());
            }
        }

        private int GetNextTierStreakTarget()
        {
            if (config == null) return 5;
            if (_streak < config.Tier2Streak) return config.Tier2Streak;
            if (_streak < config.Tier3Streak) return config.Tier3Streak;
            return config.Tier4Streak;
        }

        private void EndMatch()
        {
            _state = GameState.GameOver;
            ArcadeFeel.CancelHitStop();
            CyberAudioManager.Instance?.PlayRoundEnd();
            RetractAllHatches();
            SetHatchGridVisible(false);

            bool isNewRecord = _score > _highScore;
            if (isNewRecord)
            {
                _highScore = _score;
                PlayerPrefs.SetInt(PrefsHighScoreKey, _highScore);
                PlayerPrefs.Save();
            }

            int totalAttempts = _totalHits + _emptyTaps + _empDetonated;
            float accuracy = totalAttempts > 0 ? ((float)_totalHits / totalAttempts) * 100f : 0f;

            if (hud != null)
            {
                hud.ShowGameOver(_score, _highScore, isNewRecord, _maxStreak, accuracy, _totalHits, _empDetonated, _seed);
            }
        }

        private void HandleStartGameClicked()
        {
            StartMatch(_seed);
        }

        private void HandleRestartSameSeedClicked()
        {
            StartMatch(_seed);
        }

        private void HandleRestartNewSeedClicked()
        {
            _seed = _prng.NextInt(100000, 999999);
            StartMatch(_seed);
        }

        private void HandleReturnToMenuClicked()
        {
            ReturnToMainMenu();
        }

        private void HandlePauseClicked()
        {
            if (_state != GameState.Playing && _state != GameState.ReadyCountdown) return;
            _resumeState = _state;
            _state = GameState.Paused;
            ArcadeFeel.CancelHitStop();
            if (hud != null) hud.ShowPause();
        }

        private void HandleResumeClicked()
        {
            if (_state != GameState.Paused) return;
            _state = _resumeState;
            if (hud != null) hud.HidePause();
        }

        private void HandlePauseHotkey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool pressed = keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame;
            if (!pressed) return;

            if (_state == GameState.Paused) HandleResumeClicked();
            else if (_state == GameState.Playing || _state == GameState.ReadyCountdown) HandlePauseClicked();
        }

        private void HandleSeedRandomizeClicked()
        {
            _seed = UnityEngine.Random.Range(100000, 999999);
            _prng.Init(_seed);
            if (hud != null)
            {
                hud.SetMatchSeed(_seed);
            }
        }

        private void HandleSeedChanged(int newSeed)
        {
            _seed = newSeed;
            _prng.Init(_seed);
            if (hud != null)
            {
                hud.SetMatchSeed(_seed);
            }
        }

        /// <summary>
        /// Guarantees that all pre-baked or runtime scene objects are visually polished
        /// to match the cartoon arcade art style: warm desert background,
        /// 3D wooden title banner, preview mounds, juicy cartoon buttons,
        /// wooden stat plaques, and animated combo bar.
        /// </summary>
        private void EnsureCartoonVisualPolish()
        {
            try
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = new Color32(237, 151, 62, 255);
                }

                Sprite hatchSprite = LoadArtSprite("HatchRing.png") ?? CartoonAtlasGenerator.CreateCartoonDirtMoundSprite(320, 180);
                Sprite standardSprite = LoadArtSprite("StandardCyberMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Normal);
                Sprite goldSprite = LoadArtSprite("OverclockedGoldMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Golden);
                Sprite armoredSprite = LoadArtSprite("ArmoredTankMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Helmet);
                Sprite armoredCrackedSprite = LoadArtSprite("ArmoredTankMole_Cracked.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.HelmetCracked);
                Sprite empSprite = LoadArtSprite("EmpDecoyMine.png") ?? CartoonAtlasGenerator.CreateCartoonBombSprite(256);
                Sprite soundOnSprite = LoadArtSprite("CartoonBtn_AudioOn.png") ?? LoadArtSprite("SoundIcon_On.png") ?? CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(90);
                Sprite soundOffSprite = LoadArtSprite("CartoonBtn_AudioOff.png") ?? LoadArtSprite("SoundIcon_Off.png") ?? CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(90);
                Sprite btnGoldSprite = LoadArtSprite("CartoonBtn_Gold.png") ?? LoadArtSprite("CyberButton.png") ?? CartoonAtlasGenerator.CreateCartoonStartButtonSprite(280, 90);
                Sprite btnGreenSprite = LoadArtSprite("CartoonBtn_Green.png") ?? LoadArtSprite("CyberButton_Amber.png");
                Sprite btnWoodSprite = LoadArtSprite("CartoonBtn_Wood.png");
                Sprite bgSprite = LoadArtSprite("CartoonBackground.png");
                Sprite titleSprite = LoadArtSprite("WhackTitle.png");
                Sprite statPlaqueSprite = LoadArtSprite("CartoonStatPlaque.png");
                Sprite comboTrackSprite = LoadArtSprite("CartoonComboTrack.png");
                Sprite comboFillSprite = LoadArtSprite("CartoonComboFill.png");
                Sprite multBadgeSprite = LoadArtSprite("CartoonBadge_Multiplier.png");
                Sprite clockIconSprite = LoadArtSprite("CartoonIcon_Clock.png");
                Sprite coinIconSprite = LoadArtSprite("CartoonIcon_StarCoin.png");
                Sprite exitBtnSprite = LoadArtSprite("CartoonBtn_Exit.png");
                Sprite modalBoardSprite = LoadArtSprite("CartoonParchmentModal.png");
                Sprite parchmentRowSprite = LoadArtSprite("CartoonParchmentRow.png");
                Sprite ribbonHeaderSprite = LoadArtSprite("CartoonRibbon_Header.png");
                Sprite countdownBurstSprite = LoadArtSprite("CartoonCountdownBurst.png");

                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform bgT = canvas.transform.Find("FullscreenBackground");
                    if (bgT == null && bgSprite != null)
                    {
                        GameObject bgObj = CreateRuntimePanel(canvas.gameObject, "FullscreenBackground", Vector2.zero, Vector2.one, bgSprite);
                        bgObj.transform.SetAsFirstSibling();
                        Image bgImg = bgObj.GetComponent<Image>();
                        bgImg.raycastTarget = false;
                    }
                    else if (bgT != null && bgSprite != null)
                    {
                        Image bgImg = bgT.GetComponent<Image>();
                        if (bgImg != null)
                        {
                            bgImg.sprite = bgSprite;
                            bgImg.color = Color.white;
                        }
                    }
                }

                // Update HatchNodes sprites
                if (hatchNodes != null)
                {
                    for (int i = 0; i < hatchNodes.Length; i++)
                    {
                        if (hatchNodes[i] != null)
                        {
                            Image nodeImg = hatchNodes[i].GetComponent<Image>();
                            if (nodeImg != null)
                            {
                                nodeImg.sprite = hatchSprite;
                                nodeImg.preserveAspect = true;
                            }
                            hatchNodes[i].ConfigureSprites(standardSprite, goldSprite, armoredSprite, armoredCrackedSprite, empSprite);
                        }
                    }
                }

                // Update MainMenuPanel
                GameObject menuObj = GameObject.Find("MainMenuPanel");
                if (menuObj != null)
                {
                    // Top-right Settings Button
                    Transform audioBtnT = menuObj.transform.Find("AudioToggleBtn");
                    if (audioBtnT != null)
                    {
                        Image btnImg = audioBtnT.GetComponent<Image>();
                        if (btnImg != null && soundOnSprite != null)
                        {
                            btnImg.sprite = soundOnSprite;
                            btnImg.preserveAspect = true;
                            btnImg.color = Color.white;
                        }
                    }

                    // Hide legacy text titles
                    Transform oldTitleT = menuObj.transform.Find("HeroTitle");
                    if (oldTitleT != null) oldTitleT.gameObject.SetActive(false);
                    Transform oldSubT = menuObj.transform.Find("HeroSub");
                    if (oldSubT != null) oldSubT.gameObject.SetActive(false);

                    // 3D Cartoon Wooden Title Banner
                    Transform titleBannerT = menuObj.transform.Find("HeroTitleBanner");
                    if (titleBannerT == null && titleSprite != null)
                    {
                        GameObject titleObj = CreateRuntimePanel(menuObj, "HeroTitleBanner", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.88f), titleSprite);
                        Image titleImg = titleObj.GetComponent<Image>();
                        titleImg.preserveAspect = true;
                        titleImg.raycastTarget = false;
                    }
                    else if (titleBannerT != null && titleSprite != null)
                    {
                        Image titleImg = titleBannerT.GetComponent<Image>();
                        if (titleImg != null)
                        {
                            titleImg.sprite = titleSprite;
                            titleImg.preserveAspect = true;
                            titleImg.color = Color.white;
                        }
                    }

                    // Preview Showcase Row
                    Transform previewT = menuObj.transform.Find("PreviewShowcaseRow");
                    if (previewT == null)
                    {
                        GameObject previewRow = new GameObject("PreviewShowcaseRow", typeof(RectTransform));
                        previewRow.transform.SetParent(menuObj.transform, false);
                        RectTransform prRt = previewRow.GetComponent<RectTransform>();
                        prRt.anchorMin = new Vector2(0.04f, 0.48f);
                        prRt.anchorMax = new Vector2(0.96f, 0.70f);
                        prRt.offsetMin = Vector2.zero;
                        prRt.offsetMax = Vector2.zero;

                        CreatePreviewMound(previewRow, "LeftPreviewMound", new Vector2(0.02f, 0.05f), new Vector2(0.33f, 0.95f), hatchSprite, standardSprite, -10f, 0.95f);
                        CreatePreviewMound(previewRow, "CenterPreviewMound", new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.95f), hatchSprite, goldSprite, -10f, 1.0f);
                        CreatePreviewMound(previewRow, "RightPreviewMound", new Vector2(0.67f, 0.05f), new Vector2(0.98f, 0.95f), hatchSprite, empSprite, 10f, 1.0f);
                    }

                    // Glossy Golden Start Button
                    Transform startT = menuObj.transform.Find("StartButton");
                    if (startT != null)
                    {
                        Image startImg = startT.GetComponent<Image>();
                        if (startImg != null && btnGoldSprite != null)
                        {
                            startImg.sprite = btnGoldSprite;
                            startImg.preserveAspect = true;
                            startImg.color = Color.white;
                        }
                        RectTransform startRt = startT.GetComponent<RectTransform>();
                        startRt.anchorMin = new Vector2(0.20f, 0.35f);
                        startRt.anchorMax = new Vector2(0.80f, 0.46f);
                        startRt.offsetMin = Vector2.zero;
                        startRt.offsetMax = Vector2.zero;

                        TextMeshProUGUI startTxt = startT.GetComponentInChildren<TextMeshProUGUI>();
                        if (startTxt != null)
                        {
                            startTxt.text = "START GAME";
                            startTxt.fontSize = 46;
                            startTxt.color = Color.white;
                        }
                    }

                    // Record Card & Seed Box
                    Transform recordT = menuObj.transform.Find("RecordCard");
                    if (recordT != null)
                    {
                        Image rImg = recordT.GetComponent<Image>();
                        if (rImg != null && statPlaqueSprite != null) rImg.sprite = statPlaqueSprite;
                        RectTransform rRt = recordT.GetComponent<RectTransform>();
                        rRt.anchorMin = new Vector2(0.10f, 0.25f);
                        rRt.anchorMax = new Vector2(0.90f, 0.33f);
                        TextMeshProUGUI rTxt = recordT.GetComponentInChildren<TextMeshProUGUI>();
                        if (rTxt != null) rTxt.color = new Color32(58, 30, 12, 255);
                    }

                    Transform seedT = menuObj.transform.Find("SeedBox");
                    if (seedT != null)
                    {
                        Image sImg = seedT.GetComponent<Image>();
                        if (sImg != null && parchmentRowSprite != null) sImg.sprite = parchmentRowSprite;
                        RectTransform sRt = seedT.GetComponent<RectTransform>();
                        sRt.anchorMin = new Vector2(0.10f, 0.16f);
                        sRt.anchorMax = new Vector2(0.62f, 0.23f);
                    }

                    Transform intelT = menuObj.transform.Find("IntelButton");
                    if (intelT != null)
                    {
                        Image iImg = intelT.GetComponent<Image>();
                        if (iImg != null && btnGreenSprite != null) iImg.sprite = btnGreenSprite;
                        RectTransform iRt = intelT.GetComponent<RectTransform>();
                        iRt.anchorMin = new Vector2(0.65f, 0.16f);
                        iRt.anchorMax = new Vector2(0.90f, 0.23f);
                        TextMeshProUGUI intelTxt = intelT.GetComponentInChildren<TextMeshProUGUI>();
                        if (intelTxt != null)
                        {
                            intelTxt.text = "RULES";
                            intelTxt.color = Color.white;
                        }
                    }
                }

                // In-Game Header Visuals & Layout
                GameObject headerObj = GameObject.Find("InGameHeader");
                if (headerObj != null)
                {
                    RectTransform hRt = headerObj.GetComponent<RectTransform>();
                    if (hRt != null)
                    {
                        hRt.anchorMin = new Vector2(0f, 0.77f);
                        hRt.anchorMax = new Vector2(1f, 1f);
                        hRt.offsetMin = Vector2.zero;
                        hRt.offsetMax = Vector2.zero;
                    }

                    // Top Left Exit
                    Transform abortT = headerObj.transform.Find("AbortBtn");
                    if (abortT != null)
                    {
                        RectTransform aRt = abortT.GetComponent<RectTransform>();
                        if (aRt != null)
                        {
                            aRt.anchorMin = new Vector2(0.04f, 0.81f);
                            aRt.anchorMax = new Vector2(0.20f, 0.98f);
                            aRt.offsetMin = Vector2.zero;
                            aRt.offsetMax = Vector2.zero;
                        }
                        Image aImg = abortT.GetComponent<Image>();
                        if (aImg != null && exitBtnSprite != null) aImg.sprite = exitBtnSprite;
                        TextMeshProUGUI tmp = abortT.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.text = "EXIT";
                            tmp.fontSize = 22;
                            tmp.color = Color.white;
                        }
                    }

                    // Seed badge in top utility row
                    Transform seedBadgeT = headerObj.transform.Find("SeedBadge");
                    if (seedBadgeT != null)
                    {
                        RectTransform sbRt = seedBadgeT.GetComponent<RectTransform>();
                        sbRt.anchorMin = new Vector2(0.23f, 0.83f);
                        sbRt.anchorMax = new Vector2(0.77f, 0.97f);
                        Image sbImg = seedBadgeT.GetComponent<Image>();
                        if (sbImg != null && parchmentRowSprite != null) sbImg.sprite = parchmentRowSprite;
                    }
                    else
                    {
                        Transform igSeedT = headerObj.transform.Find("InGameSeed");
                        if (igSeedT != null)
                        {
                            RectTransform igsRt = igSeedT.GetComponent<RectTransform>();
                            igsRt.anchorMin = new Vector2(0.23f, 0.83f);
                            igsRt.anchorMax = new Vector2(0.77f, 0.97f);
                            igsRt.offsetMin = Vector2.zero;
                            igsRt.offsetMax = Vector2.zero;
                            TextMeshProUGUI igsTmp = igSeedT.GetComponent<TextMeshProUGUI>();
                            if (igsTmp != null) igsTmp.color = new Color32(58, 30, 12, 255);
                        }
                    }

                    // In-Game Audio Toggle
                    Transform igAudioT = headerObj.transform.Find("InGameAudioToggle");
                    if (igAudioT == null && soundOnSprite != null)
                    {
                        GameObject igAudioObj = CreateRuntimePanel(headerObj, "InGameAudioToggle", new Vector2(0.80f, 0.81f), new Vector2(0.96f, 0.98f), soundOnSprite);
                        Button btn = igAudioObj.AddComponent<Button>();
                        btn.onClick.AddListener(() => CyberAudioManager.Instance?.ToggleMute());
                        Image iconImg = igAudioObj.GetComponent<Image>();
                        iconImg.preserveAspect = true;
                        if (hud != null)
                        {
                            SetPrivateField(hud, "inGameAudioToggleButton", btn);
                            SetPrivateField(hud, "inGameAudioToggleIcon", iconImg);
                        }
                    }

                    // Timer Box
                    Transform timerBoxT = headerObj.transform.Find("TimerBox");
                    if (timerBoxT != null)
                    {
                        RectTransform tbRt = timerBoxT.GetComponent<RectTransform>();
                        tbRt.anchorMin = new Vector2(0.04f, 0.38f);
                        tbRt.anchorMax = new Vector2(0.48f, 0.78f);
                        tbRt.offsetMin = Vector2.zero;
                        tbRt.offsetMax = Vector2.zero;
                        Image tImg = timerBoxT.GetComponent<Image>();
                        if (tImg != null && statPlaqueSprite != null) tImg.sprite = statPlaqueSprite;

                        Transform clockIconT = timerBoxT.Find("ClockIcon");
                        if (clockIconT == null && clockIconSprite != null)
                        {
                            GameObject clkObj = CreateRuntimePanel(timerBoxT.gameObject, "ClockIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), clockIconSprite);
                            clkObj.GetComponent<Image>().preserveAspect = true;
                        }
                        Transform titleT = timerBoxT.Find("TimerTitle");
                        if (titleT != null)
                        {
                            RectTransform ttRt = titleT.GetComponent<RectTransform>();
                            ttRt.anchorMin = new Vector2(0.34f, 0.60f);
                            ttRt.anchorMax = new Vector2(0.95f, 0.92f);
                            TextMeshProUGUI tt = titleT.GetComponent<TextMeshProUGUI>();
                            if (tt != null) tt.color = new Color32(109, 58, 22, 255);
                        }
                        Transform digitsT = timerBoxT.Find("TimerDigits");
                        if (digitsT != null)
                        {
                            RectTransform tdRt = digitsT.GetComponent<RectTransform>();
                            tdRt.anchorMin = new Vector2(0.34f, 0.08f);
                            tdRt.anchorMax = new Vector2(0.95f, 0.65f);
                            TextMeshProUGUI td = digitsT.GetComponent<TextMeshProUGUI>();
                            if (td != null) { td.color = new Color32(58, 30, 12, 255); td.alignment = TextAlignmentOptions.Left; }
                        }
                    }

                    // Score Box
                    Transform scoreBoxT = headerObj.transform.Find("ScoreBox");
                    if (scoreBoxT != null)
                    {
                        RectTransform sbRt = scoreBoxT.GetComponent<RectTransform>();
                        sbRt.anchorMin = new Vector2(0.52f, 0.38f);
                        sbRt.anchorMax = new Vector2(0.96f, 0.78f);
                        sbRt.offsetMin = Vector2.zero;
                        sbRt.offsetMax = Vector2.zero;
                        Image sImg = scoreBoxT.GetComponent<Image>();
                        if (sImg != null && statPlaqueSprite != null) sImg.sprite = statPlaqueSprite;

                        Transform coinIconT = scoreBoxT.Find("CoinIcon");
                        if (coinIconT == null && coinIconSprite != null)
                        {
                            GameObject coinObj = CreateRuntimePanel(scoreBoxT.gameObject, "CoinIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), coinIconSprite);
                            coinObj.GetComponent<Image>().preserveAspect = true;
                        }
                        Transform titleT = scoreBoxT.Find("ScoreTitle");
                        if (titleT != null)
                        {
                            RectTransform stRt = titleT.GetComponent<RectTransform>();
                            stRt.anchorMin = new Vector2(0.34f, 0.60f);
                            stRt.anchorMax = new Vector2(0.95f, 0.92f);
                            TextMeshProUGUI st = titleT.GetComponent<TextMeshProUGUI>();
                            if (st != null) st.color = new Color32(109, 58, 22, 255);
                        }
                        Transform digitsT = scoreBoxT.Find("ScoreDigits");
                        if (digitsT != null)
                        {
                            RectTransform sdRt = digitsT.GetComponent<RectTransform>();
                            sdRt.anchorMin = new Vector2(0.34f, 0.08f);
                            sdRt.anchorMax = new Vector2(0.95f, 0.65f);
                            TextMeshProUGUI sd = digitsT.GetComponent<TextMeshProUGUI>();
                            if (sd != null) { sd.color = new Color32(58, 30, 12, 255); sd.alignment = TextAlignmentOptions.Left; }
                        }
                    }

                    // Combo Panel (Moved inside header, above mole matrix)
                    Transform comboT = headerObj.transform.Find("ComboPanel");
                    if (comboT != null)
                    {
                        RectTransform cbRt = comboT.GetComponent<RectTransform>();
                        cbRt.anchorMin = new Vector2(0.04f, 0.02f);
                        cbRt.anchorMax = new Vector2(0.96f, 0.34f);
                        cbRt.offsetMin = Vector2.zero;
                        cbRt.offsetMax = Vector2.zero;
                        Image cbImg = comboT.GetComponent<Image>();
                        if (cbImg != null) cbImg.color = Color.clear;

                        // Check MultiplierBadge
                        Transform badgeT = comboT.Find("MultiplierBadge");
                        if (badgeT == null && multBadgeSprite != null)
                        {
                            GameObject badgeObj = CreateRuntimePanel(comboT.gameObject, "MultiplierBadge", new Vector2(0.00f, 0.00f), new Vector2(0.20f, 1.00f), multBadgeSprite);
                            badgeObj.GetComponent<Image>().preserveAspect = true;
                            badgeT = badgeObj.transform;

                            Transform oldMultT = comboT.Find("MultiplierText");
                            if (oldMultT != null)
                            {
                                oldMultT.SetParent(badgeT, false);
                                RectTransform omRt = oldMultT.GetComponent<RectTransform>();
                                omRt.anchorMin = Vector2.zero;
                                omRt.anchorMax = Vector2.one;
                                omRt.offsetMin = Vector2.zero;
                                omRt.offsetMax = Vector2.zero;
                                TextMeshProUGUI mTmp = oldMultT.GetComponent<TextMeshProUGUI>();
                                if (mTmp != null)
                                {
                                    mTmp.alignment = TextAlignmentOptions.Center;
                                    mTmp.fontSize = 30;
                                }
                            }
                            if (hud != null) SetPrivateField(hud, "multiplierBadgeTransform", badgeT.GetComponent<RectTransform>());
                        }

                        // Check ComboTrack & Fill Bar
                        Transform trackT = comboT.Find("ComboTrack");
                        if (trackT == null && comboTrackSprite != null)
                        {
                            GameObject trackObj = CreateRuntimePanel(comboT.gameObject, "ComboTrack", new Vector2(0.22f, 0.16f), new Vector2(0.99f, 0.84f), comboTrackSprite);
                            trackObj.GetComponent<Image>().type = Image.Type.Sliced;
                            trackT = trackObj.transform;

                            GameObject fillObj = CreateRuntimePanel(trackObj, "ComboFill", Vector2.zero, Vector2.one, comboFillSprite);
                            Image fillImg = fillObj.GetComponent<Image>();
                            fillImg.type = Image.Type.Filled;
                            fillImg.fillMethod = Image.FillMethod.Horizontal;
                            fillImg.fillOrigin = 0;
                            fillImg.fillAmount = 0f;

                            Transform oldStreakT = comboT.Find("StreakText");
                            if (oldStreakT != null)
                            {
                                oldStreakT.SetParent(trackT, false);
                                RectTransform ostRt = oldStreakT.GetComponent<RectTransform>();
                                ostRt.anchorMin = Vector2.zero;
                                ostRt.anchorMax = Vector2.one;
                                ostRt.offsetMin = Vector2.zero;
                                ostRt.offsetMax = Vector2.zero;
                                TextMeshProUGUI sTmp = oldStreakT.GetComponent<TextMeshProUGUI>();
                                if (sTmp != null)
                                {
                                    sTmp.alignment = TextAlignmentOptions.Center;
                                    sTmp.fontSize = 20;
                                    sTmp.color = Color.white;
                                }
                            }

                            if (hud != null)
                            {
                                SetPrivateField(hud, "comboStreakFill", fillImg);
                            }
                        }
                    }
                }

                // Target Intel Modal & Game Over Modal Polish
                GameObject intelModal = GameObject.Find("TargetIntelModal");
                if (intelModal != null)
                {
                    Image imImg = intelModal.GetComponent<Image>();
                    if (imImg != null && modalBoardSprite != null) imImg.sprite = modalBoardSprite;
                }

                GameObject goModal = GameObject.Find("GameOverModal");
                if (goModal != null)
                {
                    Image goImg = goModal.GetComponent<Image>();
                    if (goImg != null && modalBoardSprite != null) goImg.sprite = modalBoardSprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CyberWhack] EnsureCartoonVisualPolish non-fatal notice: {ex.Message}");
            }
        }

        /// <summary>
        /// Self-healing runtime generator: constructs the complete CyberWhack UI canvas,
        /// Main Menu Terminal, Safe-Area Fitter, Target Intel Modal, 3x3 subterranean hydraulic
        /// hatch matrix, and Game Over Evaluation modal.
        /// </summary>
        private void BuildRuntimeHierarchy()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color32(237, 151, 62, 255); // Warm desert sand #ED973E
            }

            // Canvas
            GameObject canvasObj = new GameObject("CyberCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // 9:16 portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Width-Lock

            // Sprites
            Sprite hatchSprite = LoadArtSprite("HatchRing.png") ?? CartoonAtlasGenerator.CreateCartoonDirtMoundSprite(320, 180);
            Sprite standardSprite = LoadArtSprite("StandardCyberMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Normal);
            Sprite goldSprite = LoadArtSprite("OverclockedGoldMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Golden);
            Sprite armoredSprite = LoadArtSprite("ArmoredTankMole.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Helmet);
            Sprite armoredCrackedSprite = LoadArtSprite("ArmoredTankMole_Cracked.png") ?? CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.HelmetCracked);
            Sprite empSprite = LoadArtSprite("EmpDecoyMine.png") ?? CartoonAtlasGenerator.CreateCartoonBombSprite(256);
            Sprite shockwaveSprite = LoadArtSprite("ShockwaveRing.png") ?? CartoonAtlasGenerator.CreateCartoonStarburstSprite(256);
            Sprite panelFrameSprite = LoadArtSprite("PanelFrame.png");
            Sprite btnCyanSprite = LoadArtSprite("CyberButton.png") ?? CartoonAtlasGenerator.CreateCartoonStartButtonSprite(280, 90);
            Sprite btnAmberSprite = LoadArtSprite("CyberButton_Amber.png");
            Sprite intelCardSprite = LoadArtSprite("IntelCardFrame.png");
            Sprite soundOnSprite = LoadArtSprite("SoundIcon_On.png") ?? CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(90);
            Sprite soundOffSprite = LoadArtSprite("SoundIcon_Off.png") ?? CartoonAtlasGenerator.CreateCartoonSettingsButtonSprite(90);
            Sprite bgSprite = LoadArtSprite("CartoonBackground.png");
            Sprite titleSprite = LoadArtSprite("WhackTitle.png");

            // Runtime hierarchy compatibility aliases. The authored scene is
            // themed after construction, but the self-healing path still
            // needs a complete set of local references while it is built.
            Sprite exitBtnSprite = btnCyanSprite;
            Sprite parchmentRowSprite = panelFrameSprite;
            Sprite statPlaqueSprite = panelFrameSprite;
            Sprite clockIconSprite = CyberAtlasGenerator.CreateTargetReticleSprite(90);
            Sprite coinIconSprite = CyberAtlasGenerator.CreateTargetReticleSprite(90);
            Sprite multBadgeSprite = CyberAtlasGenerator.CreatePanelFrameSprite(128, 128);
            Sprite comboTrackSprite = panelFrameSprite;
            Sprite comboFillSprite = btnCyanSprite;

            // Fullscreen Background
            if (bgSprite != null)
            {
                GameObject bgObj = CreateRuntimePanel(canvasObj, "FullscreenBackground", Vector2.zero, Vector2.one, bgSprite);
                bgObj.transform.SetAsFirstSibling();
                Image bgImg = bgObj.GetComponent<Image>();
                bgImg.raycastTarget = false;
            }

            // Safe Area Container
            GameObject safeAreaObj = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaObj.transform.SetParent(canvasObj.transform, false);
            RectTransform safeRt = safeAreaObj.GetComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;

            // ==========================================
            // 1. 3x3 Subterranean Hatch Grid Container
            // ==========================================
            GameObject gridContainer = new GameObject("HatchGridContainer", typeof(RectTransform));
            gridContainer.transform.SetParent(safeAreaObj.transform, false);
            RectTransform gridRt = gridContainer.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.04f, 0.12f);
            gridRt.anchorMax = new Vector2(0.96f, 0.68f);
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;

            hatchNodes = new HatchNode[9];
            float[] colX = new float[] { 0.17f, 0.50f, 0.83f };
            float[] rowY = new float[] { 0.83f, 0.50f, 0.17f };

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int index = r * 3 + c;
                    GameObject nodeObj = new GameObject($"HatchNode_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HatchNode));
                    nodeObj.transform.SetParent(gridContainer.transform, false);

                    RectTransform nodeRt = nodeObj.GetComponent<RectTransform>();
                    nodeRt.anchorMin = new Vector2(colX[c], rowY[r]);
                    nodeRt.anchorMax = new Vector2(colX[c], rowY[r]);
                    nodeRt.sizeDelta = new Vector2(300, 300);

                    Image ringImg = nodeObj.GetComponent<Image>();
                    ringImg.sprite = hatchSprite;
                    ringImg.raycastTarget = true;

                    // RectMask2D bounding mask avoids extra GPU render passes
                    GameObject maskObj = new GameObject("MaskContainer", typeof(RectTransform), typeof(RectMask2D));
                    maskObj.transform.SetParent(nodeObj.transform, false);
                    RectTransform maskRt = maskObj.GetComponent<RectTransform>();
                    maskRt.anchorMin = new Vector2(0.12f, 0.18f);
                    maskRt.anchorMax = new Vector2(0.88f, 0.95f);
                    maskRt.offsetMin = Vector2.zero;
                    maskRt.offsetMax = Vector2.zero;

                    GameObject targetObj = new GameObject("TargetSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    targetObj.transform.SetParent(maskObj.transform, false);
                    RectTransform targetRt = targetObj.GetComponent<RectTransform>();
                    targetRt.anchorMin = new Vector2(0.5f, 0f);
                    targetRt.anchorMax = new Vector2(0.5f, 0f);
                    targetRt.sizeDelta = new Vector2(230, 230);
                    targetRt.anchoredPosition = new Vector2(0, -140);

                    Image targetImg = targetObj.GetComponent<Image>();
                    targetImg.sprite = standardSprite;
                    targetImg.raycastTarget = false;

                    HatchNode node = nodeObj.GetComponent<HatchNode>();
                    node.ConfigureSprites(standardSprite, goldSprite, armoredSprite, armoredCrackedSprite, empSprite);
                    node.SetNodeIndex(index);

                    hatchNodes[index] = node;
                }
            }

            // ==========================================
            // 2. In-Game Top Header HUD (Polished Cartoon Layout)
            // ==========================================
            GameObject headerObj = new GameObject("InGameHeader", typeof(RectTransform));
            headerObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.77f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;

            // Warm Cartoon Palette Matching Reference Game
            Color32 darkChocolate = new Color32(58, 30, 12, 255);
            Color32 warmCaramel = new Color32(109, 58, 22, 255);
            Color32 goldenText = new Color32(255, 224, 102, 255);
            Color32 goldenSub = new Color32(255, 238, 185, 255);
            Color32 hazardRed = new Color32(235, 55, 55, 255);
            Color32 darkBrown = darkChocolate;
            Color32 warmAmber = goldenText;

            // 1. Top Utility Row (Exit, Seed Badge, In-game Audio)
            GameObject abortBtnObj = CreateRuntimePanel(headerObj, "AbortBtn", new Vector2(0.04f, 0.81f), new Vector2(0.20f, 0.98f), exitBtnSprite);
            Button abortBtn = abortBtnObj.AddComponent<Button>();
            TextMeshProUGUI abortTxt = CreateRuntimeText(abortBtnObj, "Txt", "EXIT", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            abortTxt.rectTransform.anchorMin = Vector2.zero;
            abortTxt.rectTransform.anchorMax = Vector2.one;

            GameObject seedBadgeObj = CreateRuntimePanel(headerObj, "SeedBadge", new Vector2(0.23f, 0.83f), new Vector2(0.77f, 0.97f), parchmentRowSprite);
            TextMeshProUGUI igSeed = CreateRuntimeText(seedBadgeObj, "InGameSeed", $"SEED #{_seed:D6}", 18, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            igSeed.rectTransform.anchorMin = Vector2.zero;
            igSeed.rectTransform.anchorMax = Vector2.one;

            GameObject igAudioBtnObj = CreateRuntimePanel(headerObj, "InGameAudioToggle", new Vector2(0.80f, 0.81f), new Vector2(0.96f, 0.98f), soundOnSprite);
            Button igAudioBtn = igAudioBtnObj.AddComponent<Button>();
            Image igAudioIcon = igAudioBtnObj.GetComponent<Image>();
            igAudioIcon.preserveAspect = true;
            igAudioIcon.raycastTarget = true;

            // 2. Middle Row: Timer Plaque (Left) & Score Plaque (Right)
            GameObject timerBox = CreateRuntimePanel(headerObj, "TimerBox", new Vector2(0.04f, 0.38f), new Vector2(0.48f, 0.78f), statPlaqueSprite);
            if (clockIconSprite != null)
            {
                GameObject clockIconObj = CreateRuntimePanel(timerBox, "ClockIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), clockIconSprite);
                clockIconObj.GetComponent<Image>().preserveAspect = true;
            }
            TextMeshProUGUI timerTitle = CreateRuntimeText(timerBox, "TimerTitle", "TIME", 17, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Left);
            timerTitle.rectTransform.anchorMin = new Vector2(0.34f, 0.60f);
            timerTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);

            TextMeshProUGUI timerText = CreateRuntimeText(timerBox, "TimerDigits", "00:60", 42, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Left);
            timerText.rectTransform.anchorMin = new Vector2(0.34f, 0.08f);
            timerText.rectTransform.anchorMax = new Vector2(0.95f, 0.65f);

            GameObject scoreBox = CreateRuntimePanel(headerObj, "ScoreBox", new Vector2(0.52f, 0.38f), new Vector2(0.96f, 0.78f), statPlaqueSprite);
            if (coinIconSprite != null)
            {
                GameObject coinIconObj = CreateRuntimePanel(scoreBox, "CoinIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), coinIconSprite);
                coinIconObj.GetComponent<Image>().preserveAspect = true;
            }
            TextMeshProUGUI scoreTitle = CreateRuntimeText(scoreBox, "ScoreTitle", "SCORE", 17, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Left);
            scoreTitle.rectTransform.anchorMin = new Vector2(0.34f, 0.60f);
            scoreTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);

            TextMeshProUGUI scoreText = CreateRuntimeText(scoreBox, "ScoreDigits", "0", 42, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Left);
            scoreText.rectTransform.anchorMin = new Vector2(0.34f, 0.08f);
            scoreText.rectTransform.anchorMax = new Vector2(0.95f, 0.65f);

            // 3. Bottom Row: Juicy Combo & Multiplier Bar
            GameObject comboPanel = new GameObject("ComboPanel", typeof(RectTransform));
            comboPanel.transform.SetParent(headerObj.transform, false);
            RectTransform cpRt = comboPanel.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.04f, 0.02f);
            cpRt.anchorMax = new Vector2(0.96f, 0.34f);
            cpRt.offsetMin = Vector2.zero;
            cpRt.offsetMax = Vector2.zero;

            // Multiplier Starburst Badge on Left
            GameObject badgeObj = CreateRuntimePanel(comboPanel, "MultiplierBadge", new Vector2(0.00f, 0.00f), new Vector2(0.20f, 1.00f), multBadgeSprite);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.preserveAspect = true;

            TextMeshProUGUI multText = CreateRuntimeText(badgeObj, "MultiplierText", "1x", 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            multText.rectTransform.anchorMin = Vector2.zero;
            multText.rectTransform.anchorMax = Vector2.one;

            // Sunken Combo Track
            GameObject comboTrackObj = CreateRuntimePanel(comboPanel, "ComboTrack", new Vector2(0.22f, 0.16f), new Vector2(0.99f, 0.84f), comboTrackSprite);
            Image trackImg = comboTrackObj.GetComponent<Image>();
            trackImg.type = Image.Type.Sliced;

            // Gradient Energy Fill Bar
            GameObject comboFillObj = CreateRuntimePanel(comboTrackObj, "ComboFill", Vector2.zero, Vector2.one, comboFillSprite);
            Image comboFillImg = comboFillObj.GetComponent<Image>();
            comboFillImg.type = Image.Type.Filled;
            comboFillImg.fillMethod = Image.FillMethod.Horizontal;
            comboFillImg.fillOrigin = 0;
            comboFillImg.fillAmount = 0f;

            // Streak Counter Text on Bar
            TextMeshProUGUI streakText = CreateRuntimeText(comboTrackObj, "StreakText", "WHACK TO COMBO!", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            streakText.rectTransform.anchorMin = Vector2.zero;
            streakText.rectTransform.anchorMax = Vector2.one;

            headerObj.SetActive(false);

            // ==========================================
            // 3. Main Menu Terminal UI Panel
            // Exact Match with https://plays.org/whack-a-mole Reference
            // ==========================================
            GameObject menuObj = new GameObject("MainMenuPanel", typeof(RectTransform));
            menuObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform menuRt = menuObj.GetComponent<RectTransform>();
            menuRt.anchorMin = Vector2.zero;
            menuRt.anchorMax = Vector2.one;
            menuRt.offsetMin = Vector2.zero;
            menuRt.offsetMax = Vector2.zero;

            // Top Right Settings Gear Button (Green rounded square with white gear)
            GameObject audioBtnObj = CreateRuntimePanel(menuObj, "AudioToggleBtn", new Vector2(0.85f, 0.91f), new Vector2(0.96f, 0.97f), soundOnSprite);
            Button audioToggleBtn = audioBtnObj.AddComponent<Button>();
            Image audioIcon = audioBtnObj.GetComponent<Image>();
            audioIcon.preserveAspect = true;
            audioIcon.raycastTarget = true;

            // 3D Cartoon Wooden Title Banner: "WHACK-A-MOLE"
            GameObject titleObj = CreateRuntimePanel(menuObj, "HeroTitleBanner", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.88f), titleSprite);
            Image titleImg = titleObj.GetComponent<Image>();
            if (titleSprite != null)
            {
                titleImg.preserveAspect = true;
                titleImg.raycastTarget = false;
            }
            else
            {
                titleImg.color = Color.clear;
                TextMeshProUGUI fallbackTitle = CreateRuntimeText(titleObj, "FallbackTitle", "WHACK-A-MOLE", 56, FontStyles.Bold, goldenText, TextAlignmentOptions.Center);
                fallbackTitle.rectTransform.anchorMin = Vector2.zero;
                fallbackTitle.rectTransform.anchorMax = Vector2.one;
            }

            // Reference Showcase Row: 3 Preview Mounds (Left Mole, Center Mole, Black Bomb with Burning Fuse Flame)
            GameObject previewRow = new GameObject("PreviewShowcaseRow", typeof(RectTransform));
            previewRow.transform.SetParent(menuObj.transform, false);
            RectTransform prRt = previewRow.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0.04f, 0.48f);
            prRt.anchorMax = new Vector2(0.96f, 0.70f);
            prRt.offsetMin = Vector2.zero;
            prRt.offsetMax = Vector2.zero;

            // Left Preview Mound (Cute Mole Peeking Out)
            CreatePreviewMound(previewRow, "LeftPreviewMound", new Vector2(0.02f, 0.05f), new Vector2(0.33f, 0.95f), hatchSprite, standardSprite, -10f, 0.95f);
            // Center Preview Mound (Cute Mole Peeking Out)
            CreatePreviewMound(previewRow, "CenterPreviewMound", new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.95f), hatchSprite, standardSprite, -10f, 1.0f);
            // Right Preview Mound (Round Black Bomb with Burning Flame Fuse)
            CreatePreviewMound(previewRow, "RightPreviewMound", new Vector2(0.67f, 0.05f), new Vector2(0.98f, 0.95f), hatchSprite, empSprite, 10f, 1.0f);

            // Glossy Golden Yellow Pill "Start" Button
            GameObject startBtnObj = CreateRuntimePanel(menuObj, "StartButton", new Vector2(0.24f, 0.35f), new Vector2(0.76f, 0.45f), btnCyanSprite);
            Button startBtn = startBtnObj.AddComponent<Button>();
            Image startBtnImg = startBtnObj.GetComponent<Image>();
            startBtnImg.preserveAspect = true;
            TextMeshProUGUI startTxt = CreateRuntimeText(startBtnObj, "Txt", "Start", 50, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            startTxt.rectTransform.anchorMin = Vector2.zero;
            startTxt.rectTransform.anchorMax = Vector2.one;
            startTxt.rectTransform.offsetMin = Vector2.zero;
            startTxt.rectTransform.offsetMax = Vector2.zero;

            // Tournament High Score Card
            GameObject recordBox = CreateRuntimePanel(menuObj, "RecordCard", new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.33f), panelFrameSprite);
            TextMeshProUGUI recordText = CreateRuntimeText(recordBox, "RecordText", $"TOURNAMENT RECORD: {_highScore:N0} PTS", 24, FontStyles.Bold, goldenText, TextAlignmentOptions.Center);
            recordText.rectTransform.anchorMin = Vector2.zero;
            recordText.rectTransform.anchorMax = Vector2.one;
            recordText.rectTransform.offsetMin = Vector2.zero;
            recordText.rectTransform.offsetMax = Vector2.zero;

            // Seed Selector & Rules Row
            GameObject seedBox = CreateRuntimePanel(menuObj, "SeedBox", new Vector2(0.10f, 0.17f), new Vector2(0.62f, 0.24f), panelFrameSprite);
            TextMeshProUGUI seedLbl = CreateRuntimeText(seedBox, "Lbl", "SEED", 15, FontStyles.Bold, goldenSub, TextAlignmentOptions.Left);
            seedLbl.rectTransform.anchorMin = new Vector2(0.05f, 0.60f);
            seedLbl.rectTransform.anchorMax = new Vector2(0.50f, 0.95f);
            seedLbl.rectTransform.offsetMin = Vector2.zero;
            seedLbl.rectTransform.offsetMax = Vector2.zero;

            // Input field for Seed
            GameObject inputObj = CreateRuntimePanel(seedBox, "SeedInput", new Vector2(0.05f, 0.10f), new Vector2(0.64f, 0.58f), intelCardSprite);
            TMP_InputField seedInput = inputObj.AddComponent<TMP_InputField>();
            GameObject inTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inTxtObj.transform.SetParent(inputObj.transform, false);
            RectTransform inTxtRt = inTxtObj.GetComponent<RectTransform>();
            inTxtRt.anchorMin = new Vector2(0.05f, 0f);
            inTxtRt.anchorMax = new Vector2(0.95f, 1f);
            inTxtRt.offsetMin = Vector2.zero;
            inTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI inTmp = inTxtObj.GetComponent<TextMeshProUGUI>();
            inTmp.fontSize = 22;
            inTmp.color = Color.white;
            inTmp.alignment = TextAlignmentOptions.MidlineLeft;
            seedInput.textComponent = inTmp;
            seedInput.text = _seed.ToString();

            // RND Seed Button
            GameObject rndBtnObj = CreateRuntimePanel(seedBox, "RndBtn", new Vector2(0.68f, 0.10f), new Vector2(0.95f, 0.58f), btnAmberSprite);
            Button rndBtn = rndBtnObj.AddComponent<Button>();
            TextMeshProUGUI rndTxt = CreateRuntimeText(rndBtnObj, "Txt", "RND", 20, FontStyles.Bold, darkBrown, TextAlignmentOptions.Center);
            rndTxt.rectTransform.anchorMin = Vector2.zero;
            rndTxt.rectTransform.anchorMax = Vector2.one;
            rndTxt.rectTransform.offsetMin = Vector2.zero;
            rndTxt.rectTransform.offsetMax = Vector2.zero;

            // Rules & Target Intel Button
            GameObject intelBtnObj = CreateRuntimePanel(menuObj, "IntelButton", new Vector2(0.65f, 0.17f), new Vector2(0.90f, 0.24f), btnAmberSprite);
            Button intelBtn = intelBtnObj.AddComponent<Button>();
            TextMeshProUGUI intelTxt = CreateRuntimeText(intelBtnObj, "Txt", "RULES", 22, FontStyles.Bold, darkBrown, TextAlignmentOptions.Center);
            intelTxt.rectTransform.anchorMin = Vector2.zero;
            intelTxt.rectTransform.anchorMax = Vector2.one;
            intelTxt.rectTransform.offsetMin = Vector2.zero;
            intelTxt.rectTransform.offsetMax = Vector2.zero;

            // Footer info
            TextMeshProUGUI footerTxt = CreateRuntimeText(menuObj, "Footer", "MOBILE WEBGL // LOCKED 60 FPS // 3x3 MATRIX", 15, FontStyles.Normal, goldenSub, TextAlignmentOptions.Center);
            footerTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.09f);
            footerTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.14f);
            footerTxt.rectTransform.offsetMin = Vector2.zero;
            footerTxt.rectTransform.offsetMax = Vector2.zero;

            // ==========================================
            // 4. Target Intel & Rules Modal
            // ==========================================
            GameObject intelModalObj = CreateRuntimePanel(safeAreaObj, "TargetIntelModal", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), intelCardSprite);
            intelModalObj.SetActive(false);

            TextMeshProUGUI imTitle = CreateRuntimeText(intelModalObj, "Title", "HOW TO PLAY & TARGETS", 32, FontStyles.Bold, goldenText, TextAlignmentOptions.Center);
            imTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.91f);
            imTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            imTitle.rectTransform.offsetMin = Vector2.zero;
            imTitle.rectTransform.offsetMax = Vector2.zero;

            // 4 Target Dossier Cards
            CreateIntelRow(intelModalObj, "Row1", 0.73f, 0.89f, standardSprite, "STANDARD MOLE", "0.85s Active // 1 Tap Required\nCute subterranean burrower", "+100 PTS", goldenText, intelCardSprite);
            CreateIntelRow(intelModalObj, "Row2", 0.56f, 0.72f, goldSprite, "GOLDEN MOLE", "0.50s Active // 1 Fast Tap\nRoyal golden crown bonus", "+300 PTS", warmAmber, intelCardSprite);
            CreateIntelRow(intelModalObj, "Row3", 0.39f, 0.55f, armoredSprite, "HARD HAT MOLE", "1.10s Active // 2 Taps Required\nFirst cracks helmet, second whacks!", "+250 PTS", goldenText, intelCardSprite);
            CreateIntelRow(intelModalObj, "Row4", 0.22f, 0.38f, empSprite, "CARTOON BOMB", "1.00s Active // DO NOT WHACK!\nExplodes and resets combo streak!", "-200 / RESET", hazardRed, intelCardSprite);

            // Multiplier rule summary box
            GameObject comboRuleBox = CreateRuntimePanel(intelModalObj, "ComboRules", new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.20f), panelFrameSprite);
            TextMeshProUGUI comboRules = CreateRuntimeText(comboRuleBox, "Txt", "COMBO: 5 Streak = 2x | 10 Streak = 3x | 15+ Streak = 4x MAX\nMissed Hatch or Bomb Hit Resets Multiplier to 1x", 17, FontStyles.Bold, warmAmber, TextAlignmentOptions.Center);
            comboRules.rectTransform.anchorMin = Vector2.zero;
            comboRules.rectTransform.anchorMax = Vector2.one;
            comboRules.rectTransform.offsetMin = Vector2.zero;
            comboRules.rectTransform.offsetMax = Vector2.zero;

            // Close Intel Button
            GameObject closeIntelBtnObj = CreateRuntimePanel(intelModalObj, "CloseBtn", new Vector2(0.20f, 0.03f), new Vector2(0.80f, 0.10f), btnCyanSprite);
            Button closeIntelBtn = closeIntelBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateRuntimeText(closeIntelBtnObj, "Txt", "GOT IT! RETURN TO GAME", 22, FontStyles.Bold, darkBrown, TextAlignmentOptions.Center);
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;
            closeTxt.rectTransform.offsetMin = Vector2.zero;
            closeTxt.rectTransform.offsetMax = Vector2.zero;

            // ==========================================
            // 5. Ready Countdown Overlay
            // ==========================================
            GameObject readyOverlay = CreateRuntimePanel(safeAreaObj, "ReadyOverlay", new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.60f), panelFrameSprite);
            TextMeshProUGUI readyCountdown = CreateRuntimeText(readyOverlay, "CountdownText", "3", 84, FontStyles.Bold, goldenText, TextAlignmentOptions.Center);
            readyCountdown.rectTransform.anchorMin = Vector2.zero;
            readyCountdown.rectTransform.anchorMax = Vector2.one;
            readyCountdown.rectTransform.offsetMin = Vector2.zero;
            readyCountdown.rectTransform.offsetMax = Vector2.zero;
            RectTransform burstRt = readyOverlay.GetComponent<RectTransform>();
            readyOverlay.SetActive(false);

            // ==========================================
            // 6. Round Over / Game Over Modal
            // ==========================================
            GameObject gameOverModal = CreateRuntimePanel(safeAreaObj, "GameOverModal", new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), intelCardSprite);
            gameOverModal.SetActive(false);

            TextMeshProUGUI goTitle = CreateRuntimeText(gameOverModal, "Title", "ROUND FINISHED!", 36, FontStyles.Bold, goldenText, TextAlignmentOptions.Center);
            goTitle.rectTransform.anchorMin = new Vector2(0, 0.88f);
            goTitle.rectTransform.anchorMax = Vector2.one;
            goTitle.rectTransform.offsetMin = Vector2.zero;
            goTitle.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI rankBadge = CreateRuntimeText(gameOverModal, "RankBadge", "MASTER MOLE WHACKER", 24, FontStyles.Bold, warmAmber, TextAlignmentOptions.Center);
            rankBadge.rectTransform.anchorMin = new Vector2(0, 0.81f);
            rankBadge.rectTransform.anchorMax = new Vector2(1, 0.88f);
            rankBadge.rectTransform.offsetMin = Vector2.zero;
            rankBadge.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI finalScore = CreateRuntimeText(gameOverModal, "FinalScore", "0", 68, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            finalScore.rectTransform.anchorMin = new Vector2(0, 0.67f);
            finalScore.rectTransform.anchorMax = new Vector2(1, 0.81f);
            finalScore.rectTransform.offsetMin = Vector2.zero;
            finalScore.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI newRecord = CreateRuntimeText(gameOverModal, "NewRecord", ">> NEW TOURNAMENT RECORD <<", 20, FontStyles.Bold, warmAmber, TextAlignmentOptions.Center);
            newRecord.rectTransform.anchorMin = new Vector2(0, 0.62f);
            newRecord.rectTransform.anchorMax = new Vector2(1, 0.67f);
            newRecord.rectTransform.offsetMin = Vector2.zero;
            newRecord.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI highScore = CreateRuntimeText(gameOverModal, "HighScore", "RECORD: 0", 22, FontStyles.Normal, goldenSub, TextAlignmentOptions.Center);
            highScore.rectTransform.anchorMin = new Vector2(0, 0.56f);
            highScore.rectTransform.anchorMax = new Vector2(1, 0.62f);
            highScore.rectTransform.offsetMin = Vector2.zero;
            highScore.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI maxStreakText = CreateRuntimeText(gameOverModal, "MaxStreak", "MAX STREAK: 0", 22, FontStyles.Bold, warmAmber, TextAlignmentOptions.Center);
            maxStreakText.rectTransform.anchorMin = new Vector2(0, 0.49f);
            maxStreakText.rectTransform.anchorMax = new Vector2(1, 0.55f);
            maxStreakText.rectTransform.offsetMin = Vector2.zero;
            maxStreakText.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI accuracyText = CreateRuntimeText(gameOverModal, "Accuracy", "ACCURACY: 100%", 22, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            accuracyText.rectTransform.anchorMin = new Vector2(0, 0.42f);
            accuracyText.rectTransform.anchorMax = new Vector2(1, 0.48f);
            accuracyText.rectTransform.offsetMin = Vector2.zero;
            accuracyText.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI statsText = CreateRuntimeText(gameOverModal, "Stats", "WHACKED: 0 | BOMBS: 0", 20, FontStyles.Normal, goldenSub, TextAlignmentOptions.Center);
            statsText.rectTransform.anchorMin = new Vector2(0, 0.35f);
            statsText.rectTransform.anchorMax = new Vector2(1, 0.41f);
            statsText.rectTransform.offsetMin = Vector2.zero;
            statsText.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI finalSeed = CreateRuntimeText(gameOverModal, "FinalSeed", "MATCH SEED: #429182", 20, FontStyles.Normal, goldenSub, TextAlignmentOptions.Center);
            finalSeed.rectTransform.anchorMin = new Vector2(0, 0.28f);
            finalSeed.rectTransform.anchorMax = new Vector2(1, 0.34f);
            finalSeed.rectTransform.offsetMin = Vector2.zero;
            finalSeed.rectTransform.offsetMax = Vector2.zero;

            // 3 Action Buttons on Game Over
            GameObject retrySameBtnObj = CreateRuntimePanel(gameOverModal, "RetrySameBtn", new Vector2(0.12f, 0.19f), new Vector2(0.88f, 0.26f), btnCyanSprite);
            Button retrySameBtn = retrySameBtnObj.AddComponent<Button>();
            TextMeshProUGUI r1Txt = CreateRuntimeText(retrySameBtnObj, "Txt", "PLAY AGAIN (SAME SEED)", 22, FontStyles.Bold, darkBrown, TextAlignmentOptions.Center);
            r1Txt.rectTransform.anchorMin = Vector2.zero;
            r1Txt.rectTransform.anchorMax = Vector2.one;
            r1Txt.rectTransform.offsetMin = Vector2.zero;
            r1Txt.rectTransform.offsetMax = Vector2.zero;

            GameObject retryNewBtnObj = CreateRuntimePanel(gameOverModal, "RetryNewBtn", new Vector2(0.12f, 0.11f), new Vector2(0.88f, 0.18f), btnAmberSprite);
            Button retryNewBtn = retryNewBtnObj.AddComponent<Button>();
            TextMeshProUGUI r2Txt = CreateRuntimeText(retryNewBtnObj, "Txt", "NEW RANDOM MATCH", 22, FontStyles.Bold, darkBrown, TextAlignmentOptions.Center);
            r2Txt.rectTransform.anchorMin = Vector2.zero;
            r2Txt.rectTransform.anchorMax = Vector2.one;
            r2Txt.rectTransform.offsetMin = Vector2.zero;
            r2Txt.rectTransform.offsetMax = Vector2.zero;

            GameObject returnMenuBtnObj = CreateRuntimePanel(gameOverModal, "ReturnMenuBtn", new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.10f), panelFrameSprite);
            Button returnMenuBtn = returnMenuBtnObj.AddComponent<Button>();
            TextMeshProUGUI r3Txt = CreateRuntimeText(returnMenuBtnObj, "Txt", "MAIN MENU", 20, FontStyles.Bold, goldenSub, TextAlignmentOptions.Center);
            r3Txt.rectTransform.anchorMin = Vector2.zero;
            r3Txt.rectTransform.anchorMax = Vector2.one;
            r3Txt.rectTransform.offsetMin = Vector2.zero;
            r3Txt.rectTransform.offsetMax = Vector2.zero;

            // ==========================================
            // 7. Effect Pool Manager
            // ==========================================
            if (EffectPoolManager.Instance == null)
            {
                GameObject poolObj = new GameObject("EffectPoolManager", typeof(RectTransform), typeof(EffectPoolManager));
                poolObj.transform.SetParent(canvasObj.transform, false);
                RectTransform poolRt = poolObj.GetComponent<RectTransform>();
                poolRt.anchorMin = Vector2.zero;
                poolRt.anchorMax = Vector2.one;
                poolRt.offsetMin = Vector2.zero;
                poolRt.offsetMax = Vector2.zero;
            }

            // ==========================================
            // 8. Wire CyberHUD References
            // ==========================================
            hud = canvasObj.AddComponent<CyberHUD>();
            hud.ConfigureAudioSprites(soundOnSprite, soundOffSprite);

            // Reflection-based private field wiring for runtime dynamic generation
            SetPrivateField(hud, "mainMenuPanel", menuObj);
            SetPrivateField(hud, "heroTitleTransform", titleObj.GetComponent<RectTransform>());
            SetPrivateField(hud, "menuHighScoreText", recordText);
            SetPrivateField(hud, "menuSeedInput", seedInput);
            SetPrivateField(hud, "menuStartButton", startBtn);
            SetPrivateField(hud, "menuIntelButton", intelBtn);
            SetPrivateField(hud, "menuRandomSeedButton", rndBtn);
            SetPrivateField(hud, "audioToggleButton", audioToggleBtn);
            SetPrivateField(hud, "audioToggleIcon", audioIcon);
            SetPrivateField(hud, "soundOnSprite", soundOnSprite);
            SetPrivateField(hud, "soundOffSprite", soundOffSprite);

            SetPrivateField(hud, "targetIntelModal", intelModalObj);
            SetPrivateField(hud, "closeIntelButton", closeIntelBtn);

            SetPrivateField(hud, "inGameHeader", headerObj);
            SetPrivateField(hud, "abortButton", abortBtn);
            SetPrivateField(hud, "inGameAudioToggleButton", igAudioBtn);
            SetPrivateField(hud, "inGameAudioToggleIcon", igAudioIcon);
            SetPrivateField(hud, "scoreText", scoreText);
            SetPrivateField(hud, "multiplierText", multText);
            SetPrivateField(hud, "comboStreakFill", comboFillImg);
            SetPrivateField(hud, "comboStreakCountText", streakText);
            SetPrivateField(hud, "multiplierBadgeTransform", badgeRt);
            SetPrivateField(hud, "timerText", timerText);
            SetPrivateField(hud, "inGameSeedText", igSeed);

            SetPrivateField(hud, "readyOverlay", readyOverlay);
            SetPrivateField(hud, "readyCountdownText", readyCountdown);
            SetPrivateField(hud, "countdownBurstTransform", burstRt);

            SetPrivateField(hud, "gameOverModal", gameOverModal);
            SetPrivateField(hud, "rankBadgeText", rankBadge);
            SetPrivateField(hud, "finalScoreText", finalScore);
            SetPrivateField(hud, "newRecordBadge", newRecord);
            SetPrivateField(hud, "highScoreText", highScore);
            SetPrivateField(hud, "maxStreakText", maxStreakText);
            SetPrivateField(hud, "accuracyText", accuracyText);
            SetPrivateField(hud, "destroyedStatsText", statsText);
            SetPrivateField(hud, "finalSeedText", finalSeed);
            SetPrivateField(hud, "restartSameSeedButton", retrySameBtn);
            SetPrivateField(hud, "restartNewSeedButton", retryNewBtn);
            SetPrivateField(hud, "returnToMenuButton", returnMenuBtn);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        private static void CreatePreviewMound(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite moundSprite, Sprite entitySprite, float entityOffsetY, float entityScale)
        {
            GameObject moundContainer = new GameObject(name, typeof(RectTransform));
            moundContainer.transform.SetParent(parent.transform, false);
            RectTransform mcRt = moundContainer.GetComponent<RectTransform>();
            mcRt.anchorMin = anchorMin;
            mcRt.anchorMax = anchorMax;
            mcRt.offsetMin = Vector2.zero;
            mcRt.offsetMax = Vector2.zero;

            // Masked hole cavity
            GameObject maskObj = new GameObject("MaskContainer", typeof(RectTransform), typeof(RectMask2D));
            maskObj.transform.SetParent(moundContainer.transform, false);
            RectTransform maskRt = maskObj.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0.08f, 0.22f);
            maskRt.anchorMax = new Vector2(0.92f, 0.98f);
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

            // Peeking character entity (Mole or Bomb)
            GameObject entityObj = new GameObject("Entity", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            entityObj.transform.SetParent(maskObj.transform, false);
            RectTransform eRt = entityObj.GetComponent<RectTransform>();
            eRt.anchorMin = new Vector2(0.5f, 0f);
            eRt.anchorMax = new Vector2(0.5f, 0f);
            eRt.sizeDelta = new Vector2(210f * entityScale, 210f * entityScale);
            eRt.anchoredPosition = new Vector2(0f, entityOffsetY);
            Image eImg = entityObj.GetComponent<Image>();
            eImg.sprite = entitySprite;
            eImg.preserveAspect = true;
            eImg.raycastTarget = false;

            // Dirt Mound front rim / clods
            GameObject ringObj = new GameObject("DirtMound", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObj.transform.SetParent(moundContainer.transform, false);
            RectTransform ringRt = ringObj.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero;
            ringRt.offsetMax = Vector2.zero;
            Image ringImg = ringObj.GetComponent<Image>();
            ringImg.sprite = moundSprite;
            ringImg.preserveAspect = true;
            ringImg.raycastTarget = false;
        }

        private static void CreateIntelRow(GameObject parent, string name, float anchorMinY, float anchorMaxY, Sprite sprite, string title, string desc, string pts, Color ptsColor, Sprite bgSprite)
        {
            GameObject rowObj = CreateRuntimePanel(parent, name, new Vector2(0.04f, anchorMinY), new Vector2(0.96f, anchorMaxY), bgSprite);

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.03f, 0.1f);
            iconRt.anchorMax = new Vector2(0.22f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image img = iconObj.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;

            // Details
            TextMeshProUGUI titleTmp = CreateRuntimeText(rowObj, "Title", title, 20, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            titleTmp.rectTransform.anchorMin = new Vector2(0.25f, 0.50f);
            titleTmp.rectTransform.anchorMax = new Vector2(0.70f, 0.92f);
            titleTmp.rectTransform.offsetMin = Vector2.zero;
            titleTmp.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI descTmp = CreateRuntimeText(rowObj, "Desc", desc, 16, FontStyles.Normal, new Color(0.7f, 0.8f, 0.9f), TextAlignmentOptions.TopLeft);
            descTmp.rectTransform.anchorMin = new Vector2(0.25f, 0.08f);
            descTmp.rectTransform.anchorMax = new Vector2(0.70f, 0.50f);
            descTmp.rectTransform.offsetMin = Vector2.zero;
            descTmp.rectTransform.offsetMax = Vector2.zero;

            // Score Badge
            TextMeshProUGUI ptsTmp = CreateRuntimeText(rowObj, "Pts", pts, 22, FontStyles.Bold, ptsColor, TextAlignmentOptions.MidlineRight);
            ptsTmp.rectTransform.anchorMin = new Vector2(0.70f, 0.1f);
            ptsTmp.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            ptsTmp.rectTransform.offsetMin = Vector2.zero;
            ptsTmp.rectTransform.offsetMax = Vector2.zero;
        }

        private static Sprite LoadArtSprite(string filename)
        {
            string path = System.IO.Path.Combine(Application.dataPath, "CyberWhack/Art", filename);
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            return null;
        }

        private static GameObject CreateRuntimePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = obj.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
            }
            img.color = Color.white;
            return obj;
        }

        private static TextMeshProUGUI CreateRuntimeText(GameObject parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent.transform, false);
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
