# Hydraulic Press

A focused 3D risk-and-reward crushing game, authored in Unity 6.3 LTS and URP. Virtual points only.

## Play

Run `Play Hydraulic Press.cmd`, or run `python Tools/serve.py` and visit <http://localhost:8088>. Serve the build over HTTP; opening index.html directly does not load WebAssembly correctly.

Choose **PLAY** from the main menu. Mouse or touch: **CRUSH** to continue, **CASH OUT** to bank. Space crushes; Enter collects. The pause button or Escape opens **RESUME / SETTINGS / MAIN MENU**, including during a press animation. Settings control sound, music, camera shake and power saving. Returning to the main menu preserves an unfinished round for this session. Successful collections start a fresh round after a short result display; a broken press offers immediate retry.

## Rules

Every round commits twenty unique object types and exactly three hidden dangerous positions with two independent Fisher-Yates shuffles. Visual type never determines danger. No outcome changes after the round begins. Crushes and cash-outs are guarded by both domain state and presentation state.

After `k` successful crushes, survival probability is `C(20-k,3)/C(20,3)`. Multiplier is `0.96 / survival probability`, with 1.00x displayed before the first crush. Payout is `floor(100 * multiplier)` using the unrounded multiplier. The first successful cashout therefore pays **112 points**, although the two-decimal multiplier display reads 1.13x. There is no entry charge or purchasable currency. Seventeen successes trigger perfect completion and bank 109,440 points automatically.

## Project

- Editor: **6000.3.10f1** (LTS), with WebGL Build Support.
- Gameplay scene: `Assets/HydraulicPress/Scenes/HydraulicPress.unity`.
- Configuration: `Assets/HydraulicPress/Generated/Gameplay.asset`.
- Twenty object ScriptableObjects and prefabs: `Assets/HydraulicPress/Generated`.
- Runtime: `Assets/HydraulicPress/Runtime`.
- Original mesh authoring and scene assembly: `Assets/HydraulicPress/Editor/ContentBuilder.cs`.
- Audio and icon source generator: `python Tools/create_media.py` (requires Pillow).

Use **Hydraulic Press > Build WebGL** to rebuild into `Builds/WebGL`. **Create or refresh game content** regenerates this game's owned scene and generated assets; it is an authoring operation, not required to play or build an existing scene.

## Validation

Run EditMode and PlayMode suites through Unity Test Runner, or the installed Unity CLI:

```powershell
unity command test run --mode editor --filter HydraulicPress --async_tests true
unity command test status
unity command test run --mode playmode --filter HydraulicPress.PlayTests --async_tests true
unity command test status
```

The browser smoke suite is `node Tools/browser/smoke.cjs` after `npm install --prefix Tools/browser` and starting the local server. It uses installed Chrome. `HP_BROWSER` can point to an installed Edge executable. Screenshots and test evidence are saved in `Reports`.

The WebGL template caps device pixel ratio at 2 for smooth phone rendering, fills the safe viewport without scroll, and unlocks sound on the first interaction. UI uses scalable TextMeshPro text, enlarged phone controls and device-aware safe-area anchors. Native render scale and 4x MSAA keep the 2x normal mode sharp. The scene uses one shadow-casting light, bounded original meshes, pooled objects, forty pooled physical shards and three pooled particle systems. Save JSON uses validation and backup recovery; browser storage permissions and private-browsing persistence remain controlled by the browser.

Release builds apply high managed stripping, size-oriented IL2CPP generation, Disk Size LTO and Gzip. The supplied server sends the required `Content-Encoding: gzip` and original MIME types. External hosting must also send these headers for `.gz` files. The build command reapplies the optimization setting because Unity stores that choice per machine. These options follow [Unity's Web build settings](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/building-distribution/web-build-settings). Power saving retains native render resolution, uses 2x MSAA, disables shadows and occlusion, halves debris bursts, and renders every second browser frame.

Phone validation: use Unity's built-in Device Simulator with the iPhone SE (2nd generation), 12 mini, 12 and 12 Pro Max profiles. `Tools/iphone_simulator_qa.ps1` records both orientations from a running Play Mode session. `node Tools/browser/iphone_qa.cjs` exercises real touch input in Chrome viewport/DPR emulation against the local build. Neither replaces physical iPhone/Safari testing. See `Reports/2026-11-05/QA.md` for the current evidence, and `Reports/QA.md` for the original baseline. See `ASSET_CREDITS.md` for provenance and licenses.
