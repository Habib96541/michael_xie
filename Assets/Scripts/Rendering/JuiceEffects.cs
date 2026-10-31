using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DropStack.Rendering
{
    public class JuiceEffects : MonoBehaviour
    {
        public static JuiceEffects Instance { get; private set; }

        [Header("Camera Shake")]
        [SerializeField] private Transform cameraTransform;
        private Vector3 originalCameraPos;
        private Coroutine shakeCoroutine;

        [Header("Particles")]
        private ParticleSystem lineClearParticles;
        private ParticleSystem dropParticles;
        [SerializeField] private Material particleMaterial;

        [Header("Floating Text Pool")]
        [SerializeField] private RectTransform floatingTextContainer;
        private readonly List<FloatingScoreText> textPool = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                originalCameraPos = cameraTransform.position;
            }

            SetupParticleSystems();
        }

        private void SetupParticleSystems()
        {
            // Line clear burst particles
            GameObject lpsGo = new GameObject("LineClearParticles");
            lpsGo.transform.SetParent(transform, false);
            lineClearParticles = lpsGo.AddComponent<ParticleSystem>();
            lineClearParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = lineClearParticles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 8f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.95f, 0.5f, 1f);
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = lineClearParticles.emission;
            emission.rateOverTime = 0;

            var shape = lineClearParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 0.5f, 0.1f);

            var colorOverLifetime = lineClearParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f), new GradientColorKey(Color.cyan, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var renderer = lpsGo.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 30;
            if (particleMaterial != null) { renderer.material = new Material(particleMaterial); renderer.material.mainTexture = BlockSpriteGenerator.GlowParticleSprite.texture; }

            // Hard drop impact particles
            GameObject dpsGo = new GameObject("HardDropParticles");
            dpsGo.transform.SetParent(transform, false);
            dropParticles = dpsGo.AddComponent<ParticleSystem>();
            dropParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var dMain = dropParticles.main;
            dMain.playOnAwake = false;
            dMain.loop = false;
            dMain.startLifetime = 0.35f;
            dMain.startSpeed = 4f;
            dMain.startSize = 0.18f;
            dMain.startColor = new Color(0.8f, 0.9f, 1f, 0.9f);
            dMain.maxParticles = 100;
            dMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var dEmission = dropParticles.emission;
            dEmission.rateOverTime = 0;

            var dShape = dropParticles.shape;
            dShape.shapeType = ParticleSystemShapeType.Box;
            dShape.scale = new Vector3(3f, 0.2f, 0.1f);

            var dRenderer = dpsGo.GetComponent<ParticleSystemRenderer>();
            dRenderer.sortingOrder = 30;
            if (particleMaterial != null) { dRenderer.material = new Material(particleMaterial); dRenderer.material.mainTexture = BlockSpriteGenerator.GlowParticleSprite.texture; }
        }

        public void TriggerScreenShake(float intensity = 0.15f, float duration = 0.18f)
        {
            if (cameraTransform == null || PlayerPrefs.GetInt("DropStack_Motion", 1) == 0) return;
            if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); cameraTransform.position = originalCameraPos; }
            originalCameraPos = cameraTransform.position;
            shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        public void SetCameraAnchor(Vector3 position)
        {
            if(shakeCoroutine!=null) StopCoroutine(shakeCoroutine);
            shakeCoroutine=null; originalCameraPos=position;
            if(cameraTransform!=null) cameraTransform.position=position;
        }

        public void ResetForRun()
        {
            if(shakeCoroutine!=null) StopCoroutine(shakeCoroutine);
            if(cameraTransform!=null && shakeCoroutine!=null) cameraTransform.position=originalCameraPos;
            shakeCoroutine=null;
            if(lineClearParticles!=null) lineClearParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(dropParticles!=null) dropParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var item in textPool) if(item!=null) item.gameObject.SetActive(false);
            DropStack.UI.ArcadeFeedback.Cancel();
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentIntensity = Mathf.Lerp(intensity, 0f, progress);

                Vector2 offset = Random.insideUnitCircle * currentIntensity;
                cameraTransform.position = originalCameraPos + new Vector3(offset.x, offset.y, 0f);

                yield return null;
            }

            cameraTransform.position = originalCameraPos;
            shakeCoroutine = null;
        }

        public void PlayLineClearEffect(Vector3 centerWorldPos, int lineCount)
        {
            if (lineClearParticles != null)
            {
                lineClearParticles.transform.position = centerWorldPos;
                var main = lineClearParticles.main;
                main.startSize = 0.2f + lineCount * 0.05f;

                int emitCount = 30 + lineCount * 25;
                lineClearParticles.Emit(emitCount);
            }

            float shakeIntensity = 0.08f + lineCount * 0.06f;
            float shakeDuration = 0.12f + lineCount * 0.05f;
            TriggerScreenShake(shakeIntensity, shakeDuration);
        }

        public void PlayHardDropEffect(Vector3 landingWorldPos)
        {
            if (dropParticles != null)
            {
                dropParticles.transform.position = landingWorldPos;
                dropParticles.Emit(18);
            }

            TriggerScreenShake(0.12f, 0.14f);
        }

        public void ShowScorePopup(string text, Color color, Vector2 localPos)
        {
            if (floatingTextContainer == null) return;

            FloatingScoreText item = null;
            foreach (var t in textPool)
            {
                if (!t.gameObject.activeInHierarchy)
                {
                    item = t;
                    break;
                }
            }

            if (item == null)
            {
                GameObject go = new GameObject("FloatingScoreText", typeof(RectTransform), typeof(Text), typeof(CanvasGroup), typeof(FloatingScoreText));
                go.transform.SetParent(floatingTextContainer, false);

                Text txt = go.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 28;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.raycastTarget = false;
                txt.rectTransform.sizeDelta = new Vector2(650,100);

                Outline outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);

                item = go.GetComponent<FloatingScoreText>();
                textPool.Add(item);
            }

            item.Play(text, color, localPos);
        }

        public void SetContainer(RectTransform container)
        {
            floatingTextContainer = container;
        }
    }
}
