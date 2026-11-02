# ShatterLeap

Phase 1, Sprint 1: a 3D glass-bridge memory game. Memorize the preview, then choose the next left or right panel with A/D, panel clicks, touch, or the large on-screen buttons. Only a completed round banks coins. Failed and abandoned rounds pay nothing; previous winnings remain saved.

## Play the Web build

From this project directory:

```powershell
python Tools/serve.py
```

Open http://127.0.0.1:8080 in a browser. Keep the server running while playing. Do not open the HTML through `file://`.

To make the server reachable from a phone on the same network, use `python Tools/serve.py --bind 0.0.0.0` and visit the computer's LAN address on port 8080. Device and firewall configuration may affect access.

## Unity

Use Unity 6000.3.10f1 with Web Build Support. Open `Assets/ShatterLeap/Scenes/ShatterLeap.unity` and press Play. The original `Assets/Scenes/SampleScene.unity` remains separate.

- Configuration: `Assets/ShatterLeap/Config/Sprint1.asset`.
- Prefabs: `Assets/ShatterLeap/Prefabs/Contestant.prefab` and `GlassPanel.prefab`.
- Editor menu: **ShatterLeap** for inspection, setup, validation, deterministic checks, and a Web build.
- Repeat an existing build: `powershell -File Tools/Build-Web.ps1`.
- Setup only if assets are missing: `powershell -File Tools/Build-Web.ps1 -Setup`.
- Setup preserves existing prefabs, scene, and configuration. Normal builds validate and build existing assets without regenerating art or configuration.

The small `Tools/editor.py` bridge can queue commands into an already-open Editor. Use `python Tools/editor.py refresh` after changing scripts if Unity has not refreshed. Then queue `tests`, `integration`, or `build`. Integration tests run asynchronously; results appear in `SprintDoc/PlayChecks.txt` after Play Mode exits.

Browser QA tooling is separate from game/build dependencies. To reproduce it on this Windows environment, run `npm ci --prefix Tools`, start the HTTP server, then run `node Tools/web-checks.cjs`, `node Tools/visibility-check.cjs`, and `node Tools/touch-reload-check.cjs`. The scripts use the installed Chrome/Edge executables and do not download browsers. The visibility check opens its own temporary Chrome window and closes it afterward.

## Static hosting

Upload the complete contents of `Builds/Web`, preserving relative paths. The build uses uncompressed files and no WebAssembly threads. Serve `.wasm` as `application/wasm`, `.js` as JavaScript, and `.data` as `application/octet-stream`. Do not attach a Brotli or gzip `Content-Encoding` header to these uncompressed files. COOP/COEP isolation headers are not required for this build. HTTPS is recommended for public hosting.

Browser storage is local to the origin/profile and can be cleared by the user or browser. There is no server account, cloud wallet, real money, or shop in Sprint 1.

See `SprintDoc/Phase1Sprint1.txt` for executed checks, screenshot evidence, missing sounds, browser coverage, and limitations.
