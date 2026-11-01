# HI / LO — Endless

A Unity 6 card game. Predict the next card, build a streak, and collect before your luck changes. Virtual coins only.

## Play the included build

Run `Play.ps1`, or run `python Tools/serve.py` from this project. Open **http://localhost:8765**. Keep the server running. Opening `index.html` directly as a file does not work.

The same `WebGL_Build` runs on desktop and mobile. To play on a phone on your local network, use this computer’s LAN IP with port 8765 while the server is running, if your existing firewall rules permit it. Static hosting is the easiest way to share it outside your network.

## Controls and rules

- Click/tap **↑ Higher** or **↓ Lower**. Keyboard: Up/W, Down/S.
- **Cash out** collects the displayed potential. Keyboard: Space.
- Each round deducts 10 virtual coins. The multiplier starts at 1.00×.
- Ace is high: 2 through A. Equal ranks push; the new card becomes current and the multiplier stays.
- Wrong answers and timeouts end the round and lose its potential.
- After the 52nd card, a fresh shuffled deck continues the same round.
- A 200-coin starting balance and free 100-coin refills prevent a permanent dead end.
- Settings control sound, music, reduced motion, and the optional streak timer. Timers pause in settings and while the browser tab is hidden. Reloading forfeits an active round’s stake; statistics and balance remain saved.

## Fairness and progression

Fisher–Yates shuffles use a random source independent of choices and streaks. The engine never searches for a winning or losing next card. Displayed chances use **all remaining cards**, including equal ranks. Each button’s `+%` shows the multiplier growth on a correct answer.

For `W` winning, `L` losing, and `E` equal cards, the gain factor is `max(1 + minimumGrowth, 1 + (L / W) × (1 − edge))`. The default risk adjustment is 4%, with a configurable 1% minimum gain so every correct prediction grows the multiplier, including guaranteed choices. Ties retain the existing multiplier. Impossible winning directions are disabled; if only equal cards remain, both buttons allow pushes until the fresh deck. Cash out is always available during a decision window, including before the first prediction.

Stages begin at 0, 5, 10, 20, and 40 correct predictions. The initial stage has no timer; later windows are 16, 11, 8, and 7 seconds. Endless mode subtracts 0.3 seconds per ten additional wins, with a 4.2-second minimum. No final level or streak cap exists. Turning off the timer provides an untimed mode.

Multipliers and balances use scientific numbers with a double significand and an arbitrary-size `BigInteger` exponent. Streaks and lifetime counters use `BigInteger`. Arithmetic retains approximately 15 significant digits; adding a stake to an astronomical balance can fall below that precision. Compact notation switches to scientific notation beyond quadrillions. This is a virtual economy, not an exact financial ledger.

## Project

- Editor: **Unity 6000.3.10f1**, Universal 2D / URP 17.3.
- Start scene: `Assets/Scenes/HiLo.unity`.
- Config: `Assets/HiLo/Resources/GameConfig.asset` (stake, economy, edge, stages, pacing, minimum timer).
- Pure game engine: `Assets/HiLo/Core`.
- Runtime presentation, responsive layout, input, audio, saves: `Assets/HiLo/Runtime`.
- Original 52-card set, back, and icons: `Assets/HiLo/Resources/Art`.
- Outfit font: SIL Open Font License; license included next to the font.
- Art source generator: `Tools/generate_art.py` (requires Pillow).

The interface uses a Canvas Scaler, anchored safe-area root, portrait and short-landscape breakpoints, fixed card aspect ratios, sliced rounded panels, and a viewport-aware HTML template. Fonts are SDF; card and icon textures have no mipmaps. A pixel-ratio cap of 2 bounds rendering cost. URP post-processing is disabled for the screen-space card interface to keep edges and typography crisp.

No ads, purchases, accounts, external analytics, or paid asset packages are added by the game. PlayerPrefs stores local balance, best streak, best multiplier, predictions, rounds, cashouts, and preferences; browser storage availability and clearing affect retention.

## Rebuild

Open the project in Unity 6000.3.10f1 with **Web Build Support** installed. Use **HiLo → Prepare project** to regenerate the start scene, import settings, font atlas, and player configuration. Use **HiLo → Build WebGL** to output `WebGL_Build`. The scene intentionally builds its UI hierarchy at runtime. After repeating the browser checks, `python Tools/package_build.py` bundles the font notices, verifies the evidence against the current build, and generates `HiLo-WebGL.zip`.

With the Unity CLI:

```powershell
unity run . --editor-version 6000.3.10f1 -- -executeMethod ProjectBuilder.Prepare -nographics
unity test . --editor-version 6000.3.10f1 --mode EditMode --output Validation/editmode.xml
unity run . --editor-version 6000.3.10f1 --timeout 1800 -- -buildTarget WebGL -executeMethod ProjectBuilder.Build
```

Run batch commands with the project closed in the interactive Editor. Keep the complete `Assets`, `Packages`, and `ProjectSettings` folders when moving the source project. Unity regenerates `Library`.

## Deploy

Upload the **contents** of `WebGL_Build` to static hosting, with `index.html` at the root. Gzip builds use Unity’s JavaScript decompression fallback (`.unityweb`), so special content-encoding headers are not required. Serve over HTTPS for public deployment. Preserve filenames and relative `Build/` paths. For itch.io, upload `HiLo-WebGL.zip` as an HTML game and enable mobile support; allow the game to fill its embed and offer fullscreen. Other portals may require their own SDK or review before publication.

## Validation

See `Validation/TEST_REPORT.md` for the actual build, automated tests, viewport coverage, screenshots, and remaining device limitations. Browser emulation does not establish physical Android or iOS compatibility or a universal 60 FPS guarantee.

To repeat the browser checks on Windows, run `npm ci --prefix Tools`, then `node Tools/browser_test.cjs`. The default executable is installed Microsoft Edge. Set `HILO_BROWSER` to Chrome’s executable path to test Chrome. The script uses a clean browser context, real UI input and the production shuffle; it records JSON results and screenshots in `Validation`. It never injects a winning deck or changes the game’s balance or streak.

Unity reference documentation: [Web Canvas sizing](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/webgl/develop/canvas-size) and [Web deployment and compression](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/webgl/building-distribution/deploying).
