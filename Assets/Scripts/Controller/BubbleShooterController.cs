using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.javierquevedo;

public class BubbleShooterController : MonoBehaviour
{
    public bool isAiming;
    public float aimAngle = 90.0f; // 90 = straight up, 12..168 allowed

    private const float _minShootAngle = 14.0f;
    private const float _maxShootAngle = 166.0f;
    private const float _keyboardRotSpeed = 110.0f;

    private Transform _barrelTransform;
    private SpriteRenderer _baseRenderer;
    private SpriteRenderer _barrelRenderer;
    private SpriteRenderer _chamberRenderer;
    private List<SpriteRenderer> _laserDots = new List<SpriteRenderer>();
    private SpriteRenderer _ghostOrbRenderer;
    private float _recoilOffset = 0f;
    private readonly List<Vector3> _pathPoints = new List<Vector3>(128);
    private Camera _cachedCam;

    void Awake()
    {
        isAiming = true;
        aimAngle = 90.0f;
        SetupTurretVisuals();
    }

    void Start()
    {
        isAiming = true;
        _cachedCam = Camera.main;
        if (_cachedCam == null)
        {
            GameObject camGo = GameObject.Find("Camera");
            if (camGo != null) _cachedCam = camGo.GetComponent<Camera>();
        }
    }

    private void SetupTurretVisuals()
    {
        // Hide any legacy mesh renderer on the root or child objects
        MeshRenderer[] legacyRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in legacyRenderers)
        {
            if (mr.gameObject.GetComponent<TextMesh>() == null)
                mr.enabled = false;
        }

        // 1. Turret Aerospace Alloy Base
        Transform baseTr = transform.Find("TurretBase");
        GameObject baseGo = baseTr != null ? baseTr.gameObject : new GameObject("TurretBase");
        if (baseTr == null)
        {
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        }
        _baseRenderer = baseGo.GetComponent<SpriteRenderer>();
        if (_baseRenderer == null) _baseRenderer = baseGo.AddComponent<SpriteRenderer>();
        if (_baseRenderer.sprite == null) _baseRenderer.sprite = CyberArtGenerator.GetTurretBaseSprite();
        _baseRenderer.sortingOrder = 6;

        // 2. Illuminated Circular Loading Chamber
        Transform chamberTr = transform.Find("LoadingChamber");
        GameObject chamberGo = chamberTr != null ? chamberTr.gameObject : new GameObject("LoadingChamber");
        if (chamberTr == null)
        {
            chamberGo.transform.SetParent(transform, false);
            chamberGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        }
        _chamberRenderer = chamberGo.GetComponent<SpriteRenderer>();
        if (_chamberRenderer == null) _chamberRenderer = chamberGo.AddComponent<SpriteRenderer>();
        if (_chamberRenderer.sprite == null) _chamberRenderer.sprite = CyberArtGenerator.GetChamberRingSprite();
        _chamberRenderer.sortingOrder = 8;

        // 3. Swivel-mounted Railgun Barrel
        Transform barrelTr = transform.Find("RailgunBarrel");
        GameObject barrelGo = barrelTr != null ? barrelTr.gameObject : new GameObject("RailgunBarrel");
        if (barrelTr == null)
        {
            barrelGo.transform.SetParent(transform, false);
            barrelGo.transform.localPosition = new Vector3(0f, 0f, 0.08f);
        }
        _barrelTransform = barrelGo.transform;
        _barrelRenderer = barrelGo.GetComponent<SpriteRenderer>();
        if (_barrelRenderer == null) _barrelRenderer = barrelGo.AddComponent<SpriteRenderer>();
        if (_barrelRenderer.sprite == null) _barrelRenderer.sprite = CyberArtGenerator.GetTurretBarrelSprite();
        _barrelRenderer.sortingOrder = 7;

