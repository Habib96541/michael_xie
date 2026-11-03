using UnityEngine;

namespace BullseyeBlitz
{
    public enum TargetState { Hidden, Rising, Exposed, Retreating }

    public sealed class BoothTarget : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private CircleCollider2D _collider;
        [SerializeField] private TextMesh _label;
        private double _born;
        private double _retreat;
        private float _lifetime;
        private float _transition;
        private Vector3 _position;
        public TargetState State { get; private set; }
        public bool Bomb { get; private set; }
        public int Reward { get; private set; }
        public int Order { get; private set; }
        public int Activation { get; private set; }

        public void Configure(SpriteRenderer sprite, CircleCollider2D collider, TextMesh label)
        {
            _sprite = sprite; _collider = collider; _label = label;
            Hide();
        }
        public void Spawn(BlitzConfig config, bool bomb, int reward, double time, int order)
        {
            Activation++;
            Bomb = bomb; Reward = reward; Order = order;
            _sprite.sprite = bomb ? config.Bomb : config.Coin;
            _sprite.sortingLayerName = "Targets";
            _sprite.sortingOrder = order;
            _label.text = bomb ? "!" : reward.ToString();
            _label.color = bomb ? Color.white : new Color(0.22f, 0.10f, 0.06f);
            _label.GetComponent<MeshRenderer>().sortingLayerName = "Targets";
            _label.GetComponent<MeshRenderer>().sortingOrder = order + 1;
            _born = time; _retreat = double.PositiveInfinity;
            _transition = config.TransitionSeconds;
            _lifetime = Random.Range(config.Exposure.x, config.Exposure.y);
            _collider.enabled = false;
            _label.GetComponent<MeshRenderer>().enabled = false;
            State = TargetState.Rising;
            gameObject.SetActive(true);
            Tick(time);
        }
        public void Place(Vector3 position)
        {
            transform.position += position - _position;
            _position = position;
        }
        public void Tick(double time)
        {
            if (State == TargetState.Hidden) { return; }
            double age = time - _born;
            if (State == TargetState.Rising && age >= _transition)
            {
                State = TargetState.Exposed;
                _collider.enabled = true;
                _label.GetComponent<MeshRenderer>().enabled = true;
            }
            if (State == TargetState.Exposed && age >= _transition + _lifetime) { Retreat(time); }
            float exposed = Mathf.Clamp01((float)(age / _transition));
            if (State == TargetState.Retreating)
            {
                exposed = 1f - Mathf.Clamp01((float)((time - _retreat) / _transition));
                if (exposed <= 0) { Hide(); return; }
            }
            transform.position = _position + Vector3.down * (1.1f * (1f - exposed));
        }
        public bool Consume(int activation, double time)
        {
            if (activation != Activation) { return false; }
            Tick(time);
            if (State != TargetState.Exposed || !_collider.enabled) { return false; }
            Retreat(time);
            return true;
        }
        private void Retreat(double time)
        {
            _collider.enabled = false;
            _label.GetComponent<MeshRenderer>().enabled = false;
            _retreat = time;
            State = TargetState.Retreating;
        }
        public void Hide()
        {
            if (_collider != null) { _collider.enabled = false; }
            State = TargetState.Hidden;
            Reward = 0;
            Bomb = false;
            gameObject.SetActive(false);
        }
    }
}
