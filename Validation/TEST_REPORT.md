# HI / LO — validation report

Recorded 2026-11-01 09:57 (UTC+06:00). **Ready with limitations.**

The release WebGL build loads and is playable through the local HTTP server. The source workspace began empty; this is a new Unity project.

## Build and artifacts

- Unity 6000.3.10f1, URP 17.3, WebGL 2 / IL2CPP, release build.
- Build result: succeeded, 0 build errors and 0 build warnings. See [build-report.txt](build-report.txt) and the local `webgl-build.log`.
- Static build: `../WebGL_Build`; deployment archive: `../HiLo-WebGL.zip`.
- 6 files; 14.78 MiB on disk; ZIP 14.35 MiB.
- Required files, gzip integrity, ZIP CRCs, and absence of unexpanded template tokens verified.
- SHA-256 checksums: [build-manifest.json](build-manifest.json). Both full browser reports match this final WASM hash: `553647d972c625c0eba4cecd6a3b7d81efbc8f4ff234b2036b8abd2ba5925387`.
- Preview: [localhost:8765](http://localhost:8765). Keep the local server running, or run `Play.ps1` again.

## Gameplay and persistence

- **23/23 EditMode tests passed**, no skipped tests. [NUnit results](editmode.xml).
- Coverage includes deck uniqueness, seeded shuffle reproducibility, exhaustion, fresh-deck continuation, higher/lower, pushes, an all-equal deck remainder, Ace-high ranking, impossible choices, probability-based gains, guaranteed-choice growth, cash out, wrong answers, timeout, double-input rejection, restart, free refills, and exact 20-stake depletion from 200 coins.
- A 10,000-prediction test remained playable without a streak cap. A 100,000-multiplication test retained finite significands; arbitrary-size exponents and scientific formatting were checked.
- **32 browser assertions passed** across Edge 156.0.4314.50, Chrome 156.0.8078.79. [Edge results](browser-edge.json), [Chrome results](browser-chrome.json).
- Production-card draws yielded correct, wrong and equal outcomes. Actual UI input exercised mouse predictions, phone touch predictions and cash out, keyboard cash out, settings, saved balance/preferences after reload, and rapid clicks accepting exactly one prediction.
- Timed stages were reached using the production shuffle. Settings held a live decision window paused for 18 seconds; closing settings allowed it to expire and end the round without predicting.
- Additional keyboard/refill runs exhausted exactly 20 stakes, reached a zero balance, added 100 free coins, and restarted with 90 coins. [Edge refill](refill-edge.json), [Chrome refill](refill-chrome.json). Keyboard Up/Down was used for each prediction.
- Browser tests use isolated profiles and UI input. They do not replace the shuffle, inject streaks, or edit balances.

## Responsive checks

The same build was resized during play. Canvas dimensions followed the viewport, with no horizontal document overflow. The eight Edge layouts were visually inspected; equivalent Chrome captures were also recorded.

| CSS viewport | Edge | Chrome |
|---|---|---|
| 1920 × 1080 | Passed | Passed |
| 1366 × 768 | Passed | Passed |
| 1280 × 720 | Passed | Passed |
| 390 × 844 | Passed | Passed |
| 360 × 800 | Passed | Passed |
| 844 × 390 | Passed | Passed |
| 768 × 1024 | Passed | Passed |
| 1024 × 768 | Passed | Passed |

Additional touch checks passed at pixel ratio 2 on 844×390, 390×844, 768×1024 and 1024×768; and at pixel ratio 3 on both phone orientations. The Unity render target stayed at or below the configured ratio cap of 2. These checks caught and verified the fix for selecting the phone landscape layout using CSS height rather than render-buffer pixels. [Edge density](density-edge.json), [Chrome density](density-chrome.json).

All captures are in [screenshots](screenshots), including phone results, settings, timed play, timeout, bankruptcy and refill.

## Limits and observed warnings

- Physical Android, iPhone and iPad devices were unavailable. Emulated touch and viewport/density checks do not establish device performance, Safari behavior, notch handling, or browser-chrome behavior on those devices.
- Firefox 157 was downloaded through Playwright, but Windows refused to spawn its executable (`spawn UNKNOWN`). No Firefox runtime tests are claimed. Safari was unavailable.
- Both Chromium browsers emitted `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat` during initialization. This remains an observed graphics-format query warning. The final tested flows had no JavaScript exceptions, failed network requests or console errors.
- Local loading plus the test's fixed three-second stabilization wait took 6.01s in Edge and 6.81s in Chrome. This is not an internet download benchmark.
- `frameStats` records browser requestAnimationFrame cadence, not Unity FPS. The game targets 60 FPS, but a universal 60 FPS claim, full WebAssembly/GPU memory profile, and low-end mobile benchmark were not established.
- Later difficulty values and long streaks were verified in logic tests. The browser runs reached the first timed stage; no manual 40-win browser streak is claimed.
- Sound playback code ran after user input and settings persisted. Subjective audio quality and physical speaker behavior were not separately verified. Browser fullscreen was not exhaustively tested across platforms.

## Reproduce

Use the project and browser commands in [README](../README.md). Run `node Tools/refill_test.cjs` for the bankruptcy flow and `node Tools/density_test.cjs` for high-density touch layouts. After successful tests, run `python Tools/package_build.py` to rebuild the archive, notices, manifest and this report.
