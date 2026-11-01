#if UNITY_EDITOR
using System;
using UnityEngine;

namespace PegRush
{
    // Editor-only instrumentation: observes the production prefab in an isolated PhysicsScene2D.
    public sealed class DistributionProbe : MonoBehaviour
    {
        private Rigidbody2D _body;
        private Action<int, string, float> _result;
        private float _age;
        private float _stuck;
        private float _timeout;
        public bool Running { get; private set; }

        public void Launch(Vector2 position, float gravity, float damping, float timeout, Action<int, string, float> result)
        {
            _body = GetComponent<Rigidbody2D>();
            _age = 0f;
            _stuck = 0f;
            _timeout = timeout;
            _result = result;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _body.position = position;
            _body.rotation = 0f;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.linearDamping = damping;
            _body.gravityScale = gravity;
            Running = true;
            gameObject.SetActive(true);
            _body.WakeUp();
        }

        public void Tick(float delta)
        {
            if (!Running) return;
            _age += delta;
            _stuck = _body.linearVelocity.sqrMagnitude < 0.01f ? _stuck + delta : 0f;
            if (transform.position.y < -3f || Mathf.Abs(transform.position.x) > 13f) Finish(-1, "escaped");
            else if (_stuck > 4f) Finish(-1, "stuck");
            else if (_age > _timeout) Finish(-1, "timeout");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Pocket pocket = other.GetComponent<Pocket>();
            if (pocket != null && Running) Finish(pocket.Index, "pocket");
        }

        private void Finish(int pocket, string reason)
        {
            if (!Running) return;
            Running = false;
            _result(pocket, reason, _age);
            gameObject.SetActive(false);
        }
    }
}
#endif
