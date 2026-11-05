using UnityEngine;
using UnityEngine.InputSystem;

namespace TeslaRoadster
{
    public sealed class RoadsterGame : MonoBehaviour
    {
        internal const int CoinValue = 10;
        const float WarningTime = 1.55f;
        const float ClimbDuration = 2.35f;
        const float CountdownDuration = 3f;
        const float LaunchDuration = 2.1f;
        const float SteeringDuration = 0.42f;
        const float Gravity = 15f;
        const float MaxAltitude = 3.4f;
        const float ComboWindow = 4f;
        const float NitroCapacity = 4f;
        const float NitroCharge = 2f;
        const float NitroSpeed = 18f;
        const string BankKey = "Roadster.CreditBank";
        const string BestKey = "Roadster.BestScore";
        static readonly int Grayscale = Shader.PropertyToID("_RoadsterGrayscale");

        [SerializeField] Camera chaseCamera;
        [SerializeField] Shader surfaceShader;
        [SerializeField] Shader skyShader;
        [SerializeField] Shader particleShader;
        [SerializeField] RoadsterVehicle playerVehicle;
        [SerializeField] GameObject trafficFleet;
        [SerializeField] GameObject marineDrive;
        [SerializeField, Min(2f)] float earliestImpact = 14f;
        [SerializeField, Min(2f)] float latestImpact = 32f;

        enum RunState { Climbing, Staged, Countdown, Launch, Driving, Strike, Results }
        enum Trick { None, FrontFlip, BarrelRoll }

        RoadsterWorld world;
        RoadsterHud hud;
        AudioSource speaker;
        AudioClip pickupSound, warningSound, impactSound, thrusterSound;
        RunState state;
        int lane, coins, bank, best, sideJet, stuntScore, combo, peakCombo, pendingCount, pendingScore, trickDirection;
        float elapsed, impactAt, aftermath, distance, carX, lateralVelocity;
        float rampElapsed, carHeight, verticalVelocity, sideBurst, liftBurst, landingPulse;
        float comboExpires, trickElapsed, noticeUntil, recoveryPitch, recoveryRoll;
        float turnInput, turnTime, steeringYaw, yawVelocity, roadSpeed;
        float nitroFuel;
        bool warned, slamming, usedLift, missileCrash, newBest, nitroActive;
        Trick trick, lastTrick;
        string notice;

        int Score => coins * CoinValue + stuntScore;
        float TrickDuration => trick == Trick.FrontFlip ? 0.68f : 0.54f;
        float TrickAngle => Mathf.SmoothStep(0f, 360f, trickElapsed / TrickDuration);

        void Awake()
        {
            if (Application.isMobilePlatform)
                Application.targetFrameRate = 60;

            bank = PlayerPrefs.GetInt(BankKey, 0);
            best = PlayerPrefs.GetInt(BestKey, PlayerPrefs.GetInt("Roadster.BestRun", 0));
            world = new RoadsterWorld(transform, surfaceShader, skyShader, particleShader, playerVehicle, trafficFleet, marineDrive);
            hud = new RoadsterHud(transform, BeginRun, Steer, Lift, Slam, ToggleNitro);

            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            pickupSound = MakeSound("Coin", 0.18f);
            warningSound = MakeSound("Missile warning", 0.66f);
            impactSound = MakeSound("Impact", 0.72f);
            thrusterSound = MakeSound("Cold-gas thruster", 0.46f);

            chaseCamera.fieldOfView = 47f;
            chaseCamera.nearClipPlane = 0.2f;
            chaseCamera.farClipPlane = 460f;
            chaseCamera.clearFlags = CameraClearFlags.Skybox;
            chaseCamera.useOcclusionCulling = false;
            PrepareRamp();
            PlaceCamera();
        }

        void BeginRun()
        {
            if (state == RunState.Results)
                PrepareRamp();
            else if (state == RunState.Staged)
            {
                rampElapsed = 0f;
                state = RunState.Countdown;
                hud.ShowCountdown(3);
            }
        }

        void PrepareRamp()
        {
            coins = 0;
            lane = 0;
            carX = 0f;
            lateralVelocity = 0f;
            turnInput = turnTime = steeringYaw = yawVelocity = roadSpeed = 0f;
            nitroFuel = 0f;
            nitroActive = false;
            elapsed = 0f;
            aftermath = 0f;
            distance = 0f;
            rampElapsed = 0f;
            carHeight = verticalVelocity = sideBurst = liftBurst = landingPulse = 0f;
            sideJet = 0;
            stuntScore = combo = peakCombo = pendingCount = pendingScore = 0;
            comboExpires = noticeUntil = trickElapsed = recoveryPitch = recoveryRoll = 0f;
            trick = lastTrick = Trick.None;
            notice = "";
            warned = slamming = usedLift = missileCrash = newBest = false;
            ScheduleMissile();
            Shader.SetGlobalFloat(Grayscale, 0f);
            world.Reset();
            world.PoseRampClimb(0f);
            state = RunState.Climbing;
            hud.ShowClimb();
        }

