# Pocket Borough

An original, single-player, 3D city-swallowing arcade game built with Unity 6.3 LTS (6000.3.10f1), URP, the New Input System, uGUI and TextMeshPro. Compete against seven independent rivals in a two-minute match.

## Repository layout

| Folder | Contents |
|---|---|
| `Assets/PocketBorough/` | Game code, shaders, generated models/materials/prefabs/audio, scene and tests |
| `Assets/Plugins/WebGL/` | Browser telemetry bridge |
| `Assets/WebGLTemplates/Borough/` | Responsive WebGL page template |
| `Assets/Resources/` | Game catalog and runtime references |
| `Packages/`, `ProjectSettings/` | Pinned Unity dependencies and project configuration |
| `Builds/WebGL/` | Complete ready-to-host browser build |
| `Builds/HoleArena_WebGL_Desktop_Mobile.zip` | Complete upload ZIP, with index.html at its root |
| `Tools/` | Local preview, build, packaging and browser checks |
| `Reports/` | Test results, asset inventory, screenshots and SHA-256 build manifest |

Open the repository root directly in Unity Hub. Unity will recreate `Library/`, `Temp/`, `Logs/` and other local caches; these are excluded from Git. Browser dependencies are restored with `npm ci --prefix Tools/browser-tests`.

## Play the WebGL build

Double-click **`Play Pocket Borough.cmd`** in this project folder. It starts the local server when needed and opens the game in your browser. Run it again after restarting your PC; the local preview is available while its server process is running.

From PowerShell:

```powershell
# Run from the root of your cloned repository
python Tools/serve.py --port 8767
```

Open **http://127.0.0.1:8767/** in Chrome or Edge. Serve `Builds/WebGL/` through HTTP or HTTPS; opening `index.html` directly from a file browser does not work.

Desktop: WASD or arrow keys; drag the mouse to steer; Escape or the pause button pauses. Mobile: touch and drag in the city to steer; use the pause button. Landscape is preferred, with a rotation notice in portrait.

## Unity project

Open this folder in Unity 6000.3.10f1 with **Web Build Support** installed. The game scene is `Assets/PocketBorough/Scenes/PocketBorough.unity`. Press Play to start at the main menu.

The `Pocket Borough` editor menu provides reproducible asset baking and release building:

1. `Import typography` imports the bundled TMP essentials, if absent.
2. `Bake game and original assets` creates the catalog, 45 mesh assets and prefabs, shared materials, rounded UI sprite, original synthesized audio, quality profiles, lighting and main scene.
3. Switch the active build platform to **Web**. Wait for script compilation.
4. `Build WebGL release` exports `Builds/WebGL/`.

The city starts with 1,025 pooled objects. Street props and vehicles restock at 40 and 80 seconds, away from active holes. Roads, sidewalks, crossings and parks use a combined mesh. Consumable objects share vertex-colour materials and original meshes. The ground shader cuts eight real circular openings; sculpted rims and recessed interiors sit in those openings. Sinking meshes clip at ground level.

## Tests and reports

EditMode tests cover growth, fit restrictions, rival size thresholds, all model meshes, catalog references, duplicate claims and pool reset. PlayMode tests cover countdown, consumption, AI scoring, pause/resume, results and replay. Browser tests play an entire release-build match through keyboard input, then check replay, mouse steering, mobile touch cancellation, responsive layouts and Edge.

```powershell
npm ci --prefix Tools/browser-tests
node Tools/browser-tests/smoke.cjs
```

Run the HTTP server first. Browser tests require installed Chrome and Edge. Evidence, contact sheets, build receipts and test results are in `Reports/`. The read-only `window.boroughState` browser diagnostic exposes match state and frame-rate samples for these tests; it does not change gameplay.

## Hosting

Upload the complete contents of `Builds/WebGL/`, or the ZIP `Builds/HoleArena_WebGL_Desktop_Mobile.zip`, to compatible static hosting. Keep `index.html` at the site or archive root. This game has no platform-specific SDK, authentication, remote backend or network dependency after loading. Best score, skin and audio preferences use browser-local storage.

Actual physical Android and iPhone performance must be verified on target devices; viewport/touch emulation cannot establish real-device performance or Safari compatibility. See `Reports/DEVELOPMENT_REPORT.md` for the final measured coverage.



Latest iPhone layout and menu checks: Reports/iphone/REPORT.md. Portrait rotation pauses the WebGL match; return to landscape and use Keep Eating to resume.
