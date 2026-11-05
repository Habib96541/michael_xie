using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.javierquevedo.events;

namespace com.javierquevedo
{
    public class BubbleMatrixController : MonoBehaviour
    {
        /* Geometry */
        public float leftBorder = 0.0f;
        public float rightBorder = 10.5f;
        public float topBorder = 10.0f;

        /* Dimensions */
        public int rows = 10;
        public int columns = 10;
        public float bubbleRadius = 0.5f;
        public float addRowPeriod = 15.0f;

        /* View Properties */
        public BubbleMatrixGeometry geometry;
        public Game gameModel;

        /* Constants */
        private const float _bubbleLinearSpeed = 18.5f;
        private const string _bubblePrefabName = "Prefabs/BubblePrefab";
        private const int _defaultRowsCount = 5;

        /* Private iVars */
        private BubbleMatrix _matrix;
        private GameObject _bubblesContainer;
        private GameObject _bubbleShooter;
        private BubbleShooterController _shooterController;
        private BubbleController _currentBubble;
        private BubbleController _nextBubble;
        private ArrayList _bubbleControllers;
        private bool _pendingToAddRow;
        private bool _isPlaying;

        // Playfield Frame & Bottom Queue Visuals
        private GameObject _boardDecorRoot;
        private SpriteRenderer _nextChamberRenderer;
        private List<SpriteRenderer> _powerCellRenderers = new List<SpriteRenderer>();
        private List<SpriteRenderer> _neonBorderStrips = new List<SpriteRenderer>();
        private Camera _cachedCam;

        public BubbleController CurrentBubble { get { return _currentBubble; } }
        public BubbleController NextBubble { get { return _nextBubble; } }
        public bool IsPlaying { get { return _isPlaying; } }

        void Awake()
        {
            this._isPlaying = false;
            this._pendingToAddRow = false;
            this._matrix = new BubbleMatrix(rows, columns);
            this._bubbleControllers = new ArrayList();
        }

        void Start()
        {
            _cachedCam = Camera.main;
            if (_cachedCam == null)
            {
                GameObject camGo = GameObject.Find("Camera");
                if (camGo != null) _cachedCam = camGo.GetComponent<Camera>();
            }
        }

        public void startGame()
        {
            Transform bubblesTr = this.transform.Find("Bubbles");
            if (bubblesTr != null)
            {
                this._bubblesContainer = bubblesTr.gameObject;
            }
            else
            {
                this._bubblesContainer = new GameObject("Bubbles");
                this._bubblesContainer.transform.SetParent(this.transform, false);
            }

            Transform shooterTr = this.transform.Find("BubbleShooter");
            if (shooterTr != null)
            {
                this._bubbleShooter = shooterTr.gameObject;
            }
            else
            {
                this._bubbleShooter = new GameObject("BubbleShooter");
                this._bubbleShooter.transform.SetParent(this.transform, false);
                this._bubbleShooter.AddComponent<BubbleShooterController>();
            }

            this.geometry = new BubbleMatrixGeometry(leftBorder, rightBorder, topBorder, 0.0f, rows, columns, bubbleRadius);

            Vector3 shooterPos = GetShooterPosition();
            this._bubbleShooter.transform.position = shooterPos;
            this._shooterController = this._bubbleShooter.GetComponent<BubbleShooterController>();
            if (this._shooterController == null)
                this._shooterController = this._bubbleShooter.AddComponent<BubbleShooterController>();
            this._shooterController.isAiming = true;

            // Hide legacy walls on prefab and build Cyber Orbs Playfield Container
            HideLegacyWalls();
            BuildPlayfieldContainerVisuals();

            this._matrix.clear();
            this._bubbleControllers.Clear();
            this._isPlaying = true;
            this._pendingToAddRow = false;

            // Populate initial 5 rows of crystalline orbs
            for (int i = 0; i < _defaultRowsCount; i++)
            {
                this.addRow(true);
            }

            // Spawn active orb in chamber and next queued orb in side slot
            this._nextBubble = this.createBubble(true);
            PositionNextBubbleInQueue();
            PromoteNextBubbleToChamber();
            RefreshPowerCellsVisuals();
        }

