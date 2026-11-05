using UnityEngine;
using UnityEngine.InputSystem;

namespace FlapDash
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class FlapPlayer : MonoBehaviour
    {
        [SerializeField] private RoundController round;
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private PlayerFeedback feedback;
        [SerializeField] private AudioSource flapAudio;
        [SerializeField, Min(0.1f)] private float flapSpeed = 8f;
        [SerializeField, Min(0.1f)] private float fallSpeedLimit = 12f;

        private Rigidbody2D body;
        private InputAction flapAction;
        private Vector2 startPosition;
        private bool flapPending;
        private int firstInputFrame;

        private void Awake()
        {
            TryGetComponent(out body);
            startPosition = body.position;
            flapAction = controls.FindAction("Gameplay/Flap", true);
            body.simulated = false;
        }

        private void OnEnable()
        {
            flapAction.Enable();
        }

        private void OnDisable()
        {
            flapAction.Disable();
            flapPending = false;
        }

        private void Update()
        {
            if (round.IsPlaying && Time.frameCount >= firstInputFrame && flapAction.WasPressedThisFrame())
                flapPending = true;
        }

        private void FixedUpdate()
        {
            if (!round.IsPlaying)
                return;

            if (flapPending)
            {
                body.linearVelocity = Vector2.up * flapSpeed;
                flapAudio.Play();
                feedback.PlayFlap();
                flapPending = false;
            }

            if (body.linearVelocity.y < -fallSpeedLimit)
                body.linearVelocity = Vector2.down * fallSpeedLimit;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Obstacle"))
                round.EndRound("You hit an obstacle.");
        }

        public void ResetPlayer()
        {
            StopPlayer();
            feedback.ClearEffects();
            body.position = startPosition;
            body.rotation = 0f;
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
        }

        public void BeginRound()
        {
            flapPending = false;
            firstInputFrame = Time.frameCount + 1;
            body.simulated = true;
        }

        public void StopPlayer()
        {
            flapPending = false;
            flapAudio.Stop();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }
}
