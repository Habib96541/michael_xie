# Drop Stack

A Unity 6.3 block puzzle for the browser. Play a 90-second sprint with rising speed, hold, three-piece preview, a landing guide, combos, and a personal best for each difficulty.

The arcade presentation pairs glossy blocks with sharp beveled panels, a block-built logo, a subtle grid backdrop, and bold fixed-weight typography. Includes animated menus, clear/target/gear notifications, rolling scores, combo punches, a double-beat warning timer, stack-height and speed meters, synthesized sound effects and music. The HUD and menus adapt to landscape, portrait, and safe areas. Motion, screen shake, sound, music, and supported-device vibration can be changed in pause/settings; the initial motion setting respects the browser's reduced-motion preference.

On iPhones the web player uses a fixed tall **1:2.17 width-to-height** frame. It fits inside the visible browser area and the notch/home-indicator safe area, with margins where needed to keep the entire game visible. It recalculates when Safari's browser bars change, the device rotates, or fullscreen changes. Compact phone viewports share this layout; desktop and full-size iPad views remain responsive.

## Difficulty

- **Noob:** the seven classic four-block pieces, a slower starting drop, generous lock delay, and a four-line target worth 500 bonus points.
- **Neutral:** all 16 shapes, balanced pacing, and an eight-line target worth 1,200 bonus points.
- **Pro:** all 16 shapes, faster drops and shorter lock delay, and a twelve-line target worth 2,000 bonus points.

Each run lasts 90 seconds. Speed increases every 15 seconds. Target bonuses are awarded once per run; personal bests are saved separately by mode. Existing version 1 records migrate to Neutral.

## Play in Unity

Open `Assets/Scenes/SampleScene.unity` in Unity **6000.3.10f1** and press Play. The scene is already configured. `Drop Stack > Setup Scene` explicitly rebuilds it; opening the editor never regenerates the scene. Only the current arcade assets and code are retained in the project; Git history contains earlier versions.

## Controls

- Arrow keys or A/D: move; down arrow or S: soft drop.
- Up arrow, W or X: rotate clockwise; Z or Q: rotate counterclockwise.
- Space: hard drop. C, Shift or H: hold/swap once per piece.
- Escape or P: pause. The run also pauses when the window loses focus.
- R: open restart confirmation. In the menu, 1/2/3 select Noob/Neutral/Pro and Enter starts; Enter also replays from results.
- Mouse/touch: use the controls beneath the board; hold left/right/down for repeated input. Tap the hold panel to swap.

## WebGL

Published on Cloudflare Pages: [Play Drop Stack](https://drop-stack.pages.dev/).

Current web release: [versioned deployment](https://14914c28.drop-stack.pages.dev/), including the 1:2.17 iPhone portrait frame.

To upload a rebuilt game to the same project, run `Tools/Deploy-Cloudflare.ps1 -ProjectName drop-stack` from PowerShell. The script packages the build, includes Cloudflare's hosting headers and font licenses, and deploys `Builds/WebGL`. Cloudflare sign-in uses Wrangler and stays outside the project files.

With WebGL Build Support installed, use `Drop Stack > Build WebGL`, or run `Tools/Build-Web.ps1` from PowerShell. Close the project in other Unity instances before a command-line build. The script finds a standard Hub installation or the local user build tools; `-EditorPath` can override it.

Output: `Builds/WebGL`. Run `Tools/Serve-Web.ps1` and visit `http://localhost:8080/`. Serve the build over HTTP rather than opening `index.html` directly from disk.

`Tools/Package-Web.ps1` creates `Builds/DropStack-WebGL.zip` with the font licenses included. The command-line build script also runs this packaging step.

Upload the **entire build folder** to a static host. For itch.io, zip the **contents** of `Builds/WebGL` so `index.html` sits at the zip root; select HTML game, allow fullscreen, and enable mobile support if desired. No server-side game code is needed. Gzip compression uses Unity's JavaScript decompression fallback for ordinary static hosting. Local scores and sound/music/motion settings use browser storage and are specific to that browser/site.

The template includes the block logo, loading progress, retry/error messages, keyboard scroll prevention, fullscreen, touch handling, and a device-pixel-ratio cap. Bundles use content hashes to avoid stale cached versions. Builds are staged and replace the previous local release only after success; old copies are removed after the swap. Release builds omit the development smoke-test harness. Only distribute the release WebGL folder.

## Verification

`Drop Stack > Run Verification Tests` runs nine groups covering shapes/randomization, board collisions, non-adjacent row collapse and buffer rows, paused controls, speed escalation, mode-specific pacing/bags/records, scoring/combos, and serialized scene references.

A development player built with `DropStack.Editor.DropStackWebBuild.BuildSmokePlayer` can run with `--smoke-test`. Its 33 checks cover actual menu callbacks, all three difficulty modes, rendered pieces, hold limits, touch auto-repeat, a real perfect clear, rolling-score settlement, pause/restart/continue, popup/HUD overlap, portrait fit, timer expiry and replay. Results and screenshots are written to `Builds/Verification`. `VerifyAndBuildSmoke` rebuilds the scene, runs the editor checks, and builds this player in one step.

After a release build, `node Tools/Verify-Web.cjs` checks template/bundle syntax, WebAssembly validity, logo assets, the guarded vibration bridge, and the portrait viewport across ten phone sizes with safe-area, browser-toolbar, and rotation cases.

## Fonts and assets

Stack Display and Stack UI are renamed static-weight derivatives of Oxanium and Nunito, distributed under the SIL Open Font License. Only the finished fonts and required licenses are retained in `Assets/Fonts`. The dependency-free `Tools/Generate-Arcade-Assets.py` regenerates the SVG loading logo/favicon. `Drop Stack > Generate Arcade Backdrop` regenerates the current background without creating legacy UI textures. Blocks, UI panels, music and sound effects are generated by the game's code. The supplied reference screenshots are not shipped as game assets.

The reusable project consists of `Assets`, `Packages`, `ProjectSettings`, and the small `Tools` helpers. `Library`, `Logs`, IDE solution files, and test-player output are generated files and may be cleared while Unity is closed. Unity regenerates its cache on the next open. The current WebGL release lives in `Builds/WebGL`; the upload ZIP is generated by the packaging/deployment helpers when needed.
