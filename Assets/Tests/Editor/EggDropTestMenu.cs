using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EggDrop.Tests
{
    public static class EggDropTestMenu
    {
        private static TestRunnerApi runner;

        [InitializeOnLoadMethod]
        private static void RegisterReporter()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Reporter());
        }

        [MenuItem("Egg Drop/Checks/Run Rule Tests")]
        public static void RunRules() => Run(TestMode.EditMode, "EggDrop.Tests", "Rules");

        [MenuItem("Egg Drop/Checks/Run Scene Test")]
        public static void RunScene() => Run(TestMode.PlayMode, "EggDrop.PlayModeTests", "Scene");

        private static void Run(TestMode mode, string assembly, string kind)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("EggDrop.TestsRunning", true);
            SessionState.SetString("EggDrop.TestKind", kind);
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[] { assembly } }));
        }

        private sealed class Reporter : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool("EggDrop.TestsRunning", false)) return;
                SessionState.SetBool("EggDrop.TestsRunning", false);
                Directory.CreateDirectory("Logs");
                string path = "Logs/EggDrop-" + SessionState.GetString("EggDrop.TestKind", "Tests") + "Results";
                TestRunnerApi.SaveResultToFile(result, path + ".xml");
                string summary = "Passed: " + result.PassCount + ", failed: " + result.FailCount + ", skipped: " + result.SkipCount;
                File.WriteAllText(path + ".txt", summary);
                Debug.Log("Egg Drop sprint checks. " + summary);
            }
        }
    }
}
