using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceGame.Loading
{
    public sealed class DiceLoadingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private TextMeshProUGUI statusMessageText;
        [SerializeField] private TextMeshProUGUI tipsText;
        [SerializeField] private RectTransform animatedDiceIcon;
        [SerializeField] private RectTransform animatedDiceIconRight;
        [SerializeField] private TextMeshProUGUI diceLeftPipText;
        [SerializeField] private TextMeshProUGUI diceRightPipText;
        [SerializeField] private Button buttonStart;
        [SerializeField] private CanvasGroup startButtonCanvasGroup;
        [SerializeField] private CanvasGroup screenFadeCanvasGroup;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform centerContent;
        [SerializeField] private RectTransform safeAreaRoot;

        [Header("Configuration")]
        [SerializeField] private string targetSceneName = "DiceOverdrive";
        [SerializeField] private float minLoadingDuration = 1.6f;
        [SerializeField] private bool autoStartWhenReady = false;

        private AsyncOperation _asyncOperation;
        private float _loadTimer;
        private bool _isReadyToStart;
        private bool _isTransitioning;
        private int _lastPercentInt = -1;
        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private Rect _lastSafeArea;

        private float _tipTimer;
        private int _currentTipIndex;
        private static readonly string[] GameplayTips =
        {
            "TIP: Land in the Golden Crit Zone for a massive 3.0x Jackpot Payout!",
            "TIP: Build a 3+ Win Streak to trigger explosive Combo Fever multipliers!",
            "TIP: Tap [25%], [50%], [75%], [90%] for 1-touch rapid odds betting!",
            "TIP: Press SPACE to roll and A to toggle Auto-Roll on desktop keyboard!",
            "WEBGL READY: Tap 'ENTER THE ARENA' to initialize crystal-clear browser audio!"
        };

        private static readonly string[] StatusSteps =
        {
            "INITIALIZING GRAPHICS ENGINE...",
            "SYNTHESIZING PROCEDURAL AUDIO...",
            "CONFIGURING PROVABLY FAIR RNG...",
            "CALIBRATING 20s BLITZ ARENA...",
            "READY! TAP TO ENTER THE ARENA"
        };

        private static readonly string[] DigitStrings = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private readonly char[] _percentBuffer = new char[5];
        private float _pipCycleTimer;
        private AudioClip _unlockClip;
        private AudioSource _audioSource;

        private void Awake()
        {
            ApplyResponsiveLayout();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            CreateUnlockSound();

            if (buttonStart != null)
            {
                buttonStart.gameObject.SetActive(false);
                buttonStart.onClick.AddListener(OnStartButtonClicked);
            }

            if (startButtonCanvasGroup != null)
            {
                startButtonCanvasGroup.alpha = 0.0f;
                startButtonCanvasGroup.interactable = false;
                startButtonCanvasGroup.blocksRaycasts = false;
            }

            if (screenFadeCanvasGroup != null)
            {
                screenFadeCanvasGroup.alpha = 0.0f;
            }

            if (progressBarFill != null) progressBarFill.fillAmount = 0.0f;

            if (tipsText != null)
            {
                tipsText.text = GameplayTips[0];
            }
        }

        private void Start()
        {
            StartCoroutine(LoadTargetSceneRoutine());
        }

        private void OnDestroy()
        {
            if (buttonStart != null)
            {
                buttonStart.onClick.RemoveListener(OnStartButtonClicked);
            }

            if (_unlockClip != null)
            {
                Destroy(_unlockClip);
            }
        }

        private void Update()
        {
            int w = Screen.width;
            int h = Screen.height;
            Rect sa = Screen.safeArea;
            if (w != _lastWidth || h != _lastHeight || sa != _lastSafeArea)
            {
                _lastWidth = w;
                _lastHeight = h;
                _lastSafeArea = sa;
                ApplyResponsiveLayout();
            }

            if (animatedDiceIcon != null)
            {
                animatedDiceIcon.Rotate(0.0f, 0.0f, -140.0f * Time.deltaTime);
                float bounce = 1.0f + Mathf.Sin(Time.time * 6.0f) * 0.08f;
                animatedDiceIcon.localScale = new Vector3(bounce, bounce, 1.0f);
            }

            if (animatedDiceIconRight != null)
            {
                animatedDiceIconRight.Rotate(0.0f, 0.0f, 140.0f * Time.deltaTime);
                float bounceR = 1.0f + Mathf.Cos(Time.time * 6.0f) * 0.08f;
                animatedDiceIconRight.localScale = new Vector3(bounceR, bounceR, 1.0f);
            }

            _pipCycleTimer += Time.deltaTime;
            if (_pipCycleTimer >= 0.12f)
            {
                _pipCycleTimer = 0.0f;
                int r1 = UnityEngine.Random.Range(0, 10);
                int r2 = UnityEngine.Random.Range(0, 10);
                if (diceLeftPipText != null) diceLeftPipText.text = DigitStrings[r1];
                if (diceRightPipText != null) diceRightPipText.text = DigitStrings[r2];
            }

            _tipTimer += Time.deltaTime;
            if (_tipTimer >= 2.2f)
            {
                _tipTimer = 0.0f;
                _currentTipIndex = (_currentTipIndex + 1) % GameplayTips.Length;
                if (tipsText != null)
                {
                    tipsText.text = GameplayTips[_currentTipIndex];
                }
            }

            if (_isReadyToStart && startButtonCanvasGroup != null && !_isTransitioning)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 5.0f) * 0.05f;
                buttonStart.transform.localScale = new Vector3(pulse, pulse, 1.0f);
            }
        }

        private void CreateUnlockSound()
        {
            const int sampleRate = 44100;
            float duration = 0.32f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(440.0f, 880.0f, t);
                float env = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(2.0f * Mathf.PI * freq * ((float)i / sampleRate)) * env * 0.5f;
            }

            _unlockClip = AudioClip.Create("UnlockChime", totalSamples, 1, sampleRate, false);
            _unlockClip.SetData(samples, 0);
        }

        private IEnumerator LoadTargetSceneRoutine()
        {
            _asyncOperation = SceneManager.LoadSceneAsync(targetSceneName);
            if (_asyncOperation == null)
            {
                _asyncOperation = SceneManager.LoadSceneAsync(1);
            }

            if (_asyncOperation != null)
            {
                _asyncOperation.allowSceneActivation = false;
            }

            float displayProgress = 0.0f;

            while (displayProgress < 1.0f)
            {
                _loadTimer += Time.unscaledDeltaTime;
                float timeProgress = Mathf.Clamp01(_loadTimer / minLoadingDuration);

                float asyncProgress = 0.0f;
                if (_asyncOperation != null)
                {
                    asyncProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
                }

                float targetProgress = Mathf.Min(timeProgress, (_asyncOperation != null) ? asyncProgress : 1.0f);
                displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 1.5f);

                UpdateProgressUI(displayProgress);
                yield return null;
            }

            UpdateProgressUI(1.0f);
            _isReadyToStart = true;

            if (autoStartWhenReady)
            {
                StartGame();
            }
            else
            {
                if (buttonStart != null) buttonStart.gameObject.SetActive(true);
                if (startButtonCanvasGroup != null)
                {
                    float fade = 0.0f;
                    while (fade < 1.0f)
                    {
                        fade += Time.unscaledDeltaTime * 3.5f;
                        startButtonCanvasGroup.alpha = fade;
                        yield return null;
                    }
                    startButtonCanvasGroup.interactable = true;
                    startButtonCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        private void UpdateProgressUI(float progress01)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progress01;
            }

            int percent = Mathf.Clamp((int)(progress01 * 100.0f), 0, 100);
            if (percent != _lastPercentInt)
            {
                _lastPercentInt = percent;
                FormatPercentBuffer(percent);
                if (progressPercentText != null)
                {
                    progressPercentText.SetText(_percentBuffer, 0, (percent == 100) ? 4 : (percent >= 10 ? 3 : 2));
                }
            }

            if (statusMessageText != null)
            {
                int stepIndex = Mathf.Clamp((int)(progress01 * (StatusSteps.Length - 1)), 0, StatusSteps.Length - 1);
                statusMessageText.text = StatusSteps[stepIndex];
            }
        }

        private void FormatPercentBuffer(int p)
        {
            if (p >= 100)
            {
                _percentBuffer[0] = '1';
                _percentBuffer[1] = '0';
                _percentBuffer[2] = '0';
                _percentBuffer[3] = '%';
            }
            else if (p >= 10)
            {
                _percentBuffer[0] = (char)('0' + (p / 10));
                _percentBuffer[1] = (char)('0' + (p % 10));
                _percentBuffer[2] = '%';
            }
            else
            {
                _percentBuffer[0] = (char)('0' + p);
                _percentBuffer[1] = '%';
            }
        }

        public void OnStartButtonClicked()
        {
            if (!_isReadyToStart || _isTransitioning) return;
            StartGame();
        }

        private void StartGame()
        {
            _isTransitioning = true;
            if (startButtonCanvasGroup != null)
            {
                startButtonCanvasGroup.interactable = false;
            }

            AudioListener.pause = false;
            if (_audioSource != null && _unlockClip != null)
            {
                _audioSource.PlayOneShot(_unlockClip, 0.85f);
            }

            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            if (screenFadeCanvasGroup != null)
            {
                float fade = 0.0f;
                while (fade < 1.0f)
                {
                    fade += Time.unscaledDeltaTime * 4.0f;
                    screenFadeCanvasGroup.alpha = fade;
                    yield return null;
                }
            }

            if (_asyncOperation != null)
            {
                _asyncOperation.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }

        private void ApplyResponsiveLayout()
        {
            UpdateOrientationScaler();

            if (Screen.width <= 0 || Screen.height <= 0) return;

            if (safeAreaRoot != null)
            {
                Rect sa = Screen.safeArea;
                if (sa.width <= 0f || sa.height <= 0f) sa = new Rect(0f, 0f, Screen.width, Screen.height);
                Vector2 min = sa.position;
                Vector2 max = min + sa.size;
                min.x /= Screen.width;
                min.y /= Screen.height;
                max.x /= Screen.width;
                max.y /= Screen.height;

                safeAreaRoot.anchorMin = min;
                safeAreaRoot.anchorMax = max;
                safeAreaRoot.offsetMin = Vector2.zero;
                safeAreaRoot.offsetMax = Vector2.zero;
            }

            if (centerContent == null) return;

            const float boxW = 900f;
            const float boxH = 640f;
            centerContent.anchorMin = centerContent.anchorMax = new Vector2(0.5f, 0.5f);
            centerContent.pivot = new Vector2(0.5f, 0.5f);
            centerContent.anchoredPosition = Vector2.zero;
            centerContent.sizeDelta = new Vector2(boxW, boxH);

            bool isPortrait = Screen.width < Screen.height;
            float screenW = Screen.width;
            float screenH = Screen.height;
            Vector2 reference = isPortrait ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
            float match = isPortrait ? 0f : 1f;
            float logWidth = Mathf.Log(screenW / reference.x, 2f);
            float logHeight = Mathf.Log(screenH / reference.y, 2f);
            float canvasScale = Mathf.Max(0.0001f, Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match)));
            Rect safe = Screen.safeArea;
            if (safe.width <= 1f || safe.height <= 1f) safe = new Rect(0f, 0f, screenW, screenH);
            float safeW = (safe.width / screenW) * (screenW / canvasScale);
            float safeH = (safe.height / screenH) * (screenH / canvasScale);
            float fitted = Mathf.Min((safeW - 36f) / boxW, (safeH - 36f) / boxH);
            fitted = Mathf.Clamp(fitted, 0.62f, 1.28f);
            centerContent.localScale = new Vector3(fitted, fitted, 1f);

            PlaceLoadingPiece("AnimatedDiceLeft", new Vector2(-78f, 228f), new Vector2(108f, 108f));
            PlaceLoadingPiece("AnimatedDiceRight", new Vector2(78f, 228f), new Vector2(108f, 108f));
            PlaceLoadingPiece("GameTitle", new Vector2(0f, 118f), new Vector2(820f, 60f));
            PlaceLoadingPiece("SubtitleBadge", new Vector2(0f, 54f), new Vector2(340f, 34f));
            PlaceLoadingPiece("ProgressBarFrame", new Vector2(0f, 0f), new Vector2(640f, 26f));
            PlaceLoadingPiece("PercentText", new Vector2(0f, -40f), new Vector2(220f, 28f));
            PlaceLoadingPiece("StatusText", new Vector2(0f, -82f), new Vector2(760f, 28f));
            PlaceLoadingPiece("TipsBox", new Vector2(0f, -132f), new Vector2(800f, 46f));
            PlaceLoadingPiece("StartButton", new Vector2(0f, -208f), new Vector2(360f, 64f));

            FitLoadingText("GameTitle", 52f, 28f, false);
            FitLoadingText("SubText", 20f, 14f, false);
            FitLoadingText("PercentText", 26f, 16f, false);
            FitLoadingText("StatusText", 18f, 13f, false);
            FitLoadingText("TipsText", 16f, 12f, true);
            FitLoadingText("StartText", 26f, 16f, false);
            FitLoadingText("DicePipLeft", 48f, 28f, false);
            FitLoadingText("DicePipRight", 48f, 28f, false);
        }

        private void PlaceLoadingPiece(string childName, Vector2 position, Vector2 size)
        {
            if (centerContent == null) return;
            RectTransform piece = centerContent.Find(childName) as RectTransform;
            if (piece == null) return;
            piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f);
            piece.pivot = new Vector2(0.5f, 0.5f);
            piece.anchoredPosition = position;
            piece.sizeDelta = size;
        }

        private void FitLoadingText(string childName, float size, float minimum, bool wrap)
        {
            if (centerContent == null) return;
            Transform piece = centerContent.Find(childName);
            if (piece == null)
            {
                TextMeshProUGUI[] labels = centerContent.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i].name == childName)
                    {
                        piece = labels[i].transform;
                        break;
                    }
                }
            }

            TextMeshProUGUI label = piece != null ? piece.GetComponent<TextMeshProUGUI>() : null;
            if (label == null) return;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            float max = Mathf.Max(minimum, size);
            label.fontSize = max;
            label.fontSizeMin = minimum;
            label.fontSizeMax = max;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Ellipsis;
        }

        private void UpdateOrientationScaler()
        {
            if (canvasScaler == null) return;

            if (Screen.width < Screen.height)
            {
                canvasScaler.referenceResolution = new Vector2(1080, 1920);
                canvasScaler.matchWidthOrHeight = 0.0f;
            }
            else
            {
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.matchWidthOrHeight = 1.0f;
            }
        }
    }
}

