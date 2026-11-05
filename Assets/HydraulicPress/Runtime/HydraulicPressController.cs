using System;
using System.Collections;
using UnityEngine;

namespace HydraulicPress
{
    public sealed class HydraulicPressController : MonoBehaviour
    {
        public Transform plate, shaft, cylinder, machine, deck;
        public Transform[] breakableParts;
        public Renderer warningLamp;
        public Material readyLamp, failureLamp;
        private Vector3[] partPositions;
        private Quaternion[] partRotations;
        private Vector3 machinePosition, deckPosition;
        private Vector3 cylinderPosition;
        private float rest;
        private AudioManager audioManager;
        private PhysicsEffectManager effects;
        private CameraController cameraRig;
        private float speed;
        public bool Busy { get; private set; }
        public void Initialize(AudioManager audio, PhysicsEffectManager fx, CameraController cameraController, float animationSpeed)
        {
            audioManager = audio; effects = fx; cameraRig = cameraController; speed = animationSpeed;
            rest = plate.localPosition.y; machinePosition = machine.localPosition; deckPosition = deck.localPosition; cylinderPosition = cylinder.localPosition;
            partPositions = new Vector3[breakableParts.Length]; partRotations = new Quaternion[breakableParts.Length];
            for (int i = 0; i < breakableParts.Length; i++) { partPositions[i] = breakableParts[i].localPosition; partRotations[i] = breakableParts[i].localRotation; }
            UpdateShaft();
        }
        public void Restore()
        {
            StopAllCoroutines(); Busy = false; SetPlate(rest); machine.localPosition = machinePosition; deck.localPosition = deckPosition;
            plate.localRotation = Quaternion.identity; cylinder.localPosition = cylinderPosition; cylinder.localRotation = Quaternion.identity;
            for (int i = 0; i < breakableParts.Length; i++) { breakableParts[i].localPosition = partPositions[i]; breakableParts[i].localRotation = partRotations[i]; }
            warningLamp.sharedMaterial = readyLamp; audioManager.Motor(false);
        }
        public IEnumerator Crush(CrushableObject target, bool dangerous, Action impactCallback)
        {
            if (Busy) yield break;
            Busy = true; audioManager.Motor(true);
            float contact = 1.08f + target.Definition.height + .13f;
            yield return MovePlate(rest, contact, .52f, false);
            if (dangerous)
            {
                audioManager.Play(audioManager.metal, .8f, .6f);
                for (float t = 0; t < .55f; t += Time.deltaTime * speed)
                {
                    SetPlate(contact - Mathf.Sin(t * 22) * .018f);
                    machine.localPosition = machinePosition + Vector3.right * Mathf.Sin(t * 95) * .035f;
                    warningLamp.sharedMaterial = Mathf.Sin(t * 25) > 0 ? failureLamp : readyLamp;
                    yield return null;
                }
                audioManager.Motor(false); audioManager.Play(audioManager.failure, .8f);
                effects.Burst(target.transform.position + Vector3.up * .45f, new Color(.8f, .42f, .17f), CrushMaterial.Metal, true);
                cameraRig.Impact(1.2f); target.Deform(.3f); impactCallback?.Invoke();
                for (float t = 0; t < .6f; t += Time.deltaTime * speed)
                {
                    float p = Mathf.Clamp01(t / .6f);
                    SetPlate(Mathf.Lerp(contact, 1.3f, p)); plate.localRotation = Quaternion.Euler(0, 0, -14 * p);
                    cylinder.localRotation = Quaternion.Euler(0, 0, 8 * p);
                    for (int i = 0; i < breakableParts.Length; i++)
                    {
                        float side = i % 2 == 0 ? -1 : 1;
                        breakableParts[i].localPosition = partPositions[i] + new Vector3(side * p * .8f, -p * p * 1.4f + Mathf.Sin(p * Mathf.PI) * .6f, -p * .4f);
                        breakableParts[i].localRotation = partRotations[i] * Quaternion.Euler(p * 75, 0, side * p * 65);
                    }
                    machine.localPosition = machinePosition + Vector3.right * Mathf.Sin(t * 65) * .02f * (1 - p);
                    yield return null;
                }
                warningLamp.sharedMaterial = failureLamp;
                yield return Delay(.3f);
            }
            else
            {
                audioManager.MaterialImpact(target.Definition.response); cameraRig.Impact(.65f);
                yield return Delay(.035f); // Impact hold leaves browser audio and input clocks intact.
                bool burst = false;
                for (float t = 0; t < .48f; t += Time.deltaTime * speed)
                {
                    float p = Mathf.Clamp01(t / .48f);
                    SetPlate(Mathf.Lerp(contact, 1.24f, p)); target.Deform(p);
                    deck.localPosition = deckPosition + Vector3.up * Mathf.Sin(t * 70) * .014f * (1 - p);
                    if (!burst && p > (target.Definition.response == CrushMaterial.Brittle ? .25f : .65f))
                    {
                        burst = true; effects.Burst(target.transform.position + Vector3.up * .18f, target.Definition.fragmentColor, target.Definition.response);
                        cameraRig.Impact(.45f); impactCallback?.Invoke();
                        if (target.Definition.response == CrushMaterial.Brittle) target.gameObject.SetActive(false);
                    }
                    yield return null;
                }
                deck.localPosition = deckPosition;
                yield return Delay(.10f); audioManager.Motor(true, .8f);
                yield return MovePlate(1.24f, rest, .46f, true);
                if (target.Definition.response == CrushMaterial.Elastic)
                    for (float t = 0; t < .18f; t += Time.deltaTime * speed) { target.Deform(.5f + Mathf.Cos(t * 30) * .25f * (1 - t / .18f)); yield return null; }
                audioManager.Motor(false); audioManager.Play(audioManager.success, .28f);
            }
            machine.localPosition = machinePosition; Busy = false;
        }
        private IEnumerator MovePlate(float from, float to, float duration, bool retract)
        {
            for (float t = 0; t < duration; t += Time.deltaTime * speed)
            {
                float p = t / duration; p = retract ? 1 - Mathf.Pow(1 - p, 3) : p * p;
                SetPlate(Mathf.Lerp(from, to, p)); yield return null;
            }
            SetPlate(to);
        }
        private IEnumerator Delay(float duration) { float t = 0; while (t < duration) { t += Time.deltaTime * speed; yield return null; } }
        private void SetPlate(float y) { var p = plate.localPosition; p.y = y; plate.localPosition = p; UpdateShaft(); }
        private void UpdateShaft() { float top = 4.25f, bottom = plate.localPosition.y + .15f; shaft.localPosition = new Vector3(0, (top + bottom) * .5f, 0); shaft.localScale = new Vector3(1, Mathf.Max(.1f, top - bottom), 1); }
    }
}
