using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DropStack.Data;
using DropStack.Game;
using DropStack.Rendering;
using DropStack.Audio;
using DropStack.UI;

namespace DropStack.Editor
{
    public static class DropStackGameTests
    {
        [MenuItem("Drop Stack/Run Verification Tests")]
        public static void RunAllTests()
        {
            Debug.Log("==================================================");
            Debug.Log("[DropStack Test] RUNNING AUTOMATED VERIFICATION SUITE...");
            Debug.Log("==================================================");

            int passed = 0;
            int total = 0;

            RunTest("Polyomino Database & Shapes", TestPolyominoDatabase, ref passed, ref total);
            RunTest("Board Mechanics & Line Clearing", TestBoardMechanics, ref passed, ref total);
            RunTest("Non-adjacent Clears & Hidden Rows", TestLineCollapse, ref passed, ref total);
            RunTest("Pause Input Guard", TestPausedInput, ref passed, ref total);
            RunTest("Speed Curve & Escalation", TestSpeedCurve, ref passed, ref total);
            RunTest("Difficulty Pacing & Classic Bag", TestDifficultyRules, ref passed, ref total);
            RunTest("Difficulty Best Score Isolation", TestDifficultyRecords, ref passed, ref total);
            RunTest("Score & Streak Combos", TestScoringAndCombos, ref passed, ref total);
            RunTest("Scene Hierarchy & Component Binding", TestSceneIntegrity, ref passed, ref total);

            Debug.Log("==================================================");
            Debug.Log($"[DropStack Test] TEST SUITE FINISHED: {passed}/{total} TESTS PASSED!");
            Debug.Log("==================================================");

            if (passed != total)
            {
                throw new System.Exception("Drop Stack verification failed. See the failures above.");
            }
        }

