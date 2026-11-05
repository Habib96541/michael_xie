using UnityEngine;

namespace BottleFlip
{
    /// <summary>Handles framing, the subtle idle drift, hit shake and the game-over zoom.</summary>
    public class GameCamera : MonoBehaviour
    {
        public Camera Cam;

        public Vector3 HomePos = new Vector3(0f, 1.75f, -9.35f);
        public Vector3 HomeTarget = new Vector3(0f, 0.80f, 0f);
        // Game-over framing: the bottle lies to the left of the stage centre, so the camera
        // recentres on it and backs off enough to keep the whole bottle in frame.
        public Vector3 FocusPos = new Vector3(-0.80f, 1.60f, -8.6f);
        public Vector3 FocusTarget = new Vector3(-0.80f, -0.18f, 0f);

        Vector3 _pos, _target;
        float _fovKick;
        float _dolly = 1f;   // 1 = normal framing, >1 = pulled back to fit a high toss
        float _shake;
        float _shakeT;
        float _shakeDur = 1f;
        float _idleT;
        bool _built;

        public void Build(float baseFov = 33f)
        {
            _pos = HomePos;
            _target = HomeTarget;
            Cam.fieldOfView = baseFov;
            transform.position = _pos;
            transform.rotation = Quaternion.LookRotation(_target - _pos, Vector3.up);
            _built = true;
        }

        public void Punch(float strength = 1f)
        {
            float from = -2.2f * strength;
            Tween.To(0.38f, t => _fovKick = Mathf.Lerp(from, 0f, e(t)), Ease.Linear);
        }

        static float e(float t) => Ease.OutCubic(t);

        public void Shake(float strength, float duration = 0.28f)
        {
            _shake = Mathf.Max(_shake, strength);
            _shakeDur = duration;
            _shakeT = duration;
        }

        public void FocusBottle(bool focus, bool instant = false)
        {
            Vector3 p = focus ? FocusPos : HomePos;
            Vector3 t = focus ? FocusTarget : HomeTarget;
            if (instant || !_built)
            {
                _pos = p;
                _target = t;
                return;
            }
            Vector3 p0 = _pos, t0 = _target;
            Tween.To(0.75f, k =>
            {
                _pos = Vector3.Lerp(p0, p, k);
                _target = Vector3.Lerp(t0, t, k);
            }, Ease.InOutCubic);
        }

        /// <summary>Pulls the camera straight back along its view axis (a true zoom-out that
        /// keeps the lens) and eases it home again.</summary>
        public void ZoomTo(float factor, float duration)
        {
            factor = Mathf.Clamp(factor, 1f, 3f);
            float from = _dolly;
            Tween.To(duration, k => _dolly = Mathf.Lerp(from, factor, k), Ease.InOutCubic);
        }

        public void ResetZoom()
        {
            _dolly = 1f;
        }

        /// <summary>Frames a toss that will peak at <paramref name="apex"/> world units.</summary>
        public void FollowJump(float apex)
        {
            const float halfFovTan = 0.2962f;    // tan(33 deg fov / 2)
            const float baseDist = 9.398f;       // home camera distance to its look target
            const float halfSpan = halfFovTan * baseDist;   // half the frame height at the bottle
            const float lift = 0.55f;            // how far the look target rises per unit of pull-back

            // Top of an upright bottle at the apex, measured from the normal look target.
            float needed = (apex + 2.05f) - HomeTarget.y;
            float factor = (needed + lift) / (halfSpan + lift) * 1.04f;
            ZoomTo(factor, 0.25f);
        }

        void LateUpdate()
        {
            if (!_built) return;

            _idleT += Time.unscaledDeltaTime;
            Vector3 idle = new Vector3(Mathf.Sin(_idleT * 0.42f) * 0.05f, Mathf.Sin(_idleT * 0.31f) * 0.035f, 0f);

            float s = 0f;
            if (_shakeT > 0f)
            {
                _shakeT -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_shakeT / Mathf.Max(0.0001f, _shakeDur));
                s = _shake * k * k;
            }

            Vector3 noise = Vector3.zero;
            if (s > 0.0001f)
            {
                noise = new Vector3(
                    (Mathf.PerlinNoise(Time.unscaledTime * 26f, 3.1f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(7.7f, Time.unscaledTime * 23f) - 0.5f) * 2f,
                    0f) * s;
            }

            // While pulled back for a high toss, lift the look target a little so the bottle
            // sits in the middle of the frame instead of hugging the bottom edge.
            Vector3 target = _target + Vector3.up * ((_dolly - 1f) * 0.55f);
            Vector3 basePos = _pos + idle + noise;
            Vector3 look = target - basePos;
            if (look.sqrMagnitude > 0.0001f)
            {
                Vector3 dir = look.normalized;
                transform.position = target - dir * (look.magnitude * _dolly);
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            Cam.fieldOfView = 33f + _fovKick;
        }
    }
}
