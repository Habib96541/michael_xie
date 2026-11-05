using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>
    /// Procedurally built bottle plus the flip motion. The toss is integrated rather than
    /// keyframed: real gravity drives the arc, the spin has drag, and the landing is never
    /// perfect - the bottle hits with a few degrees of residual tilt and rocks itself upright.
    /// Height, airtime, spin count and settle time are re-rolled on every tap.
    /// </summary>
    public class BottleRig : MonoBehaviour
    {
        public Transform Pivot;    // rotation pivot, sits on the floor at the bottle base
        public Transform Squash;   // scale-only node for squash & stretch
        public Transform Blob;     // contact shadow
        public float BodyHeight = 1.96f;

        /// <summary>The bottle landed on its base: the run continues.</summary>
        public event Action Landed;

        /// <summary>The bottle toppled over: the run is over.</summary>
        public event Action Fell;

        public event Action ReadyAgain;

        public bool Busy { get; private set; }
        public bool IdleMotion { get; set; } = true;

        /// <summary>Apex (world units) of the toss that just started. Valid as soon as Flip() returns.</summary>
        public float PlannedApex { get; private set; }

        Coroutine _anim;
        float _idleT;
        Vector3 _pivotRest;
        float _blobBaseScale = 1.3f;
        float _shadowX;          // horizontal drift, mirrored onto the contact shadow

        const float LieHeight = 0.34f;

        // Toss envelope. Apex is measured at the base of the bottle, so even the tallest
        // toss keeps the whole bottle inside the portrait frame.
        const float Gravity = 28f;
        const float ApexMin = 2.55f, ApexMax = 3.75f;
        const float ApexFallMin = 2.70f, ApexFallMax = 3.90f;
        const float LowTossChance = 0.15f;
        const float LowApexMin = 1.95f, LowApexMax = 2.45f;

        public void Build()
        {
            Pivot = new GameObject("BottlePivot").transform;
            Pivot.SetParent(transform, false);

            Squash = new GameObject("BottleSquash").transform;
            Squash.SetParent(Pivot, false);

            BuildBottleGeometry(Squash);

            // Contact shadow on the floor.
            var blobGo = new GameObject("ContactShadow");
            blobGo.transform.SetParent(transform, false);
            blobGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            blobGo.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            Blob = blobGo.transform;

            var mf = blobGo.AddComponent<MeshFilter>();
            mf.sharedMesh = Art.QuadMesh(1f, "ShadowQuad");
            var mr = blobGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Art.Unlit(Art.Blob, Pal.Hex("000000").Alpha(0.55f), false, "ContactShadow");
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Blob.localScale = new Vector3(_blobBaseScale, _blobBaseScale, 1f);

            ResetPose();
        }

        void BuildBottleGeometry(Transform parent)
        {
            var bodyProfile = new List<Vector2>
            {
                new Vector2(0.300f, 0.000f),
                new Vector2(0.322f, 0.045f),
                new Vector2(0.335f, 0.100f),
                new Vector2(0.335f, 0.560f),
                new Vector2(0.335f, 0.960f),
                new Vector2(0.330f, 1.020f),
                new Vector2(0.318f, 1.080f),
                new Vector2(0.300f, 1.150f),
                new Vector2(0.272f, 1.225f),
                new Vector2(0.240f, 1.300f),
                new Vector2(0.206f, 1.375f),
                new Vector2(0.182f, 1.440f),
                new Vector2(0.168f, 1.500f),
                new Vector2(0.160f, 1.560f),
                new Vector2(0.158f, 1.720f),
                new Vector2(0.174f, 1.748f),
                new Vector2(0.176f, 1.780f),
                new Vector2(0.160f, 1.800f)
            };
            AddPart("BottleBody", Lathe.Build(bodyProfile, 44, "BottleBody", true, true),
                Art.Glossy(Pal.Hex("E9F5FF"), 0.06f, 0.9f, "BottleGlass"), parent);

            // Printed label around the barrel.
            var label = new List<Vector2>
            {
                new Vector2(0.3415f, 0.330f),
                new Vector2(0.3435f, 0.350f),
                new Vector2(0.3435f, 0.880f),
                new Vector2(0.3415f, 0.900f)
            };
            var labelMat = Art.Textured(Art.Label, Color.white, 0.32f);
            labelMat.mainTextureScale = new Vector2(2f, 1f);
            AddPart("BottleLabel", Lathe.Build(label, 44, "BottleLabel", false, false), labelMat, parent);

            // Cap and its little ring.
            var ring = new List<Vector2>
            {
                new Vector2(0.166f, 1.796f),
                new Vector2(0.182f, 1.802f),
                new Vector2(0.182f, 1.820f),
                new Vector2(0.166f, 1.826f)
            };
            AddPart("CapRing", Lathe.Build(ring, 40, "CapRing", false, false),
                Art.Glossy(Pal.Hex("C3D3E4"), 0.55f, 0.85f, "CapRing"), parent);

            var cap = new List<Vector2>
            {
                new Vector2(0.1705f, 1.816f),
                new Vector2(0.1745f, 1.836f),
                new Vector2(0.1745f, 1.938f),
                new Vector2(0.1665f, 1.958f),
                new Vector2(0.1200f, 1.968f)
            };
            AddPart("Cap", Lathe.Build(cap, 40, "Cap", false, true),
                Art.Glossy(Pal.Amber, 0.08f, 0.72f, "Cap"), parent);
        }

        static void AddPart(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        public void ResetPose()
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            Busy = false;
            IdleMotion = true;
            Pivot.localPosition = Vector3.zero;
            Pivot.localRotation = Quaternion.identity;
            Squash.localScale = Vector3.one;
            _shadowX = 0f;
            if (Blob != null)
            {
                Blob.localScale = new Vector3(_blobBaseScale, _blobBaseScale, 1f);
                Blob.localPosition = new Vector3(0f, 0.006f, 0f);
            }
        }

        /// <summary>Immediately freezes the bottle upright (used when the run restarts).</summary>
        public void SnapUpright()
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            Busy = false;
            IdleMotion = false;
            Pivot.localPosition = Vector3.zero;
            Pivot.localRotation = Quaternion.identity;
            Squash.localScale = Vector3.one;
            _shadowX = 0f;
        }

        /// <summary>Tosses the bottle. When <paramref name="falls"/> is true it topples over.</summary>
        public void Flip(bool falls)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(FlipRoutine(falls));
        }

        IEnumerator FlipRoutine(bool falls)
        {
            Busy = true;
            IdleMotion = false;
            _shadowX = 0f;

            // 1. Pick the toss before the crouch, so the camera knows how far it has to pull
            //    back this time. Apex and spin count are re-rolled on every tap.
            float apex = falls
                ? UnityEngine.Random.Range(ApexFallMin, ApexFallMax)
                : (UnityEngine.Random.value < LowTossChance
                    ? UnityEngine.Random.Range(LowApexMin, LowApexMax)
                    : UnityEngine.Random.Range(ApexMin, ApexMax));
            PlannedApex = apex;

            // 2. Anticipation: a short, uneven crouch. Never the same twice.
            float anticipation = UnityEngine.Random.Range(0.045f, 0.095f);
            float crouch = UnityEngine.Random.Range(0.09f, 0.15f);
            float t = 0f;
            while (t < anticipation)
            {
                t += Time.deltaTime;
                float k = Ease.OutQuad(Ease.Clamp01(t / anticipation));
                Squash.localScale = new Vector3(1f + crouch * k, 1f - crouch * 1.3f * k, 1f + crouch * k);
                yield return null;
            }

            float ground = falls ? LieHeight : 0f;
            float vy0 = Mathf.Sqrt(2f * Gravity * apex);

            // Exact flight time of a ballistic arc from the base up to its landing height.
            float flight = (vy0 + Mathf.Sqrt(Mathf.Max(0f, vy0 * vy0 - 2f * Gravity * ground))) / Gravity;

            int turns = UnityEngine.Random.value < 0.55f ? 1 : 2;

            // Where the spin should be when the base touches down. A stand lands within a few
            // degrees of upright; a fall is caught badly short and goes over.
            float impactAngle;
            if (falls)
            {
                impactAngle = turns * 360f + UnityEngine.Random.Range(60f, 104f);
            }
            else
            {
                float err = UnityEngine.Random.Range(2.5f, 9.5f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                if (UnityEngine.Random.value < 0.18f) err *= 1.9f;   // the occasional near-miss
                impactAngle = turns * 360f + err;
            }

            // Angular braking shape: some tosses coast almost evenly, others shed most of their
            // speed in the last third. Whatever the shape, the total is exactly `impactAngle`,
            // so the bottle can never end up spinning a fraction more than its 1 or 2 turns.
            float brake = UnityEngine.Random.Range(0.5f, 2.4f);

            float px = 0f;
            float py = 0f;
            float vx = UnityEngine.Random.Range(-0.15f, 0.15f);
            float angle = 0f;
            float airT = 0f;

            // Slow out-of-plane lean so the flip reads as a body tumbling in 3D, not a decal.
            float tiltPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float tiltAmp = UnityEngine.Random.Range(2.5f, 7f);
            float tiltHz = UnityEngine.Random.Range(1.1f, 2.2f);

            bool landed = false;
            while (!landed)
            {
                // Advance the whole frame in small fixed steps. Without this a slow device
                // (or a low frame rate) would stretch the toss out and rob it of its timing.
                float remaining = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
                while (remaining > 0f && !landed)
                {
                    float dt = Mathf.Min(remaining, 1f / 120f);
                    remaining -= dt;
                    airT += dt;

                    vy0 -= Gravity * dt;
                    py += vy0 * dt;
                    px += vx * dt;
                    vx *= Mathf.Exp(-0.8f * dt);

                    // The spin is a function of how far through the flight we are, not something
                    // accumulated frame by frame. It therefore always finishes on exactly the
                    // planned angle - no drifting into an extra half turn on a long toss or a
                    // slow frame, and no catch-up spin at the end.
                    angle = impactAngle * SpinProgress(airT / flight, brake);

                    if (py <= ground && vy0 < 0f) landed = true;
                }

                float speed = Mathf.Clamp01(Mathf.Abs(vy0) / Mathf.Max(1f, Mathf.Sqrt(2f * Gravity * apex)));
                Squash.localScale = new Vector3(1f - 0.05f * speed, 1f + 0.07f * speed, 1f - 0.05f * speed);

                float lean = tiltAmp * Mathf.Sin(airT * tiltHz * Mathf.PI * 2f + tiltPhase)
                             * Mathf.Sin(Ease.Clamp01(airT / Mathf.Max(0.001f, flight)) * Mathf.PI);

                Pivot.localPosition = new Vector3(px, Mathf.Max(py, 0f), 0f);
                Pivot.localRotation = Quaternion.Euler(lean, 0f, angle);
                _shadowX = px;
                yield return null;
            }

            Squash.localScale = Vector3.one;
            Pivot.localPosition = new Vector3(px, ground, 0f);
            Pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (falls) yield return FallRoutine(px, angle);
            else yield return StandRoutine(px, angle);
        }

        /// <summary>
        /// Successful landing. The base plants off-square, so the bottle rocks: a damped
        /// oscillation that dies out over a random settle time, with a small hop on the
        /// harder contacts. Only then is it ready for the next tap.
        /// </summary>
        IEnumerator StandRoutine(float px, float impactAngle)
        {
            Landed?.Invoke();

            // How badly the landing was caught (degrees off square).
            float err = Mathf.DeltaAngle(0f, impactAngle);
            float hard = Mathf.Clamp01(Mathf.Abs(err) / 16f);

            float settle = Mathf.Lerp(
                UnityEngine.Random.Range(0.24f, 0.38f),
                UnityEngine.Random.Range(0.42f, 0.62f), hard);
            float cycles = Mathf.Lerp(1.4f, 2.6f, hard);
            float hop = UnityEngine.Random.value < 0.40f
                ? UnityEngine.Random.Range(0.035f, 0.10f) * (0.4f + hard)
                : UnityEngine.Random.Range(0.004f, 0.022f);
            float squashK = Mathf.Lerp(0.07f, 0.16f, hard);

            float t = 0f;
            while (t < settle)
            {
                t += Time.deltaTime;
                float u = Ease.Clamp01(t / settle);

                float decay = Mathf.Exp(-4.2f * u);
                float carry = 1f - Mathf.Pow(u, 3f);          // drives everything home at the end

                float wobble = err * decay * Mathf.Cos(u * Mathf.PI * 2f * cycles) * carry;
                float lift = hop * Mathf.Abs(Mathf.Sin(u * Mathf.PI * cycles)) * carry;
                float sy = 1f - squashK * Mathf.Exp(-9f * u) * Mathf.Cos(u * Mathf.PI * 5f);

                Squash.localScale = new Vector3(1f - (1f - sy) * 0.55f, sy, 1f - (1f - sy) * 0.55f);
                Pivot.localPosition = new Vector3(px * carry, lift, 0f);
                Pivot.localRotation = Quaternion.Euler(0f, 0f, wobble);
                _shadowX = px * carry;
                yield return null;
            }

            Pivot.localPosition = Vector3.zero;
            Pivot.localRotation = Quaternion.identity;
            Squash.localScale = Vector3.one;
            _shadowX = 0f;
            Finish();
        }

        /// <summary>
        /// Failed landing: caught on the shoulder, teeters with the weight still almost over
        /// the base, then gravity wins and it goes over onto its side.
        /// </summary>
        IEnumerator FallRoutine(float px, float impactAngle)
        {
            float tilt = Mathf.Clamp(Mathf.DeltaAngle(0f, impactAngle), 55f, 112f);
            float teeter = UnityEngine.Random.Range(0.16f, 0.34f);
            float rockAmp = UnityEngine.Random.Range(5f, 15f);
            float rockCycles = UnityEngine.Random.Range(0.9f, 1.9f);
            float rest = UnityEngine.Random.Range(90f, 96f);

            float t = 0f;
            while (t < teeter)
            {
                t += Time.deltaTime;
                float u = Ease.Clamp01(t / teeter);
                float ang = tilt + rockAmp * Mathf.Sin(u * Mathf.PI * 2f * rockCycles) * (0.35f + 0.65f * u);
                Pivot.localPosition = new Vector3(px, LieHeight - 0.02f * Mathf.Sin(u * Mathf.PI), 0f);
                Pivot.localRotation = Quaternion.Euler(0f, 0f, ang);
                yield return null;
            }

            Fell?.Invoke();

            // Gravity takes over. GameManager drops timeScale here, so keep using scaled time.
            float collapse = UnityEngine.Random.Range(0.26f, 0.42f);
            t = 0f;
            while (t < collapse)
            {
                t += Time.deltaTime;
                float u = Ease.Clamp01(t / collapse);
                float ang = Mathf.Lerp(tilt, rest, Ease.InQuad(u)) + Mathf.Sin(u * Mathf.PI * 2.2f) * 3.2f * (1f - u);
                Pivot.localPosition = new Vector3(px + 0.03f * u, LieHeight - 0.012f * u, 0f);
                Pivot.localRotation = Quaternion.Euler(0f, 0f, ang);
                yield return null;
            }

            Pivot.localPosition = new Vector3(px + 0.03f, LieHeight, 0f);
            Pivot.localRotation = Quaternion.Euler(0f, 0f, rest);
            Finish();
        }

        /// <summary>Spin progress 0..1: launches at full tilt, coasts into the landing.</summary>
        static float SpinProgress(float u, float k)
        {
            u = Ease.Clamp01(u);
            return k <= 0.001f ? u : (1f - Mathf.Exp(-k * u)) / (1f - Mathf.Exp(-k));
        }

        void Finish()
        {
            Busy = false;
            _anim = null;
            ReadyAgain?.Invoke();
        }

        void LateUpdate()
        {
            if (IdleMotion)
            {
                _idleT += Time.deltaTime;
                Pivot.localPosition = new Vector3(0f, Mathf.Sin(_idleT * 1.5f) * 0.035f, 0f);
                Pivot.localRotation = Quaternion.Euler(0f, Mathf.Sin(_idleT * 0.55f) * 20f, Mathf.Sin(_idleT * 0.9f) * 2.6f);
            }

            if (Blob != null)
            {
                float h = Mathf.Clamp(Pivot.localPosition.y, 0f, 2f);
                float k = h / 2f;
                float s = _blobBaseScale * (1f - 0.42f * k);
                Blob.localScale = new Vector3(s, s * 1.02f, 1f);
                Blob.localPosition = new Vector3(_shadowX * 0.6f, 0.006f, 0f);

                var mr = Blob.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var c = Pal.Hex("000000").Alpha(Mathf.Lerp(0.55f, 0.16f, k));
                    mr.sharedMaterial.SetColor("_Color", c);
                }
            }
        }
    }
}