        private static void RunTest(string testName, System.Action testAction, ref int passed, ref int total)
        {
            total++;
            try
            {
                testAction();
                Debug.Log($"[PASS] {testName}");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void TestPolyominoDatabase()
        {
            var allTypes = PolyominoDatabase.GetAllTypes();
            Assert(allTypes.Count == 16, $"Expected 16 polyomino types, found {allTypes.Count}");

            // Verify Trominoes (3 blocks)
            var i3 = PolyominoDatabase.GetShape(PolyominoType.I3);
            Assert(i3 != null && i3.blockCount == 3, "I3 shape should have 3 blocks");
            var l3 = PolyominoDatabase.GetShape(PolyominoType.L3);
            Assert(l3 != null && l3.blockCount == 3, "L3 shape should have 3 blocks");

            // Verify Classic Tetrominoes (4 blocks)
            var i4 = PolyominoDatabase.GetShape(PolyominoType.I4);
            Assert(i4 != null && i4.blockCount == 4, "I4 shape should have 4 blocks");
            var o4 = PolyominoDatabase.GetShape(PolyominoType.O4);
            Assert(o4 != null && o4.blockCount == 4, "O4 shape should have 4 blocks");
            var t4 = PolyominoDatabase.GetShape(PolyominoType.T4);
            Assert(t4 != null && t4.blockCount == 4, "T4 shape should have 4 blocks");

            // Verify Pentaminoes (5 blocks)
            var p5 = PolyominoDatabase.GetShape(PolyominoType.P5);
            Assert(p5 != null && p5.blockCount == 5, "P5 shape should have 5 blocks");
            var u5 = PolyominoDatabase.GetShape(PolyominoType.U5);
            Assert(u5 != null && u5.blockCount == 5, "U5 shape should have 5 blocks");
            var i5 = PolyominoDatabase.GetShape(PolyominoType.I5);
            Assert(i5 != null && i5.blockCount == 5, "I5 shape should have 5 blocks");

            // Verify 4 rotations per shape
            foreach (var type in allTypes)
            {
                var shape = PolyominoDatabase.GetShape(type);
                Assert(shape != null, $"Shape {type} must not be null");
                Assert(shape.rotations.Length == 4, $"Shape {type} must have 4 rotation states");
                for (int r = 0; r < 4; r++)
                {
                    var blocks = shape.GetBlocks(r);
                    Assert(blocks.Length == shape.blockCount, $"Shape {type} rotation {r} must have {shape.blockCount} blocks");
                }
            }

            // Verify Bag Randomizer
            var bag = new BagRandomizer();
            HashSet<PolyominoType> drawn = new HashSet<PolyominoType>();
            for (int i = 0; i < 16; i++)
            {
                drawn.Add(bag.Next());
            }
            Assert(drawn.Count == 16, "First 16 draws from bag randomizer must contain all 16 unique shapes");
        }

        private static void TestBoardMechanics()
        {
            GameObject go = new GameObject("TestBoardHolder");
            try
            {
                var board = go.AddComponent<DropStackBoard>();
                board.ClearMatrix();

                Assert(DropStackBoard.Columns == 10, "Columns should be 10");
                Assert(DropStackBoard.VisibleRows == 20, "VisibleRows should be 20");

                var i4 = PolyominoDatabase.GetShape(PolyominoType.I4);

                // In bounds check
                Assert(board.IsValidPosition(i4, new Vector2Int(4, 10), 0), "Valid position (4,10) should return true");

                // Out of left bound
                Assert(!board.IsValidPosition(i4, new Vector2Int(0, 10), 0), "I4 at x=0 rot=0 has block at x=-1 -> should be invalid");

                // Out of bottom floor
                Assert(!board.IsValidPosition(i4, new Vector2Int(4, -1), 0), "Below floor (y=-1) should be invalid");

                // Test Line Fill & Detection
                // Fill row 0 completely
                for (int x = 0; x < DropStackBoard.Columns; x++)
                {
                    // Lock a block at (x, 0)
                    var singleShape = PolyominoDatabase.GetShape(PolyominoType.O4);
                    // Manually simulate full row
                }

                // Place a 4-block piece at (2, 0)
                board.LockPiece(i4, new Vector2Int(2, 0), 0); // covers (1,0), (2,0), (3,0), (4,0)
                Assert(board.IsCellOccupied(1, 0), "Cell (1,0) should be occupied");
                Assert(board.IsCellOccupied(2, 0), "Cell (2,0) should be occupied");
                Assert(board.IsCellOccupied(3, 0), "Cell (3,0) should be occupied");
                Assert(board.IsCellOccupied(4, 0), "Cell (4,0) should be occupied");
                Assert(!board.IsCellOccupied(0, 0), "Cell (0,0) should be empty");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void TestSpeedCurve()
        {
            GameObject go = new GameObject("TestSpeedHolder");
            try
            {
                var speed = go.AddComponent<SpeedCurveManager>();
                speed.ResetSpeed();

                Assert(speed.CurrentLevel == 1, "Initial speed level should be 1");
                Assert(Mathf.Approximately(speed.CurrentDropInterval, 1.00f), "Level 1 interval should be 1.00s (1.0 drop/sec)");

                // After 16 seconds (Level 2)
                speed.UpdateTime(16f);
                Assert(speed.CurrentLevel == 2, $"After 16s, speed level should be 2, got {speed.CurrentLevel}");
                Assert(speed.CurrentDropInterval < 1.00f, "Drop interval should accelerate in Level 2");

                // After 61 seconds (Final 30s escalation, Level 5)
                speed.UpdateTime(61f);
                Assert(speed.CurrentLevel == 5, $"After 61s, speed level should be 5, got {speed.CurrentLevel}");
                Assert(speed.IsPeakVelocity, "Level 5 should be Peak Velocity phase");

                // After 76 seconds (Final 15s frenzy, Level 6)
                speed.UpdateTime(76f);
                Assert(speed.CurrentLevel == 6, $"After 76s, speed level should be 6, got {speed.CurrentLevel}");
                Assert(Mathf.Approximately(speed.CurrentDropInterval, 0.24f), "Neutral level 6 should remain manageable at 0.24s");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static PolyominoShape SingleCell(PolyominoType type)
        {
            var cells = new[] { Vector2Int.zero };
            return new PolyominoShape { type=type, blockCount=1, rotations=new[]{cells,cells,cells,cells} };
        }

        private static void TestLineCollapse()
        {
            var go = new GameObject("LineCollapseTest");
            try
            {
                var board = go.AddComponent<DropStackBoard>(); board.ClearMatrix();
                var cell = SingleCell(PolyominoType.O4);
                foreach (int row in new[]{0,2}) for(int x=0;x<10;x++) board.LockPiece(cell,new Vector2Int(x,row),0);
                board.LockPiece(SingleCell(PolyominoType.S4),new Vector2Int(2,1),0);
                board.LockPiece(SingleCell(PolyominoType.T4),new Vector2Int(4,3),0);
                board.LockPiece(SingleCell(PolyominoType.I4),new Vector2Int(6,DropStackBoard.TotalRows-1),0);
                var lines=board.FindFullLines(); Assert(lines.Count==2,"Both non-adjacent full rows detected");
                var clear=board.ClearLinesCoroutine(lines,null);
                Assert(clear.MoveNext() && board.IsClearingLines,"Line clear freezes piece input during animation");
                Assert(!clear.MoveNext(),"Line clear completes after its animation wait");
                Assert(board.GetCell(2,0)==(int)PolyominoType.S4,"Row between cleared lines falls one cell");
                Assert(board.GetCell(4,1)==(int)PolyominoType.T4,"Row above both cleared lines falls two cells");
                Assert(board.GetCell(6,DropStackBoard.TotalRows-3)==(int)PolyominoType.I4,"Hidden buffer rows collapse with the visible board");
                Assert(board.GetCell(6,DropStackBoard.TotalRows-1)==0,"Top vacated rows become empty");
                board.ClearMatrix(); board.LockPiece(cell,new Vector2Int(0,DropStackBoard.VisibleRows),0);
                Assert(!board.IsCompletelyEmpty(),"Hidden blocks prevent a false perfect-clear bonus");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void TestPausedInput()
        {
            var go = new GameObject("PausedInputTest");
            try
            {
                var board=go.AddComponent<DropStackBoard>(); var score=go.AddComponent<ScoreManager>(); var speed=go.AddComponent<SpeedCurveManager>();
                var controller=go.AddComponent<ActivePieceController>(); controller.Initialize(board,score,speed);
                Assert(controller.SpawnPiece(PolyominoDatabase.GetShape(PolyominoType.T4)),"Piece spawns");
                var before=controller.BoardPosition; int rotation=controller.RotationIndex;
                controller.SetInputEnabled(false); controller.StartMove(-1); controller.Rotate(1); controller.ExecuteHardDrop(); controller.RequestHold();
                Assert(controller.BoardPosition==before && controller.RotationIndex==rotation && score.CurrentScore==0,"Paused controls cannot move, rotate, drop or score");
                controller.SetInputEnabled(true); controller.StartMove(-1);
                Assert(controller.BoardPosition.x==before.x-1,"Resuming re-enables movement");
                controller.StopMove();
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void TestDifficultyRules()
        {
            for(int level=1;level<=6;level++) {
                Assert(DifficultyRules.DropInterval(GameDifficulty.Noob,level)>DifficultyRules.DropInterval(GameDifficulty.Neutral,level),"Noob is slower at every level");
                Assert(DifficultyRules.DropInterval(GameDifficulty.Neutral,level)>DifficultyRules.DropInterval(GameDifficulty.Pro,level),"Pro is faster at every level");
                foreach(GameDifficulty mode in System.Enum.GetValues(typeof(GameDifficulty)))
                    if(level>1) Assert(DifficultyRules.DropInterval(mode,level)<DifficultyRules.DropInterval(mode,level-1),"Each mode escalates without an inverted speed step");
            }
            var bag=new BagRandomizer(); bag.Reset(DifficultyRules.Pieces(GameDifficulty.Noob));
            for(int cycle=0;cycle<2;cycle++) { var types=new HashSet<PolyominoType>(); for(int i=0;i<7;i++) { var type=bag.Next(); types.Add(type); Assert(PolyominoDatabase.GetShape(type).blockCount==4,"Noob only includes classic four-block pieces"); } Assert(types.Count==7,"Noob seven-bag stays balanced across refills"); }
            bag.Reset(DifficultyRules.Pieces(GameDifficulty.Pro)); var full=new HashSet<PolyominoType>(); for(int i=0;i<16;i++) full.Add(bag.Next()); Assert(full.Count==16,"Switching to Pro restores all sixteen pieces");
            Assert(DifficultyRules.LockDelay(GameDifficulty.Noob)>DifficultyRules.LockDelay(GameDifficulty.Pro),"Noob grants more placement time");
        }

        private static void TestDifficultyRecords()
        {
            string[] keys={"DropStack_HighScore_v1","DropStack_Best_Noob_v2","DropStack_Best_Neutral_v2","DropStack_Best_Pro_v2"};
            var existed=new bool[keys.Length]; var saved=new int[keys.Length];
            for(int i=0;i<keys.Length;i++) { existed[i]=PlayerPrefs.HasKey(keys[i]); saved[i]=PlayerPrefs.GetInt(keys[i],0); PlayerPrefs.DeleteKey(keys[i]); }
            var go=new GameObject("DifficultyRecordsTest");
            try {
                PlayerPrefs.SetInt(keys[0],123); var score=go.AddComponent<ScoreManager>(); score.ResetScore(); Assert(score.HighScore==123,"Existing records migrate to Neutral");
                score.SetDifficulty(GameDifficulty.Noob); score.ResetScore(); score.RegisterDropScore(50,true); score.SaveHighScore();
                score.SetDifficulty(GameDifficulty.Pro); score.ResetScore(); score.RegisterDropScore(10,true); score.SaveHighScore();
                Assert(ScoreManager.BestFor(GameDifficulty.Noob)==100,"Noob keeps its own best"); Assert(ScoreManager.BestFor(GameDifficulty.Pro)==20,"Pro keeps its own best"); Assert(ScoreManager.BestFor(GameDifficulty.Neutral)==123,"Noob and Pro cannot overwrite Neutral");
            } finally { Object.DestroyImmediate(go); for(int i=0;i<keys.Length;i++) { if(existed[i]) PlayerPrefs.SetInt(keys[i],saved[i]); else PlayerPrefs.DeleteKey(keys[i]); } PlayerPrefs.Save(); }
        }

        private static void TestScoringAndCombos()
        {
            GameObject go = new GameObject("TestScoreHolder");
            try
            {
                var score = go.AddComponent<ScoreManager>();
                score.ResetScore();

                // Single line clear (Streak 1): 100 * 1.0 = 100
                int pts1 = score.RegisterLineClear(1, false);
                Assert(pts1 == 100, $"Single line clear should award 100 pts, got {pts1}");
                Assert(score.CurrentStreak == 1, "Current streak should be 1");
                Assert(Mathf.Approximately(score.CurrentMultiplier, 1.0f), "Multiplier for streak 1 should be 1.0x");

                // Double line clear (Streak 2): 300 * 1.2 = 360
                int pts2 = score.RegisterLineClear(2, false);
                Assert(pts2 == 360, $"Double line clear with streak 2 should award 360 pts, got {pts2}");
                Assert(score.CurrentStreak == 2, "Current streak should be 2");
                Assert(Mathf.Approximately(score.CurrentMultiplier, 1.2f), "Multiplier for streak 2 should be 1.2x");

                // Triple line clear (Streak 3): 700 * 1.5 = 1050
                int pts3 = score.RegisterLineClear(3, false);
                Assert(pts3 == 1050, $"Triple line clear with streak 3 should award 1050 pts, got {pts3}");
                Assert(score.CurrentStreak == 3, "Current streak should be 3");
                Assert(Mathf.Approximately(score.CurrentMultiplier, 1.5f), "Multiplier for streak 3 should be 1.5x");

                // Drop Stack 4 lines clear (Streak 4): 1500 * 2.0 = 3000
                int pts4 = score.RegisterLineClear(4, false);
                Assert(pts4 == 3000, $"4 lines clear with streak 4 should award 3000 pts, got {pts4}");
                Assert(score.CurrentStreak == 4, "Current streak should be 4");
                Assert(Mathf.Approximately(score.CurrentMultiplier, 2.0f), "Multiplier for streak 4 should be 2.0x");

                // Mega Pentris 5 lines clear (Streak 5): 2500 * 2.5 = 6250
                int pts5 = score.RegisterLineClear(5, false);
                Assert(pts5 == 6250, $"5 lines clear with streak 5 should award 6250 pts, got {pts5}");
                Assert(score.CurrentStreak == 5, "Current streak should be 5");

                // Piece locked with no lines cleared -> Streak resets!
                score.RegisterPieceLockedNoClear();
                Assert(score.CurrentStreak == 0, "Streak should reset to 0 after piece locked with no lines cleared");
                Assert(Mathf.Approximately(score.CurrentMultiplier, 1.0f), "Multiplier should reset to 1.0x");

                // Drop score
                int beforeDrop = score.CurrentScore;
                score.RegisterDropScore(10, true); // Hard drop 10 cells -> +20 pts
                Assert(score.CurrentScore == beforeDrop + 20, "Hard drop 10 cells should add 20 points");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void TestSceneIntegrity()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            foreach(var root in scene.GetRootGameObjects())
                foreach(var child in root.GetComponentsInChildren<Transform>(true))
                    Assert(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject)==0,$"No missing scripts on {child.name}");

            var gameMgr = Object.FindFirstObjectByType<DropStackGameManager>();
            Assert(gameMgr != null, "DropStackGameManager must exist in scene");

            var board = Object.FindFirstObjectByType<DropStackBoard>();
            Assert(board != null, "DropStackBoard must exist in scene");

            var pieceCtrl = Object.FindFirstObjectByType<ActivePieceController>();
            Assert(pieceCtrl != null, "ActivePieceController must exist in scene");

            var scoreMgr = Object.FindFirstObjectByType<ScoreManager>();
            Assert(scoreMgr != null, "ScoreManager must exist in scene");

            var speedMgr = Object.FindFirstObjectByType<SpeedCurveManager>();
            Assert(speedMgr != null, "SpeedCurveManager must exist in scene");

            var boardRenderer = Object.FindFirstObjectByType<BoardRenderer>();
            Assert(boardRenderer != null, "BoardRenderer must exist in scene");

            var activeRenderer = Object.FindFirstObjectByType<ActivePieceRenderer>();
            Assert(activeRenderer != null, "ActivePieceRenderer must exist in scene");

            var audioMgr = Object.FindFirstObjectByType<DropStackAudioManager>();
            Assert(audioMgr != null, "DropStackAudioManager must exist in scene");

            var juice = Object.FindFirstObjectByType<JuiceEffects>();
            Assert(juice != null, "JuiceEffects must exist in scene");

            var uiMgr = Object.FindFirstObjectByType<DropStackUIManager>();
            Assert(uiMgr != null, "DropStackUIManager must exist in scene");
            foreach(var component in new Component[]{gameMgr,pieceCtrl,boardRenderer,activeRenderer,uiMgr})
            {
                var serialized=new SerializedObject(component); var prop=serialized.GetIterator();
                while(prop.NextVisible(true))
                    if(prop.propertyType==SerializedPropertyType.ObjectReference && prop.name!="m_GameObject" && prop.name!="m_PrefabAsset" && prop.name!="m_PrefabInstance" && prop.name!="m_CorrespondingSourceObject")
                        Assert(prop.objectReferenceValue!=null,$"Saved binding {component.GetType().Name}.{prop.propertyPath} must resolve");
            }

            var touchUi = Object.FindFirstObjectByType<TouchControlsUI>();
            Assert(touchUi != null, "TouchControlsUI must exist in scene");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception($"Assertion Failed: {message}");
            }
        }
    }
}
