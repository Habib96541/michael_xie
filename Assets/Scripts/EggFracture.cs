using System.Collections;
using UnityEngine;

namespace EggDrop
{
    public sealed class EggFracture : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] fragments;
        [SerializeField] private Transform eggWhite;
        [SerializeField] private Transform yolk;
        [SerializeField, Min(0f)] private float scatterSpeed = 1.6f;
        [SerializeField, Min(0f)] private float hopSpeed = 1f;
        [SerializeField, Min(0.05f)] private float spreadDuration = 0.45f;

        private Vector3[] restPositions;
        private Collider[] fragmentColliders;
        private bool releaseRequested;
        private bool broken;

        private void Awake()
        {
            restPositions = new Vector3[fragments.Length];
            fragmentColliders = new Collider[fragments.Length];
            for (int index = 0; index < fragments.Length; index++)
            {
                restPositions[index] = fragments[index].transform.localPosition;
                fragmentColliders[index] = fragments[index].GetComponentInChildren<Collider>(true);
            }
        }

        private void FixedUpdate()
        {
            if (!releaseRequested) return;
            releaseRequested = false;
            for (int index = 0; index < fragments.Length; index++)
            {
                Rigidbody fragment = fragments[index];
                Vector3 direction = new Vector3(restPositions[index].x, 0f, restPositions[index].z).normalized;
                fragment.isKinematic = false;
                fragment.linearVelocity = direction * scatterSpeed * (0.75f + index % 3 * 0.12f)
                    + Vector3.up * hopSpeed * (0.85f + index % 4 * 0.1f);
                fragment.angularVelocity = new Vector3(direction.z * 5f, (index % 2 == 0 ? 1f : -1f) * 2f, -direction.x * 5f);
            }
        }

        public void BreakAt(Vector3 impactPoint)
        {
            if (broken) return;
            broken = true;
            transform.SetPositionAndRotation(impactPoint + Vector3.up * 0.68f, Quaternion.identity);
            foreach (Rigidbody fragment in fragments) fragment.gameObject.SetActive(true);
            for (int first = 0; first < fragmentColliders.Length; first++)
                for (int second = first + 1; second < fragmentColliders.Length; second++)
                    Physics.IgnoreCollision(fragmentColliders[first], fragmentColliders[second]);
            releaseRequested = true;
            eggWhite.gameObject.SetActive(true);
            yolk.gameObject.SetActive(true);
            StartCoroutine(SpreadContents());
        }

        public void ResetEffect()
        {
            StopAllCoroutines();
            broken = false;
            releaseRequested = false;
            foreach (Rigidbody fragment in fragments)
            {
                if (!fragment.isKinematic)
                {
                    fragment.linearVelocity = Vector3.zero;
                    fragment.angularVelocity = Vector3.zero;
                }
                fragment.isKinematic = true;
                fragment.gameObject.SetActive(false);
            }
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            for (int index = 0; index < fragments.Length; index++)
                fragments[index].transform.SetLocalPositionAndRotation(restPositions[index], Quaternion.identity);
            eggWhite.localScale = Vector3.one * 0.06f;
            yolk.localScale = Vector3.one * 0.06f;
            eggWhite.gameObject.SetActive(false);
            yolk.gameObject.SetActive(false);
        }

        private IEnumerator SpreadContents()
        {
            for (float elapsed = 0f; elapsed < spreadDuration; elapsed += Time.deltaTime)
            {
                float progress = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / spreadDuration), 3f);
                eggWhite.localScale = Vector3.one * Mathf.Lerp(0.06f, 1f, progress);
                yolk.localScale = Vector3.one * Mathf.Lerp(0.06f, 1f, Mathf.Clamp01(progress * 0.85f));
                yield return null;
            }
            eggWhite.localScale = Vector3.one;
            yolk.localScale = Vector3.one;
        }
    }
}
