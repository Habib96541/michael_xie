using UnityEngine;

namespace ShatterLeap
{
    public sealed class ContestantMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody _body;
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        private Vector3 _from;
        private Vector3 _to;
        private float _elapsed;
        private float _duration;
        private float _height;
        private bool _jumping;
        private bool _falling;
        private float _fallElapsed;
        public float FallElapsed => _fallElapsed;
        public bool IsFalling => _falling;
        public Vector3 Position => _body.position;
        public bool Arrived { get; private set; }

        public void ResetAt(Vector3 position)
        {
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.position = position;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _visual.localRotation = Quaternion.identity;
            _visual.localPosition = Vector3.zero;
            _fallElapsed = 0;
            _jumping = false;
            _falling = false;
            Arrived = false;
            Pose(0);
        }
        public void Jump(Vector3 destination, float duration, float height)
        {
            _from = _body.position;
            _to = destination;
            _elapsed = 0;
            _duration = duration;
            _height = height;
            Arrived = false;
            _jumping = true;
        }
        public void Advance(float delta)
        {
            if (_falling)
            {
                _fallElapsed += delta;
                if (_fallElapsed >= 0.18f) { _body.useGravity = true; }
                float surprise = Mathf.SmoothStep(0, 1, _fallElapsed / 0.18f);
                float plunge = Mathf.SmoothStep(0, 1, (_fallElapsed - 0.18f) / 0.5f);
                _visual.localRotation = Quaternion.Euler(Mathf.Lerp(-12, 34, plunge) * surprise, 0, Mathf.Lerp(-10, 18, plunge) * surprise);
                _leftArm.localRotation = Quaternion.Euler(Mathf.Lerp(-35, -145, plunge) * surprise, -12 * surprise, -65 * surprise);
                _rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(-50, -125, plunge) * surprise, 12 * surprise, 70 * surprise);
                _leftLeg.localRotation = Quaternion.Euler(42 * surprise, 0, -12 * surprise);
                _rightLeg.localRotation = Quaternion.Euler(-28 * surprise, 0, 16 * surprise);
                return;
            }
            if (!_jumping)
            {
                _visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 2.4f) * 0.018f);
                Pose(0);
                return;
            }
            _elapsed += delta;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Vector3 position = Vector3.Lerp(_from, _to, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _height;
            _body.position = position;
            transform.position = position;
            Pose(Mathf.Sin(t * Mathf.PI));
            if (t >= 1)
            {
                _jumping = false;
                Arrived = true;
                _body.position = _to;
                transform.position = _to;
            }
        }
        public bool ValidateLanding(Collider expected)
        {
            Physics.SyncTransforms();
            return expected && expected.enabled && Physics.SphereCast(Position + Vector3.up * 0.55f, 0.15f,
                Vector3.down, out RaycastHit hit, 0.8f, 1 << 8, QueryTriggerInteraction.Ignore) && hit.collider == expected;
        }
        public void Fall()
        {
            _jumping = false;
            Arrived = false;
            _falling = true;
            _fallElapsed = 0;
            _visual.localPosition = Vector3.zero;
            _body.isKinematic = false;
            _body.useGravity = false;
            // Descend immediately while the startled pose develops, then release to gravity.
            _body.linearVelocity = new Vector3(0, -0.45f, 0.12f);
        }
        private void Pose(float amount)
        {
            _leftArm.localRotation = Quaternion.Euler(-65 * amount, 0, -8 - 25 * amount);
            _rightArm.localRotation = Quaternion.Euler(-65 * amount, 0, 8 + 25 * amount);
            _leftLeg.localRotation = Quaternion.Euler(-22 * amount, 0, 0);
            _rightLeg.localRotation = Quaternion.Euler(30 * amount, 0, 0);
        }
    }
}
