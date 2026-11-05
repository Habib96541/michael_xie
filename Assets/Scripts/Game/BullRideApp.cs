using BullRide.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Screen = UnityEngine.Device.Screen;
using DeviceApplication = UnityEngine.Device.Application;

namespace BullRide
{
    [DefaultExecutionOrder(-40)]
    public sealed class BullRideApp : MonoBehaviour
    {
        readonly Wallet wallet = new Wallet();
        RideSim sim;
        Camera cam;
        ArenaView arena;
        BullView bull;
        DustFx dust;
        AudioBuzz sound;
        UiLobby lobby;
        UiHud hud;
        UiResult result;
        CanvasScaler scaler;
        RectTransform safeUi;
        Rect lastSafeArea;
        Vector2 lastDisplay;
        bool lastTouchLayout;
        double remainder, popUntil;
        int milestone;
        bool portrait, layoutReady;
        public RidePhase Phase => sim.Phase;

        void Awake()
        {
            Application.targetFrameRate = 60;
            cam = Camera.main;
            if (cam == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = cameraObject.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.SkyTop;
            cam.fieldOfView = 41;
            cam.nearClipPlane = .12f;
            cam.farClipPlane = 110;
            wallet.Load();
            sim = new RideSim(System.Environment.TickCount);

            arena = Child<ArenaView>(transform, "Arena view");
            arena.Build();
            bull = Child<BullView>(transform, "Bull");
            bull.Build();
            dust = Child<DustFx>(transform, "Hoof dust");
            dust.Build();
            sound = Child<AudioBuzz>(transform, "Rodeo audio");
            sound.Build();
            dust.HoofContact += sound.HoofContact;

            Canvas canvas = UiKit.CreateCanvas("Bull Ride UI");
            canvas.transform.SetParent(transform, false);
            scaler = canvas.GetComponent<CanvasScaler>();
            safeUi = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeUi.SetParent(canvas.transform, false);
            UiKit.Stretch(safeUi);
            lobby = Widget<UiLobby>(safeUi, "Menu");
            lobby.Build(this);
            hud = Widget<UiHud>(safeUi, "HUD");
            hud.Build();
            result = Widget<UiResult>(safeUi, "Result");
            result.Build(this, sound);
            EnterLobby();
        }

        static T Child<T>(Transform parent, string name) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        static T Widget<T>(Transform parent, string name) where T : MonoBehaviour
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        void Update()
        {
            if ((Phase == RidePhase.Fall || Phase == RidePhase.Over) && Input.GetKeyDown(KeyCode.R))
                EnterLobby();

            float dt = Mathf.Min(Time.unscaledDeltaTime, .10f);
            Vector2 stick = sim.CanBalance ? ReadStick() : Vector2.zero;
            remainder += dt;
            double tick = 1d / (RideSim.StepsPerSecond * sim.PlaybackRate);
            while (remainder >= tick)
            {
                RidePhase previous = Phase;
                bool closedGate = sim.Gate <= 0;
                sim.Step(stick);
                if (closedGate && sim.Gate > 0) sound.GateOpen();
                remainder -= tick;
                if (previous != Phase) OnPhaseChanged();
                tick = 1d / (RideSim.StepsPerSecond * sim.PlaybackRate);
            }

            if (Phase == RidePhase.Ride || Phase == RidePhase.Done)
            {
                int whole = (int)System.Math.Floor(sim.Multiplier + 1e-9);
                if (whole > milestone)
                {
                    milestone = whole;
                    popUntil = sim.RideTime + .42;
                }
            }

            ApplyLayout();
            arena.SetGate(sim.Gate);
            bull.Apply(sim, sim.CanBalance ? stick : Vector2.zero, dt * sim.PlaybackRate);
            UpdateCamera(dt);
            UpdateSpotlight();
            dust.Tick(sim, dt * sim.PlaybackRate);
            sound.Tick(sim, dt);
            if (hud.gameObject.activeSelf)
            {
                hud.Present(sim, wallet, stick, milestone, popUntil);
                hud.PresentGuide(sim, cam);
            }
        }

        void OnPhaseChanged()
        {
            if (Phase == RidePhase.Done || Phase == RidePhase.Fall)
            {
                wallet.Settle(sim.Multiplier);
                hud.ResetStick();
                if (Phase == RidePhase.Fall)
                {
                    bull.Apply(sim, Vector2.zero);
                    sound.Throw();
                    bull.ThrowRider(sim, sound.Land);
                }
                else sound.Finish();
            }
            else if (Phase == RidePhase.Over)
            {
                hud.gameObject.SetActive(false);
                result.Present(sim, wallet);
                result.gameObject.SetActive(true);
            }
        }

        Vector2 ReadStick()
        {
            int x = Held(KeyCode.D, KeyCode.RightArrow) - Held(KeyCode.A, KeyCode.LeftArrow);
            int y = Held(KeyCode.W, KeyCode.UpArrow) - Held(KeyCode.S, KeyCode.DownArrow);
            Vector2 keys = new Vector2(x, y);
            return keys == Vector2.zero ? hud.Stick : Vector2.ClampMagnitude(keys, 1);
        }

        static int Held(KeyCode letter, KeyCode arrow) => Input.GetKey(letter) || Input.GetKey(arrow) ? 1 : 0;

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && hud != null) hud.ResetStick();
        }

