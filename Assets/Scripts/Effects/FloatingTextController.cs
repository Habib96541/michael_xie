using System.Collections.Generic;
using UnityEngine;

namespace DiceGame.Effects
{
    public sealed class FloatingTextController : MonoBehaviour
    {
        [SerializeField] private FloatingTextItem prefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private int poolSize = 16;

        private readonly List<FloatingTextItem> _pool = new List<FloatingTextItem>(16);

        private void Awake()
        {
            if (container == null)
            {
                container = GetComponent<RectTransform>();
            }

            if (prefab != null)
            {
                for (int i = 0; i < poolSize; i++)
                {
                    FloatingTextItem item = Instantiate(prefab, container);
                    item.gameObject.SetActive(false);
                    _pool.Add(item);
                }
            }
        }

        public void RegisterItem(FloatingTextItem item)
        {
            if (item != null && !_pool.Contains(item))
            {
                _pool.Add(item);
            }
        }

        public void Show(string message, Vector2 anchoredPos, Color color, float size = 38.0f, float duration = 1.0f)
        {
            FloatingTextItem available = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && _pool[i].IsAvailable)
                {
                    available = _pool[i];
                    break;
                }
            }

            if (available == null && prefab != null)
            {
                // Dynamic expansion if needed
                available = Instantiate(prefab, container);
                _pool.Add(available);
            }

            if (available != null)
            {
                available.Spawn(message, anchoredPos, color, size, duration);
            }
        }
    }
}
