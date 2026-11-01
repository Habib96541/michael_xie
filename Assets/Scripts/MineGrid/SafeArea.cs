using UnityEngine;
using UnityEngine.UI;

namespace MineGrid
{
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _last;
        private Vector2 _size;
        private void Update()
        {
            var area = Screen.safeArea;
            if (area == _last && _size == new Vector2(Screen.width, Screen.height))
            {
                return;
            }

            _last = area;
            _size = new Vector2(Screen.width, Screen.height);
            var r = (RectTransform)transform;
            r.anchorMin = area.position / _size;
            r.anchorMax = (area.position + area.size) / _size;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
