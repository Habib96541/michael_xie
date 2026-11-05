# Hydraulic Press — phone responsiveness and menus

Date: 2026-11-05. Status: **Ready with limitations**. Simulator, Unity, WebGL and browser viewport checks passed. No physical iPhone/Safari was available.

## Problems and corrections

- The original build entered gameplay immediately and had no main menu. It now opens PLAY / SETTINGS, with CONTINUE for an unfinished session.
- The old settings panel could pause only between presses. A dedicated pause button and Escape now open RESUME / SETTINGS / MAIN MENU during animation too. Simulation and gameplay audio pause; closing settings keeps the pause menu paused. Returning home preserves the committed unfinished round.
- Enlarged phone buttons, settings sliders, labels and dialog spacing. Action icons and text have separate space, including CASH OUT. Portrait and landscape layout use device-aware dimensions and safe areas.
- Kept the browser DPR ceiling at 2 after measurement: at an emulated 390 × 844 phone view, DPR 3 averaged 25.4 requestAnimationFrame callbacks per second (p50 41.6 ms, p95 58.5 ms), while DPR 2 averaged 50.4 (p50 19.3 ms, p95 29.7 ms). The DPR 2 canvas is 780 × 1688 and TextMeshPro remains vector distance-field rendered. Normal mode keeps URP scale 1 and 4x MSAA; power saving uses 2x MSAA and fewer effects without reducing resolution.
- Browser diagnostics now expose read-only navigation state and normalized visible button rectangles for touch testing, without exposing hazards.

## Unity Device Simulator

Unity 6000.3.10f1 has the following built-in profiles available; no package installation was necessary.

| Profile | Portrait render pixels | Orientations | Views checked |
| --- | --- | --- | --- |
| iPhone SE (2nd generation) | 750 × 1334 | Portrait and landscape | Main, gameplay, pause, settings |
| iPhone 12 mini | 1080 × 2338 | Portrait and landscape | Main, gameplay, pause, settings |
| iPhone 12 | 1170 × 2532 | Portrait and landscape | Main, gameplay, pause, settings |
| iPhone 12 Pro Max | 1284 × 2778 | Portrait and landscape | Main, gameplay, pause, settings |

All **32 rendered views** passed safe-area bounds and minimum 44-point button target checks. Rendered screenshots were visually inspected for cropping, text overlap, press framing and readability. The mini dimensions above are the installed Unity profile's dimensions. The browser's 375 × 812 at DPR 3 is a separate viewport case.

Evidence: [simulator.jsonl](simulator.jsonl), [SE contact sheet](se2-contact.png), [other iPhones](iphones-contact.png), plus full-resolution `sim-*.png` files. Reproduce with `Tools/iphone_simulator_qa.ps1` in a running Editor Play Mode session. The editor adapter explicitly renders before reading the texture to avoid stale offscreen screenshots. Background execution/focus was enabled only for the automated Editor session.

Unity's [Device Simulator documentation](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/cross-platform-features/device-simulator/introduction) distinguishes simulation of screen, safe area, orientation and touch from actual device GPU/OS execution. This is layout evidence, not an iPhone performance certification.

## Unity tests and scene validation

- **21/21 EditMode tests passed**, including committed hazards, payout calculations, input guards, save corruption and 10,000 randomized rounds. [Results](editmode.json).
- **4/4 PlayMode tests passed**. Startup is a paused main menu; crush/collect cannot start there. New coverage freezes a press mid-animation, checks stationary plate/audio pause, opens/closes settings, returns through home and resumes the exact same committed round. Existing tests cover rapid input/single banking, all twenty deformations and twelve failure/reset cycles with bounded physics objects. The original six-second stress-test wait failed once; a twelve-second test-only wait let the full 12-round sequence complete in 41.6 seconds. [Final results](playmode.json).
- Sole enabled gameplay scene is valid, twenty object definitions present, all new menu icons assigned and **zero missing scripts**. [Scene audit](scene-validation.json).
- Final Unity Console: **0 errors, 0 warnings, compilation succeeded**. [Console](final-console.json), [scene audit](scene-validation.json).

## Browser build and runtime

Final WebGL / IL2CPP player build succeeded with **0 build errors and 0 warnings**, 11,962,747 compressed player bytes. The final script-only rebuild took 3.45 seconds; it contains the final 2x pixel-ratio cap. The packaged ZIP is 11,851,422 bytes across 10 manifested files. All manifest hashes and ZIP CRCs passed. Open the served game at http://localhost:8088. [Build response](build.json), [final player summary](../build-summary.txt).

Final Chrome phone measurement at a 390 × 844 emulated iPhone viewport with browser DPR 3 and player DPR 2 produced a 780 × 1688 canvas. Six crush/retry actions averaged **50.2 requestAnimationFrame timestamps per second** (p50 19.3 ms, p95 29.6 ms); one isolated 239 ms maximum interval occurred during the sample. At player DPR 3, the same machine averaged 25.4/sec (p50 41.6 ms, p95 58.5 ms). The DPR2 result motivated the cap. These measurements used Chrome 156 on Intel UHD Graphics 730/ANGLE Direct3D11, not iPhone hardware. [Final phone pacing](browser-performance-phone.json), [DPR3 comparison](browser-performance-phone-dpr3-diagnostic.json).

Seven browser viewport scenarios passed end-to-end: desktop, portrait, short portrait, landscape, square, tablet and DPR2 retina. They exercise mouse/touch, locked repeated crush/cashout during animation, payout, retry, pause, settings, rotation, persistence and no page scrolling. Four iPhone-shaped Chrome viewports also passed all menu, settings, gameplay, mid-crush pause/resume, cashout/retry, persistence and portrait/landscape checks at final DPR2. The focus-resume check passed. No browser JavaScript errors occurred; Chrome logged the existing WebGL warning `INVALID_ENUM: getInternalformatParameter: invalid internalformat`, without missing visuals or gameplay failure. [Viewport smoke](browser-smoke.json), [phone flows](browser-iphone.json), [focus check](browser-focus.json).

## Coverage limits

No physical iPhone, macOS/Xcode iOS Simulator or Safari is available on this Windows workstation. Unity Device Simulator validates layouts; Chrome mobile viewport emulation validates the actual WebGL build and touch input. Neither measures a real iPhone GPU, iOS Safari memory limits, thermal behavior or OS backgrounding. Hardware smoothness still needs an attached iPhone test.