        void OnDestroy() => Shader.SetGlobalFloat(Grayscale, 0f);

        [UnityEngine.Scripting.Preserve]
        public void PressWebControl(string coordinates)
        {
            int separator = coordinates.IndexOf(',');
            if (separator < 0)
                return;
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            const System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Float;
            if (float.TryParse(coordinates[..separator], style, culture, out float x)
                && float.TryParse(coordinates[(separator + 1)..], style, culture, out float y))
                hud.PressAt(new Vector2(x, y));
        }

        void ScheduleMissile()
        {
            impactAt = elapsed + Random.Range(earliestImpact, Mathf.Max(earliestImpact, latestImpact));
        }

        void Steer(int direction)
        {
            if (state != RunState.Driving)
                return;
            int nextLane = Mathf.Clamp(lane + direction, -1, 1);
            turnInput = direction * (nextLane == lane ? 0.35f : 1f);
            turnTime = SteeringDuration;
            if (nextLane == lane)
                return;
            lane = nextLane;
            sideJet = direction;
            sideBurst = 0.46f;
            if (!slamming)
            {
                if (carHeight <= 0f)
                    verticalVelocity = Mathf.Max(verticalVelocity, 5.8f);
                StartTrick(Trick.BarrelRoll, -direction);
            }
            speaker.PlayOneShot(thrusterSound, 0.26f);
        }

        void Lift()
        {
            if (state != RunState.Driving || slamming || usedLift)
                return;
            usedLift = true;
            liftBurst = 0.43f;
            verticalVelocity = Mathf.Max(verticalVelocity, 9.8f);
            StartTrick(Trick.FrontFlip, 1);
            speaker.PlayOneShot(thrusterSound, 0.34f);
        }

        void ToggleNitro()
        {
            if (state != RunState.Driving || nitroFuel <= 0f)
                return;
            nitroActive = !nitroActive;
            if (nitroActive)
                speaker.PlayOneShot(thrusterSound, 0.52f);
        }

        void Slam()
        {
            if (state != RunState.Driving || slamming || carHeight <= 0.025f)
                return;
            slamming = true;
            liftBurst = 0f;
            verticalVelocity = -14f;
            recoveryPitch = trick == Trick.FrontFlip ? Mathf.DeltaAngle(0f, TrickAngle) : 0f;
            recoveryRoll = trick == Trick.BarrelRoll ? Mathf.DeltaAngle(0f, TrickAngle * trickDirection) : 0f;
            trick = Trick.None;
        }

        void StartTrick(Trick next, int direction)
        {
            if (trick != Trick.None)
                return;
            trick = next;
            trickDirection = direction;
            trickElapsed = 0f;
        }

