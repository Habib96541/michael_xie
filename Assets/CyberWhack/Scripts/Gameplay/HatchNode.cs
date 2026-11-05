using System;
using CyberWhack.Art;
using CyberWhack.Audio;
using CyberWhack.Data;
using CyberWhack.VFX;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyberWhack.Gameplay
{
    public enum HatchState
    {
        Idle,
        Emerging,
        Active,
        Submerging,
        Struck
    }

    /// <summary>
    /// Subterranean hydraulic hatch node. Uses a RectMask2D bounding mask
    /// to slide entities vertically upward from beneath the circular hatch rim
    /// with zero additional stencil render passes.
    /// Implements IPointerDownHandler for zero-latency initial contact hit detection.
    /// </summary>
    public class HatchNode : MonoBehaviour, IPointerDownHandler
    {
        [Header("Node Identification")]
        [SerializeField] private int nodeIndex;

        [Header("Masked Entity Container")]
        [SerializeField] private RectTransform maskedContainer;
        [SerializeField] private RectTransform targetTransform;
        [SerializeField] private Image targetImage;
        [SerializeField] private Image crackOverlayImage;

        [Header("Sprites Configuration")]
        [SerializeField] private Sprite standardMoleSprite;
        [SerializeField] private Sprite goldMoleSprite;
        [SerializeField] private Sprite armoredMoleSprite;
        [SerializeField] private Sprite armoredCrackedSprite;
        [SerializeField] private Sprite empDecoySprite;

        [Header("Slide Dynamics")]
        [SerializeField] private float slideDuration = 0.07f;
        [SerializeField] private float subterraneanYOffset = -140f;
        [SerializeField] private float activeYPosition = 0f;

        public int NodeIndex => nodeIndex;
        public bool IsOccupied => _state != HatchState.Idle;
        public TargetType CurrentTargetType => _currentTargetType;

        public event Action<HatchNode, TargetType, bool> OnTargetStruck; // node, type, isDestroyed
        public event Action<HatchNode> OnEmptyHatchTapped;

        private HatchState _state = HatchState.Idle;
        private TargetType _currentTargetType = TargetType.None;
        private float _activeTimeRemaining;
        private float _slideTimer;
        private int _currentHitsTaken;
        private int _requiredHits;
        private RectTransform _rectTransform;
        private float _flash;
        private Color _flashTint = Color.white;
        private float _nodeScale = 1f;

        private Image _lidLeft;
        private Image _lidRight;
        private bool _lidsAllowed;
        private float _blinkTimer = 2f;
        private float _blinkTime = -1f;
        private const float BlinkDuration = 0.15f;

        private static Sprite s_CachedStandard;
        private static Sprite s_CachedGold;
        private static Sprite s_CachedArmored;
        private static Sprite s_CachedArmoredCracked;
        private static Sprite s_CachedEmp;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (targetTransform == null)
            {
                Transform foundMask = transform.Find("MaskContainer");
                if (foundMask != null)
                {
                    Transform foundTarget = foundMask.Find("TargetSprite");
                    if (foundTarget != null)
                    {
                        targetTransform = foundTarget as RectTransform;
                        if (targetImage == null)
                        {
                            targetImage = foundTarget.GetComponent<Image>();
                        }
                    }
                }
            }
            ResetToSubterranean();
        }

        public void ConfigureSprites(Sprite standard, Sprite gold, Sprite armored, Sprite armoredCracked, Sprite emp)
        {
            standardMoleSprite = standard;
            goldMoleSprite = gold;
            armoredMoleSprite = armored;
            armoredCrackedSprite = armoredCracked;
            empDecoySprite = emp;
        }

        public void SetNodeIndex(int index)
        {
            nodeIndex = index;
        }

        public void UseArcadeMotion(float slideSeconds)
        {
            slideDuration = Mathf.Max(0.05f, slideSeconds);
        }

        /// <summary>
        /// Keeps the touch target and slide distance proportional when the
        /// responsive layout reflows for a different mobile aspect ratio.
        /// </summary>
        public void ConfigureResponsiveSize(float nodeSize)
        {
            if (targetTransform != null)
            {
                float targetSize = nodeSize * 0.76f;
                targetTransform.sizeDelta = new Vector2(targetSize, targetSize);
            }

            subterraneanYOffset = -nodeSize * 0.48f;
            // The face artwork sits above its pivot. Lift the active pose so
            // the eyes sit in the middle of the hatch opening rather than
            // peeking out from below the dirt lip.
            activeYPosition = nodeSize * 0.30f;
            if (!IsOccupied && targetTransform != null)
            {
                targetTransform.anchoredPosition = new Vector2(0f, subterraneanYOffset);
            }
        }

        public void SpawnTarget(TargetType type, float activeDuration, int requiredHits)
        {
            _currentTargetType = type;
            _activeTimeRemaining = activeDuration;
            _requiredHits = requiredHits;
            _currentHitsTaken = 0;

            if (crackOverlayImage != null)
            {
                crackOverlayImage.gameObject.SetActive(false);
            }

            // Ensure sprites are populated (runtime cartoon fallback cached once)
            if (standardMoleSprite == null)
            {
                if (s_CachedStandard == null)
                {
                    s_CachedStandard = CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Normal);
                    s_CachedGold = CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Golden);
                    s_CachedArmored = CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Helmet);
                    s_CachedArmoredCracked = CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.HelmetCracked);
                    s_CachedEmp = CartoonAtlasGenerator.CreateCartoonBombSprite(256);
                }
                standardMoleSprite = s_CachedStandard;
                goldMoleSprite = s_CachedGold;
                armoredMoleSprite = s_CachedArmored;
                armoredCrackedSprite = s_CachedArmoredCracked;
                empDecoySprite = s_CachedEmp;
            }

            // Assign sprite based on target type
            if (targetImage != null)
            {
                targetImage.sprite = type switch
                {
                    TargetType.StandardCyberMole => standardMoleSprite,
                    TargetType.OverclockedGoldMole => goldMoleSprite,
                    TargetType.ArmoredTankMole => armoredMoleSprite,
                    TargetType.EmpDecoyMine => empDecoySprite,
                    _ => null
                };
                targetImage.gameObject.SetActive(true);
            }

            ConfigureEyes(type);

            _state = HatchState.Emerging;
            _slideTimer = 0.0f;

            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.PlayImpact(
                    EffectPoolManager.Instance.FromWorld(transform.position),
                    ImpactFlavor.Emerge);
            }
        }

        private void Update()
        {
            bool paused = CyberGameDirector.Instance != null && CyberGameDirector.Instance.CurrentState == GameState.Paused;
            float dt = paused ? 0f : Time.deltaTime;

            _nodeScale = Mathf.Lerp(_nodeScale, 1f, 1f - Mathf.Exp(-14f * Mathf.Max(dt, 0f)));
            if (Mathf.Abs(_nodeScale - 1f) < 0.01f) _nodeScale = 1f;
            transform.localScale = new Vector3(_nodeScale, _nodeScale, 1f);

            if (_flash > 0f && targetImage != null)
            {
                _flash = Mathf.MoveTowards(_flash, 0f, dt * 7.5f);
                targetImage.color = Color.Lerp(Color.white, _flashTint, _flash);
            }

            UpdateBlink(dt);

            if (paused || _state == HatchState.Idle || targetTransform == null) return;

            switch (_state)
            {
                case HatchState.Emerging:
                    _slideTimer += dt;
                    float emergeRatio = Mathf.Clamp01(_slideTimer / slideDuration);
                    float eased = EaseOutBack(emergeRatio);
                    float curY = Mathf.LerpUnclamped(subterraneanYOffset, activeYPosition, eased);
                    targetTransform.anchoredPosition = new Vector2(0f, curY);
                    targetTransform.localRotation = Quaternion.identity;
                    float widen = Mathf.Sin(emergeRatio * Mathf.PI);
                    targetTransform.localScale = new Vector3(1f + widen * 0.18f, 1f - widen * 0.12f, 1f);

                    if (emergeRatio >= 1.0f)
                    {
                        _state = HatchState.Active;
                        targetTransform.localScale = Vector3.one;
                    }
                    break;

                case HatchState.Active:
                    _activeTimeRemaining -= dt;
                    float speed = _currentTargetType == TargetType.EmpDecoyMine ? 9.5f : 4.2f;
                    float amp = _currentTargetType == TargetType.EmpDecoyMine ? 7f : 3.5f;
                    float bob = Mathf.Sin(Time.time * speed + nodeIndex) * amp;
                    targetTransform.anchoredPosition = new Vector2(0f, activeYPosition + bob);
                    float breathe = Mathf.Sin(Time.time * 3.2f + nodeIndex);
                    targetTransform.localScale = new Vector3(1f + breathe * 0.035f, 1f - breathe * 0.03f, 1f);
                    targetTransform.localRotation = Quaternion.Euler(
                        0f, 0f, Mathf.Sin(Time.time * 2.4f + nodeIndex * 1.7f) * 2.4f);

                    if (_activeTimeRemaining <= 0.0f)
                    {
                        _state = HatchState.Submerging;
                        _slideTimer = 0.0f;
                    }
                    break;

                case HatchState.Submerging:
                case HatchState.Struck:
                    _slideTimer += dt;
                    float submergeRatio = Mathf.Clamp01(_slideTimer / slideDuration);
                    float drop = submergeRatio * submergeRatio;
                    float subY = Mathf.Lerp(activeYPosition, subterraneanYOffset, drop);
                    targetTransform.anchoredPosition = new Vector2(0f, subY);
                    float settle = Mathf.Clamp01(_slideTimer / 0.1f);
                    Vector3 struckScale = _state == HatchState.Struck
                        ? new Vector3(1.26f, 0.72f, 1f)
                        : new Vector3(0.92f, 1.08f, 1f);
                    targetTransform.localScale = Vector3.Lerp(struckScale, Vector3.one, settle);
                    targetTransform.localRotation = Quaternion.Slerp(
                        targetTransform.localRotation, Quaternion.identity, 1f - Mathf.Exp(-18f * dt));

                    if (submergeRatio >= 1.0f)
                    {
                        ResetToSubterranean();
                    }
                    break;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        public void ResetToSubterranean()
        {
            _state = HatchState.Idle;
            _currentTargetType = TargetType.None;
            _currentHitsTaken = 0;
            if (targetTransform != null)
            {
                targetTransform.anchoredPosition = new Vector2(0f, subterraneanYOffset);
                targetTransform.localScale = Vector3.one;
            }
            if (targetImage != null) targetImage.color = Color.white;
            if (targetTransform != null) targetTransform.localRotation = Quaternion.identity;
            _flash = 0f;
            _blinkTime = -1f;
            SetLidsVisible(false);
            if (crackOverlayImage != null)
            {
                crackOverlayImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Two fur-coloured lids over the painted eyes. Hiding them makes the
        /// single-sprite mole blink, which is the cheapest possible life sign.
        /// </summary>
        private void ConfigureEyes(TargetType type)
        {
            if (targetTransform == null) return;

            EnsureLids();
            LayoutLids();

            _lidsAllowed = type != TargetType.EmpDecoyMine;
            Color fur = type == TargetType.OverclockedGoldMole
                ? new Color32(245, 176, 65, 255)
                : new Color32(49, 49, 53, 255);

            if (_lidLeft != null) _lidLeft.color = fur;
            if (_lidRight != null) _lidRight.color = fur;
            SetLidsVisible(false);

            _blinkTime = -1f;
            _blinkTimer = UnityEngine.Random.Range(1.1f, 2.9f);
        }

        private void EnsureLids()
        {
            if (_lidLeft != null || targetTransform == null) return;
            _lidLeft = CreateLid("EyeLidLeft");
            _lidRight = CreateLid("EyeLidRight");
        }

        private Image CreateLid(string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(targetTransform, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(24f, 24f);

            Image image = obj.GetComponent<Image>();
            image.sprite = PolishSprites.Ellipse;
            image.color = new Color32(49, 49, 53, 255);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private void LayoutLids()
        {
            if (targetTransform == null || _lidLeft == null || _lidRight == null) return;

            Vector2 size = targetTransform.rect.size;
            if (size.x <= 1f || size.y <= 1f) return;

            float lidSize = size.x * 0.098f;
            RectTransform left = _lidLeft.rectTransform;
            left.sizeDelta = new Vector2(lidSize, lidSize);
            left.anchoredPosition = new Vector2(-size.x * 0.164f, size.y * 0.223f);

            RectTransform right = _lidRight.rectTransform;
            right.sizeDelta = new Vector2(lidSize, lidSize);
            right.anchoredPosition = new Vector2(size.x * 0.164f, size.y * 0.223f);
        }

        private void UpdateBlink(float dt)
        {
            if (!_lidsAllowed || _lidLeft == null || _lidRight == null) return;

            if (_state == HatchState.Idle || _state == HatchState.Submerging || _state == HatchState.Struck)
            {
                SetLidsVisible(false);
                return;
            }

            if (_blinkTime >= 0f)
            {
                _blinkTime += dt;
                float t = _blinkTime / BlinkDuration;
                if (t >= 1f)
                {
                    _blinkTime = -1f;
                    _blinkTimer = UnityEngine.Random.Range(1.7f, 4.4f);
                    SetLidsVisible(false);
                    return;
                }

                float cover = Mathf.Sin(t * Mathf.PI);
                float lidScaleY = 0.35f + cover * 0.95f;
                SetLidsVisible(true);
                _lidLeft.rectTransform.localScale = new Vector3(1f, lidScaleY, 1f);
                _lidRight.rectTransform.localScale = new Vector3(1f, lidScaleY, 1f);
                return;
            }

            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blinkTime = 0f;
            }
            else
            {
                SetLidsVisible(false);
            }
        }

        private void SetLidsVisible(bool visible)
        {
            if (_lidLeft != null) _lidLeft.enabled = visible;
            if (_lidRight != null) _lidRight.enabled = visible;
        }

        /// <summary>
        /// Zero-latency pointer down callback. Fires on initial finger contact,
        /// avoiding standard 300ms mobile browser tap delays.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            Vector2 screenPos = eventData.position;
            CyberAudioManager.Instance.UnlockMobileAudioContext();

            // Resolve the tap in the pooled-effect space so the dust lands
            // exactly under the finger on every aspect ratio.
            Vector2 localPoint = EffectPoolManager.Instance != null
                ? EffectPoolManager.Instance.FromScreen(screenPos)
                : screenPos;

            if (_state == HatchState.Emerging || _state == HatchState.Active)
            {
                HandleTargetHit(localPoint);
            }
            else if (_state == HatchState.Struck)
            {
                // Already sinking. A second tap is not a miss.
            }
            else
            {
                CyberAudioManager.Instance?.PlayMiss();
                _nodeScale = 0.9f;
                ArcadeFeel.Impact(0.08f, 0f);
                if (EffectPoolManager.Instance != null)
                {
                    EffectPoolManager.Instance.PlayImpact(localPoint, ImpactFlavor.Miss);
                    EffectPoolManager.Instance.SpawnCallout(localPoint, "MISS", new Color(0.45f, 0.24f, 0.12f, 1f));
                }
                OnEmptyHatchTapped?.Invoke(this);
            }
        }

        private void HandleTargetHit(Vector2 localPos)
        {
            _currentHitsTaken++;

            if (_currentTargetType == TargetType.ArmoredTankMole)
            {
                if (_currentHitsTaken < _requiredHits)
                {
                    // First tap cracks visor
                    if (targetImage != null && armoredCrackedSprite != null)
                    {
                        targetImage.sprite = armoredCrackedSprite;
                    }
                    CyberAudioManager.Instance?.PlayArmorCrack();
                    _flash = 1f;
                    _flashTint = new Color(1f, 0.82f, 0.35f, 1f);
                    _nodeScale = 1.06f;
                    if (targetTransform != null) targetTransform.localScale = new Vector3(1.18f, 0.84f, 1f);
                    ArcadeFeel.Impact(0.22f, 0.12f);
                    if (EffectPoolManager.Instance != null)
                    {
                        EffectPoolManager.Instance.PlayImpact(localPos, ImpactFlavor.ArmorCrack);
                        EffectPoolManager.Instance.SpawnCallout(localPos, "CRACK", new Color(1f, 0.78f, 0.25f, 1f));
                    }
                    OnTargetStruck?.Invoke(this, _currentTargetType, false);
                    return;
                }
            }

            // Target is fully destroyed or triggered
            bool wasEmp = (_currentTargetType == TargetType.EmpDecoyMine);
            TargetType destroyedType = _currentTargetType;

            if (wasEmp)
            {
                CyberAudioManager.Instance?.PlayEmpHazard();
                _flash = 1f;
                _flashTint = new Color(1f, 0.45f, 0.3f, 1f);
                _nodeScale = 1.12f;
                ArcadeFeel.ImpactTinted(0.82f, new Color(1f, 0.38f, 0.2f, 1f), 0.46f);
                ArcadeFeel.HitStop(0.07f, 0.04f);
                if (EffectPoolManager.Instance != null)
                {
                    EffectPoolManager.Instance.PlayImpact(localPos, ImpactFlavor.Bomb);
                    EffectPoolManager.Instance.SpawnCallout(localPos, "BOOM", new Color(1f, 0.42f, 0.28f, 1f));
                }
            }
            else if (destroyedType == TargetType.OverclockedGoldMole)
            {
                CyberAudioManager.Instance?.PlayGoldHit();
                _flash = 1f;
                _flashTint = new Color(1f, 0.95f, 0.55f, 1f);
                _nodeScale = 1.1f;
                ArcadeFeel.Impact(0.48f, 0.22f);
                ArcadeFeel.HitStop(0.045f, 0.06f);
                if (EffectPoolManager.Instance != null)
                {
                    EffectPoolManager.Instance.PlayImpact(localPos, ImpactFlavor.Gold);
                }
            }
            else
            {
                CyberAudioManager.Instance?.PlayStandardHit();
                _flash = 1f;
                _flashTint = new Color(1f, 0.97f, 0.82f, 1f);
                _nodeScale = 1.07f;
                float trauma = destroyedType == TargetType.ArmoredTankMole ? 0.4f : 0.24f;
                ArcadeFeel.Impact(trauma, 0.14f);
                if (EffectPoolManager.Instance != null)
                {
                    EffectPoolManager.Instance.PlayImpact(localPos,
                        destroyedType == TargetType.ArmoredTankMole ? ImpactFlavor.ArmorBreak : ImpactFlavor.Whack);
                }
            }

            _state = HatchState.Struck;
            _slideTimer = 0.0f;
            OnTargetStruck?.Invoke(this, destroyedType, true);
        }
    }
}