        void ApplyLayout()
        {
            Vector2 display = new Vector2(Screen.width, Screen.height);
            if (display.x <= 0 || display.y <= 0) return;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(Vector2.zero, display);
            bool touchLayout = DeviceApplication.isMobilePlatform || Input.touchSupported;
            if (layoutReady && display == lastDisplay && safe == lastSafeArea && touchLayout == lastTouchLayout)
                return;

            layoutReady = true;
            lastDisplay = display;
            lastSafeArea = safe;
            lastTouchLayout = touchLayout;
            portrait = safe.width < safe.height;
            Vector2 reference = portrait ? new Vector2(500, 860) :
                touchLayout || display.x / display.y >= 1.85f ? new Vector2(960, 480) : new Vector2(1280, 720);
            scaler.referenceResolution = reference;
            safeUi.anchorMin = new Vector2(safe.xMin / display.x, safe.yMin / display.y);
            safeUi.anchorMax = new Vector2(safe.xMax / display.x, safe.yMax / display.y);
            safeUi.offsetMin = safeUi.offsetMax = Vector2.zero;

            float scale = Mathf.Min(display.x / reference.x, display.y / reference.y);
            Vector2 available = safe.size / scale;
            lobby.Layout(portrait, available);
            hud.Layout(portrait, available);
            result.Layout(available);
        }

        void UpdateCamera(float dt, bool instant = false)
        {
            Vector3 center = new Vector3(sim.Position.x, 0, sim.Position.y);
            Vector3 eye, target;
            bool afterFall = !sim.Whistle && (Phase == RidePhase.Fall || Phase == RidePhase.Over);
            if (Phase == RidePhase.Menu || Phase == RidePhase.Chute)
            {
                eye = center + (portrait ? new Vector3(3.15f, 3.19f, 6.77f) : new Vector3(2.75f, 3.05f, 5.45f));
                target = center + new Vector3(0, 1.55f, .1f);
                if (Phase == RidePhase.Menu)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, target - eye).normalized;
                    target -= portrait ? Vector3.up * 1.35f : right * 1.7f;
                }
            }
            else
            {
                float distance = afterFall ? 8.2f : portrait ? 7.5f : 5.75f;
                float angle = 1.12f + .37f * Mathf.Sin(sim.Yaw * .72f);
                Vector3 view = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                if (afterFall)
                {
                    Vector3 fallen = bull.Rider.position;
                    fallen.y = 0;
                    Vector3 travel = fallen - center;
                    if (travel.sqrMagnitude > .04f)
                    {
                        // A side view keeps the rider from tumbling directly at the lens.
                        Vector3 side = Vector3.Cross(Vector3.up, travel.normalized);
                        if (Vector3.Dot(view, side) < 0) side = -side;
                        float turn = Mathf.SmoothStep(0, .8f, Mathf.InverseLerp(.5f, 2.6f, travel.magnitude));
                        view = Vector3.Slerp(view, side, turn);
                    }
                    center = Vector3.Lerp(center, fallen, .69f);
                }
                eye = center + view * distance + Vector3.up * (afterFall ? 3 : portrait ? 2.44f : 1.88f);
                eye.x = Mathf.Clamp(eye.x, -12.6f, 12.6f);
                eye.z = Mathf.Clamp(eye.z, -18.8f, 18f);
                if (new Vector2(eye.x - center.x, eye.z - center.z).magnitude < 4.2f)
                    eye = center + new Vector3(center.x > 0 ? -4.75f : 4.75f, 2.15f, center.z > 0 ? -3.25f : 3.25f);
                target = center + Vector3.up * (afterFall ? .67f : 1.23f + sim.Pose.Hop * .45f);
            }

            float follow = instant ? 1 : 1 - Mathf.Exp(-(portrait ? 9f : 5.6f) * dt);
            Quaternion rotation = Quaternion.LookRotation(target - eye);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(cam.transform.position, eye, follow),
                Quaternion.Slerp(cam.transform.rotation, rotation, follow));
            float fov = afterFall ? 50 : 41;
            if (portrait && Phase != RidePhase.Menu && Phase != RidePhase.Chute)
            {
                Vector3 offset = target - cam.transform.position;
                float distance = Mathf.Max(4.2f, new Vector2(offset.x, offset.z).magnitude);
                fov = Mathf.Clamp(2 * Mathf.Atan(2.55f / (distance * cam.aspect)) * Mathf.Rad2Deg, fov, 90);
            }
            cam.fieldOfView = fov;
        }

        void UpdateSpotlight() => Shader.SetGlobalVector("_RideSpot",
            new Vector4(sim.Position.x, 0, sim.Position.y, 0));

        public void EnterLobby()
        {
            sim.Reset();
            remainder = popUntil = milestone = 0;
            bull.ResetPose();
            dust.Clear();
            sound.ResetRide();
            hud.ResetStick();
            arena.SetGate(0);
            bull.Apply(sim, Vector2.zero);
            lobby.ResetInput();
            lobby.Refresh(wallet);
            lobby.gameObject.SetActive(true);
            hud.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            ApplyLayout();
            UpdateCamera(0, true);
            UpdateSpotlight();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void StartFromLobby()
        {
            if (Phase != RidePhase.Menu) return;
            string error = wallet.Begin(lobby.InputText);
            if (error != null)
            {
                lobby.ShowError(error);
                return;
            }
            sim.Play();
            sound.StartRide();
            lobby.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            hud.Present(sim, wallet, Vector2.zero, 0, 0);
        }

        public void TopUp()
        {
            if (Phase != RidePhase.Menu) return;
            wallet.Refill();
            lobby.ResetInput();
            lobby.Refresh(wallet);
        }
    }
}

