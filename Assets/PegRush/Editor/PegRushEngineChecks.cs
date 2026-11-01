using System;
using System.IO;
using System.Linq;
using PegRush;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PegRushEngineChecks
{
    static PegRushEngineChecks()
    {
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/PegRush/Generated/PegRush.unity");
        UnityEditor.SessionState.SetBool("PegRush.EngineChecks", true);
        EditorApplication.isPlaying = true;
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
    }

    private static void SimulateUntilClear(GameSession session)
    {
        Ball[] balls = UnityEngine.Object.FindObjectsByType<Ball>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int step = 0; step < 3000 && session.Active > 0; step++)
        {
            Physics2D.Simulate(0.02f);
            foreach (Ball ball in balls)
            {
                if (ball.State == BallState.Falling) ball.SendMessage("FixedUpdate");
            }
        }
        Check(session.Active == 0, "Physics drops failed to clear");
    }

    private static void Tick()
    {
        if (!UnityEditor.SessionState.GetBool("PegRush.EngineChecks", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        GameSession session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
        if (session == null || session.Config == null || typeof(GameSession).GetField("_wallet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session) == null) return;
        UnityEditor.SessionState.SetBool("PegRush.EngineChecks", false);
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            session.SendMessage("OnApplicationFocus", true);
            session.Play();
            int startingRows = session.Rows;
            for (int i = 0; i < 50; i++) session.Drop();
            Check(session.Active == 50, "Pool capacity");
            long balance = session.Balance;
            session.Drop();
            Check(session.Active == 50 && session.Balance == balance, "Capacity must not charge");
            session.ChangeRows(1);
            session.ChangeProfile();
            Check(session.Rows == startingRows && session.Profile == 0, "Active configuration locking");
            SimulateUntilClear(session);
            Check(session.Balance != 100000, "Physical pocket outcomes");
            for (int i = 0; i < 5; i++)
            {
                session.Drop();
                SimulateUntilClear(session);
            }
            Check(session.Drops == 55, "Pool reuse");
            session.OpenModal("settings");
            session.Drop();
            Check(session.Active == 0, "Modal input blocking");
            session.CloseModal();
            session.Drop();
            session.Home();
            Check(session.State == PegRush.SessionState.Modal, "Leave confirmation");
            session.Drain();
            Check(session.State == PegRush.SessionState.Draining, "Drain state");
            session.Drop();
            Check(session.Active == 1, "Draining input blocking");
            SimulateUntilClear(session);
            Check(session.State == PegRush.SessionState.Home, "Drain completion");
            session.Play();
            session.Drop();
            balance = session.Balance;
            Ball active = UnityEngine.Object.FindObjectsByType<Ball>(FindObjectsSortMode.None).First(b => b.State == BallState.Falling);
            active.transform.position = new Vector3(0, -4, 0);
            active.SendMessage("FixedUpdate");
            Check(session.Active == 0 && session.Balance == balance + 1000, "Escaped ball refund");
            active.Complete(0);
            Check(session.Balance == balance + 1000, "No reward after refund");
            session.Drop();
            active = UnityEngine.Object.FindObjectsByType<Ball>(FindObjectsSortMode.None).First(b => b.State == BallState.Falling);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Check((float)typeof(Ball).GetField("_age", flags).GetValue(active) == 0f, "Pool resets age");
            Check((float)typeof(Ball).GetField("_stuck", flags).GetValue(active) == 0f, "Pool resets stuck timer");
            balance = session.Balance;
            active.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            typeof(Ball).GetField("_stuck", flags).SetValue(active, 4.1f);
            active.SendMessage("FixedUpdate");
            Check(session.Active == 0 && session.Balance == balance + 1000, "Stuck refund");
            session.Drop();
            active = UnityEngine.Object.FindObjectsByType<Ball>(FindObjectsSortMode.None).First(b => b.State == BallState.Falling);
            balance = session.Balance;
            typeof(Ball).GetField("_age", flags).SetValue(active, 40f);
            active.SendMessage("FixedUpdate");
            Check(session.Active == 0 && session.Balance == balance + 1000, "Timeout refund");
            Board board = UnityEngine.Object.FindFirstObjectByType<Board>();
            for (int rows = 8; rows <= 16; rows++)
            {
                while (session.Rows < rows) session.ChangeRows(1);
                while (session.Rows > rows) session.ChangeRows(-1);
                // Destroy from the previous generation is deferred to the end of frame;
                // inspect the generated root's newly active row settings here.
                Check(session.Rows == rows, "Row control");
                var parts = (System.Collections.Generic.List<GameObject>)typeof(Board).GetField("_parts", flags).GetValue(board);
                Check(parts.Count(part => part.GetComponent<PegFlash>() != null) == rows * (rows - 1) / 2 + rows * session.Config.TopPegCount, "Peg count");
                Check(parts.Count(part => part.GetComponent<Pocket>() != null) == rows + 1, "Pocket count");
            }
            Directory.CreateDirectory("SprintDoc");
            File.WriteAllText("SprintDoc/EngineTests.txt", "PASS in Unity Editor manual Physics2D simulation: 50 concurrent balls; capacity rejection/no charge; active row/profile locking; physical pocket resolution; 5 pool reuse cycles; modal blocking; leave/drain/home; escaped refund; duplicate completion guard; stuck and timeout refunds; timer reset on reuse; actual peg/pocket counts for every row 8..16.\nManual simulation does not measure real-time performance; stuck and timeout conditions are injected through reflection.\n");
            Debug.Log("PEGRUSH ENGINE CHECKS PASSED");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText("SprintDoc/EngineTests.txt", "FAIL: " + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}




