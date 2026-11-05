# Coinflip project context

The supplied workspace was empty. Created a Unity 6.3 LTS (6000.3.10f1) Universal 2D project on 2026-11-02. WebGL support is installed locally. No Unity MCP bridge is configured; validation uses the Unity Editor in batch mode and real local browsers.

## Runtime

- `Assets/CoinFlip/Scenes/CoinFlip.unity`: single build scene, orthographic camera, one GameManager with serialized settings and static Outfit SDF font.
- `GameSession`: plain C# authoritative round state and virtual economy. One binary outcome draw after validation. Stake deducted once; guarded settlement pays 2× on a win. Saves store in-flight outcome/choice/stake and recover by settling once.
- `GameManager`: composition root, round presentation and lifecycle. `CoinAnimationController`: 2D squash/scale, edge-on sprite swaps, ascent and bounce. Never controls payouts.
- `UIManager` and `UIFactory`: runtime-generated uGUI/TMP hierarchy, safe area and viewport reflow. Uses the Input System UI module for mouse/touch. CanvasScaler reference adapts to physical viewport; coin animation remains local to its responsive home rect.
- `AudioManager`: six reused 2D voices, original PCM SFX, ambient loop, separate music/effects controls, mute and browser gesture unlock.
- `VFXManager`: fixed 32-particle UI pool and landing ripple; no physics, 3D models or postprocessing.
- `SaveManager`: versioned JSON in PlayerPrefs (IndexedDB on Web). Corrupt/missing data resets to defaults. Settings and best streak persist through fresh starts.

## Authoring and builds

- `Tools/generate_assets.py`: original vector-style raster artwork and original synthesized WAV assets. Pillow required; fontTools optional for instantiating the Outfit variable font. Outfit is SIL OFL licensed, included beside the font.
- `ProjectBuilder.Prepare`: configures sprite/audio imports, generates static font atlas and scene, configures lean WebGL player. TMP essentials require the asynchronous `ImportEssentials` method on first bootstrap without `-quit`.
- `ProjectBuilder.BuildWebGL`: prepares and builds `Builds/WebGL`, writes `Logs/build-receipt.json`.
- Web template: full browser viewport, mobile loading screen, capped 2× pixel ratio, gzip with decompression fallback so simple HTTP/static servers work.
- `CoinFlip.Tests`: deterministic economy, double settlement, interruption/reload recovery, corrupt saves, bounded history and repeated rounds. Browser test script and evidence are under `Tools/` and `Docs/`.

No network backend, payments, gambling services, analytics or external runtime dependencies.