        // 4. Holographic Laser Targeting Sight Dots
        Transform laserTr = transform.Find("HolographicLaserSight");
        GameObject laserContainer = laserTr != null ? laserTr.gameObject : new GameObject("HolographicLaserSight");
        if (laserTr == null) laserContainer.transform.SetParent(transform, false);
        _laserDots.Clear();
        for (int i = 0; i < 28; i++)
        {
            Transform dotTr = laserContainer.transform.Find("LaserDot_" + i);
            GameObject dotGo = dotTr != null ? dotTr.gameObject : new GameObject("LaserDot_" + i);
            if (dotTr == null) dotGo.transform.SetParent(laserContainer.transform, false);
            SpriteRenderer sr = dotGo.GetComponent<SpriteRenderer>();
            if (sr == null) sr = dotGo.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = CyberArtGenerator.GetAimDotSprite();
            sr.sortingOrder = 5;
            sr.enabled = false;
            _laserDots.Add(sr);
        }

        // 5. Ghost Orb Target Preview
        Transform ghostTr = transform.Find("GhostOrbPreview");
        GameObject ghostGo = ghostTr != null ? ghostTr.gameObject : new GameObject("GhostOrbPreview");
        if (ghostTr == null) ghostGo.transform.position = Vector3.zero;
        _ghostOrbRenderer = ghostGo.GetComponent<SpriteRenderer>();
        if (_ghostOrbRenderer == null) _ghostOrbRenderer = ghostGo.AddComponent<SpriteRenderer>();
        if (_ghostOrbRenderer.sprite == null) _ghostOrbRenderer.sprite = CyberArtGenerator.GetGhostOrbSprite();
        _ghostOrbRenderer.sortingOrder = 9;
        _ghostOrbRenderer.enabled = false;
    }

    public void TriggerFireRecoil()
    {
        _recoilOffset = 0.24f;
    }

    void Update()
    {
        if (_recoilOffset > 0f)
        {
            _recoilOffset = Mathf.Max(0f, _recoilOffset - Time.deltaTime * 1.8f);
        }

        if (!isAiming)
        {
            SetTrajectoryVisible(false);
            return;
        }

        // Keyboard aiming (A/D or Left/Right arrows)
        float horiz = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(horiz) > 0.01f)
        {
            aimAngle = Mathf.Clamp(aimAngle - horiz * _keyboardRotSpeed * Time.deltaTime, _minShootAngle, _maxShootAngle);
        }

        // Mouse / Touch aiming toward cursor in world space
        if (_cachedCam == null)
            _cachedCam = Camera.main;

        if (_cachedCam != null && (Input.mousePresent || Input.touchCount > 0))
        {
            Vector3 screenPos = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            if (screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                Vector3 worldMouse = _cachedCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_cachedCam.transform.position.z)));
                Vector2 diff = new Vector2(worldMouse.x - transform.position.x, worldMouse.y - transform.position.y);
                if (diff.y > 0.15f && diff.sqrMagnitude > 0.1f)
                {
                    float rawAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                    aimAngle = Mathf.Clamp(rawAngle, _minShootAngle, _maxShootAngle);
                }
            }
        }

        // Sync transform rotation so legacy shootingRotation() also stays in sync
        float zEuler = aimAngle - 90.0f;
        transform.eulerAngles = new Vector3(0f, 0f, zEuler);

        if (_barrelTransform != null)
        {
            _barrelTransform.localPosition = new Vector3(0f, -_recoilOffset, 0.08f);
        }
    }

    public void UpdateLaserTrajectory(
        Vector3 origin,
        BubbleColor activeOrbColor,
        BubbleMatrix matrix,
        BubbleMatrixGeometry geometry,
        bool showSight)
    {
        if (!showSight || !isAiming || matrix == null || geometry == null)
        {
            SetTrajectoryVisible(false);
            return;
        }

        Color orbColor = CyberArtGenerator.GetPrimaryColor(activeOrbColor);
        Color laserColor = Color.Lerp(new Color(0f, 0.95f, 1f, 1f), orbColor, 0.45f);
        if (_chamberRenderer != null)
            _chamberRenderer.color = Color.Lerp(Color.white, orbColor, 0.3f);

        // Trace trajectory with wall reflections
        float simAngle = aimAngle;
        Vector3 pos = origin;
        float stepSize = 0.22f;
        int maxSteps = 95;
        float collisionRadius = geometry.bubbleRadius * 1.82f;
        float collisionRadiusSq = collisionRadius * collisionRadius;

        _pathPoints.Clear();
        Vector3 impactPos = origin;
        bool hitDetected = false;

        for (int s = 0; s < maxSteps; s++)
        {
            float rad = simAngle * Mathf.Deg2Rad;
            Vector3 next = pos + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * stepSize;

            if (next.x + geometry.bubbleRadius >= geometry.rightBorder)
            {
                next.x = geometry.rightBorder - geometry.bubbleRadius;
                simAngle = 180f - simAngle;
            }
            else if (next.x - geometry.bubbleRadius <= geometry.leftBorder)
            {
                next.x = geometry.leftBorder + geometry.bubbleRadius;
                simAngle = 180f - simAngle;
            }

            if (next.y + geometry.bubbleRadius >= geometry.topBorder)
            {
                next.y = geometry.topBorder - geometry.bubbleRadius;
                impactPos = next;
                _pathPoints.Add(next);
                hitDetected = true;
                break;
            }

            // Check distance to occupied cells in matrix
            bool hitBubble = false;
            for (int r = 0; r < geometry.rows; r++)
            {
                for (int c = 0; c < geometry.columns; c++)
                {
                    if (!matrix.hasBubble(r, c)) continue;
                    Vector3 bPos = BubbleMatrixControllerHelper.PositionForCell(new Vector2(r, c), geometry, matrix.isBaselineAlignedLeft);
                    float dx = next.x - bPos.x;
                    float dy = next.y - bPos.y;
                    if (dx * dx + dy * dy <= collisionRadiusSq)
                    {
                        hitBubble = true;
                        break;
                    }
                }
                if (hitBubble) break;
            }

            if (hitBubble)
            {
                impactPos = pos;
                _pathPoints.Add(pos);
                hitDetected = true;
                break;
            }

            pos = next;
            if (s % 2 == 0)
                _pathPoints.Add(pos);
        }

        // Animate marching holographic laser dots
        float pulseOffset = Time.time * 4.5f;
        int dotCount = _laserDots.Count;
        for (int i = 0; i < dotCount; i++)
        {
            if (i < _pathPoints.Count && i > 0)
            {
                _laserDots[i].enabled = true;
                _laserDots[i].transform.position = new Vector3(_pathPoints[i].x, _pathPoints[i].y, -0.15f);
                float fade = 1f - (float)i / Mathf.Max(1, _pathPoints.Count);
                float wave = 0.65f + 0.35f * Mathf.Sin(pulseOffset - i * 0.6f);
                Color dc = laserColor;
                dc.a = Mathf.Clamp01(fade * wave * 0.9f);
                _laserDots[i].color = dc;
                float s = Mathf.Lerp(0.30f, 0.14f, (float)i / dotCount) * wave;
                _laserDots[i].transform.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _laserDots[i].enabled = false;
            }
        }

        // Position Ghost Orb at exact snap cell
        if (_ghostOrbRenderer != null)
        {
            if (hitDetected)
            {
                Vector2 snapCell = BubbleMatrixControllerHelper.FindBestSnapCell(impactPos, matrix, geometry);
                if (snapCell.x >= 0 && snapCell.x < geometry.rows)
                {
                    Vector3 snapWorld = BubbleMatrixControllerHelper.PositionForCell(snapCell, geometry, matrix.isBaselineAlignedLeft);
                    _ghostOrbRenderer.enabled = true;
                    _ghostOrbRenderer.transform.position = new Vector3(snapWorld.x, snapWorld.y, -0.12f);
                    float pulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 8f);
                    _ghostOrbRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
                    Color gc = orbColor;
                    gc.a = 0.55f + 0.2f * Mathf.Sin(Time.time * 6f);
                    _ghostOrbRenderer.color = gc;
                }
                else
                {
                    _ghostOrbRenderer.enabled = false;
                }
            }
            else
            {
                _ghostOrbRenderer.enabled = false;
            }
        }
    }

    private void SetTrajectoryVisible(bool visible)
    {
        for (int i = 0; i < _laserDots.Count; i++)
        {
            if (_laserDots[i] != null)
                _laserDots[i].enabled = visible;
        }
        if (_ghostOrbRenderer != null)
            _ghostOrbRenderer.enabled = visible;
    }

    void OnDestroy()
    {
        if (_ghostOrbRenderer != null && _ghostOrbRenderer.gameObject != null)
            Destroy(_ghostOrbRenderer.gameObject);
    }
}
