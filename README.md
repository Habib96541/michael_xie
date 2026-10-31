# Drop Stack

A Unity 6.3 block puzzle for the browser. Play a 90-second sprint with rising speed, hold, three-piece preview, a landing guide, combos, and a personal best for each difficulty.

The arcade presentation pairs glossy blocks with sharp beveled panels, a block-built logo, a subtle grid backdrop, and bold fixed-weight typography. Includes animated menus, clear/target/gear notifications, rolling scores, combo punches, a double-beat warning timer, stack-height and speed meters, synthesized sound effects and music. The HUD and menus adapt to landscape, portrait, and safe areas. Motion, screen shake, sound, music, and supported-device vibration can be changed in pause/settings; the initial motion setting respects the browser's reduced-motion preference.

## Difficulty

- **Noob:** the seven classic four-block pieces, a slower starting drop, generous lock delay, and a four-line target worth 500 bonus points.
- **Neutral:** all 16 shapes, balanced pacing, and an eight-line target worth 1,200 bonus points.
- **Pro:** all 16 shapes, faster drops and shorter lock delay, and a twelve-line target worth 2,000 bonus points.

Each run lasts 90 seconds. Speed increases every 15 seconds. Target bonuses are awarded once per run; personal bests are saved separately by mode. Existing version 1 records migrate to Neutral.

## Play in Unity

Open `Assets/Scenes/SampleScene.unity` in Unity **6000.3.10f1** and press Play. The scene is already configured. `Drop Stack > Setup Scene` explicitly rebuilds it; opening the editor never regenerates the scene. The original scene is backed up in `Backups/SampleScene.before-polish.unity`.

## Controls

- Arrow keys or A/D: move; down arrow or S: soft drop.
- Up arrow, W or X: rotate clockwise; Z or Q: rotate counterclockwise.
- Space: hard drop. C, Shift or H: hold/swap once per piece.
- Escape or P: pause. The run also pauses when the window loses focus.
- R: open restart confirmation. In the menu, 1/2/3 select Noob/Neutral/Pro and Enter starts; Enter also replays from results.
- Mouse/touch: use the controls beneath the board; hold left/right/down for repeated input. Tap the hold panel to swap.

## WebGL

Published on Cloudflare Pages: [Play Drop Stack](https://drop-stack.pages.dev/).

Arcade UI release **1.1.0**: [versioned deployment](https://b6034931.drop-stack.pages.dev/).

To upload a rebuilt game to the same project, run `Tools/Deploy-Cloudflare.ps1 -ProjectName drop-stack` from PowerShell. The script packages the build, includes Cloudflare's hosting headers and font licenses, and deploys `Builds/WebGL`. Cloudflare sign-in uses Wrangler and stays outside the project files.

With WebGL Build Support installed, use `Drop Stack > Build WebGL`, or run `Tools/Build-Web.ps1` from PowerShell. Close the project in other Unity instances before a command-line build. The script finds a standard Hub installation or the local user build tools; `-EditorPath` can override it.

Output: `Builds/WebGL`. Run `Tools/Serve-Web.ps1` and visit `http://localhost:8080/`. Serve the build over HTTP rather than opening `index.html` directly from disk.

`Tools/Package-Web.ps1` creates `Builds/DropStack-WebGL.zip` with the font licenses included. The command-line build script also runs this packaging step.

Upload the **entire build folder** to a static host. For itch.io, zip the **contents** of `Builds/WebGL` so `index.html` sits at the zip root; select HTML game, allow fullscreen, and enable mobile support if desired. No server-side game code is needed. Gzip compression uses Unity's JavaScript decompression fallback for ordinary static hosting. Local scores and sound/music/motion settings use browser storage and are specific to that browser/site.

The template includes the block logo, loading progress, retry/error messages, keyboard scroll prevention, fullscreen, touch handling, and a device-pixel-ratio cap. Bundles use content hashes to avoid stale cached versions. Builds are staged and previous local output is archived after a successful build. Release builds omit the development smoke-test harness. Only distribute the release WebGL folder.

## Verification

`Drop Stack > Run Verification Tests` runs nine groups covering shapes/randomization, board collisions, non-adjacent row collapse and buffer rows, paused controls, speed escalation, mode-specific pacing/bags/records, scoring/combos, and serialized scene references.

A development player built with `DropStack.Editor.DropStackWebBuild.BuildSmokePlayer` can run with `--smoke-test`. Its 33 checks cover actual menu callbacks, all three difficulty modes, rendered pieces, hold limits, touch auto-repeat, a real perfect clear, rolling-score settlement, pause/restart/continue, popup/HUD overlap, portrait fit, timer expiry and replay. Results and screenshots are written to `Builds/Verification`. `VerifyAndBuildSmoke` rebuilds the scene, runs the editor checks, and builds this player in one step.

After a release build, `node Tools/Verify-Web.cjs` checks template/bundle syntax, WebAssembly validity, logo assets, and the guarded vibration bridge.

## Fonts and assets

Stack Display and Stack UI are renamed static-weight derivatives of Oxanium and Nunito, distributed under the SIL Open Font License; the source fonts and license files are in `Assets/Fonts`. `Tools/Generate-Arcade-Assets.py` creates these weights and the SVG loading logo/favicon. All block textures, UI panels, background art, music and sound effects are generated by the project's own code. The supplied reference screenshots are not shipped as game assets.