        void Update()
        {
            hud.FitViewport(chaseCamera);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame)
                    BeginRun();
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    if (state == RunState.Driving)
                        Lift();
                    else
                        BeginRun();
                }
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    Steer(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    Steer(1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                    Lift();
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                    Slam();
                if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                    ToggleNitro();
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            if (state == RunState.Climbing)
            {
                rampElapsed = Mathf.Min(ClimbDuration, rampElapsed + dt);
                world.PoseRampClimb(rampElapsed / ClimbDuration);
                if (rampElapsed >= ClimbDuration)
                {
                    state = RunState.Staged;
                    hud.ShowStaged(bank, best);
                }
            }
            else if (state == RunState.Countdown)
            {
                rampElapsed += dt;
                if (rampElapsed >= CountdownDuration)
                {
                    rampElapsed = 0f;
                    state = RunState.Launch;
                    hud.ShowLaunch();
                    speaker.PlayOneShot(thrusterSound, 0.44f);
                }
                else
                    hud.ShowCountdown(Mathf.CeilToInt(CountdownDuration - rampElapsed));
            }
            else if (state == RunState.Launch)
            {
                rampElapsed = Mathf.Min(LaunchDuration, rampElapsed + dt);
                world.PoseLaunch(rampElapsed / LaunchDuration, dt);
                if (rampElapsed >= LaunchDuration)
                {
                    state = RunState.Driving;
                    hud.ShowDriving(best);
                }
            }
            else if (state == RunState.Driving)
            {
                while (dt > 0.00001f && state == RunState.Driving)
                {
                    float step = Mathf.Min(dt, 1f / 60f);
                    Drive(step);
                    dt -= step;
                }
            }
            else if (state == RunState.Strike)
            {
                aftermath += dt;
                world.AnimateImpact(dt, aftermath);
                hud.FadeImpact(aftermath);
                if (aftermath >= 0.95f)
                {
                    state = RunState.Results;
                    hud.ShowResults(missileCrash, Score, coins, bank, best, peakCombo, newBest);
                }
            }

            if (state == RunState.Driving)
            {
                float streakBoost = nitroActive ? 1f : Mathf.Max(sideBurst / 0.46f, liftBurst / 0.43f);
                hud.ShowSpeedLines(distance, roadSpeed, streakBoost);
                hud.ShowNitro(nitroFuel / NitroCapacity, nitroActive);
            }
            PlaceCamera();
        }

        void Drive(float dt)
        {
            elapsed += dt;
            if (elapsed >= comboExpires && trick == Trick.None && pendingCount == 0)
                combo = 0;
            UpdateFlight(dt);
            float thrust = Mathf.Max(sideBurst / 0.46f, liftBurst / 0.43f);
            turnTime = Mathf.Max(0f, turnTime - dt);
            float targetYaw = 35f * turnInput * (turnTime / SteeringDuration);
            steeringYaw = Mathf.SmoothDamp(steeringYaw, targetYaw, ref yawVelocity, 0.085f, Mathf.Infinity, dt);
            carX = Mathf.SmoothDamp(carX, lane * RoadsterWorld.LaneWidth, ref lateralVelocity,
                0.14f, Mathf.Infinity, dt);
            float pitch = recoveryPitch + (trick == Trick.FrontFlip ? TrickAngle : Mathf.Clamp(-verticalVelocity * 0.75f, -6f, 10f));
            float roll = recoveryRoll + (trick == Trick.BarrelRoll ? TrickAngle * trickDirection : Mathf.Clamp(-lateralVelocity * 0.70f, -13f, 13f));
            float nitroTime = nitroActive ? Mathf.Min(nitroFuel, dt) : 0f;
            nitroFuel -= nitroTime;
            if (nitroFuel <= 0f)
                nitroActive = false;
            roadSpeed = Mathf.Lerp(24f, 48f, Mathf.Clamp01(elapsed / 24f)) + thrust * 5f + NitroSpeed * nitroTime / dt;
            float nextDistance = distance + roadSpeed * dt;
            world.PlaceCar(carX, steeringYaw, nextDistance, carHeight, pitch, roll, thrust, dt, sideBurst > 0f ? sideJet : 0);
            world.EmitNitro(nitroTime);
            world.ShowLanding(carX, landingPulse);
            Bounds player = world.PlayerBounds;
            bool trafficHit = world.UpdateTraffic(dt, elapsed, roadSpeed, player);
            int collected = world.Advance(distance, nextDistance, carX, elapsed, out int nitroCollected);
            distance = nextDistance;
            coins += collected;
            if (collected > 0 || nitroCollected > 0)
                speaker.PlayOneShot(pickupSound, 0.38f);
            if (nitroCollected > 0)
            {
                nitroFuel = Mathf.Min(NitroCapacity, nitroFuel + NitroCharge * nitroCollected);
                ShowNotice("Nitro picked up");
            }

            if (trafficHit)
            {
                FinishRun(false);
                return;
            }

            float remaining = impactAt - elapsed;
            if (remaining <= WarningTime)
            {
                if (!warned)
                {
                    warned = true;
                    world.LockMissile(player.center);
                    speaker.PlayOneShot(warningSound, 0.28f);
                }
                if (world.MoveMissile(Mathf.Clamp01(1f - remaining / WarningTime)))
                {
                    FinishRun(true);
                    return;
                }
                if (remaining <= 0f)
                {
                    world.ClearMissile();
                    warned = false;
                    stuntScore += 150;
                    ShowNotice("Missile dodged   +150");
                    ScheduleMissile();
                }
            }

            hud.UpdateDriving(Score, coins, roadSpeed, warned, combo + pendingCount,
                elapsed < noticeUntil ? notice : "");
        }

        void UpdateFlight(float dt)
        {
            sideBurst = Mathf.Max(0f, sideBurst - dt);
            liftBurst = Mathf.Max(0f, liftBurst - dt);
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, dt * 3.4f);
            recoveryPitch = Mathf.MoveTowards(recoveryPitch, 0f, 1200f * dt);
            recoveryRoll = Mathf.MoveTowards(recoveryRoll, 0f, 1200f * dt);
            if (carHeight <= 0f && verticalVelocity <= 0f)
                return;

            float timeToGround = (verticalVelocity + Mathf.Sqrt(verticalVelocity * verticalVelocity + 2f * Gravity * carHeight)) / Gravity;
            if (trick != Trick.None)
            {
                trickElapsed += Mathf.Min(dt, timeToGround);
                if (trickElapsed >= TrickDuration)
                {
                    pendingCount++;
                    int value = trick == Trick.FrontFlip ? 250 : 200;
                    pendingScore += value * Mathf.Min(8, combo + pendingCount);
                    lastTrick = trick;
                    trick = Trick.None;
                }
            }
            carHeight += verticalVelocity * dt - 0.5f * Gravity * dt * dt;
            verticalVelocity -= Gravity * dt;
            if (carHeight >= MaxAltitude)
            {
                carHeight = MaxAltitude;
                verticalVelocity = Mathf.Min(0f, verticalVelocity);
            }
            else if (carHeight <= 0f)
            {
                carHeight = verticalVelocity = 0f;
                recoveryPitch = recoveryRoll = 0f;
                trick = Trick.None;
                usedLift = false;
                if (pendingCount > 0)
                {
                    stuntScore += pendingScore;
                    combo += pendingCount;
                    peakCombo = Mathf.Max(peakCombo, combo);
                    comboExpires = elapsed + ComboWindow;
                    string name = pendingCount > 1 ? "Tricks landed" : lastTrick == Trick.FrontFlip ? "Front flip" : "Barrel roll";
                    ShowNotice($"{name}   +{pendingScore:N0}");
                    pendingScore = pendingCount = 0;
                }
                if (slamming)
                {
                    landingPulse = 1f;
                    world.BurstAt(new Vector3(carX, 0.08f, 0f), 1f);
                    speaker.PlayOneShot(impactSound, 0.25f);
                }
                slamming = false;
            }
        }

