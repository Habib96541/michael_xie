using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace com.javierquevedo.Editor
{
    // Manual only. This used to run on every script reload and wipe the whole scene,
    // which destroyed any hand-made scene edits. Use the CyberOrbs menu to run it on purpose.
    public static class CyberOrbsHierarchySetup
    {
        private const string ScenePath = "Assets/Scenes/Bust a Move.unity";

        [MenuItem("CyberOrbs/Build Hierarchy in Scene")]
        public static void BuildHierarchyMenu()
        {
            BuildAndSaveHierarchy(true);
        }

        private static void BuildHierarchyOnce()
        {
            BuildAndSaveHierarchy(false);
        }

        public static void BuildAndSaveHierarchy(bool force)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[CyberOrbsHierarchySetup] Building complete Cyber Orbs Hierarchy in: " + activeScene.path);

            // Clean up all roots in the scene to ensure a pristine Cyber Orbs setup
            var roots = activeScene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Object.DestroyImmediate(root);
            }

            // 1. --- MANAGERS ---
            GameObject mgrGroup = new GameObject("--- MANAGERS ---");
            mgrGroup.transform.position = Vector3.zero;

            GameObject gameMgr = new GameObject("GameManager");
            gameMgr.transform.SetParent(mgrGroup.transform, false);
            GameController gameController = gameMgr.AddComponent<GameController>();
            gameController.MenuSceneName = "MainMenu";

            GameObject audioGo = new GameObject("CyberAudioSynthesizer");
            audioGo.transform.SetParent(mgrGroup.transform, false);
            audioGo.AddComponent<CyberAudioSynthesizer>();

            GameObject vfxGo = new GameObject("CyberVFXManager");
            vfxGo.transform.SetParent(mgrGroup.transform, false);
            vfxGo.AddComponent<CyberVFXManager>();

            // 2. Camera & HUD
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.4f;
            cam.backgroundColor = new Color(0.035f, 0.045f, 0.075f, 1f); // #0b0e14 obsidian
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(5.25f, 5.15f, -10f);
            camGo.AddComponent<HUD>();

            // 3. --- ENVIRONMENT & PLAYFIELD ---
            GameObject envGroup = new GameObject("--- ENVIRONMENT & PLAYFIELD ---");
            envGroup.transform.position = Vector3.zero;

            GameObject playfield = new GameObject("CyberOrbs_Playfield");
            playfield.transform.SetParent(envGroup.transform, false);

            float leftBorder = 0.0f;
            float rightBorder = 10.5f;
            float topBorder = 10.0f;
            float bottomY = -1.45f;
            float width = rightBorder - leftBorder;
            float centerX = leftBorder + width * 0.5f;
            float height = topBorder - bottomY;
            float centerY = bottomY + height * 0.5f;

            // Outer Ambient Backdrop (scaled for ultra-wide & tablets)
            GameObject outerBg = CreateQuadSprite("OuterObsidianBackdrop", playfield.transform,
                new Vector3(centerX, centerY, 0.8f), new Vector2(42f, 32f),
                CyberArtGenerator.GetHexBackdropSprite(), new Color(0.55f, 0.65f, 0.78f, 0.45f), -20);

            // Playfield Hexagonal Grid Backdrop
            GameObject hexBg = CreateQuadSprite("HexGridBackdrop", playfield.transform,
                new Vector3(centerX, centerY, 0.5f), new Vector2(width + 0.1f, height + 0.1f),
                CyberArtGenerator.GetHexBackdropSprite(), Color.white, -10);

            // Danger Threshold Line (y = 0.02)
            Texture2D whiteTex = CyberArtGenerator.WhiteTexture;
            Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f), whiteTex.width);

            GameObject dangerLine = CreateQuadSprite("DangerThresholdLine", playfield.transform,
                new Vector3(centerX, 0.02f, 0.2f), new Vector2(width - 0.1f, 0.035f),
                whiteSprite, new Color(1f, 0.18f, 0.45f, 0.42f), 1);

            // Titanium Frame Borders
            GameObject frameGroup = new GameObject("Frame_TitaniumBorders");
            frameGroup.transform.SetParent(playfield.transform, false);
            Color titaniumDark = new Color(0.10f, 0.13f, 0.19f, 1f);
            Color titaniumBevel = new Color(0.22f, 0.29f, 0.39f, 1f);

            CreateQuadSprite("LeftTitaniumFrame", frameGroup.transform,
                new Vector3(leftBorder - 0.18f, centerY, 0.1f), new Vector2(0.36f, height + 0.68f), whiteSprite, titaniumDark, 2);
            CreateQuadSprite("RightTitaniumFrame", frameGroup.transform,
                new Vector3(rightBorder + 0.18f, centerY, 0.1f), new Vector2(0.36f, height + 0.68f), whiteSprite, titaniumDark, 2);
            CreateQuadSprite("TopTitaniumFrame", frameGroup.transform,
                new Vector3(centerX, topBorder + 0.18f, 0.1f), new Vector2(width + 0.72f, 0.36f), whiteSprite, titaniumDark, 2);
            CreateQuadSprite("BottomTitaniumDeck", frameGroup.transform,
                new Vector3(centerX, bottomY - 0.18f, 0.1f), new Vector2(width + 0.72f, 0.36f), whiteSprite, titaniumDark, 2);

            // Chamfered corner joint plates
            CreateQuadSprite("CornerTL", frameGroup.transform, new Vector3(leftBorder - 0.18f, topBorder + 0.18f, 0.08f), new Vector2(0.46f, 0.46f), whiteSprite, titaniumBevel, 3);
            CreateQuadSprite("CornerTR", frameGroup.transform, new Vector3(rightBorder + 0.18f, topBorder + 0.18f, 0.08f), new Vector2(0.46f, 0.46f), whiteSprite, titaniumBevel, 3);
            CreateQuadSprite("CornerBL", frameGroup.transform, new Vector3(leftBorder - 0.18f, bottomY - 0.18f, 0.08f), new Vector2(0.46f, 0.46f), whiteSprite, titaniumBevel, 3);
            CreateQuadSprite("CornerBR", frameGroup.transform, new Vector3(rightBorder + 0.18f, bottomY - 0.18f, 0.08f), new Vector2(0.46f, 0.46f), whiteSprite, titaniumBevel, 3);

            // Neon Perimeter Lighting Strips
            GameObject neonGroup = new GameObject("Neon_PerimeterLighting");
            neonGroup.transform.SetParent(playfield.transform, false);
            Color neonCyan = new Color(0.0f, 0.94f, 1.0f, 0.92f);
            Color neonPurple = new Color(0.68f, 0.20f, 1.0f, 0.88f);

            CreateQuadSprite("LeftNeonStrip", neonGroup.transform,
                new Vector3(leftBorder - 0.02f, centerY, 0.05f), new Vector2(0.055f, height), whiteSprite, neonCyan, 4);
            CreateQuadSprite("RightNeonStrip", neonGroup.transform,
                new Vector3(rightBorder + 0.02f, centerY, 0.05f), new Vector2(0.055f, height), whiteSprite, neonCyan, 4);
            CreateQuadSprite("TopNeonStrip", neonGroup.transform,
                new Vector3(centerX, topBorder + 0.02f, 0.05f), new Vector2(width, 0.06f), whiteSprite, neonPurple, 4);

            // Ammo Queue Side Slot ("NEXT [SWAP]")
            GameObject queueGroup = new GameObject("AmmoQueue_SideSlot");
            queueGroup.transform.SetParent(playfield.transform, false);
            Vector3 nextPos = new Vector3(leftBorder + 0.95f, -0.65f, 0f);

            GameObject nextChamber = CreateQuadSprite("NextOrbChamber", queueGroup.transform,
                new Vector3(nextPos.x, nextPos.y, 0.05f), new Vector2(0.88f, 0.88f),
                CyberArtGenerator.GetChamberRingSprite(), Color.white, 6);

            CreateTextLabel("NextSlotLabel", queueGroup.transform,
                new Vector3(nextPos.x, nextPos.y - 0.62f, -0.1f), "NEXT [SWAP]", 0.052f, new Color(0.55f, 0.88f, 1f, 0.85f));

            // 5-Cell Miss Penalty Shield Battery Tracker
            GameObject batteryGroup = new GameObject("ShieldBattery_Tracker");
            batteryGroup.transform.SetParent(playfield.transform, false);

            GameObject socketsGroup = new GameObject("Sockets");
            socketsGroup.transform.SetParent(batteryGroup.transform, false);
            GameObject fillsGroup = new GameObject("Fills");
            fillsGroup.transform.SetParent(batteryGroup.transform, false);

            float startCellX = nextPos.x + 1.15f;
            float cellSpacing = 0.56f;
            for (int i = 0; i < 5; i++)
            {
                Vector3 cellPos = new Vector3(startCellX + i * cellSpacing, nextPos.y, 0.05f);
                CreateQuadSprite("PowerCellSocket_" + i, socketsGroup.transform, cellPos, new Vector2(0.42f, 0.26f), whiteSprite, new Color(0.08f, 0.11f, 0.17f, 0.95f), 5);
                CreateQuadSprite("PowerCellFill_" + i, fillsGroup.transform, cellPos, new Vector2(0.34f, 0.18f), whiteSprite, new Color(0f, 0.92f, 1f, 1f), 6);
            }
            CreateTextLabel("BatteryLabel", batteryGroup.transform,
                new Vector3(startCellX + 2f * cellSpacing, nextPos.y - 0.34f, -0.1f), "SHIELD CELLS (MISS LIMIT)", 0.042f, new Color(0.48f, 0.75f, 0.92f, 0.75f));

            // 4. --- LAUNCHER & AMMO ---
            GameObject launcherGroup = new GameObject("--- LAUNCHER & AMMO ---");
            launcherGroup.transform.position = Vector3.zero;

            Vector3 shooterPos = new Vector3(centerX, -0.65f, 0f);
            GameObject shooterGo = new GameObject("BubbleShooter");
            shooterGo.transform.SetParent(launcherGroup.transform, false);
            shooterGo.transform.position = shooterPos;
            var shooterCtrl = shooterGo.AddComponent<BubbleShooterController>();

            // Turret Base
            GameObject tBase = CreateQuadSprite("TurretBase", shooterGo.transform,
                new Vector3(0f, 0f, 0.1f), Vector2.one, CyberArtGenerator.GetTurretBaseSprite(), Color.white, 6);

            // Swivel Railgun Barrel
            GameObject tBarrel = CreateQuadSprite("RailgunBarrel", shooterGo.transform,
                new Vector3(0f, 0f, 0.08f), Vector2.one, CyberArtGenerator.GetTurretBarrelSprite(), Color.white, 7);

            // Circular Loading Chamber
            GameObject tChamber = CreateQuadSprite("LoadingChamber", shooterGo.transform,
                new Vector3(0f, 0f, 0.05f), Vector2.one, CyberArtGenerator.GetChamberRingSprite(), Color.white, 8);

            // Holographic Laser Sight container & 28 dots
            GameObject laserSight = new GameObject("HolographicLaserSight");
            laserSight.transform.SetParent(shooterGo.transform, false);
            Sprite sparkSprite = CyberArtGenerator.GetAimDotSprite();
            for (int i = 0; i < 28; i++)
            {
                var dot = CreateQuadSprite("LaserDot_" + i, laserSight.transform, Vector3.zero, Vector2.one * 0.2f, sparkSprite, neonCyan, 5);
                dot.GetComponent<SpriteRenderer>().enabled = false;
            }

            // Ghost Orb Preview
            GameObject ghostOrb = CreateQuadSprite("GhostOrbPreview", shooterGo.transform,
                Vector3.zero, Vector2.one, CyberArtGenerator.GetGhostOrbSprite(), neonCyan, 9);
            ghostOrb.GetComponent<SpriteRenderer>().enabled = false;

            // 5. --- GAMEPLAY MATRIX ---
            GameObject matrixGroup = new GameObject("--- GAMEPLAY MATRIX ---");
            matrixGroup.transform.position = Vector3.zero;

            GameObject matrixRoot = new GameObject("BubbleMatrixRoot");
            matrixRoot.transform.SetParent(matrixGroup.transform, false);
            var matrixCtrl = matrixRoot.AddComponent<BubbleMatrixController>();
            matrixCtrl.leftBorder = leftBorder;
            matrixCtrl.rightBorder = rightBorder;
            matrixCtrl.topBorder = topBorder;
            matrixCtrl.rows = 10;
            matrixCtrl.columns = 10;
            matrixCtrl.bubbleRadius = 0.5f;

            GameObject bubblesContainer = new GameObject("Bubbles");
            bubblesContainer.transform.SetParent(matrixRoot.transform, false);

            // Frame the playfield in the Scene view in 2D mode
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(centerX, centerY, 0f), new Vector3(width + 4f, height + 4f, 5f)), false);
            }
            Selection.activeGameObject = playfield;

            // Save the scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            // Generate report
            string hierarchyTree = GenerateHierarchyTree(activeScene);
            File.WriteAllText("Assets/HierarchyReport.txt", hierarchyTree);
            Debug.Log("[CyberOrbsHierarchySetup] Hierarchy successfully built and saved!\n" + hierarchyTree);
        }

        private static GameObject CreateQuadSprite(string name, Transform parent, Vector3 localPos, Vector2 localScale, Sprite sprite, Color color, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        private static GameObject CreateTextLabel(string name, Transform parent, Vector3 localPos, string text, float charSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

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
            return go;
        }

        private static string GenerateHierarchyTree(UnityEngine.SceneManagement.Scene scene)
        {
            var sw = new System.Text.StringBuilder();
            sw.AppendLine("Scene Hierarchy for: " + scene.name + " (" + scene.path + ")");
            sw.AppendLine("============================================================");
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                PrintNode(r, 0, sw);
            }
            return sw.ToString();
        }

        private static void PrintNode(GameObject go, int indent, System.Text.StringBuilder sw)
        {
            string prefix = new string(' ', indent * 2) + "â€¢ " + go.name;
            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
            {
                if (c != null && c.GetType() != typeof(Transform))
                    compNames.Add(c.GetType().Name);
            }
            if (compNames.Count > 0)
                prefix += " [" + string.Join(", ", compNames.ToArray()) + "]";

            sw.AppendLine(prefix);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                PrintNode(go.transform.GetChild(i).gameObject, indent + 1, sw);
            }
        }
    }
}
 