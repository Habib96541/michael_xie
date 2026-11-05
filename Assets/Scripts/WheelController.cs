using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinTheWheel
{
    public sealed class WheelController : MonoBehaviour
    {
        [SerializeField] private RectTransform wheel;
        [SerializeField] private Button spinButton;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text[] segmentLabels;
        [SerializeField, Min(0.1f)] private float spinDuration = 4.5f;
        [SerializeField, Range(3, 10)] private int fullTurns = 6;
        [SerializeField] private float[] multipliers =
        {
            0.1f, 5f, 0.5f, 15f, 1f, 3f, 0.2f, 25f, 2f, 8f,
            0.3f, 10f, 1.5f, 50f, 0.4f, 4f, 0.8f, 20f, 0.6f, 2.5f
        };
        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip spinClip;
        [SerializeField] private AudioClip tickClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Min(0f)] private float minTickInterval = 0.045f;

        private float elapsedTime;
        private float startAngle;
        private float endAngle;
        private int targetIndex;
        private float lastTickTime = float.NegativeInfinity;
        private int lastTickSegment = -1;

        public bool IsSpinning { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public float ResultMultiplier => SelectedIndex < 0 ? 0f : multipliers[SelectedIndex];

        private void Awake()
        {
            if (!HasValidConfiguration())
            {
                Debug.LogError("The wheel needs its UI references and 20 distinct multipliers from 0.1 to 50.", this);
                if (spinButton != null)
                    spinButton.interactable = false;
                enabled = false;
                return;
            }

            for (int index = 0; index < multipliers.Length; index++)
                segmentLabels[index].text = FormatMultiplier(multipliers[index]);

            resultLabel.text = "--";
            statusLabel.text = "Press SPIN to reveal your multiplier.";
            buttonLabel.text = "SPIN";
            spinButton.interactable = true;

            EnsureAudioSource();
        }

        private void EnsureAudioSource()
        {
            if (sfxSource == null)
                sfxSource = GetComponent<AudioSource>();

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }

        private void OnEnable()
        {
            spinButton.onClick.AddListener(Spin);
        }

        private void OnDisable()
        {
            if (spinButton != null)
                spinButton.onClick.RemoveListener(Spin);

            if (IsSpinning)
                CompleteSpin(false);
        }

        private void Update()
        {
            if (!IsSpinning)
                return;

            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / Mathf.Max(0.1f, spinDuration));
            float easedProgress = 1f - Mathf.Pow(1f - progress, 4f);
            SetWheelAngle(Mathf.LerpUnclamped(startAngle, endAngle, easedProgress));
            PlayTick(progress);

            if (progress >= 1f)
                CompleteSpin();
        }

        public void Spin()
        {
            if (!isActiveAndEnabled || IsSpinning)
                return;

            targetIndex = Random.Range(0, multipliers.Length);
            startAngle = wheel.localEulerAngles.z;
            lastTickSegment = SegmentIndexAt(startAngle);
            lastTickTime = float.NegativeInfinity;
            PlayOneShot(clickClip, 0.7f);
            PlayOneShot(spinClip, 0.5f);
            float targetAngle = targetIndex * (360f / multipliers.Length);
            float clockwiseDistance = Mathf.Repeat(startAngle - targetAngle, 360f);
            endAngle = startAngle - Mathf.Max(3, fullTurns) * 360f - clockwiseDistance;
            elapsedTime = 0f;
            IsSpinning = true;
            spinButton.interactable = false;
            buttonLabel.text = "SPINNING...";
            resultLabel.text = "--";
            statusLabel.text = "The wheel is spinning...";
        }

        private void CompleteSpin(bool playSound = true)
        {
            SetWheelAngle(targetIndex * (360f / multipliers.Length));
            SelectedIndex = targetIndex;
            IsSpinning = false;
            resultLabel.text = FormatMultiplier(ResultMultiplier);
            statusLabel.text = "Your multiplier is ready. Spin again!";
            buttonLabel.text = "SPIN AGAIN";
            spinButton.interactable = true;

            if (playSound)
                PlayOneShot(winClip, 0.9f);
        }

        private void PlayTick(float progress)
        {
            if (sfxSource == null || tickClip == null)
                return;

            int segment = SegmentIndexAt(wheel.localEulerAngles.z);
            if (segment == lastTickSegment)
                return;

            lastTickSegment = segment;
            float now = Time.unscaledTime;
            if (now - lastTickTime < minTickInterval)
                return;

            lastTickTime = now;
            sfxSource.pitch = Mathf.Lerp(1.3f, 1f, progress);
            sfxSource.PlayOneShot(tickClip, 0.55f * sfxVolume);
        }

        private void PlayOneShot(AudioClip clip, float clipVolume)
        {
            if (sfxSource == null || clip == null)
                return;

            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, clipVolume * sfxVolume);
        }

        private int SegmentIndexAt(float angle)
        {
            float step = 360f / multipliers.Length;
            return Mathf.Abs(Mathf.RoundToInt(angle / step)) % multipliers.Length;
        }

        private void SetWheelAngle(float angle)
        {
            wheel.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private bool HasValidConfiguration()
        {
            if (wheel == null || spinButton == null || buttonLabel == null ||
                resultLabel == null || statusLabel == null || multipliers == null ||
                multipliers.Length != 20 || segmentLabels == null || segmentLabels.Length != 20)
                return false;

            if (!wheel.TryGetComponent(out WheelGraphic graphic) || graphic.SegmentCount != multipliers.Length)
                return false;

            float minimum = float.MaxValue;
            float maximum = float.MinValue;
            for (int index = 0; index < multipliers.Length; index++)
            {
                float multiplier = multipliers[index];
                if (segmentLabels[index] == null || float.IsNaN(multiplier) ||
                    multiplier < 0.1f || multiplier > 50f)
                    return false;

                minimum = Mathf.Min(minimum, multiplier);
                maximum = Mathf.Max(maximum, multiplier);
                for (int previous = 0; previous < index; previous++)
                    if (Mathf.Approximately(multiplier, multipliers[previous]))
                        return false;
            }

            return Mathf.Approximately(minimum, 0.1f) && Mathf.Approximately(maximum, 50f);
        }

        public static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        }
    }
}
