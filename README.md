# Flap Dash

A skill-based 2D flapping arcade game built with **Unity 6 (6000.3.27f2)** using the Universal Render Pipeline (2D Renderer).

Guide the bird through alternating vertical pipe obstacles within a 60-second round. Each passed pipe earns one point. As score increases, pipe speed progressively escalates every 10 points.

## Play in Unity

1. Open this project folder in **Unity 6000.3.27f2** (or Unity 6000.3.x) with the **Web Build Support** module installed.
2. In the Project panel, open `Assets/Scenes/FlapDash.unity`.
3. Press **Play**.
4. Press **Space**, **Click**, or **Tap** to flap. Avoid colliding with pipes, the ground, or the ceiling.

## Play the WebGL Build

The compiled WebGL browser build is located in `Builds/WebGL_2026-11-05/` (with a deployment archive at `Builds/FlapDash_WebGL_2026-11-05.zip`).

To play locally:
1. Open a terminal in `Builds/WebGL_2026-11-05/`.
2. Run a local HTTP server (e.g. using Python):
   ```powershell
   python -m http.server 8765 --bind 127.0.0.1
   ```
3. Open `http://127.0.0.1:8765/` in any modern browser supporting WebGL 2.0. Keep the server running while playing.
   *(Note: Opening `index.html` directly via `file://` is not supported by browsers due to CORS/WASM security policies.)*

## Features

- **Responsive Controls:** Spacebar, mouse click, and touchscreen tap support.
- **Dynamic Speed Progression:** Target pipe speed increases smoothly by +0.20 units every 10 points (from 2.65 up to 3.65) with per-round reset.
- **Alternating Pipe Variations:** Vertical openings change dynamically across rounds with a 12-entry height sequence (centers from -2.6 to +2.6).
- **Sound Effects:** CC0 frog chirp audio triggered on flaps, automatically cleared on round reset.
- **Visual Energy Settings:** Adjustable visual energy on the menu to reduce decorative motion if desired.
- **HUD & UI:** In-game score display, 60-second countdown timer, game-over screen, and Play Again flow.
- **Safe Area & Portrait Canvas:** Configured for 360 × 640 portrait mobile/browser viewports.

## Project Structure

- `Assets/`: Scenes, scripts, audio, sprites, and URP 2D pipeline settings.
  - `Assets/Scenes/FlapDash.unity`: Main gameplay scene.
  - `Assets/Scripts/`: Game controller, bird mechanics, pipe generator, UI, and audio scripts.
  - `Assets/Art/`: CC0 2D art assets.
  - `Assets/ThirdParty/`: Audio chirp (`frog.wav`) and CC0 pipe assets.
- `Packages/`: Project manifest and package dependencies (URP 17.3.0, Input System 1.20.1, uGUI 2.0.0, 2D Sprite 1.0.0).
- `ProjectSettings/`: Unity project configuration and WebGL build settings.
- `Builds/`: Verified WebGL packages and release archives.
  - `Builds/WebGL_2026-11-05/`: Extracted ready-to-serve WebGL package.
  - `Builds/FlapDash_WebGL_2026-11-05.zip`: Distribution archive.
  - `Builds/WebGL/` & `Builds/FlapDash_WebGL.zip`: Historical Sprint 03 baseline.
- `Docs/`: Development sprint documentation, verification evidence, and asset license records.

## License & Assets

- Audio and art assets are CC0 Public Domain. Full provenance and creator declarations are documented in `Builds/WebGL_2026-11-05/AssetLicenses.txt` and `Docs/Asset_Licenses.txt`.
- Unity engine packages retain their original Unity Software licenses.