        void ShowNotice(string message)
        {
            notice = message;
            noticeUntil = elapsed + 2f;
        }

        void FinishRun(bool missileHit)
        {
            state = RunState.Strike;
            missileCrash = missileHit;
            int credits = coins * CoinValue;
            bank = (int)System.Math.Min((long)bank + credits, int.MaxValue);
            newBest = Score > best;
            best = Mathf.Max(best, Score);
            PlayerPrefs.SetInt(BankKey, bank);
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();

            Shader.SetGlobalFloat(Grayscale, 1f);
            world.Impact(missileHit);
            speaker.PlayOneShot(impactSound, 0.62f);
            hud.ShowImpact();
            hud.FadeImpact(0f);
        }

        void PlaceCamera()
        {
            float speedFov = state == RunState.Driving ? 4f * Mathf.InverseLerp(24f, 48f, roadSpeed) : 0f;
            if (state == RunState.Driving && nitroActive)
                speedFov += 3f;
            chaseCamera.fieldOfView = Mathf.Lerp(chaseCamera.fieldOfView, 47f + speedFov, 1f - Mathf.Exp(-8f * Time.deltaTime));
            Vector3 position = new(carX * 0.28f, 4.0f + carHeight * 0.24f, -10.35f);
            Vector3 look = new(carX * 0.12f, 0.22f + carHeight * 0.42f, 13.3f);
            if (state == RunState.Climbing || state == RunState.Staged || state == RunState.Countdown || state == RunState.Launch)
            {
                float progress = state == RunState.Launch ? rampElapsed / LaunchDuration : 0f;
                float merge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, progress));
                position = Vector3.Lerp(new Vector3(-23.7f, 6.8f, -13.6f), position, merge);
                float follow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.60f, progress));
                Vector3 rampLook = Vector3.Lerp(new Vector3(0f, 7f, -20f), world.CarCenter, follow);
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 1f, progress));
                look = Vector3.Lerp(rampLook, look, settle);
            }
            position.y -= landingPulse * 0.13f;
            if (state == RunState.Strike)
            {
                float shake = Mathf.Max(0f, 1f - aftermath * 2.9f) * 0.32f;
                position += new Vector3(Mathf.Sin(aftermath * 93f), Mathf.Cos(aftermath * 77f), 0f) * shake;
            }
            chaseCamera.transform.position = position;
            chaseCamera.transform.LookAt(look);
        }

        static AudioClip MakeSound(string name, float duration)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * sampleRate)];
            var noise = new System.Random(406);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float attack = Mathf.Clamp01(t * 180f);
                float decay = 1f - t / duration;
                float wave;
                if (name == "Coin")
                    wave = (Mathf.Sin(t * 5529f) + Mathf.Sin(t * 8289f) * 0.5f) * 0.45f;
                else if (name == "Missile warning")
                    wave = Mathf.Sin(t * 3250f + Mathf.Sin(t * 38f) * 4f) * (Mathf.Sin(t * 19f) > 0f ? 0.5f : 0f);
                else if (name == "Cold-gas thruster")
                    wave = ((float)noise.NextDouble() - 0.5f) * 0.8f + Mathf.Sin(t * (840f - 510f * t)) * 0.15f;
                else
                    wave = Mathf.Sin(t * (420f - t * 230f)) * 0.55f + ((float)noise.NextDouble() - 0.5f) * 0.7f;
                samples[i] = wave * attack * decay * decay;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
