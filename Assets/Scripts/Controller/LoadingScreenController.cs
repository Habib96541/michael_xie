using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace com.javierquevedo
{
    /// <summary>
    /// Boot stage for Cyber Orbs.
    ///
    /// Runs once when a menu scene starts: warms the authored art, orb sprites, VFX,
    /// UI textures and synthesised audio behind the loading screen, then cross-fades into
    /// the main menu. Gameplay scenes skip the boot stage and go straight into the match.
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        /// <summary>True while the boot screen owns the display. Menus stay hidden until it ends.</summary>
        public static bool BootActive { get; private set; }

        /// <summary>Menu scene loaded after the boot stage. Empty keeps the old same-scene hand-off.</summary>
        public string NextSceneName = "";

        private const float MinimumBootSeconds = 1.75f;
        private const float ReadyHoldSeconds = 0.50f;

        private LoadingScreenGUI _gui;
        private GameObject _camera;
        private static bool _armed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ArmBoot()
        {
            BootActive = true;
            _armed = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBoot()
        {
            if (!_armed) return;
            _armed = false;

            // A gameplay scene already has its own presentation; never cover it with a boot screen.
            if (Object.FindAnyObjectByType<GameController>() != null ||
                Object.FindAnyObjectByType<BubbleMatrixController>() != null)
            {
                BootActive = false;
                return;
            }

            if (Object.FindAnyObjectByType<LoadingScreenController>() != null) return;

            GameObject loader = new GameObject("LoadingScreen");
            loader.AddComponent<LoadingScreenController>();
        }

        private IEnumerator Start()
        {
            _camera = GameObject.Find("Camera");
            if (_camera == null && Camera.main != null)
                _camera = Camera.main.gameObject;
            if (_camera == null)
            {
                _camera = new GameObject("Camera");
                _camera.tag = "MainCamera";
                _camera.AddComponent<Camera>();
            }

            Camera cam = _camera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            }

            // The boot scene authors the display on this object; the camera is only a fallback.
            _gui = GetComponent<LoadingScreenGUI>();
            if (_gui == null)
                _gui = Object.FindAnyObjectByType<LoadingScreenGUI>();
            if (_gui == null)
                _gui = _camera.GetComponent<LoadingScreenGUI>();
            if (_gui == null)
                _gui = _camera.AddComponent<LoadingScreenGUI>();

            float bootStart = Time.unscaledTime;
            yield return StartCoroutine(WarmUp());

            // A hard floor keeps the boot readable on fast desktops instead of flashing past.
            while (Time.unscaledTime - bootStart < MinimumBootSeconds)
                yield return null;

            if (_gui != null) _gui.MarkReady();
            yield return new WaitForSecondsRealtime(ReadyHoldSeconds);

            BootActive = false;
            SplashScreenGUI.BootHandoffTime = Time.unscaledTime;

            // Load the authored main menu scene when one is configured. The menu fades
            // itself in from the boot black using BootHandoffTime.
            if (!string.IsNullOrEmpty(NextSceneName) && Application.CanStreamedLevelBeLoaded(NextSceneName))
            {
                SceneManager.LoadScene(NextSceneName);
                yield break;
            }

            if (Object.FindAnyObjectByType<SplashScreenController>() == null)
            {
                GameObject splash = new GameObject("SplashScreenController");
                splash.AddComponent<SplashScreenController>();
            }

            if (_gui != null) Destroy(_gui);
            Destroy(gameObject);
        }

        private IEnumerator WarmUp()
        {
            yield return null;

            // 1. Type. The whole UI is laid out with Rajdhani, so the fonts come first.
            _gui.SetStage("LINKING ARENA NETWORK", 0.10f);
            yield return null;
            bool fonts = ArtUI.Bold != null && ArtUI.SemiBold != null;
            _gui.AddLog(fonts ? "TYPE SYSTEM ONLINE" : "TYPE SYSTEM FALLBACK", fonts);
            yield return new WaitForSecondsRealtime(0.12f);

            // 2. Authored art. One load pass keeps the disk reads together on WebGL and mobile.
            _gui.SetStage("DECRYPTING ARENA SHELL", 0.24f);
            yield return null;
            Texture2D[] atlas = Resources.LoadAll<Texture2D>("Art/Sprites");
            int atlasCount = atlas != null ? atlas.Length : 0;
            _gui.AddLog("ART LATTICE " + atlasCount + " NODES", atlasCount > 0);
            yield return new WaitForSecondsRealtime(0.10f);

            // 3. Orb sprites. Each one is a cached Sprite create, never a per-shot allocation.
            _gui.SetStage("CALIBRATING SIX CORES", 0.42f);
            yield return null;
            for (int i = 0; i < LoadingScreenGUI.Cores.Length; i++)
            {
                CyberArtGenerator.GetOrbSprite(LoadingScreenGUI.Cores[i]);
                if (i % 2 == 0) yield return null;
            }
            _gui.AddLog("SIX ELEMENTAL CORES LOCKED", true);
            yield return new WaitForSecondsRealtime(0.10f);

            // 4. Railgun and board dressing.
            _gui.SetStage("SPINNING UP RAILGUN", 0.58f);
            yield return null;
            CyberArtGenerator.GetTurretBaseSprite();
            CyberArtGenerator.GetTurretBarrelSprite();
            CyberArtGenerator.GetChamberRingSprite();
            CyberArtGenerator.GetAimDotSprite();
            CyberArtGenerator.GetGhostOrbSprite();
            _gui.AddLog("RAILGUN COILS PRIMED", true);
            yield return new WaitForSecondsRealtime(0.10f);

            // 5. Effects and playfield surfaces.
            _gui.SetStage("ARMING VFX ARRAY", 0.72f);
            yield return null;
            CyberArtGenerator.GetShockwaveSprite();
            CyberArtGenerator.GetShardSprite();
            CyberArtGenerator.GetSparkSprite();
            CyberArtGenerator.GetScreenBackgroundSprite();
            CyberArtGenerator.GetBoardBackdropSprite();
            CyberArtGenerator.GetBoardFrameSprite();
            CyberArtGenerator.GetBatteryCellSprite(true);
            CyberArtGenerator.GetBatteryCellSprite(false);
            _gui.AddLog("PARTICLE ARRAY ARMED", true);
            yield return new WaitForSecondsRealtime(0.10f);

            // 6. Synthesised SFX. This builds the clips once and keeps the audio service alive.
            _gui.SetStage("SYNTHESISING AUDIO", 0.84f);
            yield return null;
            CyberAudioSynthesizer audio = CyberAudioSynthesizer.Instance;
            _gui.AddLog(audio != null ? "SFX SYNTH CORE ONLINE" : "SFX SYNTH OFFLINE", audio != null);
            yield return new WaitForSecondsRealtime(0.12f);

            // 7. UI kit, digits and icons used by the menu, HUD and results card.
            _gui.SetStage("CACHING UI SPRITES", 0.93f);
            yield return null;
            for (int i = 0; i < 10; i++)
            {
                CyberArtGenerator.LoadArtTexture("digit_cyan_" + i);
                CyberArtGenerator.LoadArtTexture("digit_amber_" + i);
                CyberArtGenerator.LoadArtTexture("digit_red_" + i);
            }

            string[] uiArt =
            {
                "hud_panel", "rank_badge", "board_frame",
                "pill_cyan", "pill_blue", "pill_green", "pill_magenta", "pill_yellow",
                "button_green", "button_cyan", "button_yellow", "button_magenta", "button_blue",
                "icon_sound", "icon_muted", "icon_restart", "icon_pause", "icon_play",
                "icon_help", "icon_swap", "icon_trophy", "icon_close",
                "colon_cyan", "colon_amber", "colon_red",
                "battery_full", "battery_empty", "aim_dot", "vfx_ring", "vfx_shard", "vfx_spark"
            };
            for (int i = 0; i < uiArt.Length; i++)
            {
                CyberArtGenerator.LoadArtTexture(uiArt[i]);
                if (i % 6 == 0) yield return null;
            }
            _gui.AddLog("UI ATLAS WARM", true);
            yield return new WaitForSecondsRealtime(0.10f);

            _gui.SetStage("ENTERING NEO-KYOTO ARENA", 1f);
            _gui.AddLog("BOOT SEQUENCE COMPLETE", true);
            yield return new WaitForSecondsRealtime(0.10f);
        }
    }
}
