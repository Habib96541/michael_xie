#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DropStack.Game;
using DropStack.UI;
using DropStack.Rendering;
using DropStack.Audio;

namespace DropStack.Diagnostics
{
    // Opt-in integration checks in a development player; absent from release WebGL.
    public class DropStackSmokeTest : MonoBehaviour
    {
        private string output;
        private int passed;
        private bool runtimeError;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")<0) return;
            new GameObject("SmokeTest").AddComponent<DropStackSmokeTest>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            output=Path.GetFullPath("Builds/Verification"); Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            var run=Run();
            while(true) {
                object next;
                try { if(!run.MoveNext()) break; next=run.Current; }
                catch(Exception ex) { File.WriteAllText(Path.Combine(output,"smoke-result.json"),"{\"passed\":false,\"error\":\""+ex.Message.Replace("\"","'")+"\"}"); Debug.LogException(ex); Application.Quit(1); yield break; }
                yield return next;
            }
            File.WriteAllText(Path.Combine(output,"smoke-result.json"),"{\"passed\":true,\"checks\":"+passed+",\"runtimeErrors\":"+(runtimeError?"true":"false")+"}");
            Debug.Log("[DropStack Smoke] "+passed+" checks passed. Runtime errors: "+runtimeError);
            Application.Quit(runtimeError?1:0);
        }
        private void Log(string message,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) runtimeError=true; }
        private void Check(bool condition,string message) { if(!condition) throw new Exception(message); passed++; Debug.Log("[Smoke PASS] "+message); }
        private void Click(string path) => GameObject.Find("Canvas").transform.Find(path).GetComponent<Button>().onClick.Invoke();
        private IEnumerator Capture(string name) { yield return new WaitForSecondsRealtime(.3f); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png")); yield return new WaitForSecondsRealtime(.25f); }
        private IEnumerator Run()
        {
            Screen.SetResolution(1190,890,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.2f);
            Screen.SetResolution(1200,900,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.7f);
            var gm=DropStackGameManager.Instance; var ui=FindFirstObjectByType<DropStackUIManager>();
            var piece=gm.GetComponent<ActivePieceController>(); var score=gm.GetComponent<ScoreManager>();
            Check(gm.CurrentState==GameState.StartMenu && !piece.InputEnabled,"Start menu is idle");
            Click("StartMenu/StartCard/Mode_NOOB"); yield return null;
            Check(gm.Difficulty==GameDifficulty.Noob&&gm.NextQueue.Count==3,"Noob selector updates the run profile and queue");
            foreach(var next in gm.NextQueue) Check(DropStack.Data.PolyominoDatabase.GetShape(next).blockCount==4,"Noob preview uses classic pieces");
            Click("StartMenu/StartCard/Mode_PRO"); yield return null;
            Check(gm.Difficulty==GameDifficulty.Pro,"Pro selector is functional");
            Click("StartMenu/StartCard/Mode_NEUTRAL"); yield return null;
            yield return Capture("01-start");
            Click("StartMenu/StartCard/Play"); yield return null;
            Check(gm.CurrentState==GameState.Playing&&piece.HasActivePiece,"Play button starts the run");
            var hudRoot=GameObject.Find("Canvas").transform;
            var fieldCorners=new Vector3[4]; var sideCorners=new Vector3[4];
            hudRoot.Find("PlayfieldFrame").GetComponent<RectTransform>().GetWorldCorners(fieldCorners);
            hudRoot.Find("StatsPanel").GetComponent<RectTransform>().GetWorldCorners(sideCorners);
            Check(sideCorners[2].x<fieldCorners[0].x,"Desktop score panel cannot cover the playfield");
            hudRoot.Find("NextPanel").GetComponent<RectTransform>().GetWorldCorners(sideCorners);
            Check(sideCorners[0].x>fieldCorners[2].x,"Desktop previews stay outside the playfield");
            hudRoot.Find("ActionToast").GetComponent<RectTransform>().GetWorldCorners(sideCorners);
            Check(sideCorners[0].y>=fieldCorners[2].y-.1f,"Feedback banners never cover the active playfield");
            Check(!gm.SetDifficulty(GameDifficulty.Pro)&&gm.Difficulty==GameDifficulty.Neutral,"Difficulty cannot change during a live run");
            int visible=0; foreach(var sr in FindFirstObjectByType<ActivePieceRenderer>().GetComponentsInChildren<SpriteRenderer>()) if(sr.enabled) visible++;
            Check(visible>0,"Active piece and landing guide render after scene reload");
            Click("Controls/Hold"); yield return null;
            Check(gm.HoldPiece!=DropStack.Data.PolyominoType.None&&!gm.CanHold,"Hold swaps and locks until placement");
            var held=gm.HoldPiece; Click("Controls/Hold");
            Check(gm.HoldPiece==held,"Repeated hold cannot bypass the once-per-piece rule");
            var eventData=new PointerEventData(EventSystem.current);
            var right=GameObject.Find("Canvas").transform.Find("Controls/Right").gameObject;
            var before=piece.BoardPosition;
            ExecuteEvents.Execute(right,eventData,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.34f);
            Check(piece.BoardPosition.x>=before.x+2,"Touch horizontal auto-repeat survives keyboard polling");
            ExecuteEvents.Execute(right,eventData,ExecuteEvents.pointerUpHandler);
            Click("Controls/HardDrop"); yield return null;
            Check(score.CurrentScore>0&&gm.CanHold,"Hard drop scores, locks, and unlocks hold");
            visible=0; foreach(var sr in FindFirstObjectByType<ActivePieceRenderer>().GetComponentsInChildren<SpriteRenderer>()) if(sr.enabled) visible++;
            Check(visible>0,"Next active piece stays visible after lock events");
            for(int i=0;i<9;i++) {
                int direction=i%2==0?-1:1;
                for(int step=0;step<(i%3)+1;step++) piece.StartMove(direction);
                piece.StopMove(); piece.Rotate(i%2==0?1:-1); piece.ExecuteHardDrop(); yield return new WaitForSecondsRealtime(.05f);
            }
            Check(gm.CurrentState==GameState.Playing,"A sequence of rotated drops remains playable");
            yield return new WaitForSecondsRealtime(.7f);
            var scoreLabel=GameObject.Find("Canvas").transform.Find("StatsPanel/Score").GetComponent<Text>();
            Check(scoreLabel.text==score.CurrentScore.ToString("N0"),"Rolling score reaches its exact target without getting stuck one point short");
            yield return Capture("02-gameplay");
            int linesBefore=score.TotalLinesCleared; var matrix=gm.GetComponent<DropStackBoard>(); matrix.ClearMatrix();
            var single=new DropStack.Data.PolyominoShape { type=DropStack.Data.PolyominoType.O4,blockCount=1,rotations=new[]{new[]{Vector2Int.zero}} };
            for(int x=0;x<10;x++) if(x<3||x>6) matrix.LockPiece(single,new Vector2Int(x,0),0);
            piece.SpawnPiece(DropStack.Data.PolyominoDatabase.GetShape(DropStack.Data.PolyominoType.I4)); piece.ExecuteHardDrop();
            yield return new WaitForSecondsRealtime(.25f);
            Check(score.TotalLinesCleared==linesBefore+1,"A real hard drop clears a completed row and updates the line count");
            yield return Capture("02b-perfect-clear");
            ui.OpenRestartModal(); yield return null;
            before=piece.BoardPosition; int savedScore=score.CurrentScore; float savedTime=gm.TimeRemaining;
            piece.StartMove(-1); piece.Rotate(1); piece.ExecuteHardDrop(); piece.RequestHold();
            yield return new WaitForSecondsRealtime(.15f);
            Check(gm.CurrentState==GameState.Paused&&piece.BoardPosition==before&&score.CurrentScore==savedScore&&gm.TimeRemaining==savedTime,"Pause blocks every gameplay input and freezes the clock");
            yield return Capture("03-restart");
            Click("RestartModal/RestartCard/ContinueRun"); yield return null;
            Check(gm.CurrentState==GameState.Playing&&score.CurrentScore==savedScore,"Continue preserves the current run");
            ui.OpenSettingsModal(); yield return null;
            Check(gm.CurrentState==GameState.Paused,"Pause button opens settings");
            yield return new WaitForSecondsRealtime(.3f);
            Check(GameObject.Find("Canvas").transform.Find("PauseMenu").GetComponent<CanvasGroup>().alpha>.99f,"Modal animations complete while gameplay time is paused");
            ui.OpenRestartModal(); ui.CloseRestartModal();
            Check(gm.CurrentState==GameState.Paused,"Canceling a restart opened from pause preserves pause");
            yield return Capture("04-paused");
            Click("PauseMenu/PauseCard/Resume"); yield return null;
            ui.OpenRestartModal(); Click("RestartModal/RestartCard/ConfirmRestart"); yield return null;
            Check(gm.CurrentState==GameState.Playing&&score.CurrentScore==0&&gm.TimeRemaining>89&&gm.HoldPiece==DropStack.Data.PolyominoType.None,"Confirmed restart resets board, score, timer, hold and pause");
            Screen.SetResolution(660,1200,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("05-portrait");
            var canvas=GameObject.Find("Canvas").GetComponent<RectTransform>();
            var corners=new Vector3[4]; canvas.Find("PlayfieldFrame").GetComponent<RectTransform>().GetWorldCorners(corners);
            Check(corners[0].x>=0&&corners[2].x<=Screen.width&&corners[0].y>=0&&corners[2].y<=Screen.height,"Portrait playfield fits on screen");
            gm.ReturnToMenu(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("05b-portrait-menu");
            var card=canvas.Find("StartMenu/StartCard").GetComponent<RectTransform>(); card.GetWorldCorners(corners);
            Check(corners[0].x>=0&&corners[2].x<=Screen.width&&corners[0].y>=0&&corners[2].y<=Screen.height,"Portrait difficulty menu and its buttons fit on screen");
            Click("StartMenu/StartCard/Play"); yield return null;
            score.RegisterDropScore(250,true);
            typeof(DropStackGameManager).GetField("<TimeRemaining>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gm,.01f);
            yield return new WaitForSecondsRealtime(.2f);
            Check(gm.CurrentState==GameState.GameOver&&!piece.HasActivePiece,"Timer expiry shows results and removes the active piece");
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("06-results");
            Click("ResultsMenu/ResultsCard/PlayAgain"); yield return null;
            Check(gm.CurrentState==GameState.Playing&&score.CurrentScore==0&&piece.InputEnabled,"Play again starts a fresh playable run");
            gm.ReturnToMenu(); Click("StartMenu/StartCard/Mode_NOOB"); Click("StartMenu/StartCard/Play"); yield return null;
            Check(gm.Difficulty==GameDifficulty.Noob&&piece.CurrentShape.blockCount==4,"Noob starts a real classic-piece run");
            gm.ReturnToMenu(); Click("StartMenu/StartCard/Mode_PRO"); Click("StartMenu/StartCard/Play"); yield return null;
            Check(gm.Difficulty==GameDifficulty.Pro&&gm.GetComponent<SpeedCurveManager>().CurrentDropInterval<.7f,"Pro starts at its faster pace");
            gm.ReturnToMenu(); Click("StartMenu/StartCard/Mode_NEUTRAL");
            Check(!runtimeError,"No runtime errors or exceptions");
        }
    }
}
#endif

