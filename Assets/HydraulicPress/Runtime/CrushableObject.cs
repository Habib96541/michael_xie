using UnityEngine;

namespace HydraulicPress
{
    public sealed class CrushableObject : MonoBehaviour
    {
        private MeshFilter[] filters;
        private Mesh[] meshes;
        private Vector3[][] original;
        private Vector3[][] working;
        private Vector3[] scales;
        private Vector3[] positions;
        public CrushableObjectDefinition Definition { get; private set; }
        public void Initialize(CrushableObjectDefinition definition)
        {
            Definition = definition;
            filters = GetComponentsInChildren<MeshFilter>(true);
            meshes = new Mesh[filters.Length]; original = new Vector3[filters.Length][]; working = new Vector3[filters.Length][];
            positions = new Vector3[filters.Length]; scales = new Vector3[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                meshes[i] = filters[i].mesh; meshes[i].MarkDynamic();
                original[i] = meshes[i].vertices; working[i] = new Vector3[original[i].Length];
                positions[i] = filters[i].transform.localPosition; scales[i] = filters[i].transform.localScale;
            }
        }

        public void ResetObject()
        {
            for (int i = 0; i < meshes.Length; i++) { meshes[i].vertices = original[i]; meshes[i].RecalculateBounds(); filters[i].transform.localPosition = positions[i]; filters[i].transform.localScale = scales[i]; }
            transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity;
        }

        public void Deform(float compression)
        {
            float c = Mathf.Clamp01(compression);
            float h = Mathf.Lerp(1, .10f, c);
            float spread = Definition.response == CrushMaterial.Soft || Definition.response == CrushMaterial.Liquid ? .7f : .25f;
            transform.localScale = new Vector3(1 + c * spread, h, 1 + c * spread);
            if (Definition.response != CrushMaterial.Metal && Definition.response != CrushMaterial.Plastic) return;
            for (int i = 0; i < meshes.Length; i++)
            {
                for (int j = 0; j < original[i].Length; j++)
                {
                    Vector3 v = original[i][j];
                    float wrinkle = Mathf.Sin(v.y * 36 + v.x * 15) * c * .055f;
                    working[i][j] = v + new Vector3(v.x * wrinkle, Mathf.Sin(v.x * 20 + v.z * 16) * c * .035f, v.z * wrinkle);
                }
                meshes[i].vertices = working[i]; meshes[i].RecalculateBounds();
            }
        }

        private void OnDestroy() { if (meshes != null) foreach (var mesh in meshes) if (mesh != null) Destroy(mesh); }
    }
}
