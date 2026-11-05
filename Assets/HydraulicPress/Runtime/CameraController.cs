using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace HydraulicPress
{
    public sealed class CameraController : MonoBehaviour
    {
        private Camera view;
        private Vector3 home;
        private float trauma;
        private float zoom;
        private SaveData settings;
        public void Initialize(SaveData data) { settings = data; view = GetComponent<Camera>(); home = transform.position; }
        public void Impact(float power) { if (settings.shake) trauma = Mathf.Max(trauma, power); zoom = .09f; }
        private void LateUpdate()
        {
            if (view == null) return;
            // Fit the same machine bounds into the central unobstructed stage in both orientations.
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float referenceHeight = 720f / Mathf.Max(.2f, aspect);
            float portraitFit = 2.8f * referenceHeight / Mathf.Max(180, referenceHeight - 560);
            float size = aspect < 1 ? Mathf.Max(5.75f, Mathf.Max(2.75f / aspect, portraitFit)) : Mathf.Lerp(8.5f, 4.65f, Mathf.InverseLerp(1, 1.5f, aspect));
            view.orthographicSize = size - zoom;
            trauma = Mathf.MoveTowards(trauma, 0, Time.deltaTime * 2.5f); zoom = Mathf.MoveTowards(zoom, 0, Time.deltaTime * .35f);
            float strength = settings.shake ? trauma * trauma * .055f : 0;
            transform.position = home + new Vector3(Mathf.Sin(Time.time * 83), Mathf.Cos(Time.time * 69), 0) * strength;
        }
    }
}