        private Vector3 GetShooterPosition()
        {
            return new Vector3((rightBorder - leftBorder) * 0.5f, -0.65f, 0f);
        }

        private Vector3 GetNextQueuePosition()
        {
            return new Vector3(leftBorder + 0.95f, -0.65f, 0f);
        }

        private void HideLegacyWalls()
        {
            MeshRenderer[] allMeshes = GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer mr in allMeshes)
            {
                if (mr.gameObject.GetComponent<TextMesh>() == null)
                    mr.enabled = false;
            }
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in allColliders)
            {
                col.enabled = false;
            }
        }

        private void BuildPlayfieldContainerVisuals()
        {
            _powerCellRenderers.Clear();
            _neonBorderStrips.Clear();

            // Check if playfield container already exists in hierarchy
            GameObject existingContainer = GameObject.Find("CyberOrbs_Playfield");
            if (existingContainer == null)
                existingContainer = GameObject.Find("CyberPlayfieldContainer");

            if (existingContainer != null)
            {
                _boardDecorRoot = existingContainer;

                Transform leftNeon = _boardDecorRoot.transform.Find("Neon_PerimeterLighting/LeftNeonStrip");
                if (leftNeon == null) leftNeon = _boardDecorRoot.transform.Find("LeftNeonStrip");
                if (leftNeon != null) _neonBorderStrips.Add(leftNeon.GetComponent<SpriteRenderer>());

                Transform rightNeon = _boardDecorRoot.transform.Find("Neon_PerimeterLighting/RightNeonStrip");
                if (rightNeon == null) rightNeon = _boardDecorRoot.transform.Find("RightNeonStrip");
                if (rightNeon != null) _neonBorderStrips.Add(rightNeon.GetComponent<SpriteRenderer>());

                Transform topNeon = _boardDecorRoot.transform.Find("Neon_PerimeterLighting/TopNeonStrip");
                if (topNeon == null) topNeon = _boardDecorRoot.transform.Find("TopNeonStrip");
                if (topNeon != null) _neonBorderStrips.Add(topNeon.GetComponent<SpriteRenderer>());

                Transform nextChamber = _boardDecorRoot.transform.Find("AmmoQueue_SideSlot/NextOrbChamber");
                if (nextChamber == null) nextChamber = _boardDecorRoot.transform.Find("NextOrbChamber");
                if (nextChamber != null) _nextChamberRenderer = nextChamber.GetComponent<SpriteRenderer>();

                for (int i = 0; i < 5; i++)
                {
                    Transform cellFill = _boardDecorRoot.transform.Find("ShieldBattery_Tracker/Fills/PowerCellFill_" + i);
                    if (cellFill == null) cellFill = _boardDecorRoot.transform.Find("PowerCellFill_" + i);
                    if (cellFill != null) _powerCellRenderers.Add(cellFill.GetComponent<SpriteRenderer>());
                }

                if (_neonBorderStrips.Count >= 3 && _powerCellRenderers.Count == 5 && _nextChamberRenderer != null)
                {
                    return;
                }
            }

            if (_boardDecorRoot != null && _boardDecorRoot.name == "CyberPlayfieldContainer")
                Destroy(_boardDecorRoot);

            _boardDecorRoot = new GameObject("CyberPlayfieldContainer");
            _boardDecorRoot.transform.SetParent(this.transform, false);

            float width = rightBorder - leftBorder;
            float centerX = leftBorder + width * 0.5f;
            float bottomY = -1.45f;
            float height = topBorder - bottomY;
            float centerY = bottomY + height * 0.5f;

            // 0. Outer Matte Carbon-Fiber & Obsidian Ambient Backdrop
            GameObject outerBgGo = new GameObject("OuterObsidianBackdrop");
            outerBgGo.transform.SetParent(_boardDecorRoot.transform, false);
            outerBgGo.transform.position = new Vector3(centerX, centerY, 0.8f);
            SpriteRenderer outerBgSr = outerBgGo.AddComponent<SpriteRenderer>();
            outerBgSr.sprite = CyberArtGenerator.GetHexBackdropSprite();
            outerBgSr.color = new Color(0.55f, 0.65f, 0.78f, 0.45f);
            outerBgSr.sortingOrder = -20;
            outerBgSr.transform.localScale = new Vector3(42f, 32f, 1f);

            // 1. Deep Obsidian & Hexagonal Grid Playfield Window (#0b0e14)
            GameObject bgGo = new GameObject("HexGridBackdrop");
            bgGo.transform.SetParent(_boardDecorRoot.transform, false);
            bgGo.transform.position = new Vector3(centerX, centerY, 0.5f);
            SpriteRenderer bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = CyberArtGenerator.GetHexBackdropSprite();
            bgSr.sortingOrder = -10;
            bgSr.transform.localScale = new Vector3(width + 0.1f, height + 0.1f, 1f);

            // 2. Outer Brushed Titanium Beveled Frame Panels
            Color titaniumDark = new Color(0.10f, 0.13f, 0.19f, 1f);
            Color titaniumBevel = new Color(0.22f, 0.29f, 0.39f, 1f);
            Color neonCyan = new Color(0.0f, 0.94f, 1.0f, 0.92f);
            Color neonPurple = new Color(0.68f, 0.20f, 1.0f, 0.88f);

            CreateSolidBeam("LeftTitaniumFrame", new Vector3(leftBorder - 0.18f, centerY, 0.1f), new Vector2(0.36f, height + 0.68f), titaniumDark, 2);
            CreateSolidBeam("RightTitaniumFrame", new Vector3(rightBorder + 0.18f, centerY, 0.1f), new Vector2(0.36f, height + 0.68f), titaniumDark, 2);
            CreateSolidBeam("TopTitaniumFrame", new Vector3(centerX, topBorder + 0.18f, 0.1f), new Vector2(width + 0.72f, 0.36f), titaniumDark, 2);
            CreateSolidBeam("BottomTitaniumDeck", new Vector3(centerX, bottomY - 0.18f, 0.1f), new Vector2(width + 0.72f, 0.36f), titaniumDark, 2);

            // Chamfered corner joint accents
            CreateSolidBeam("CornerTL", new Vector3(leftBorder - 0.18f, topBorder + 0.18f, 0.08f), new Vector2(0.46f, 0.46f), titaniumBevel, 3);
            CreateSolidBeam("CornerTR", new Vector3(rightBorder + 0.18f, topBorder + 0.18f, 0.08f), new Vector2(0.46f, 0.46f), titaniumBevel, 3);
            CreateSolidBeam("CornerBL", new Vector3(leftBorder - 0.18f, bottomY - 0.18f, 0.08f), new Vector2(0.46f, 0.46f), titaniumBevel, 3);
            CreateSolidBeam("CornerBR", new Vector3(rightBorder + 0.18f, bottomY - 0.18f, 0.08f), new Vector2(0.46f, 0.46f), titaniumBevel, 3);

            // Recessed glowing cyan & purple neon edge lighting strips
            _neonBorderStrips.Add(CreateSolidBeam("LeftNeonStrip", new Vector3(leftBorder - 0.02f, centerY, 0.05f), new Vector2(0.055f, height), neonCyan, 4));
            _neonBorderStrips.Add(CreateSolidBeam("RightNeonStrip", new Vector3(rightBorder + 0.02f, centerY, 0.05f), new Vector2(0.055f, height), neonCyan, 4));
            _neonBorderStrips.Add(CreateSolidBeam("TopNeonStrip", new Vector3(centerX, topBorder + 0.02f, 0.05f), new Vector2(width, 0.06f), neonPurple, 4));

            // Danger threshold line at row overflow boundary (y = 0.0f)
            CreateSolidBeam("DangerThresholdLine", new Vector3(centerX, 0.02f, 0.2f), new Vector2(width - 0.1f, 0.035f), new Color(1f, 0.18f, 0.45f, 0.42f), 1);

            // 3. Secondary Side-Queue Display Slot ("NEXT" Chamber)
            Vector3 nextPos = GetNextQueuePosition();
            GameObject nextRingGo = new GameObject("NextOrbChamber");
            nextRingGo.transform.SetParent(_boardDecorRoot.transform, false);
            nextRingGo.transform.position = new Vector3(nextPos.x, nextPos.y, 0.05f);
            nextRingGo.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
            _nextChamberRenderer = nextRingGo.AddComponent<SpriteRenderer>();
            _nextChamberRenderer.sprite = CyberArtGenerator.GetChamberRingSprite();
            _nextChamberRenderer.sortingOrder = 6;

            // "NEXT / SWAP" world label under side queue
            CreateWorldLabel("NextSlotLabel", new Vector3(nextPos.x, nextPos.y - 0.62f, -0.1f), "NEXT [SWAP]", 0.052f, new Color(0.55f, 0.88f, 1f, 0.85f));

            // 4. 5-Cell Miss Penalty Battery Tracker (between Next Slot and Turret, like classic screenshot + Prompt #5)
            float startCellX = nextPos.x + 1.15f;
            float cellSpacing = 0.56f;
            for (int i = 0; i < 5; i++)
            {
                Vector3 cellPos = new Vector3(startCellX + i * cellSpacing, nextPos.y, 0.05f);
                CreateSolidBeam("PowerCellSocket_" + i, cellPos, new Vector2(0.42f, 0.26f), new Color(0.08f, 0.11f, 0.17f, 0.95f), 5);
                SpriteRenderer cellSr = CreateSolidBeam("PowerCellFill_" + i, cellPos, new Vector2(0.34f, 0.18f), new Color(0f, 0.92f, 1f, 1f), 6);
                _powerCellRenderers.Add(cellSr);
            }
            CreateWorldLabel("BatteryLabel", new Vector3(startCellX + 2f * cellSpacing, nextPos.y - 0.34f, -0.1f), "SHIELD CELLS (MISS LIMIT)", 0.042f, new Color(0.48f, 0.75f, 0.92f, 0.75f));
        }

        private SpriteRenderer CreateSolidBeam(string name, Vector3 pos, Vector2 size, Color color, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_boardDecorRoot.transform, false);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = CyberArtGenerator.WhiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return sr;
        }

        private TextMesh CreateWorldLabel(string name, Vector3 pos, string text, float charSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_boardDecorRoot.transform, false);
            go.transform.position = pos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 15;
            return tm;
        }

        public void RefreshPowerCellsVisuals()
        {
            if (gameModel == null) return;
            int active = gameModel.activePowerCells;
            for (int i = 0; i < _powerCellRenderers.Count; i++)
            {
                if (_powerCellRenderers[i] == null) continue;
                bool isLit = i < active;
                if (isLit)
                {
                    _powerCellRenderers[i].color = active == 1
                        ? new Color(1.0f, 0.32f, 0.15f, 1.0f)
                        : new Color(0.0f, 0.92f, 1.0f, 1.0f);
                }
                else
                {
                    _powerCellRenderers[i].color = new Color(0.14f, 0.17f, 0.23f, 0.65f);
                }
            }
        }

        void Update()
        {
            // Subtle neon border pulse
            for (int i = 0; i < _neonBorderStrips.Count; i++)
            {
                if (_neonBorderStrips[i] != null)
                {
                    Color c = _neonBorderStrips[i].color;
                    c.a = 0.72f + 0.22f * Mathf.Sin(Time.time * 3.2f + i);
                    _neonBorderStrips[i].color = c;
                }
            }

            if (!this._isPlaying || (gameModel != null && gameModel.isPaused))
            {
                if (_shooterController != null)
                    _shooterController.UpdateLaserTrajectory(GetShooterPosition(), BubbleColor.Blue, _matrix, geometry, false);
                return;
            }

            // Update holographic laser trajectory sight when a ready orb is in the chamber
            if (_shooterController != null)
            {
                bool readyToFire = (_currentBubble != null && !_currentBubble.isMoving);
                BubbleColor activeCol = readyToFire ? _currentBubble.bubble.color : BubbleColor.Blue;
                _shooterController.UpdateLaserTrajectory(GetShooterPosition(), activeCol, _matrix, geometry, readyToFire);
            }

            // Swap current & next orb on Right-Click, S, or Tab
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Tab))
            {
                SwapCurrentAndNextOrb();
                return;
            }

            // Fire on Space / Up Arrow
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                FireCurrentOrb();
                return;
            }

            // Fire on Left Mouse Click / Touch inside playfield (ignoring HUD button region)
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUIOrSwap())
                {
                    FireCurrentOrb();
                }
            }
        }

        private bool IsPointerOverUIOrSwap()
        {
            Vector3 mp = Input.mousePosition;
            if (HUD.IsScreenPointOverHUD(mp))
                return true;

            if (_cachedCam == null)
                _cachedCam = Camera.main;

            if (_cachedCam != null)
            {
                Vector3 wp = _cachedCam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, Mathf.Abs(_cachedCam.transform.position.z)));
                // Check if user clicked on the Next Orb Chamber to swap orbs
                if (Vector2.Distance(new Vector2(wp.x, wp.y), new Vector2(GetNextQueuePosition().x, GetNextQueuePosition().y)) <= 0.78f)
                {
                    SwapCurrentAndNextOrb();
                    return true;
                }
                // Check if pointer is outside the playfield horizontally or below the deck
                if (wp.x < leftBorder - 0.25f || wp.x > rightBorder + 0.25f || wp.y < -1.1f || wp.y > topBorder + 0.25f)
                    return true;
            }
            return false;
        }

        public void SwapCurrentAndNextOrb()
        {
            if (!_isPlaying || (gameModel != null && gameModel.isPaused)) return;
            if (_currentBubble == null || _currentBubble.isMoving || _nextBubble == null) return;

            BubbleColor temp = _currentBubble.bubble.color;
            _currentBubble.SetColor(_nextBubble.bubble.color);
            _nextBubble.SetColor(temp);
            _currentBubble.PlaySpawnPop(0f);
            _nextBubble.PlaySpawnPop(0f);
            if (_nextChamberRenderer != null)
                _nextChamberRenderer.color = Color.Lerp(Color.white, CyberArtGenerator.GetPrimaryColor(_nextBubble.bubble.color), 0.3f);

            CyberAudioSynthesizer.Instance.Play("click", 0.75f, 1.15f);
        }

        public void FireCurrentOrb()
        {
            if (!this._isPlaying || (gameModel != null && gameModel.isPaused)) return;
            if (this._currentBubble != null && !this._currentBubble.isMoving)
            {
                this._currentBubble.isMoving = true;
                this._currentBubble.angle = this.shootingRotation();
                this._currentBubble = null;

                if (_shooterController != null)
                    _shooterController.TriggerFireRecoil();

                CyberAudioSynthesizer.Instance.Play("fire", 0.85f, Random.Range(0.96f, 1.05f));
            }
        }

        private BubbleController createBubble(bool useSmartColor = false)
        {
            UnityEngine.Object prefabResource = Resources.Load(_bubblePrefabName);
            GameObject bubblePrefab;
            if (prefabResource != null)
            {
                bubblePrefab = Instantiate(prefabResource) as GameObject;
            }
            else
            {
                bubblePrefab = new GameObject("CyberBubble");
                bubblePrefab.AddComponent<BubbleController>();
            }

            bubblePrefab.transform.parent = _bubblesContainer.transform;
            bubblePrefab.transform.position = GetShooterPosition();

            BubbleController bubbleController = bubblePrefab.GetComponent<BubbleController>();
            if (bubbleController == null)
                bubbleController = bubblePrefab.AddComponent<BubbleController>();

            if (useSmartColor && _matrix != null)
            {
                bubbleController.SetColor(JQUtils.GetSmartBubbleColor(_matrix.bubbles));
            }
            else
            {
                bubbleController.RefreshVisuals();
            }

            bubbleController.leftBorder = this.geometry.leftBorder;
            bubbleController.rightBorder = this.geometry.rightBorder;
            bubbleController.topBorder = this.geometry.topBorder;
            bubbleController.radius = this.geometry.bubbleRadius;
            bubbleController.linearSpeed = _bubbleLinearSpeed;
            bubbleController.angle = 90.0f;
            bubbleController.isMoving = false;
            bubbleController.CollisionDelegate = onBubbleCollision;
            bubbleController.MotionDelegate = canMoveToPosition;
            this._bubbleControllers.Add(bubbleController);
            return bubbleController;
        }

        private void PositionNextBubbleInQueue()
        {
            if (_nextBubble == null) return;
            _nextBubble.transform.position = GetNextQueuePosition();
            _nextBubble.transform.localScale = Vector3.one * 0.85f;
            if (_nextChamberRenderer != null)
                _nextChamberRenderer.color = Color.Lerp(Color.white, CyberArtGenerator.GetPrimaryColor(_nextBubble.bubble.color), 0.3f);
        }

        private void PromoteNextBubbleToChamber()
        {
            if (_nextBubble == null)
                _nextBubble = createBubble(true);

            _currentBubble = _nextBubble;

            // Preserve the previewed color unless that color no longer exists on the board
            ArrayList boardBubbles = _matrix.bubbles;
            if (boardBubbles.Count > 0 && JQUtils.FilterByColor(boardBubbles, _currentBubble.bubble.color).Count == 0)
            {
                _currentBubble.SetColor(JQUtils.GetSmartBubbleColor(boardBubbles));
            }

            _currentBubble.transform.localScale = Vector3.one;
            _currentBubble.moveTo(GetShooterPosition(), 0.12f);

            _nextBubble = createBubble(true);
            PositionNextBubbleInQueue();
            _nextBubble.PlaySpawnPop(0.04f);
        }

        private float shootingRotation()
        {
            if (_shooterController != null)
                return _shooterController.aimAngle;

            float shooterRotation = this._bubbleShooter.transform.eulerAngles.z;
            float ballRotation = 90;
            if (shooterRotation <= 360 && shooterRotation >= 270.0f)
                ballRotation = shooterRotation - 270;
            if (shooterRotation <= 90 && shooterRotation >= 0)
                ballRotation = 90 + shooterRotation;
            return ballRotation;
        }

        private void destroyBubble(BubbleController bubbleController, bool explodes)
        {
            if (bubbleController == null) return;
            this._matrix.remove(bubbleController.bubble);
            this._bubbleControllers.Remove(bubbleController);
            bubbleController.CollisionDelegate = null;
            bubbleController.MotionDelegate = null;
            bubbleController.kill(explodes);
        }

        private BubbleController controllerForBubble(Bubble bubble)
        {
            foreach (BubbleController bubbleController in this._bubbleControllers)
            {
                if (bubbleController != null && bubbleController.bubble == bubble)
                    return bubbleController;
            }
            return null;
        }

        private void destroyCluster(ArrayList cluster, bool explodes)
        {
            foreach (Bubble bubble in cluster)
            {
                BubbleController bc = this.controllerForBubble(bubble);
                if (bc != null)
                    this.destroyBubble(bc, explodes);
            }
        }

        private void addRow(bool isInitialSetup = false)
        {
            this._pendingToAddRow = false;
            bool overflows = this._matrix.shiftOneRow();

            for (int i = 0; i < this.geometry.columns; i++)
            {
                BubbleController bubbleController = this.createBubble(false);
                bubbleController.isMoving = false;
                this._matrix.insert(bubbleController.bubble, 0, i);
                if (isInitialSetup)
                    bubbleController.PlaySpawnPop(i * 0.015f);
            }

            foreach (BubbleController bubbleController in this._bubbleControllers)
            {
                if (bubbleController != null && bubbleController != this._currentBubble && bubbleController != this._nextBubble)
                {
                    Vector2 loc = this._matrix.location(bubbleController.bubble);
                    if (loc.x >= 0 && loc.y >= 0)
                    {
                        Vector3 position = BubbleMatrixControllerHelper.PositionForCell(loc, geometry, this._matrix.isBaselineAlignedLeft);
                        if (isInitialSetup)
                            bubbleController.transform.position = position;
                        else
                            bubbleController.moveTo(position, 0.16f);
                    }
                }
            }

            if (overflows)
            {
                this.FinishGame(GameState.Loose);
                return;
            }
        }

        public void TriggerTimeUpFinish()
        {
            if (!_isPlaying) return;
            GameState finalState = (gameModel != null && gameModel.score >= gameModel.opponentScore)
                ? GameState.Win
                : GameState.Loose;
            FinishGame(finalState);
        }

        private void FinishGame(GameState state)
        {
            if (!_isPlaying) return;
            this._isPlaying = false;

            if (this._shooterController != null)
                this._shooterController.isAiming = false;

            if (state == GameState.Win)
                CyberAudioSynthesizer.Instance.Play("victory", 0.9f, 1.0f);
            else
                CyberAudioSynthesizer.Instance.Play("gameover", 0.9f, 1.0f);

            GameEvents.GameFinished(state);
        }

        /* Collision Delegate Handler */
        void onBubbleCollision(GameObject bubble)
        {
            if (!_isPlaying) return;

            BubbleController bubbleController = bubble.GetComponent<BubbleController>();
            Vector2 snapCell = BubbleMatrixControllerHelper.FindBestSnapCell(bubble.transform.position, this._matrix, this.geometry);
            int targetRow = Mathf.RoundToInt(snapCell.x);
            int targetCol = Mathf.RoundToInt(snapCell.y);

            // If the ball snaps below the bottom row, the game is over
            if (targetRow >= this.geometry.rows)
            {
                CyberVFXManager.Instance.AddCameraTrauma(0.55f);
                this.FinishGame(GameState.Loose);
                return;
            }

            // Insert into model
            this._matrix.insert(bubbleController.bubble, targetRow, targetCol);
            Vector3 snapWorldPos = BubbleMatrixControllerHelper.PositionForCell(snapCell, geometry, this._matrix.isBaselineAlignedLeft);
            bubbleController.moveTo(snapWorldPos, 0.08f);

            // Check for matching color cluster (3 or more)
            ArrayList cluster = this._matrix.colorCluster(bubbleController.bubble);
            bool poppedCluster = cluster.Count > 2;

            if (gameModel != null)
            {
                gameModel.RegisterShotResult(poppedCluster);
            }

            if (poppedCluster)
            {
                bubbleController.transform.position = snapWorldPos;
                Color orbNeon = CyberArtGenerator.GetPrimaryColor(bubbleController.bubble.color);

                this.destroyCluster(cluster, true);
                GameEvents.BubblesRemoved(cluster.Count, true);

                int currentCombo = gameModel != null ? gameModel.combo : 1;
                int earnedPop = cluster.Count * Game.PointsPerExplosion * currentCombo;

                float pitch = Mathf.Clamp(0.95f + (currentCombo - 1) * 0.12f, 0.95f, 1.75f);
                CyberAudioSynthesizer.Instance.Play("pop", 0.9f, pitch);
                CyberVFXManager.Instance.AddCameraTrauma(Mathf.Clamp(0.18f + cluster.Count * 0.04f, 0.2f, 0.65f));

                // Floating score & combo callout
                string callout = "+" + earnedPop;
                if (currentCombo >= 3)
                    callout = "SUPER COMBO x" + currentCombo + "\n+" + earnedPop;
                else if (currentCombo == 2)
                    callout = "x2 COMBO  +" + earnedPop;

                CyberVFXManager.Instance.SpawnFloatingCallout(
                    snapWorldPos,
                    callout,
                    orbNeon,
                    currentCombo >= 3 ? 1.22f : 1.0f
                );

                // Drop loose disconnected bubbles (Avalanche!)
                ArrayList loose = this._matrix.looseBubbles();
                if (loose.Count > 0)
                {
                    this.destroyCluster(loose, false);
                    GameEvents.BubblesRemoved(loose.Count, false);
                    int earnedFall = loose.Count * Game.PointsPerFall * currentCombo;
                    CyberAudioSynthesizer.Instance.Play("avalanche", 0.85f, 1.05f);
                    CyberVFXManager.Instance.AddCameraTrauma(0.32f);
                    CyberVFXManager.Instance.SpawnFloatingCallout(
                        snapWorldPos + new Vector3(0f, -0.65f, 0f),
                        "AVALANCHE +" + earnedFall,
                        new Color(1.0f, 0.82f, 0.15f, 1f),
                        1.15f
                    );
                }
            }
            else
            {
                CyberAudioSynthesizer.Instance.Play("snap", 0.75f, 1.0f);

                // Check if all 5 power cells depleted -> drop a new row!
                if (gameModel != null && gameModel.ConsumeRowDropIfDepleted())
                {
                    this._pendingToAddRow = true;
                }
            }

            RefreshPowerCellsVisuals();

            // Add a new row if miss battery depleted
            if (_pendingToAddRow)
            {
                CyberAudioSynthesizer.Instance.Play("rowdrop", 0.8f, 1.0f);
                CyberVFXManager.Instance.AddCameraTrauma(0.35f);
                CyberVFXManager.Instance.SpawnFloatingCallout(
                    new Vector3((rightBorder - leftBorder) * 0.5f, topBorder - 1.2f, 0f),
                    "SHIELD DEPLETED - ROW DROP!",
                    new Color(1f, 0.25f, 0.55f, 1f),
                    1.05f
                );
                this.addRow(false);
                RefreshPowerCellsVisuals();
                if (!_isPlaying) return;
            }

            // If all bubbles are cleared from the board -> PERFECT CLEAR!
            if (this._matrix.bubbles.Count == 0)
            {
                if (gameModel != null)
                    gameModel.AddClearBonus(2500);

                CyberVFXManager.Instance.SpawnFloatingCallout(
                    new Vector3((rightBorder - leftBorder) * 0.5f, topBorder * 0.5f, 0f),
                    "PERFECT CLEAR! +2,500",
                    new Color(0.22f, 1f, 0.45f, 1f),
                    1.35f
                );
                this.FinishGame(GameState.Win);
                return;
            }

            // Load next orb into the railgun chamber
            PromoteNextBubbleToChamber();
        }

        /// <summary>
        /// Circle-circle collision check against all occupied cells in the matrix.
        /// Prevents orbs from passing through gaps or overwriting occupied cells.
        /// </summary>
        bool canMoveToPosition(Vector3 position)
        {
            float hitDiameter = this.geometry.bubbleRadius * 1.84f;
            float hitDiameterSq = hitDiameter * hitDiameter;

            for (int r = 0; r < this.geometry.rows; r++)
            {
                for (int c = 0; c < this.geometry.columns; c++)
                {
                    if (!this._matrix.hasBubble(r, c)) continue;

                    Vector3 cellWorld = BubbleMatrixControllerHelper.PositionForCell(
                        new Vector2(r, c),
                        this.geometry,
                        this._matrix.isBaselineAlignedLeft
                    );

                    float dx = position.x - cellWorld.x;
                    float dy = position.y - cellWorld.y;
                    if (dx * dx + dy * dy <= hitDiameterSq)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
