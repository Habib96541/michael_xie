#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using BullRide.Core;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BullRide.EditorTools
{
    public static class BuildWebGL
    {
        const string OutputPath = "Build/WebGL";

        [MenuItem("Bull Ride/Build WebGL")]
        public static void Build()
        {
            ProjectSetup.EnsureScene();

            PlayerSettings.companyName = "Bull Ride Studio";
            PlayerSettings.productName = "Bull Ride";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectSetup.ScenePath, true) };

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("WebGL build failed: " + report.summary.result
                                    + " (" + report.summary.totalErrors + " errors)");

            PatchWebPage(OutputPath);
            CopyDeliverable(OutputPath);
            Debug.Log("Bull Ride WebGL build complete: " + report.summary.outputPath
                      + " (" + report.summary.totalSize + " bytes)");
        }

        [MenuItem("Bull Ride/Run Logic Checks")]
        public static void RunLogicChecks()
        {
            Check(RideRules.BaseMultiplierAt(0) == .1 && RideRules.BaseMultiplierAt(3) == .75 &&
                RideRules.BaseMultiplierAt(8) == 2.5, "Baseline multiplier endpoints");
            Check(RideRules.PayoutFor(10m, 3.333) == 33.33m, "Payout rounds to cents");

            var sim = new RideSim(17);
            for (int i = 0; i < 240; i++) sim.Step(Vector2.one);
            Check(sim.Phase == RidePhase.Menu && sim.RideTime == 0 && sim.Gate == 0 &&
                sim.Position == RideSim.StartPosition, "Menu only fidgets");
            sim.Play();
            for (int i = 0; i < 137; i++) sim.Step(Vector2.zero);
            Check(sim.Phase == RidePhase.Chute && sim.Gate == 0 && sim.Multiplier == 1, "Closed chute and full payout");
            for (int i = 0; i < 150 && sim.Phase == RidePhase.Chute; i++) sim.Step(Vector2.zero);
            Check(sim.Phase == RidePhase.Ride && sim.Position.y + RideSim.ShoulderOffset >= RideSim.GateZ &&
                sim.RideTime == 0, "Shoulder starts the clock");
            for (int i = 0; i < 900 && sim.Phase == RidePhase.Ride; i++) sim.Step(Vector2.zero);
            Check(sim.Phase == RidePhase.Fall && sim.RideTime > 0 && sim.RideTime < RideRules.Duration,
                "Unattended ride falls at " + RideRules.Time(sim.RideTime));

            sim.Reset();
            sim.Play();
            for (int i = 0; i < 3900 && sim.CanBalance; i++)
            {
                Vector2 correction = sim.Lean + .19f * sim.LeanVelocity;
                Vector2 stick = new Vector2(DigitalAxis(correction.x), DigitalAxis(correction.y));
                sim.Step(stick);
            }
            Check(sim.Phase == RidePhase.Done && sim.Whistle, "Digital balance input reaches 8 seconds");
            for (int i = 0; i < 143; i++) sim.Step(Vector2.zero);
            Check(sim.Phase == RidePhase.Done, "Final multiplier holds before showing results");
            sim.Step(Vector2.zero);
            Check(sim.Phase == RidePhase.Over, "Whistle reaches results");
            CheckBalanceBonus();
            CheckArenaCollisions();
            CheckRig();
            CheckRagdoll();
            Debug.Log("Bull Ride logic checks passed.");
        }

        static float DigitalAxis(float value) => value > .15f ? -1 : value < -.15f ? 1 : 0;

        static void CheckRig()
        {
            var stage = new GameObject("Rig check");
            var baked = new Mesh();
            try
            {
                var arena = stage.AddComponent<ArenaView>();
                arena.Build();
                arena.SetGate(1);
                var model = new GameObject("Bull");
                model.transform.SetParent(stage.transform);
                var view = model.AddComponent<BullView>();
                view.Build();
                var renderer = model.GetComponent<SkinnedMeshRenderer>();
                var sim = new RideSim(17);
                view.Apply(sim, Vector2.zero);
                renderer.BakeMesh(baked);
                float[] ground = HoofSoles(renderer, baked);
                Check(Array.TrueForAll(ground, value => Mathf.Abs(value) < .01f), "Four standing hooves touch the dirt");
                float[] lengths = BoneLengths(renderer.bones);
                sim.Play();
                while (sim.RideTime < .68 / 1.46) sim.Step(Vector2.zero);
                view.Apply(sim, Vector2.zero);
                renderer.BakeMesh(baked);
                float[] air = HoofSoles(renderer, baked);
                Check(Array.TrueForAll(air, value => value > .15f), "Four hooves clear the dirt during a buck");
                float[] moved = BoneLengths(renderer.bones);
                for (int i = 0; i < lengths.Length; i++)
                    if (Mathf.Abs(lengths[i] - moved[i]) > .0001f) throw new Exception("Bull leg segment changed length");
                Debug.Log("PASS: Buck rotations preserve all leg segment lengths");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(stage);
            }
        }

        static void CheckBalanceBonus()
        {
            bool sawHighBuck = false, sawRegularRide = false;
            for (int seed = 1; seed <= 12; seed++)
            {
                var sim = new RideSim(seed);
                sim.Play();
                int slowTicks = 0;
                float highBuckLift = 0;
                while (sim.CanBalance)
                {
                    Vector2 stick = (-sim.Pose.Force * 4.35f - sim.Lean * 10 - sim.LeanVelocity * 2) / 6.15f;
                    if (sim.PlaybackRate < 1)
                    {
                        slowTicks++;
                        highBuckLift = Mathf.Max(highBuckLift, sim.Pose.Hop);
                    }
                    sim.Step(stick);
                }

                Check(sim.Whistle && sim.Multiplier > 4 && sim.Multiplier <= RideRules.MaxMultiplier,
                    "Sustained center balance earns " + sim.Multiplier.ToString("F2") + "x");
                if (slowTicks > 0)
                {
                    sawHighBuck = true;
                    Check(slowTicks == 90 && highBuckLift > .65f && sim.Multiplier >= 4.9,
                        "High buck lasts 1.5 real seconds, raises lift, and rewards center balance");
                }
                else sawRegularRide = true;
                if (sawHighBuck && sawRegularRide) break;
            }
            Check(sawHighBuck && sawRegularRide, "High buck is occasional");

            var offCenter = new RideSim(17);
            offCenter.Play();
            while (offCenter.CanBalance)
            {
                Vector2 error = offCenter.Lean - new Vector2(.45f, 0);
                Vector2 stick = (-offCenter.Pose.Force * 4.35f - error * 10 - offCenter.LeanVelocity * 2) / 6.15f;
                offCenter.Step(stick);
            }
            Check(offCenter.Whistle && Math.Abs(offCenter.Multiplier - 2.5) < .03,
                "Staying aboard outside the center earns baseline growth");
        }

        static void CheckArenaCollisions()
        {
            var stage = new GameObject("Arena collision check");
            var mode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                var arena = stage.AddComponent<ArenaView>();
                arena.Build();
                Vector3 approach = new Vector3(RideSim.StartPosition.x, 1.02f, -21.2f);
                Physics.SyncTransforms();
                Check(Physics.SphereCast(approach, .13f, Vector3.forward, out RaycastHit closed, 2) &&
                    closed.collider.gameObject.name == "Gate", "Shifted gate blocks the new starting lane");
                arena.SetGate(1);
                Physics.SyncTransforms();
                Check(!Physics.SphereCast(approach, .13f, Vector3.forward, out _, 2),
                    "Opened gate clears the new starting lane");
                Check(Physics.SphereCast(new Vector3(-11, 1.16f, -3), .13f, Vector3.left, out _, 4),
                    "Arena rails intercept the thrown rider");

                var probe = new GameObject("Thrown body collision probe");
                probe.transform.SetParent(stage.transform);
                probe.transform.position = new Vector3(-11.7f, 1.12f, -3);
                var capsule = probe.AddComponent<CapsuleCollider>();
                capsule.radius = .13f;
                capsule.height = .5f;
                var body = probe.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.linearVelocity = Vector3.left * 14;
                for (int i = 0; i < 15; i++) Physics.Simulate(.02f);
                Check(body.position.x > -13.2f && body.linearVelocity.x > -1,
                    "A fast thrown body stops at the rail");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage);
                Physics.simulationMode = mode;
            }
        }

        static void CheckRagdoll()
        {
            var mode = Physics.simulationMode;
            var stage = new GameObject("Ragdoll check");
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                stage.AddComponent<ArenaView>().Build();
                var model = new GameObject("Bull");
                model.transform.SetParent(stage.transform);
                var view = model.AddComponent<BullView>();
                view.Build();
                var sim = new RideSim(17);
                view.Apply(sim, Vector2.zero);
                Transform rider = view.Rider, seat = rider.parent;
                Transform[] limbs = rider.GetComponentsInChildren<Transform>();
                var parents = Array.ConvertAll(limbs, limb => limb.parent);
                var offsets = Array.ConvertAll(limbs, limb => limb.localPosition);
                var saddlePose = Array.ConvertAll(limbs, limb => limb.localRotation);

                void CheckAttachments(bool seated)
                {
                    if (seated && (rider.parent != seat || rider.localPosition.sqrMagnitude > .000001f))
                        throw new Exception("Rider left the saddle before being thrown");
                    for (int i = 1; i < limbs.Length; i++)
                    {
                        if (limbs[i].parent != parents[i])
                            throw new Exception("Rider joint was detached: " + limbs[i].name);
                        if (Vector3.Distance(limbs[i].localPosition, offsets[i]) > .0001f)
                            throw new Exception("Rider joint stretched: " + limbs[i].name);
                        if (seated && (limbs[i].name == "hips" || limbs[i].name.StartsWith("leg")) &&
                            Quaternion.Angle(limbs[i].localRotation, saddlePose[i]) > .15f)
                            throw new Exception("Lower body slipped in the saddle: " + limbs[i].name);
                    }
                }

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    sim.Play();
                    int count = 0;
                    while (sim.CanBalance && count++ < 1200)
                    {
                        // Repeat at uneven frame intervals to cover slower browser frames.
                        int ticks = count % 5 == 0 ? 8 : 2;
                        for (int i = 0; i < ticks && sim.CanBalance; i++) sim.Step(Vector2.zero);
                        view.Apply(sim, Vector2.zero, ticks * RideSim.StepSeconds);
                        CheckAttachments(true);
                    }
                    Check(sim.Phase == RidePhase.Fall, "Uncontrolled test ride throws the rider");
                    Vector3 launch = rider.position, flank = model.transform.right;
                    flank.y = 0;
                    flank.Normalize();
                    view.ThrowRider(sim);
                    var elbow = Array.Find(limbs, limb => limb.name == "armL_lower");
                    Quaternion launchElbow = elbow.localRotation;
                    float elbowMotion = 0, anchorError = 0, lowest = launch.y;
                    var constraints = stage.GetComponentsInChildren<ConfigurableJoint>();
                    for (int i = 0; i < 120; i++)
                    {
                        Physics.Simulate(1f / 60);
                        if (sim.Phase != RidePhase.Over) sim.Step(Vector2.zero);
                        view.Apply(sim, Vector2.zero);
                        CheckAttachments(false);
                        if (i == 11)
                        {
                            float clearance = Mathf.Abs(Vector3.Dot(rider.position - seat.position, flank));
                            Check(clearance > 1.15f, "Throw clears the bull in 0.2 seconds (" +
                                clearance.ToString("F2") + " m sideways)");
                        }
                        elbowMotion = Mathf.Max(elbowMotion, Quaternion.Angle(launchElbow, elbow.localRotation));
                        lowest = Mathf.Min(lowest, rider.position.y);
                        foreach (var joint in constraints)
                        {
                            var anchor = joint.transform.TransformPoint(joint.anchor);
                            var attached = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
                            anchorError = Mathf.Max(anchorError, Vector3.Distance(anchor, attached));
                        }
                    }
                    Check(constraints.Length == 11 && anchorError < .14f &&
                        Vector3.Distance(launch, rider.position) > .5f && lowest > -.2f && lowest < .5f &&
                        elbowMotion > 8, "Connected ragdoll articulates and lands (" + anchorError.ToString("F3") +
                        " m max anchor error, " + lowest.ToString("F2") + " m low point, " +
                        elbowMotion.ToString("F0") + " deg elbow travel)");
                    view.ResetPose();
                    sim.Reset();
                    view.Apply(sim, Vector2.zero);
                    CheckAttachments(true);
                    Check(stage.GetComponentsInChildren<Rigidbody>().Length == 0, "Replay clears the old ragdoll");
                }

                sim.Play();
                int frame = 0;
                while (sim.CanBalance && frame++ < 900)
                {
                    int ticks = frame % 5 == 0 ? 7 : 2;
                    Vector2 stick = Vector2.zero;
                    for (int i = 0; i < ticks && sim.CanBalance; i++)
                    {
                        Vector2 correction = sim.Lean + .19f * sim.LeanVelocity;
                        stick = new Vector2(DigitalAxis(correction.x), DigitalAxis(correction.y));
                        sim.Step(stick);
                    }
                    view.Apply(sim, stick, ticks * RideSim.StepSeconds);
                    CheckAttachments(true);
                }
                Check(sim.Whistle, "Rider stays attached through a full eight-second ride");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage);
                Physics.simulationMode = mode;
            }
        }

        static float[] HoofSoles(SkinnedMeshRenderer renderer, Mesh baked)
        {
            Vector3[] bind = renderer.sharedMesh.vertices, pose = baked.vertices;
            float[] minimum = { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
            for (int i = 0; i < bind.Length; i++)
            {
                if (bind[i].y > .08f) continue;
                int hoof = (bind[i].z > 0 ? 0 : 2) + (bind[i].x > 0 ? 1 : 0);
                minimum[hoof] = Mathf.Min(minimum[hoof], renderer.transform.TransformPoint(pose[i]).y);
            }
            return minimum;
        }

        static float[] BoneLengths(Transform[] bones)
        {
            int[] joints = { 10, 11, 13, 14, 16, 17, 18, 20, 21, 22, 34, 35, 36, 37, 38, 39, 40, 41 };
            return Array.ConvertAll(joints, i => Vector3.Distance(bones[i].position, bones[i].parent.position));
        }

        static void Check(bool pass, string name)
        {
            if (!pass) throw new Exception("Check failed: " + name);
            Debug.Log("PASS: " + name);
        }

        static void PatchWebPage(string outputPath)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath)) return;

            string html = File.ReadAllText(indexPath);
            html = html.Replace("<title>Unity Web Player | Bull Ride</title>",
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\"><title>Bull Ride</title>");
            // Unity's stock mobile branch adds a second viewport tag.
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"var meta = document\.createElement\('meta'\);[\s\S]*?appendChild\(meta\);", "");
            html = html.Replace("/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)",
                "/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || " +
                "(navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)");
            html = html.Replace("// config.devicePixelRatio = 1;",
                "config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);");
            File.WriteAllText(indexPath, html);

            string cssPath = Path.Combine(outputPath, "TemplateData", "style.css");
            if (File.Exists(cssPath))
            {
                File.AppendAllText(cssPath, @"
                    
                    html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: #0d121f; }
                    #unity-container, #unity-container.unity-desktop, #unity-container.unity-mobile {
                        position: fixed; inset: 0; width: 100%; height: 100%; height: 100dvh; transform: none;
                    }
                    #unity-fullscreen-container, .unity-mobile #unity-fullscreen-container {
                        position: absolute;
                        inset: env(safe-area-inset-top, 0px) env(safe-area-inset-right, 0px)
                               env(safe-area-inset-bottom, 0px) env(safe-area-inset-left, 0px);
                        width: auto !important; height: auto !important;
                    }
                    #unity-canvas, #unity-canvas.unity-mobile {
                        position: absolute; inset: 0; width: 100%; height: 100%; display: block;
                        background: #0d121f; outline: none; touch-action: none;
                    }
                    #unity-footer { display: none; }
                    #unity-warning { z-index: 20; }
                    ");
            }
        }

        static void CopyDeliverable(string outputPath)
        {
            string dest = Path.Combine("Deliverables", "BullRide-WebGL");
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            CopyDirectory(outputPath, dest);
            Debug.Log("Copied WebGL build to " + dest);
        }

        static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }
}
#endif
