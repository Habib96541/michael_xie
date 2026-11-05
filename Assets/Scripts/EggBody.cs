using System;
using System.Collections;
using UnityEngine;

namespace EggDrop
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class EggBody : MonoBehaviour
    {
        [SerializeField] private Collider landingSurface;
        [SerializeField] private Transform intactShell;
        [SerializeField] private EggFracture fracture;

        private Rigidbody eggRigidbody;
        private SphereCollider eggCollider;
        private bool releaseRequested;
        private bool awaitingLanding;
        private Vector3 impactPoint;

        public event Action Landed;

        private void Awake()
        {
            TryGetComponent(out eggRigidbody);
            TryGetComponent(out eggCollider);
        }

        private void FixedUpdate()
        {
            if (!releaseRequested) return;
            releaseRequested = false;
            awaitingLanding = true;
            eggRigidbody.isKinematic = false;
            eggRigidbody.WakeUp();
        }

        public void Prepare(float height)
        {
            StopAllCoroutines();
            fracture.ResetEffect();
            Freeze();
            transform.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.identity);
            intactShell.gameObject.SetActive(true);
            intactShell.localScale = Vector3.one;
            eggCollider.enabled = true;
        }

        public void RequestDrop() => releaseRequested = true;

        public IEnumerator LiftTo(float height)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = new Vector3(0f, height, 0f);
            for (float elapsed = 0f; elapsed < 0.85f; elapsed += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / 0.85f);
                transform.position = Vector3.Lerp(startPosition, endPosition, progress);
                yield return null;
            }
            transform.position = endPosition;
        }

        public IEnumerator Squash()
        {
            for (float elapsed = 0f; elapsed < 0.32f; elapsed += Time.deltaTime)
            {
                float amount = Mathf.Sin(elapsed / 0.32f * Mathf.PI) * 0.22f;
                intactShell.localScale = new Vector3(1f + amount, 1f - amount, 1f + amount);
                yield return null;
            }
            intactShell.localScale = Vector3.one;
        }

        public void Crack()
        {
            eggCollider.enabled = false;
            intactShell.gameObject.SetActive(false);
            fracture.BreakAt(impactPoint);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!awaitingLanding || collision.collider != landingSurface) return;
            impactPoint = collision.GetContact(0).point;
            Freeze();
            Landed?.Invoke();
        }

        private void Freeze()
        {
            releaseRequested = false;
            awaitingLanding = false;
            if (!eggRigidbody.isKinematic)
            {
                eggRigidbody.linearVelocity = Vector3.zero;
                eggRigidbody.angularVelocity = Vector3.zero;
            }
            eggRigidbody.isKinematic = true;
        }
    }
}
