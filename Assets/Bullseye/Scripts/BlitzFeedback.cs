using UnityEngine;
using UnityEngine.InputSystem;

namespace BullseyeBlitz
{
    public sealed class BlitzFeedback : MonoBehaviour
    {
        private sealed class Item
        {
            public Transform Root;
            public TextMesh Label;
            public SpriteRenderer Flash;
            public Vector3 Origin;
            public float Until;
            public float Duration;
        }
        private readonly Item[] _pool = new Item[20];
        private SpriteRenderer _crosshair;
        private Camera _camera;
        private Sprite _ring;
        private Sprite _disc;
        private Material _material;
        private bool _enabled;
        private bool _touchMode;
        private float _touchUntil;
        private int _cursor;

        public void Configure(Camera camera, Font font)
        {
            _camera = camera;
            _ring = MakeSprite(true); _disc = MakeSprite(false);
            Material material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            _material = material;
            _crosshair = new GameObject("Pointer crosshair").AddComponent<SpriteRenderer>();
            _crosshair.sprite = _ring; _crosshair.sharedMaterial = material;
            _crosshair.sortingLayerName = "Effects"; _crosshair.sortingOrder = 100;
            _crosshair.transform.localScale = Vector3.one * .3f;
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject root = new GameObject("Pooled feedback " + i);
                root.transform.SetParent(transform, false);
                SpriteRenderer flash = root.AddComponent<SpriteRenderer>();
                flash.sprite = _disc; flash.sharedMaterial = material; flash.sortingLayerName = "Effects";
                GameObject textObject = new GameObject("Caption"); textObject.transform.SetParent(root.transform, false);
                TextMesh label = textObject.AddComponent<TextMesh>();
                if (font != null)
                {
                    label.font = font;
                    label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
                label.fontSize = 64; label.characterSize = .08f; label.anchor = TextAnchor.MiddleCenter;
                label.GetComponent<MeshRenderer>().sortingLayerName = "Effects";
                label.GetComponent<MeshRenderer>().sortingOrder = 1;
                _pool[i] = new Item { Root = root.transform, Label = label, Flash = flash };
                root.SetActive(false);
            }
        }
        public void SetPlaying(bool playing) { _enabled = playing; }
        public void Show(Vector2 point, string caption, Color color, bool reduced)
        {
            Item item = _pool[_cursor++ % _pool.Length];
            item.Origin = point; item.Root.position = point;
            item.Duration = reduced ? .2f : .5f;
            item.Until = Time.unscaledTime + item.Duration;
            item.Label.text = caption; item.Label.color = color;
            item.Flash.color = new Color(color.r, color.g, color.b, .35f);
            item.Root.gameObject.SetActive(true);
            _crosshair.transform.position = new Vector3(point.x, point.y, 0);
            _touchUntil = Time.unscaledTime + .2f;
        }
        public void Explosion(Vector2 point, bool reduced)
        {
            Show(point, "BOOM!", new Color(1, .35f, .15f), reduced);
            if (reduced) { return; }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Show(point + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .55f, "*", new Color(1, .7f, .15f), false);
            }
        }
        private void Update()
        {
            if (_crosshair == null) { return; }
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            if (touch) { _touchMode = true; }
            _crosshair.enabled = _enabled && (!_touchMode || Time.unscaledTime < _touchUntil);
            if (_enabled && !_touchMode && Mouse.current != null)
            {
                Vector2 p = Mouse.current.position.ReadValue();
                _crosshair.transform.position = _camera.ScreenToWorldPoint(new Vector3(p.x, p.y, -_camera.transform.position.z));
            }
            foreach (Item item in _pool)
            {
                if (!item.Root.gameObject.activeSelf) { continue; }
                float remaining = item.Until - Time.unscaledTime;
                if (remaining <= 0) { item.Root.gameObject.SetActive(false); continue; }
                float progress = 1 - remaining / item.Duration;
                item.Root.position = item.Origin + Vector3.up * progress * .55f;
                item.Flash.transform.localScale = Vector3.one;
                Color c = item.Label.color; c.a = 1 - progress; item.Label.color = c;
                c = item.Flash.color; c.a = .25f * (1 - progress); item.Flash.color = c;
            }
        }
        private Sprite MakeSprite(bool ring)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[4096];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    bool visible = ring ? (d > 22 && d < 27) || (Mathf.Abs(x - 32) < 2 && (y < 16 || y > 48)) || (Mathf.Abs(y - 32) < 2 && (x < 16 || x > 48)) : d < 28;
                    pixels[y * 64 + x] = visible ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
        }
        private void OnDestroy()
        {
            if (_crosshair != null) { Destroy(_crosshair.gameObject); }
            if (_ring != null) { Destroy(_ring.texture); Destroy(_ring); }
            if (_disc != null) { Destroy(_disc.texture); Destroy(_disc); }
            if (_material != null) { Destroy(_material); }
        }
    }
}
