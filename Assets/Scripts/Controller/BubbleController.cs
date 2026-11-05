using UnityEngine;
using System.Collections;

namespace com.javierquevedo
{
    public class BubbleController : MonoBehaviour
    {
        /* Model */
        public Bubble bubble;

        /* View Properties */
        public float leftBorder;
        public float rightBorder;
        public float topBorder;

        public float linearSpeed; // units / sec
        public float radius;
        public float angle; // Trajectory angle in degrees (90 = straight up)
        public bool isMoving;

        private SpriteRenderer _spriteRenderer;
        private bool _isDying = false;

        /* Delegates */
        MotionDetectionDelegate motionDelegate;
        public delegate bool MotionDetectionDelegate(Vector3 position);

        CollisionDetectionDelegate collisionDelegate;
        public delegate void CollisionDetectionDelegate(GameObject bubble);

        public CollisionDetectionDelegate CollisionDelegate
        {
            set { collisionDelegate = value; }
        }

        public MotionDetectionDelegate MotionDelegate
        {
            set { motionDelegate = value; }
        }

        void Awake()
        {
            bubble = new Bubble(JQUtils.GetRandomEnum<BubbleColor>());
            SetupCyberVisuals();
        }

        void Start()
        {
            RefreshVisuals();
        }

        public void SetColor(BubbleColor newColor)
        {
            if (bubble == null)
                bubble = new Bubble(newColor);
            else
                bubble.color = newColor;

            RefreshVisuals();
        }

        private void SetupCyberVisuals()
        {
            // Hide legacy 3D sphere mesh renderer if present on prefab
            MeshRenderer legacyMesh = GetComponent<MeshRenderer>();
            if (legacyMesh != null)
                legacyMesh.enabled = false;

            // Disable 3D physics collider/rigidbody interference so deterministic hex math handles collisions cleanly
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            Transform visualChild = transform.Find("CyberOrbSprite");
            if (visualChild == null)
            {
                GameObject go = new GameObject("CyberOrbSprite");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                go.transform.localScale = Vector3.one;
                _spriteRenderer = go.AddComponent<SpriteRenderer>();
                _spriteRenderer.sortingOrder = 10;
            }
            else
            {
                _spriteRenderer = visualChild.GetComponent<SpriteRenderer>();
            }
        }

        public void RefreshVisuals()
        {
            if (_spriteRenderer == null)
                SetupCyberVisuals();

            if (_spriteRenderer != null && bubble != null)
            {
                _spriteRenderer.sprite = CyberArtGenerator.GetOrbSprite(bubble.color);
                _spriteRenderer.color = Color.white;
            }
        }

        void Update()
        {
            if (!isMoving || _isDying) return;

            // Sub-step movement to prevent tunneling through gaps at high speeds
            const int subSteps = 4;
            float dt = Time.deltaTime / subSteps;

            for (int i = 0; i < subSteps; i++)
            {
                if (!isMoving) break;

                float rad = Mathf.Deg2Rad * this.angle;
                Vector3 step = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * (this.linearSpeed * dt);
                Vector3 nextPos = this.transform.position + step;

                // Wall reflection check (left & right titanium borders)
                if (nextPos.x + this.radius >= this.rightBorder)
                {
                    nextPos.x = this.rightBorder - this.radius;
                    this.angle = 180.0f - this.angle;
                    CyberAudioSynthesizer.Instance.Play("bounce", 0.7f, Random.Range(0.95f, 1.08f));
                }
                else if (nextPos.x - this.radius <= this.leftBorder)
                {
                    nextPos.x = this.leftBorder + this.radius;
                    this.angle = 180.0f - this.angle;
                    CyberAudioSynthesizer.Instance.Play("bounce", 0.7f, Random.Range(0.95f, 1.08f));
                }

                // Check ceiling collision
                if (nextPos.y + this.radius >= this.topBorder)
                {
                    nextPos.y = this.topBorder - this.radius;
                    this.transform.position = nextPos;
                    this.isMoving = false;
                    if (collisionDelegate != null)
                        collisionDelegate(this.gameObject);
                    break;
                }

                // Check collision against existing grid orbs via motionDelegate
                if (this.motionDelegate != null && !this.motionDelegate(nextPos))
                {
                    this.isMoving = false;
                    if (collisionDelegate != null)
                        collisionDelegate(this.gameObject);
                    break;
                }

                this.transform.position = nextPos;
            }
        }

        public void kill(bool explodes)
        {
            if (_isDying) return;
            _isDying = true;
            this.isMoving = false;
            StopAllCoroutines();

            if (explodes)
            {
                if (bubble != null)
                    CyberVFXManager.Instance.SpawnOrbPopBurst(this.transform.position, bubble.color);
                StartCoroutine(PopScaleOut(0.14f));
            }
            else
            {
                StartCoroutine(AvalancheFallOut());
            }
        }

        public void moveTo(Vector3 destination, float duration)
        {
            if (_isDying) return;
            StopAllCoroutines();
            StartCoroutine(TweenTo(destination, duration));
        }

        public void PlaySpawnPop(float delay = 0f)
        {
            if (_isDying) return;
            StartCoroutine(SpawnPopCoroutine(delay));
        }

        private IEnumerator SpawnPopCoroutine(float delay)
        {
            if (delay > 0f)
            {
                transform.localScale = Vector3.zero;
                yield return new WaitForSeconds(delay);
            }
            float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float norm = Mathf.Clamp01(t / duration);
                // Back-out overshoot curve
                float s = 1f + 2.2f * Mathf.Pow(norm - 1f, 3f) + 1.2f * Mathf.Pow(norm - 1f, 2f);
                transform.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        private IEnumerator TweenTo(Vector3 destination, float duration)
        {
            float elapsed = 0.0f;
            Vector3 initialPosition = transform.position;
            duration = Mathf.Max(0.01f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad + subtle squash & stretch on snap
                float ease = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(initialPosition, destination, ease);
                float squash = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                transform.localScale = new Vector3(squash, 2f - squash, 1f);
                yield return null;
            }
            transform.position = destination;
            transform.localScale = Vector3.one;
        }

        private IEnumerator PopScaleOut(float duration)
        {
            float elapsed = 0.0f;
            Vector3 initialScale = transform.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = t < 0.3f
                    ? Mathf.Lerp(1.0f, 1.32f, t / 0.3f)
                    : Mathf.Lerp(1.32f, 0.0f, (t - 0.3f) / 0.7f);
                transform.localScale = initialScale * scale;
                yield return null;
            }
            Destroy(this.gameObject);
        }

        private IEnumerator AvalancheFallOut()
        {
            // Disconnected orbs hop slightly and accelerate downward with gravity, then burst at the bottom
            Vector3 velocity = new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(1.2f, 2.6f), 0f);
            float gravity = 24.0f;
            float targetBottomY = 0.2f;

            while (transform.position.y > targetBottomY)
            {
                velocity.y -= gravity * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(0f, 0f, velocity.x * -90f * Time.deltaTime);
                yield return null;
            }

            if (bubble != null)
                CyberVFXManager.Instance.SpawnOrbPopBurst(transform.position, bubble.color);

            Destroy(this.gameObject);
        }
    }
}
