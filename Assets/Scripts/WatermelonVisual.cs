using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public sealed class WatermelonVisual : MonoBehaviour
    {
        [SerializeField] private Transform wholeWatermelon;
        [SerializeField] private Transform bandsRoot;
        [SerializeField] private GameObject bandPrefab;
        [SerializeField] private Transform fragmentsRoot;
        [SerializeField] private Rigidbody[] fragments;
        [SerializeField] private Material[] bandMaterials;
        [SerializeField, Min(0.1f)] private float burstForce = 5f;
        [SerializeField, Range(0.01f, 0.15f)] private float pressurePerBand = 0.045f;
        [SerializeField, Range(0.1f, 0.7f)] private float maximumIndentation = 0.6f;
        [SerializeField, Range(0.25f, 0.7f)] private float pressureWidth = 0.35f;
        [SerializeField, Min(0.05f)] private float tighteningDuration = 0.18f;

        private readonly List<GameObject> bands = new();
        private Vector3[] fragmentPositions;
        private Quaternion[] fragmentRotations;
        private Transform newestBand;
        private float bandAnimation;
        private MeshFilter bodyFilter;
        private Mesh originalMesh;
        private Mesh pressureMesh;
        private Vector3[] originalVertices;
        private Vector3[] deformedVertices;
        private Vector3[] originalNormals;
        private float meshHalfHeight;
        private Vector3 bodyExtents;
        private float pressure;
        private float pressureStart;
        private float pressureTarget;
        private float tighteningProgress;
        private float fragmentLifetime;
        private bool burstPending;

        private void Awake()
        {
            bodyFilter = wholeWatermelon.GetComponentInChildren<MeshFilter>();
            originalMesh = bodyFilter.sharedMesh;
            pressureMesh = Instantiate(originalMesh);
            pressureMesh.name = "Watermelon Pressure Mesh";
            pressureMesh.MarkDynamic();
            bodyFilter.sharedMesh = pressureMesh;
            originalVertices = originalMesh.vertices;
            originalNormals = originalMesh.normals;
            deformedVertices = new Vector3[originalVertices.Length];
            meshHalfHeight = originalMesh.bounds.extents.y;
            bodyExtents = Vector3.Scale(originalMesh.bounds.extents, bodyFilter.transform.localScale);

            fragmentPositions = new Vector3[fragments.Length];
            fragmentRotations = new Quaternion[fragments.Length];

            for (int index = 0; index < fragments.Length; index++)
            {
                fragmentPositions[index] = fragments[index].transform.localPosition;
                fragmentRotations[index] = fragments[index].transform.localRotation;
            }
        }

        private void Update()
        {
            bool pressureChanged = pressure != pressureTarget;
            if (pressureChanged)
            {
                tighteningProgress = Mathf.MoveTowards(tighteningProgress, 1f, Time.deltaTime / tighteningDuration);
                pressure = Mathf.Lerp(pressureStart, pressureTarget, Mathf.SmoothStep(0f, 1f, tighteningProgress));
                ApplyPressure();
            }

            bool bandChanged = newestBand != null && bandAnimation < 1f;
            if (bandChanged)
                bandAnimation = Mathf.MoveTowards(bandAnimation, 1f, Time.deltaTime * 7f);

            if (pressureChanged || bandChanged)
                UpdateBands();

            if (fragmentsRoot.gameObject.activeSelf)
            {
                fragmentLifetime += Time.deltaTime;
                if (fragmentLifetime >= 4f)
                    fragmentsRoot.gameObject.SetActive(false);
            }
        }

        private void ApplyPressure()
        {
            for (int index = 0; index < originalVertices.Length; index++)
            {
                Vector3 vertex = originalVertices[index];
                float radius = GetPressureScale(vertex.y / meshHalfHeight);
                vertex.x *= radius;
                vertex.z *= radius;
                deformedVertices[index] = vertex;
            }
            pressureMesh.vertices = deformedVertices;
            pressureMesh.RecalculateNormals();
            pressureMesh.RecalculateBounds();
        }

        private float GetPressureScale(float normalizedHeight)
        {
            float distance = normalizedHeight / pressureWidth;
            float distanceSquared = distance * distance;
            return 1f - pressure * Mathf.Exp(-distanceSquared * distanceSquared);
        }

        private void UpdateBands()
        {
            foreach (GameObject band in bands)
            {
                Transform bandTransform = band.transform;
                float height = bandTransform.localPosition.y / bodyExtents.y;
                float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - height * height)) * GetPressureScale(height);
                float placement = bandTransform == newestBand ? Mathf.Lerp(1.35f, 1f, bandAnimation) : 1f;
                bandTransform.localScale = new Vector3(bodyExtents.x * radius, 1f, bodyExtents.z * radius) * placement;
            }
        }

        private void FixedUpdate()
        {
            if (!burstPending)
                return;

            burstPending = false;
            Vector3 center = transform.position - Vector3.up * 0.3f;

            foreach (Rigidbody fragment in fragments)
            {
                fragment.isKinematic = false;
                fragment.AddExplosionForce(burstForce, center, 4f, 0.8f, ForceMode.VelocityChange);
                fragment.AddTorque(Random.insideUnitSphere * 4f, ForceMode.VelocityChange);
            }
        }

        public void AddBand(int count)
        {
            GameObject band = Instantiate(bandPrefab, bandsRoot);
            band.name = $"Band {count:00}";
            float height = ((count - 1) % 2 == 0 ? 1f : -1f) * Mathf.Ceil((count - 1) / 2f) * 0.025f;
            height = Mathf.Clamp(height, -bodyExtents.y * 0.3f, bodyExtents.y * 0.3f);
            band.transform.localPosition = Vector3.up * height;
            band.transform.localRotation = Quaternion.identity;
            if (band.TryGetComponent(out MeshRenderer bandRenderer))
                bandRenderer.sharedMaterial = bandMaterials[(count - 1) % bandMaterials.Length];
            bands.Add(band);
            newestBand = band.transform;
            bandAnimation = 0f;
            pressureStart = pressure;
            pressureTarget = maximumIndentation * (1f - Mathf.Exp(-count * pressurePerBand / maximumIndentation));
            tighteningProgress = 0f;
            UpdateBands();
        }

        public void Burst()
        {
            wholeWatermelon.gameObject.SetActive(false);
            fragmentsRoot.gameObject.SetActive(true);
            fragmentLifetime = 0f;
            burstPending = true;
        }

        public void ResetRound()
        {
            newestBand = null;
            foreach (GameObject band in bands)
            {
                band.SetActive(false);
                Destroy(band);
            }
            bands.Clear();
            burstPending = false;
            pressure = 0f;
            pressureStart = 0f;
            pressureTarget = 0f;
            tighteningProgress = 1f;
            pressureMesh.vertices = originalVertices;
            pressureMesh.normals = originalNormals;
            pressureMesh.RecalculateBounds();
            wholeWatermelon.gameObject.SetActive(true);
            fragmentsRoot.gameObject.SetActive(false);

            for (int index = 0; index < fragments.Length; index++)
            {
                Rigidbody fragment = fragments[index];
                if (!fragment.isKinematic)
                {
                    fragment.linearVelocity = Vector3.zero;
                    fragment.angularVelocity = Vector3.zero;
                }
                fragment.isKinematic = true;
                fragment.transform.SetLocalPositionAndRotation(fragmentPositions[index], fragmentRotations[index]);
            }
        }

        private void OnDestroy()
        {
            if (bodyFilter != null)
                bodyFilter.sharedMesh = originalMesh;
            if (pressureMesh != null)
                Destroy(pressureMesh);
        }
    }
}
