using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlapDash
{
    public sealed class VisualEffectsSettings : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject panel;
        public float Intensity { get; private set; } = 1f;

        private void OnEnable() => slider.onValueChanged.AddListener(SetIntensity);
        private void OnDisable() => slider.onValueChanged.RemoveListener(SetIntensity);
        private void Start() => SetIntensity(Intensity);
        private void LateUpdate() => panel.SetActive(!round.IsPlaying);

        public void SetIntensity(float value)
        {
            Intensity = Mathf.Clamp01(value);
            slider.SetValueWithoutNotify(Intensity);
            label.text = $"VISUAL ENERGY  {Mathf.RoundToInt(Intensity * 100f)}%";
        }
    }
}
