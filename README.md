# Bullseye Blitz

Unity 6000.3.10f1, URP 2D, new Input System. Phase 1, Sprint 1.

Open `Assets/Bullseye/Scenes/BullseyeBlitz.unity` and press Play. Imported demo scenes are preserved.

## Web build

Run from PowerShell in this project:

```powershell
./Tools/build-web.ps1
python -m http.server 8765 --bind 127.0.0.1 --directory Builds/Web
```

Open http://127.0.0.1:8765 in a browser. Do not open the HTML using `file://`.
Stop Play Mode before building. The build script uses the open project Editor when available; otherwise it runs Unity in batch mode. Web Build Support and an active Unity license are required. Normal builds do not regenerate source assets or balance configuration.

Web output is `Builds/Web/`, including `index.html` and every file under `Build/`. Deploy the whole folder. Compression is disabled: no `Content-Encoding` header is required. Serve `.wasm` as `application/wasm`, `.js` as JavaScript, and `.data` as `application/octet-stream`. Python's HTTP server supplies suitable local MIME handling. The build uses a single-threaded Web runtime and does not require COOP/COEP headers. Use HTTPS for public hosting and vibration support where available. Local saves belong to the browser origin; changing ports/domains changes the wallet storage location.

The page fills the viewport and includes a fullscreen button. Browser fullscreen may be restricted on some mobile devices. A resize/rotation updates the camera and UI without restarting the round. Audio begins after a user interaction. Tab visibility/focus loss pauses active gameplay and requires Resume.

## Controls and rules

Click or tap exposed coin targets. Avoid bombs. Survive 20 seconds to bank round coins. Misses are free; ammunition and retries are unlimited. Pause offers Resume, Restart, and Home. Leaving a round requires confirmation and discards pending coins.

## Development and validation

- Configuration: `Assets/Bullseye/BlitzConfig.asset`.
- Target prefab: `Assets/Bullseye/Prefabs/BoothTarget.prefab`.
- Menu: **Bullseye Blitz → Run deterministic checks**.
- Browser checks: start the HTTP server, install Python dependencies (`python -m pip install playwright pillow`), then run `python Tools/verify_web.py` with Google Chrome installed. Fullscreen/DPR checks: `python Tools/verify_browser_extras.py`. Delivery checks: `python Tools/verify_delivery.py`.
- `?qa=1` enables read-only browser telemetry for the verification script; it does not add gameplay controls.
- Full sprint report: `SprintDoc/Phase1Sprint1.txt`.
- Imported asset reuse: `SprintDoc/AssetInventory.md`.

The initial generation menu refuses to overwrite an existing scene. Edit existing assets directly. Preserve imported assets and their licenses.

Text visibility regression checks run with python Tools/verify_text_layout.py. They include the reported wide window and verify every generated Home/Settings label.
