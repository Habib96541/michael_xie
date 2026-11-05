# Hydraulic Press — project context

Inspected and created on 2026-11-02. The supplied workspace was empty; no existing assets or systems were overwritten.

- Confirmed project root: `D:/30 games/Hydraulic Press`.
- Unity: 6000.3.10f1 LTS, revision e35f0c77bd8e. Installed WebGL module.
- Render pipeline: URP 17.3.0. Studio lighting, real-time soft key shadows, reflection cubemap, ACES grading.
- Input: Input System 1.18.0, uGUI 2.0.0, TextMeshPro. Touch and pointer through InputSystemUIInputModule; Space/Enter optional keyboard controls.
- Runtime assembly: `HydraulicPress.Runtime`. Plain C# RoundManager owns committed rounds, action guards and combinatorial rewards. GameManager coordinates presentation, save data and banking. GameplayConfig and twenty CrushableObjectDefinition assets supply content.
- Scene: `Assets/HydraulicPress/Scenes/HydraulicPress.unity`, sole enabled build scene.
- Art: original authored beveled meshes, turned profiles and tubing. Twenty prefabs and pooled deformation instances; bounded physical shards and pooled particle systems. Original reproducible icons and synthesized sounds in `Tools/create_media.py`.
- UI: screen overlay Canvas with CanvasScaler, UnityEngine.Device.Screen safe-area anchors and portrait/landscape layouts. Main menu at launch; pause/resume/settings/home navigation freezes active press animations and preserves the committed round. No emoji icons.
- Framing: portrait fit reserves space for the HUD and captions; square and landscape windows use side controls with a width-aware header. Test short portraits and square windows before changing camera size or HUD spacing.
- Persistence: versioned PlayerPrefs JSON with validated backup recovery. Virtual points only. No network services.
- Editor tooling: Unity Pipeline 0.9.1-exp.1 with Unity CLI. No separate community MCP installed.
- Build: `HydraulicPress.Editor.ContentBuilder.BuildWebGL()`. Output `Builds/WebGL`. Local host: `python Tools/serve.py`, port 8088.
- Release settings are reapplied by the builder: high stripping, OptimizeSize IL2CPP, DiskSizeLTO, Gzip, 128 MB initial / 512 MB maximum memory. The server sends gzip and original MIME headers. `Tools/package_build.py` adds a portable launcher and SHA256 manifest.
- Authoring: `ContentBuilder.CreateGame()` regenerates owned generated content and the gameplay scene; run outside Play Mode. Mesh asset identities are preserved with CopySerialized.
- Tests: Unity Test Framework, EditMode domain/save tests in `Assets/HydraulicPress/Tests/Editor`.
- Phone QA added 2026-11-05: built-in Unity Device Simulator has iPhone SE2, 12 mini, 12 and 12 Pro Max profiles. Editor-only `PhoneSimulatorQA` adapter captures fresh rendered frames and checks safe bounds/44-point button targets. Browser QA uses read-only normalized button bounds; it never exports hazards. WebGL DPR cap 2 for phone frame pacing, normal URP renderScale 1/MSAA4; power saving keeps resolution and lowers effects/cadence.
- Generated directories: Library, Temp, Logs, UserSettings, Builds. Do not manually edit caches.

Hardware/mobile-browser performance must be measured on actual target devices. Responsive viewport emulation alone is not physical-device validation.
