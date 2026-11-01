using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("button")]
        private Button _button;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("face")]
        private Image _face;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("label")]
        private TMP_Text _label;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("hidden")]
        private Sprite _hidden;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("gem")]
        private Sprite _gem;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("mine")]
        private Sprite _mine;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("treasure")]
        private Image _treasure;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("gemIcon")]
        private Sprite _gemIcon;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("mineIcon")]
        private Sprite _mineIcon;
        public Sprite HiddenSprite => _hidden;
        public Material GemMaterial => _gemMaterial;
        public RevealStyle ActiveRevealStyle => _revealStyle;

        private Material _gemMaterial;
        private RevealStyle _revealStyle;
        private Color _effectColor = new Color(.9f, 1, .6f);
        private RevealEffectPlayer _revealEffect;
        public void ApplyCosmetics(Sprite tile, Sprite gemSprite, Material material, RevealStyle style, Color effectColor)
        {
            _hidden = tile;
            _gemIcon = gemSprite;
            _gemMaterial = material;
            _revealStyle = style;
            _effectColor = effectColor;
            if (!_revealEffect)
            {
                _revealEffect = gameObject.AddComponent<RevealEffectPlayer>();
            }

            _face.sprite = _hidden;
        }

        private Action<int> _onClick;
        private int _index;
        private Coroutine _pop;
        private GameUI _ui;
        public void Bind(int i, Action<int> callback)
        {
            _index = i;
            _onClick = callback;
            _ui = GetComponentInParent<GameUI>();
            _button.onClick.RemoveListener(Click);
            _button.onClick.AddListener(Click);
        }

        private void Click() => _onClick?.Invoke(_index);
        public void Show(bool revealed, bool isMine, bool active, bool animate, bool triggered = false, bool unselected = false)
        {
            if (!revealed && !active)
            {
                StopAllCoroutines();
                _pop = null;
                transform.localScale = Vector3.one;
            }

            _button.interactable = active && !revealed;
            _treasure.gameObject.SetActive(revealed);
            _treasure.sprite = isMine ? _mineIcon : _gemIcon;
            _treasure.material = isMine ? null : _gemMaterial;
            _treasure.color = unselected && !isMine ? new Color(.60f, .70f, .75f) : Color.white;
            _face.sprite = revealed ? (isMine ? _mine : _gem) : _hidden;
            _face.color = triggered ? new Color(1, .62f, .4f) : unselected && !isMine ? new Color(.53f, .65f, .68f) : Color.white;
            _label.text = triggered ? "HIT" : "";
            _label.fontSize = 18;
            _label.alignment = TextAlignmentOptions.Bottom;
            if (animate && !isMine && _revealEffect)
            {
                _revealEffect.Play(_revealStyle, _effectColor, _ui && _ui.ReducedEffects);
            }

            if (animate && (!_ui || !_ui.ReducedEffects))
            {
                if (_pop != null)
                {
                    StopCoroutine(_pop);
                }

                _pop = StartCoroutine(Pop(isMine));
            }
        }

        private IEnumerator Pop(bool isMine)
        {
            var sparks = new RectTransform[isMine ? 8 : 0];
            for (int i = 0; i < sparks.Length; i++)
            {
                var go = new GameObject("Reveal sparkle", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                sparks[i] = (RectTransform)go.transform;
                sparks[i].sizeDelta = new Vector2(isMine ? 12 : 5, isMine ? 12 : 13);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = isMine ? new Color(1, .5f, .12f) : new Color(.9f, 1, .6f);
                Destroy(go, .55f);
            }

            for (float t = 0; t < .5f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1 + Mathf.Sin(t / .5f * Mathf.PI) * (isMine ? .2f : .12f));
                for (int i = 0; i < sparks.Length; i++)
                {
                    if (sparks[i])
                    {
                        float a = i * Mathf.PI / 4;
                        sparks[i].anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (20 + t * 100);
                        var image = sparks[i].GetComponent<Image>();
                        var c = image.color;
                        c.a = 1 - t * 2;
                        image.color = c;
                    }
                }

                yield return null;
            }

            transform.localScale = Vector3.one;
            _pop = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _pop = null;
            transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Click);
            _onClick = null;
        }
    }
}
