# Hydraulic Press — delivery QA

Date: 2026-11-02. Status: **Ready with the device-testing limitations below.**

## Build and artifacts

- Unity 6000.3.10f1 LTS, URP 17.3.0, WebGL / IL2CPP.
- Final Unity build: **Succeeded, 0 errors, 0 warnings**, 6 minutes 42.7 seconds.
- Compressed player payload: **11,886,044 bytes**, compared with the first 57,165,240-byte uncompressed build (79.2% smaller).
- Output: `D:/30 games/Hydraulic Press/Builds/WebGL`.
- Portable ZIP: `D:/30 games/Hydraulic Press/Builds/HydraulicPress-WebGL.zip` (11,775,681 bytes).
- HTML, loader JavaScript, compressed framework JavaScript, WebAssembly and data files are present.
- The build includes a standalone Python HTTP server, Windows launcher, asset credits, Liberation Sans license and SHA256 manifest. All ten manifest hashes and ZIP CRCs passed verification.
- The supplied server was launched directly from the packaged build folder and the game was loaded and exercised through it. It remains available at http://localhost:8088.
- Raw HTTP verification confirmed `application/wasm` and `Content-Encoding: gzip` for the compressed WebAssembly file. External hosting needs the same encoding/MIME configuration.

## Automated Unity validation

**21 EditMode tests passed, 0 failed, 0 skipped.** Evidence: `editmode-tests.json`.

This includes 10,000 round generations with twenty unique object indices and exactly three hazards; position-frequency checks; seed reproducibility; sequence immutability; action locking; first cash-out of exactly 112 points; failure loss; perfect completion at seventeen successes and 109,440 points; survival probability calculations; monotonically increasing multipliers; invalid configuration; corrupted JSON; save round-trip; and lifetime-score overflow handling.

**3 PlayMode tests passed, 0 failed, 0 skipped.** The suite was rerun after the graphics changes. Evidence: `playmode-tests.json`.

- Rapid crushes, collection and restart requests cannot overlap an active crush. A successful collection banks exactly once and automatically starts another round.
- Twelve failure/restart cycles restore the machine, clear fragments, retain twenty object instances and keep Rigidbody counts constant.
- All twenty pooled object instances deform and reset. Opening settings pauses gameplay and blocks crush input; closing settings resumes it.

The final scene audit confirms the sole gameplay scene, twenty definitions, the intended URP assets, and valid press/rounded/check icon references. The final Unity Console has no current compile errors, errors or warnings; retained tool history includes development errors that were fixed before the passing builds.

## Actual browser validation

Installed Windows browsers tested through Playwright, headless:

- Chrome **156.0.8078.79**.
- Edge **156.0.4314.58**.

Each browser passed all six viewport scenarios:

| Shape | CSS viewport | Input |
| --- | --- | --- |
| Desktop | 1440 × 900 | Mouse |
| Portrait | 390 × 844 | Emulated touch |
| Short portrait | 320 × 568 | Emulated touch |
| Landscape | 844 × 390 | Emulated touch |
| Square | 600 × 600 | Mouse |
| Tablet shape | 800 × 600 | Emulated touch |

Chrome also passed an additional 390 × 844 touch scenario at DPR 2, verifying an actual **780 × 1688** canvas. **13 viewport scenarios passed with zero browser errors.** Evidence: `browser-smoke.json`, `edge/browser-smoke.json`, `retina/browser-smoke.json`, and their screenshots.

The checks use actual button clicks/taps for crushing, rapid repeated input, attempted collection during crushing, cash-out, result retry, settings, audio-slider input and power-saving toggles. They also verify automatic rounds, failure without changing banked points, saved bank values after page reload, live resizing/rotation and absence of page scrolling. Representative impact, collection, failure, settings and responsive screenshots were visually reviewed. The square-window and short-portrait overlaps found during QA were fixed and the corrected build was retested.

A separate focus-switch check against the packaged server held an active crush across a 3.2-second focus change and resumed it afterward without errors. In this headless test `document.visibilityState` remained `visible`; this establishes focus-loss/resume behavior, not physical-device backgrounding. Evidence: `browser-focus.json`.

## Frame pacing and bounds of the evidence

A separate isolated Chrome headless sample at 1280 × 720 exercised six crush inputs on Intel UHD Graphics 730 through ANGLE / Direct3D11. Deduplicated requestAnimationFrame timestamps averaged 71.8 per second over 14.46 seconds; median interval was 10 ms, 95th percentile 33.3 ms and maximum 200.1 ms. This is browser callback pacing, **not a certified measurement of displayed game FPS**. Earlier concurrent-run diagnostics are retained separately. Evidence: `browser-performance.json`.

Fixed pools and stable Rigidbody counts were validated. A long-duration heap/memory soak and physical-device FPS certification were not performed. Default Web rendering follows browser pacing; power saving lowers render resolution, disables shadows/occlusion, halves fragment bursts and renders every second browser frame.

## Remaining limitations

- **No physical Android or iPhone was attached.** Emulated touch and viewport checks do not establish physical mobile-browser performance, notch behavior or OS backgrounding behavior.
- Safari and Firefox were unavailable and were not tested. Mobile and cross-browser certification remains device work.
- Chrome and Edge emit the startup warning `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat`. No associated browser errors, missing visuals or gameplay failures were observed. This warning remains in the browser logs and is not suppressed.
- Audio assets and their control/input paths were validated; there was no calibrated speaker/headphone listening test.

The game uses virtual points only. No payment, wager purchase, deposit or withdrawal service exists.
