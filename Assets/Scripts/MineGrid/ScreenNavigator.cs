using System;
using System.Collections;
using UnityEngine;

namespace MineGrid
{
    public sealed class ScreenNavigator : MonoBehaviour
    {
        private GameObject[] _pages;
        private GameObject _brand;
        private Func<bool> _reduced;
        private Coroutine _fade;
        public void Initialize(GameObject[] pages, GameObject brand, Func<bool> reduced)
        {
            _pages = pages;
            _brand = brand;
            _reduced = reduced;
        }

        public void Show(GameObject page)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }

            foreach (var screen in _pages)
            {
                screen.SetActive(screen == page);
                var group = screen.GetComponent<CanvasGroup>();
                if (group)
                {
                    group.alpha = 1;
                }
            }

            _brand.SetActive(page != _pages[0] && page != _pages[1]);
            _fade = StartCoroutine(Fade(page));
        }

        private IEnumerator Fade(GameObject page)
        {
            var group = page.GetComponent<CanvasGroup>();
            if (!group)
            {
                yield break;
            }

            group.alpha = _reduced() ? 1 : 0;
            for (float time = 0; !_reduced() && time < .18f; time += Time.unscaledDeltaTime)
            {
                group.alpha = time / .18f;
                yield return null;
            }

            group.alpha = 1;
            _fade = null;
        }
    }
}
