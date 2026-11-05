using System;
using System.Collections;
using CyberScratch.Audio;
using CyberScratch.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyberScratch.Scratch
{
    public class ScratchCardController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const int Rows = 5;
        public const int Columns = 3;
        public const int TotalCells = Rows * Columns;
        private const int PointsPerCell = 9;

        [Header("Foil Display & Render Target")]
        [SerializeField] private RawImage _foilRawImage;
        [SerializeField] private RectTransform _cardRectTransform;
        [SerializeField] private Shader _foilShader;
        [SerializeField] private Shader _brushEraseShader;
        [SerializeField] private Texture2D _brushTexture;
        [SerializeField] private Texture2D _foilPatternTexture;
        [SerializeField] private ParticleSystem _dustParticles;
        [SerializeField] private Image[] _cellFoilImages;

        [Header("Scratch Configuration")]
        [SerializeField] private float _brushRadiusUV = 0.065f;
        [SerializeField] private int _renderTextureSize = 512;
        [SerializeField] private int _revealThresholdPoints = 6; // 6 of 9 points (~67%)
        [SerializeField] private float _cellFadeDuration = 0.15f;

        private RenderTexture _maskRenderTexture;
        private Material _foilMaterial;
        private Material _brushMaterial;
        private Material[] _cellFoilMaterials;

        // Subsampled Check-Point Grid (Zero Alloc)
        private readonly Vector2[,] _cellAnchorUVs = new Vector2[TotalCells, PointsPerCell];
        private readonly Rect[] _cellRectUVs = new Rect[TotalCells];
        private readonly bool[,] _clearedPoints = new bool[TotalCells, PointsPerCell];
        private readonly int[] _clearedPointCounts = new int[TotalCells];
        private readonly bool[] _cellRevealed = new bool[TotalCells];

        private bool _isDragging;
        private bool _isAutoRevealing;
        private bool _inputLocked;
        private Vector2 _lastTouchUV;
        private Vector2 _lastTouchScreenPos;
        private float _lastTouchTime;

        private static readonly int MaskTexProp = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskUVProp = Shader.PropertyToID("_MaskUV");
        private static readonly int StrengthProp = Shader.PropertyToID("_Strength");
        private static readonly int UseTextureAlphaProp = Shader.PropertyToID("_UseTextureAlpha");

        public event Action<int, int> OnCellRevealed;
        public event Action OnAllCellsRevealed;

        public bool IsInputLocked
        {
            get => _inputLocked || _isAutoRevealing;
            set => _inputLocked = value;
        }

        public bool IsAutoRevealing => _isAutoRevealing;

        private void Awake()
        {
            if (_cardRectTransform == null)
            {
                _cardRectTransform = GetComponent<RectTransform>();
            }

            InitializeRenderTextureAndMaterials();
        }

        private void OnDestroy()
        {
            if (_maskRenderTexture != null)
            {
                _maskRenderTexture.Release();
                Destroy(_maskRenderTexture);
                _maskRenderTexture = null;
            }

            if (_foilMaterial != null)
            {
                Destroy(_foilMaterial);
                _foilMaterial = null;
            }

            if (_brushMaterial != null)
            {
                Destroy(_brushMaterial);
                _brushMaterial = null;
            }

            DestroyCellFoilMaterials();
        }

        public void Initialize(Shader foilShader, Shader eraseShader, Texture2D brushTex, Texture2D foilTex)
        {
            if (foilShader != null) _foilShader = foilShader;
            if (eraseShader != null) _brushEraseShader = eraseShader;
            if (brushTex != null) _brushTexture = brushTex;
            if (foilTex != null) _foilPatternTexture = foilTex;

            InitializeRenderTextureAndMaterials();
        }

        private void InitializeRenderTextureAndMaterials()
        {
            if (_maskRenderTexture == null)
            {
                _maskRenderTexture = new RenderTexture(_renderTextureSize, _renderTextureSize, 0, RenderTextureFormat.ARGB32)
                {
                    name = "CyberScratch_MaskRT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false
                };
                _maskRenderTexture.Create();
            }

            if (_brushMaterial == null && _brushEraseShader != null)
            {
                _brushMaterial = new Material(_brushEraseShader);
                if (_brushTexture != null)
                {
                    _brushMaterial.mainTexture = _brushTexture;
                }
            }

            if (_foilMaterial == null && _foilShader != null)
            {
                _foilMaterial = new Material(_foilShader);
                if (_foilPatternTexture != null)
                {
                    _foilMaterial.mainTexture = _foilPatternTexture;
                }
            }

            if (_foilMaterial != null)
            {
                _foilMaterial.SetTexture(MaskTexProp, _maskRenderTexture);
                if (_foilRawImage != null)
                {
                    _foilRawImage.material = _foilMaterial;
                    if (_foilPatternTexture != null)
                    {
                        _foilRawImage.texture = _foilPatternTexture;
                    }
                }
            }

            ClearMaskToSolidWhite();
            ApplyCellFoilMaterials();
        }

        public void ConfigureCellFoils(Image[] foilImages)
        {
            _cellFoilImages = foilImages;

            // Runtime-built scenes assign shaders after AddComponent has already run Awake.
            // Re-run material setup here so the erase brush is created with those references.
            InitializeRenderTextureAndMaterials();
        }

        public void ResetCard()
        {
            StopAllCoroutines();
            _isAutoRevealing = false;
            _isDragging = false;
            _inputLocked = false;

            ClearMaskToSolidWhite();

            for (int i = 0; i < TotalCells; i++)
            {
                _clearedPointCounts[i] = 0;
                _cellRevealed[i] = false;
                for (int p = 0; p < PointsPerCell; p++)
                {
                    _clearedPoints[i, p] = false;
                }
            }

            CyberAudioService.Instance?.StopScratchSound();
        }

        public void ClearMaskToSolidWhite()
        {
            if (_maskRenderTexture == null) return;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _maskRenderTexture;
            GL.Clear(false, true, Color.white);
            RenderTexture.active = prev;
        }

        /// <summary>
        /// Calculates mathematically accurate UV bounding rectangles for each of the 15 cells
        /// based on the visual layout of ScratchCellView RectTransforms.
        /// </summary>
        public void CalculateCellUVRects(ScratchCellView[] cells)
        {
            if (_cardRectTransform == null || cells == null) return;

            Vector3[] cardCorners = new Vector3[4];
            _cardRectTransform.GetWorldCorners(cardCorners);
            float cardMinX = cardCorners[0].x;
            float cardMaxX = cardCorners[2].x;
            float cardMinY = cardCorners[0].y;
            float cardMaxY = cardCorners[2].y;
            float cardWidth = Mathf.Max(1f, cardMaxX - cardMinX);
            float cardHeight = Mathf.Max(1f, cardMaxY - cardMinY);

            Vector3[] cellCorners = new Vector3[4];
            for (int i = 0; i < cells.Length && i < TotalCells; i++)
            {
                if (cells[i] == null) continue;

                RectTransform cellRT = cells[i].GetComponent<RectTransform>();
                cellRT.GetWorldCorners(cellCorners);

                float uMin = Mathf.Clamp01((cellCorners[0].x - cardMinX) / cardWidth);
                float uMax = Mathf.Clamp01((cellCorners[2].x - cardMinX) / cardWidth);
                float vMin = Mathf.Clamp01((cellCorners[0].y - cardMinY) / cardHeight);
                float vMax = Mathf.Clamp01((cellCorners[2].y - cardMinY) / cardHeight);

                Rect uvRect = new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
                int r = i / Columns;
                int c = i % Columns;
                SetCellLayoutBounds(r, c, uvRect);
            }

            PushCellMaskUVs();
        }

        public void SetCellLayoutBounds(int row, int col, Rect normalizedUVRect)
        {
            int index = row * Columns + col;
            if (index < 0 || index >= TotalCells) return;

            _cellRectUVs[index] = normalizedUVRect;

            // Subsampled 3x3 Check-Point Grid inside cell
            float[] fx = new float[] { 0.22f, 0.50f, 0.78f };
            float[] fy = new float[] { 0.22f, 0.50f, 0.78f };

            int pt = 0;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    _cellAnchorUVs[index, pt] = new Vector2(
                        normalizedUVRect.xMin + normalizedUVRect.width * fx[x],
                        normalizedUVRect.yMin + normalizedUVRect.height * fy[y]
                    );
                    pt++;
                }
            }
        }

        #region Pointer Scratch Input

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsInputLocked) return;

            if (ScreenPointToCardUV(eventData.position, out Vector2 uv))
            {
                _isDragging = true;
                _lastTouchUV = uv;
                _lastTouchScreenPos = eventData.position;
                _lastTouchTime = Time.unscaledTime;

                StampBrushAt(uv);
                CheckSubsampledPoints(uv);
                EmitParticles(eventData.position);
                CyberAudioService.Instance?.UpdateScratchIntensity(0.5f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsInputLocked || !_isDragging) return;

            if (ScreenPointToCardUV(eventData.position, out Vector2 uv))
            {
                float dt = Mathf.Max(0.001f, Time.unscaledTime - _lastTouchTime);
                float distScreen = Vector2.Distance(eventData.position, _lastTouchScreenPos);
                float velocity = distScreen / (dt * 1500f);

                CyberAudioService.Instance?.UpdateScratchIntensity(velocity);

                _lastTouchScreenPos = eventData.position;
                _lastTouchTime = Time.unscaledTime;

                // Interpolate brush stamps along path to avoid gaps
                float distUV = Vector2.Distance(_lastTouchUV, uv);
                float step = _brushRadiusUV * 0.4f;
                int steps = Mathf.Max(1, Mathf.CeilToInt(distUV / step));

                for (int i = 1; i <= steps; i++)
                {
                    Vector2 p = Vector2.Lerp(_lastTouchUV, uv, (float)i / steps);
                    StampBrushAt(p);
                    CheckSubsampledPoints(p);
                }

                _lastTouchUV = uv;
                EmitParticles(eventData.position);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            CyberAudioService.Instance?.StopScratchSound();
        }

        private bool ScreenPointToCardUV(Vector2 screenPos, out Vector2 uv)
        {
            uv = Vector2.zero;
            if (_cardRectTransform == null) return false;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_cardRectTransform, screenPos, null, out Vector2 localPt))
            {
                Rect rect = _cardRectTransform.rect;
                float u = (localPt.x - rect.xMin) / rect.width;
                float v = (localPt.y - rect.yMin) / rect.height;

                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                {
                    uv = new Vector2(u, v);
                    return true;
                }
            }
            return false;
        }

        private void EmitParticles(Vector2 screenPos)
        {
            if (_dustParticles == null) return;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_cardRectTransform, screenPos, null, out Vector3 worldPos))
            {
                _dustParticles.transform.position = worldPos;
                _dustParticles.Emit(2);
            }
        }

        #endregion

        #region RenderTexture Stamp & Reveal Threshold Grid

        private void ApplyCellFoilMaterials()
        {
            if (_foilShader == null || _cellFoilImages == null || _cellFoilImages.Length == 0)
            {
                return;
            }

            if (_maskRenderTexture == null)
            {
                return;
            }

            bool rebuild = _cellFoilMaterials == null || _cellFoilMaterials.Length != _cellFoilImages.Length;
            if (rebuild)
            {
                DestroyCellFoilMaterials();
                _cellFoilMaterials = new Material[_cellFoilImages.Length];
            }

            for (int i = 0; i < _cellFoilImages.Length; i++)
            {
                Image img = _cellFoilImages[i];
                if (img == null) continue;

                if (_cellFoilMaterials[i] == null)
                {
                    _cellFoilMaterials[i] = new Material(_foilShader)
                    {
                        name = "CyberScratch_FoilCell_" + i,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }

                Material mat = _cellFoilMaterials[i];
                mat.SetTexture(MaskTexProp, _maskRenderTexture);
                if (_foilPatternTexture != null)
                {
                    mat.mainTexture = _foilPatternTexture;
                }
                img.material = mat;
                img.raycastTarget = false;
            }

            PushCellMaskUVs();
        }

        private void PushCellMaskUVs()
        {
            if (_cellFoilMaterials == null) return;

            for (int i = 0; i < _cellFoilMaterials.Length && i < TotalCells; i++)
            {
                Material mat = _cellFoilMaterials[i];
                if (mat == null) continue;

                Rect rect = _cellRectUVs[i];
                if (rect.width <= 0.0001f || rect.height <= 0.0001f)
                {
                    mat.SetVector(MaskUVProp, new Vector4(0f, 0f, 1f, 1f));
                }
                else
                {
                    mat.SetVector(MaskUVProp, new Vector4(rect.x, rect.y, rect.width, rect.height));
                }
            }
        }

        private void DestroyCellFoilMaterials()
        {
            if (_cellFoilMaterials == null) return;

            for (int i = 0; i < _cellFoilMaterials.Length; i++)
            {
                if (_cellFoilMaterials[i] == null) continue;
                if (Application.isPlaying)
                {
                    Destroy(_cellFoilMaterials[i]);
                }
                else
                {
                    DestroyImmediate(_cellFoilMaterials[i]);
                }
                _cellFoilMaterials[i] = null;
            }
        }

        private void StampBrushAt(Vector2 uv)
        {
            if (_maskRenderTexture == null || _brushMaterial == null) return;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _maskRenderTexture;

            GL.PushMatrix();
            GL.LoadOrtho();

            _brushMaterial.SetFloat(StrengthProp, 1.0f);
            _brushMaterial.SetFloat(UseTextureAlphaProp, 1.0f); // Circular soft brush
            _brushMaterial.SetPass(0);

            float r = _brushRadiusUV;
            float left = uv.x - r;
            float right = uv.x + r;
            float bottom = uv.y - r;
            float top = uv.y + r;

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0f, 0f); GL.Vertex3(left, bottom, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(right, bottom, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(right, top, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(left, top, 0f);
            GL.End();

            GL.PopMatrix();
            RenderTexture.active = prev;
        }

        private void CheckSubsampledPoints(Vector2 uv)
        {
            float sqrRadius = _brushRadiusUV * _brushRadiusUV;

            for (int i = 0; i < TotalCells; i++)
            {
                if (_cellRevealed[i]) continue;

                // Check 9 anchor points
                for (int p = 0; p < PointsPerCell; p++)
                {
                    if (_clearedPoints[i, p]) continue;

                    Vector2 anchor = _cellAnchorUVs[i, p];
                    float dx = anchor.x - uv.x;
                    float dy = anchor.y - uv.y;
                    if (dx * dx + dy * dy <= sqrRadius)
                    {
                        _clearedPoints[i, p] = true;
                        _clearedPointCounts[i]++;

                        // Threshold check: >= 6 out of 9 points (~67%)
                        if (_clearedPointCounts[i] >= _revealThresholdPoints)
                        {
                            _cellRevealed[i] = true;
                            int row = i / Columns;
                            int col = i % Columns;
                            StartCoroutine(AutoClearCellRoutine(row, col, i));
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator AutoClearCellRoutine(int row, int col, int cellIndex)
        {
            Rect rect = _cellRectUVs[cellIndex];
            CyberAudioService.Instance?.PlayFoilSnap();

            // Smooth quad erase on RenderTexture over duration
            float elapsed = 0f;
            while (elapsed < _cellFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = Mathf.Clamp01(elapsed / _cellFadeDuration);
                EraseRectOnMask(rect, strength);
                yield return null;
            }

            // Ensure 100% erased
            EraseRectOnMask(rect, 1.0f);

            OnCellRevealed?.Invoke(row, col);

            // Check if all 15 cells are cleared
            bool allDone = true;
            for (int i = 0; i < TotalCells; i++)
            {
                if (!_cellRevealed[i])
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                EraseAll();
                OnAllCellsRevealed?.Invoke();
            }
        }

        public void EraseRectOnMask(Rect uvRect, float strength = 1.0f)
        {
            if (_maskRenderTexture == null || _brushMaterial == null) return;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _maskRenderTexture;

            GL.PushMatrix();
            GL.LoadOrtho();

            _brushMaterial.SetFloat(StrengthProp, strength);
            _brushMaterial.SetFloat(UseTextureAlphaProp, 0.0f); // Solid quad erasure without circular edge falloff
            _brushMaterial.SetPass(0);

            // Inset slightly expanded to ensure clean visual reveal
            float padX = uvRect.width * 0.05f;
            float padY = uvRect.height * 0.05f;
            float xMin = Mathf.Clamp01(uvRect.xMin - padX);
            float xMax = Mathf.Clamp01(uvRect.xMax + padX);
            float yMin = Mathf.Clamp01(uvRect.yMin - padY);
            float yMax = Mathf.Clamp01(uvRect.yMax + padY);

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0f, 0f); GL.Vertex3(xMin, yMin, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(xMax, yMin, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(xMax, yMax, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(xMin, yMax, 0f);
            GL.End();

            GL.PopMatrix();
            RenderTexture.active = prev;
        }

        public void EraseAll()
        {
            if (_maskRenderTexture == null) return;

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _maskRenderTexture;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;
        }

        #endregion

        #region Reveal All (Quick Play)

        public void RevealAll(float duration = 0.8f)
        {
            if (_isAutoRevealing) return;
            StartCoroutine(RevealAllRoutine(duration));
        }

        private IEnumerator RevealAllRoutine(float totalDuration)
        {
            _isAutoRevealing = true;
            _isDragging = false;
            CyberAudioService.Instance?.StopScratchSound();

            float delayPerCell = totalDuration / TotalCells;

            // Sequential left-to-right, top-to-bottom
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    int index = r * Columns + c;
                    if (!_cellRevealed[index])
                    {
                        _cellRevealed[index] = true;
                        Rect rect = _cellRectUVs[index];
                        EraseRectOnMask(rect, 1.0f);
                        CyberAudioService.Instance?.PlayFoilSnap();
                        OnCellRevealed?.Invoke(r, c);

                        yield return new WaitForSecondsRealtime(delayPerCell);
                    }
                }
            }

            // Guarantee 100% of foil is cleared across the entire card
            EraseAll();

            _isAutoRevealing = false;
            OnAllCellsRevealed?.Invoke();
        }

        #endregion
    }
}
