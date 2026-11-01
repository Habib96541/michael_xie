using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PegRush;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PegRushDistribution
{
    private const string Root = "Assets/PegRush/Generated/";
    private const int Samples = 2000;
    private const int Seed = 741923;

    static PegRushDistribution()
    {
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        string label = UnityEditor.SessionState.GetString("PegRush.Distribution", "");
        if (label.Length == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        UnityEditor.SessionState.SetString("PegRush.Distribution", "");
        try
        {
            GameConfig config = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "GameConfig.asset"));
            if (label == "before")
            {
                config.Gravity = 1.6f;
                config.Timeout = 35f;
                config.TopPegCount = 1;
                config.PegDiameter = 0.21f;
                config.BallDiameter = 0.28f;
                config.RowSpacing = 1f;
                config.Restitution = 0.58f;
                config.Friction = 0.05f;
                config.Damping = 0f;
                config.SpawnSpread = 0.16f;
                config.SpawnAbovePeg = 1.1f;
                config.BoundaryPadding = 0.35f;
            }
            if (label.StartsWith("candidate")) JsonUtility.FromJsonOverwrite(File.ReadAllText("SprintDoc/Polish/Distribution/tuning.json"), config);
            Run(label, config);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void Start(string label)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnityEditor.SessionState.SetString("PegRush.Distribution", label);
        EditorApplication.isPlaying = true;
    }

    public static void Before()
    {
        Start("before");
    }

    public static void After()
    {
        Start("after");
    }

    public static void Candidate()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(arguments, "-distributionLabel");
        Start(index >= 0 ? arguments[index + 1] : "candidate-latest");
    }

    [Serializable]
    private sealed class Result
    {
        public int Rows;
        public int Seed;
        public int Accepted;
        public int Completed;
        public int Refunds;
        public int Escaped;
        public int Stuck;
        public int TimedOut;
        public int[] Pockets;
        public double[] Frequencies;
        public double[] StandardErrors;
        public double MeanSeconds;
        public double P95Seconds;
        public double MeanPocket;
        public double LeftRightZ;
        public double CentralShare;
        public double OuterTwoShare;
    }

    private static void Run(string label, GameConfig config)
    {
        string output = "SprintDoc/Polish/Distribution/";
        Directory.CreateDirectory(output);
        var originalMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Physics2D.IgnoreLayerCollision(8, 8);
        try
        {
            for (int rows = 8; rows <= 16; rows++)
            {
                Scene scene = SceneManager.CreateScene("Distribution " + rows, new CreateSceneParameters(LocalPhysicsMode.Physics2D));
                GameObject root = new GameObject("Measured board");
                SceneManager.MoveGameObjectToScene(root, scene);
                Board board = root.AddComponent<Board>();
                board.Wire(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Peg.prefab"), AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Pocket.prefab"), AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Root + "Bounce.physicsMaterial2D"));
                board.Configure(config);
                board.Generate(rows);
                PhysicsScene2D physics = scene.GetPhysicsScene2D();
                DistributionProbe[] balls = new DistributionProbe[50];
                for (int i = 0; i < balls.Length; i++)
                {
                    GameObject ball = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Ball.prefab"), root.transform);
                    ball.GetComponent<Ball>().enabled = false;
                    ball.transform.localScale = Vector3.one * config.BallDiameter;
                    ball.GetComponent<Collider2D>().sharedMaterial = board.Material;
                    balls[i] = ball.AddComponent<DistributionProbe>();
                    ball.SetActive(false);
                }
                Result result = new Result { Rows = rows, Seed = Seed + rows, Pockets = new int[rows + 1] };
                List<float> durations = new List<float>();
                System.Random random = new System.Random(result.Seed);
                using (StreamWriter raw = new StreamWriter(output + label + "-rows" + rows + ".csv"))
                {
                    raw.WriteLine("sample,spawn_x,pocket,outcome,seconds");
                    while (result.Completed < Samples)
                    {
                        int batch = Math.Min(50, Samples - result.Completed);
                        for (int i = 0; i < batch; i++)
                        {
                            int sample = result.Accepted++;
                            float x = (float)(random.NextDouble() * 2 - 1) * config.SpawnSpread;
                            balls[i].Launch(new Vector2(x, config.SpawnY(rows)), config.Gravity, config.Damping, config.Timeout, (pocket, reason, seconds) =>
                            {
                                raw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:R},{2},{3},{4:R}", sample, x, pocket, reason, seconds));
                                if (pocket >= 0)
                                {
                                    result.Completed++;
                                    result.Pockets[pocket]++;
                                    durations.Add(seconds);
                                }
                                else
                                {
                                    result.Refunds++;
                                    if (reason == "escaped") result.Escaped++;
                                    if (reason == "stuck") result.Stuck++;
                                    if (reason == "timeout") result.TimedOut++;
                                }
                            });
                        }
                        Physics2D.SyncTransforms();
                        for (int step = 0; step < 2000 && balls.Any(ball => ball.Running); step++)
                        {
                            physics.Simulate(0.02f);
                            foreach (DistributionProbe ball in balls) ball.Tick(0.02f);
                        }
                        if (result.Accepted > 20000) throw new Exception("Excessive recovery rate");
                    }
                }
                result.Frequencies = result.Pockets.Select(count => count / (double)result.Completed).ToArray();
                result.StandardErrors = result.Frequencies.Select(p => Math.Sqrt(p * (1 - p) / result.Completed)).ToArray();
                durations.Sort();
                result.MeanSeconds = durations.Average();
                result.P95Seconds = durations[(int)(durations.Count * 0.95f)];
                result.MeanPocket = result.Pockets.Select((count, index) => count * (double)index).Sum() / result.Completed;
                int left = result.Pockets.Where((count, index) => index < rows / 2f).Sum();
                int right = result.Pockets.Where((count, index) => index > rows / 2f).Sum();
                result.LeftRightZ = (left - right) / Math.Sqrt(Math.Max(1, left + right));
                result.CentralShare = result.Pockets.Where((count, index) => Math.Abs(index - rows / 2f) <= rows / 4f).Sum() / (double)result.Completed;
                result.OuterTwoShare = (result.Pockets[0] + result.Pockets[1] + result.Pockets[rows - 1] + result.Pockets[rows]) / (double)result.Completed;
                File.WriteAllText(output + label + "-rows" + rows + ".json", JsonUtility.ToJson(result, true));
                Debug.Log("DISTRIBUTION " + label + " R=" + rows + " center=" + result.CentralShare + " outer=" + result.OuterTwoShare + " refunds=" + result.Refunds);
                UnityEngine.Object.DestroyImmediate(root);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
        finally
        {
            Physics2D.simulationMode = originalMode;
        }
    }
}
