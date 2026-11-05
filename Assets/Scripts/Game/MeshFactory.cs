using UnityEngine;

namespace BullRide
{
    /// <summary>Runtime primitives + shared unlit material (Resources asset preferred for WebGL).</summary>
    public static class MeshFactory
    {
        static Material sharedMat;

        public static Material SharedMaterial
        {
            get
            {
                if (sharedMat != null) return sharedMat;
                Material stored = Resources.Load<Material>("BullRideUnlit");
                if (stored != null)
                {
                    sharedMat = stored;
                    return sharedMat;
                }
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Standard");
                sharedMat = new Material(shader);
                sharedMat.name = "BullRide Unlit Runtime";
                return sharedMat;
            }
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
            return go;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
            return go;
        }

        public static GameObject Capsule(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
            return go;
        }

        public static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            var mat = new Material(SharedMaterial);
            if (mat.HasProperty("_Color")) mat.color = color;
            else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
