using UnityEngine;

namespace ShatterLeap
{
    public sealed class BridgeCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        private Vector3 _position;
        private Quaternion _rotation;
        private bool _overview = true;
        public void Overview(float length)
        {
            _overview = true;
            _camera.orthographic = true;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _camera.orthographicSize = Mathf.Max((length + 7) * 0.65f, 6.8f / aspect);
            _position = new Vector3(0, 32, length * 0.5f - 9);
            _rotation = Quaternion.LookRotation(new Vector3(0, 0, length * 0.5f) - _position);
        }
        public void Follow(Vector3 player, bool reduced)
        {
            _overview = false;
            _camera.orthographic = false;
            _camera.fieldOfView = Screen.height > Screen.width ? 58 : 52;
            float distance = Screen.height > Screen.width ? 9.5f : 7.5f;
            _position = player + new Vector3(0, 7.8f, -distance);
            _rotation = Quaternion.LookRotation(player + new Vector3(0, 0.1f, 4) - _position);
            if (reduced) { Snap(); }
        }
        public void Snap() => transform.SetPositionAndRotation(_position, _rotation);
        public void Advance(float delta)
        {
            float blend = 1 - Mathf.Exp(-delta * (_overview ? 12 : 7));
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, _position, blend), Quaternion.Slerp(transform.rotation, _rotation, blend));
        }
    }
}
