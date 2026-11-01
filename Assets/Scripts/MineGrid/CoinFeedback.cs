using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class CoinFeedback : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("wallet")]
        private RectTransform _wallet;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("layer")]
        private RectTransform _layer;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("coin")]
        private Sprite _coin;
        private GameUI _ui;
        private Coroutine _pulse;
        private void Awake() => _ui = GetComponent<GameUI>();
        public void Reward()
        {
            if (_ui.ReducedEffects)
            {
                return;
            }

            if (_pulse != null)
            {
                StopCoroutine(_pulse);
            }

            _pulse = StartCoroutine(Animate(false));
        }

        public void Spend()
        {
            if (_ui.ReducedEffects)
            {
                return;
            }

            if (_pulse != null)
            {
                StopCoroutine(_pulse);
            }

            _pulse = StartCoroutine(Animate(true));
        }

        private IEnumerator Animate(bool spending)
        {
            var pieces = new RectTransform[7];
            for (int i = 0; i < pieces.Length; i++)
            {
                var go = new GameObject("Collected coin", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_layer, false);
                Destroy(go, 1f);
                pieces[i] = (RectTransform)go.transform;
                pieces[i].sizeDelta = Vector2.one * 36;
                var image = go.GetComponent<Image>();
                image.sprite = _coin;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            Vector3 destination = _wallet.position;
            for (float t = 0; t < .65f; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    float k = Mathf.Clamp01((t - i * .025f) / .45f);
                    Vector3 from = _layer.TransformPoint(new Vector3((i - 3) * 20, -70 - Mathf.Abs(i - 3) * 12, 0));
                    pieces[i].position = spending ? Vector3.Lerp(destination, from, k * k) : Vector3.Lerp(from, destination, k * k);
                    pieces[i].localScale = Vector3.one * Mathf.Lerp(1, .5f, k);
                }

                _wallet.localScale = Vector3.one * (1 + Mathf.Sin(t / .65f * Mathf.PI) * .07f);
                yield return null;
            }

            foreach (var piece in pieces)
            {
                if (piece)
                {
                    Destroy(piece.gameObject);
                }
            }

            _wallet.localScale = Vector3.one;
            _pulse = null;
        }

        private void OnDisable()
        {
            _wallet.localScale = Vector3.one;
        }
    }
}
