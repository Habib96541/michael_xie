using UnityEngine;

namespace MineGrid
{
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("source")]
        private AudioSource _source;
        private AudioClip _gem;
        private AudioClip _mine;
        private AudioClip _reward;
        private AudioClip _click;
        public void Initialize(bool muted)
        {
            _source.mute = muted;
            _gem = Tone(660, .14f);
            _mine = Tone(90, .45f);
            _reward = Tone(990, .35f);
            _click = Tone(440, .055f);
        }

        private AudioClip Tone(float frequency, float length)
        {
            int n = (int)(44100 * length);
            var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                samples[i] = Mathf.Sin(i * frequency * 2 * Mathf.PI / 44100) * .18f * (1f - (float)i / n);
            }

            var clip = AudioClip.Create("MineGrid tone", n, 1, 44100, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Play(int kind) => _source.PlayOneShot(kind == 0 ? _gem : kind == 1 ? _mine : kind == 3 ? _click : _reward);
        public void Mute(bool value) => _source.mute = value;
        private void OnDestroy()
        {
            Destroy(_gem);
            Destroy(_mine);
            Destroy(_reward);
            Destroy(_click);
        }
    }
}
