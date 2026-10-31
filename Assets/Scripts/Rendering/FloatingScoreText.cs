using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DropStack.Rendering
{
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private Text textComponent;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (textComponent == null) textComponent = GetComponent<Text>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Play(string message, Color color, Vector2 startPosition)
        {
            if (textComponent == null) textComponent = GetComponent<Text>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            textComponent.text = message;
            textComponent.color = color;

            RectTransform rt = GetComponent<RectTransform>();
            rt.anchoredPosition = startPosition;

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            RectTransform rt = GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            Vector2 targetPos = startPos + new Vector2(0f, 60f);

            float duration = 1.0f;
            float elapsed = 0f;

            rt.localScale = Vector3.one * 0.4f;
            canvasGroup.alpha = 1.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Punch scale up then settle
                float scale;
                if (t < 0.2f)
                {
                    scale = Mathf.Lerp(0.4f, 1.35f, t / 0.2f);
                }
                else if (t < 0.35f)
                {
                    scale = Mathf.Lerp(1.35f, 1.0f, (t - 0.2f) / 0.15f);
                }
                else
                {
                    scale = 1.0f;
                }

                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

                // Fade out in last 40%
                if (t > 0.6f)
                {
                    canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, (t - 0.6f) / 0.4f);
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
