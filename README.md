# Coinflip

A polished, entirely 2D, virtual-coin arcade built with **Unity 6.3 LTS (6000.3.10f1)**. The Heads side has a simple portrait and the Tails side a feather emblem. Start with 10 virtual coins; a win returns twice the wager. No accounts, payments or real currency.

## Play the WebGL build

From the project folder:

```powershell
.\Tools\serve.ps1
```

Open **http://localhost:8765** in a current WebGL 2 browser. Keep the server running while playing. Opening `index.html` directly through `file://` will not work.

The complete static site is in `Builds/WebGL/`, with an upload-ready archive at `Builds/Coinflip-WebGL.zip`. Upload that directory's contents to any HTTPS static host. Gzip compression with Unity's decompression fallback is enabled; no custom `Content-Encoding` headers are required for `.unityweb` files. Serve the generated `.js` files as JavaScript. Threads are not used, so cross-origin isolation headers are unnecessary.

## Open and build

Open this folder with Unity **6000.3.10f1**, with the **Web Build Support** module installed. The playable scene is `Assets/CoinFlip/Scenes/CoinFlip.unity`.

In the Project panel, open `Assets/CoinFlip/Scenes/CoinFlip.unity` before pressing Play. The same scene is configured for builds.

The project includes Unity's **Device Simulator Devices 1.0.1** package with iPhone 11, 12, and 13 profiles. Open the playable scene, then **Window → General → Device Simulator** to inspect device layouts, safe areas, orientations, and basic touch interaction in the Editor. The simulator does not profile the phone's actual performance or graphics backend.

Use **Coin Flip → Build WebGL**, or:

```powershell
.\Tools\build.ps1
```

The build command regenerates the scene, import settings and static font atlas when needed, then writes `Builds/WebGL/`. Runtime UI is assembled by `UIManager`; no manual scene wiring is needed. `GameSettings.asset` exposes animation timing, launch height and particle pool size.

For a completely fresh bootstrap without the already-included TMP resources, run `CoinFlipEditor.ProjectBuilder.ImportEssentials` in the Editor in batch mode **without `-quit`**; it exits when import finishes. This is not required for the delivered project.

## Controls and persistence

- Choose Heads or Tails. Adjust the wager with − / +. Press Flip coin.
- The main menu opens first. Choose Play to start.
- Tap the pause icon in the game header for Resume, Sound settings, or Main menu.
- Sound settings has separate effects/music sliders, mute, and a fresh-start button. It is available from the main or pause menu.
- After losing the last coin, the primary button starts a new run with 10 coins.
- Wagers range from 1–99; + is disabled at the available balance. Invalid or unaffordable wagers cannot flip.
- Balance, sound settings, recent results and best streak persist locally using PlayerPrefs/IndexedDB. Clearing browser site data clears the save.
- A flip's outcome is drawn exactly once using the uniform integer API `Random.Range(0, 2)` and stored with its wager before animation. Reloading during a flip settles that saved round once.
- A fresh start resets balance, wager, current streak and recent history; best streak and audio preferences remain.

## Code and assets

`GameSession` owns the state machine and economy; `GameManager` coordinates presentation. Animation, UI, pooled VFX, audio and persistence have separate owners. `Assets/CoinFlip/Tests/Editor/` covers economy and recovery behavior.

All coin illustrations, UI shapes and sound effects were authored for this project. `Tools/generate_assets.py` reproduces the artwork and synthesized WAVs using Python/Pillow. The Outfit font is distributed under the included SIL Open Font License (`Assets/CoinFlip/Resources/Fonts/OFL.txt`). Unity and its packages retain their respective licenses.

The interface uses a responsive Canvas Scaler, safe-area handling, portrait/landscape reflow and capped browser pixel density. Coin movement uses only 2D position, scale, sprite swaps and Z-axis tilt. No 3D models, physical coin simulation, postprocessing or paid asset dependencies are used.

## Validation

Run deterministic tests:

```powershell
unity test . --mode EditMode --output Logs/test-results.xml
unity test . --mode PlayMode --output Logs/playmode-results.xml
```

For real browser checks, serve the build, run `npm install` in `Tools`, then `npm test`. The browser harness defaults to the installed Windows Chrome executable; override `CHROME_PATH` or `GAME_URL` if needed. It writes screenshots and a JSON report to `Docs/Evidence/`.

See `Docs/Testing.md` for validation evidence, Unity startup diagnostics, iPhone profiles, and device limitations. iPhone-sized layout and touch flows were checked in Chrome; physical-device performance/audio and native iOS runtime were not measured. Firefox automation could not launch in this environment.
