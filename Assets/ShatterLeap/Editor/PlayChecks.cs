using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShatterLeap.Editor
{
    [InitializeOnLoad]
    public static class PlayChecks
    {
        private const string Pending = "ShatterLeap.IntegrationPending";
        private static readonly List<string> Results = new List<string>();
        private static int _stage;
        private static double _deadline;
        private static double _pauseUntil;
        private static float _pausedSeconds;
        private static int _wallet;
        private static float _originalDuration;
        private static RunController _controller;
        private static GameConfig _config;

        static PlayChecks()
        {
            EditorApplication.update += Advance;
        }
        public static void Begin()
        {
            EditorSceneManager.OpenScene(SprintWorkflow.ScenePath);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        private static void Check(bool condition, string label)
        {
            if (!condition) { throw new Exception(label); }
            Results.Add("PASS " + label);
        }
        private static void Advance()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) { return; }
            try
            {
                if (_controller == null || _deadline == 0)
                {
                    _controller = UnityEngine.Object.FindFirstObjectByType<RunController>();
                    if (!_controller || _controller.Saves == null) { return; }
                    _config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ShatterLeap/Config/Sprint1.asset");
                    _deadline = EditorApplication.timeSinceStartup + 100;
                    Results.Clear();
                }
                if (EditorApplication.timeSinceStartup > _deadline) { throw new Exception("Integration deadline exceeded, stage " + _stage); }
                RunRules run = _controller.Run;
                switch (_stage)
                {
                    case 0:
                        _controller.Gesture();
                        _controller.SelectDifficulty(0);
                        _wallet = _controller.Saves.Data.Coins;
                        _controller.BeginRun();
                        _stage++;
                        break;
                    case 1:
                        if (_controller.Paused) { _controller.Resume(); }
                        if (run.State == RunState.Playing && !run.AtFinish)
                        {
                            _controller.Choose(run.SafeSide(run.Steps));
                            _controller.Choose(1 - run.ChosenSide);
                            Check(run.State == RunState.Jumping, "Real controller rejects rapid duplicate choice at step " + (run.Steps + 1));
                        }
                        if (run.State == RunState.Results)
                        {
                            Check(run.Outcome == RunState.Won && run.Steps == 6, $"Real 3D supports validate all six safe landings and finish (outcome={run.Outcome}, steps={run.Steps}, remaining={run.Remaining:0.00})");
                            Check(_controller.Saves.Data.Coins == _wallet + 260, "Actual controller banks exactly 260 on Easy win");
                            _wallet = _controller.Saves.Data.Coins;
                            _controller.BeginRun();
                            _stage++;
                        }
                        break;
                    case 2:
                        if (_controller.Paused) { _controller.Resume(); }
                        if (run.State == RunState.Playing)
                        {
                            GlassPanel panel = UnityEngine.Object.FindObjectsByType<GlassPanel>(FindObjectsSortMode.None)
                                [0];
                            Check(panel.Support != null && panel.Support.enabled, "Real panel prefab has active 3D support");
                            _controller.Choose(1 - run.SafeSide(0));
                            _stage++;
                        }
                        break;
                    case 3:
                        if (run.State == RunState.Results)
                        {
                            Check(run.Outcome == RunState.Falling && run.Payout == 0 && _controller.Saves.Data.Coins == _wallet, "Fragile contact triggers falling with zero payout");
                            Check(UnityEngine.Object.FindFirstObjectByType<ContestantMotor>().GetComponent<Rigidbody>().useGravity, "Falling uses 3D gravity");
                            _controller.BeginRun();
                            _stage++;
                        }
                        break;
                    case 4:
                        if (_controller.Paused) { _controller.Resume(); }
                        if (run.State == RunState.Playing)
                        {
                            _controller.Choose(run.SafeSide(0));
                            _controller.Pause();
                            _pausedSeconds = run.Remaining;
                            _pauseUntil = EditorApplication.timeSinceStartup + 0.6;
                            _stage++;
                        }
                        break;
                    case 5:
                        if (EditorApplication.timeSinceStartup < _pauseUntil) { return; }
                        Check(Mathf.Approximately(run.Remaining, _pausedSeconds) && run.State == RunState.Jumping, "Pause freezes timer and movement during jump");
                        _controller.Resume();
                        _stage++;
                        break;
                    case 6:
                        if (run.Steps == 1 && run.State == RunState.Playing)
                        {
                            _controller.ShowHome();
                            Check(_controller.Saves.Data.Coins == _wallet, "Abandoning after a real safe landing banks nothing");
                            _originalDuration = _config.Difficulties[0].PlaySeconds;
                            _config.Difficulties[0].PlaySeconds = 0.1f;
                            _controller.BeginRun();
                            _config.Difficulties[0].PlaySeconds = _originalDuration;
                            _stage++;
                        }
                        break;
                    case 7:
                        if (_controller.Paused) { _controller.Resume(); }
                        if (run.State == RunState.Playing)
                        {
                            _controller.Choose(run.SafeSide(0));
                            _stage++;
                        }
                        break;
                    case 8:
                        if (run.State == RunState.Results)
                        {
                            Check(run.Outcome == RunState.TimedOut && run.Steps == 0 && _controller.Saves.Data.Coins == _wallet, "Actual timer expiration during jump prevents reward");
                            _controller.BeginRun();
                            _controller.BrowserVisibility("1");
                            Check(_controller.Paused, "Browser visibility hook pauses preview");
                            _controller.BrowserVisibility("0");
                            Check(_controller.Paused, "Returning does not automatically resume");
                            _controller.Resume();
                            Check(!_controller.Paused, "Explicit resume works");
                            // Do not leave test winnings or test settings in the user's Editor save.
                            _controller.Saves.Data.Coins -= 260;
                            _controller.Saves.Save();
                            File.WriteAllLines("SprintDoc/PlayChecks.txt", Results);
                            SessionState.SetBool(Pending, false);
                            _controller.ShowHome();
                            EditorApplication.isPlaying = false;
                            _stage = 0;
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                if (_config && _originalDuration > 0) { _config.Difficulties[0].PlaySeconds = _originalDuration; }
                Results.Add("FAIL " + exception);
                File.WriteAllLines("SprintDoc/PlayChecks.txt", Results);
                SessionState.SetBool(Pending, false);
                EditorApplication.isPlaying = false;
                Debug.LogException(exception);
            }
        }
    }
}
