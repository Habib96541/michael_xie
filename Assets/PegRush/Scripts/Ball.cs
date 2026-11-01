using UnityEngine;

namespace PegRush
{
    public enum BallState { Pooled, Falling, Resolving }

    public sealed class Ball : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private SpriteRenderer _visual;
        private GameSession _session;
        private DropTransaction _transaction;
        private float _age;
        private float _stuck;
        public BallState State { get; private set; }

        public void Wire(Rigidbody2D body, SpriteRenderer visual)
        {
            _body = body;
            _visual = visual;
        }

        public void Launch(GameSession session, DropTransaction transaction, Vector2 position, Color color, float gravity)
        {
            _session = session;
            _transaction = transaction;
            _age = 0f;
            _stuck = 0f;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _visual.color = color;
            State = BallState.Falling;
            gameObject.SetActive(true);
            _body.gravityScale = gravity;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.WakeUp();
        }

        private void FixedUpdate()
        {
            if (State != BallState.Falling) return;
            _age += Time.fixedDeltaTime;
            _stuck = _body.linearVelocity.sqrMagnitude < 0.01f ? _stuck + Time.fixedDeltaTime : 0f;
            if (_age > _session.Config.Timeout || _stuck > 4f || transform.position.y < -3f || Mathf.Abs(transform.position.x) > 13f) Complete(-1);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (State != BallState.Falling) return;
            PegFlash peg = collision.gameObject.GetComponent<PegFlash>();
            if (peg != null) peg.Flash(_session.Reduced);
        }

        public void Complete(int pocket)
        {
            if (State != BallState.Falling) return;
            State = BallState.Resolving;
            if (!_session.Complete(this, _transaction.Id, pocket))
            {
                State = BallState.Falling;
                return;
            }
            _transaction = null;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            State = BallState.Pooled;
            gameObject.SetActive(false);
        }
    }
}

