# Validation — 2026-11-05

**Playable WebGL build delivered, with platform limits and Unity startup diagnostics documented below.** The source project, custom artwork/audio, scene, build, scripts, screenshots and reports are included.

## Build and automated Unity tests

- Unity **6000.3.10f1**, Universal 2D, WebGL, non-development build, managed/engine stripping, disk-size code optimization, gzip plus decompression fallback.
- Final WebGL build: **Succeeded**, **0 errors**, **0 build warnings**, **15,555,001 bytes** including the loading page and generated build files. Files are under `Builds/WebGL/`; `Builds/Coinflip-WebGL.zip` has `index.html` at its root.
- **16 Edit Mode tests passed.** Covers starting currency, selection, both winning faces, stake deduction and 2× return, losing all currency, invalid/unaffordable wagers, one outcome draw per flip, forged/duplicate settlement rejection, in-flight save recovery, corrupt JSON/data, bounded history, best streak, and 1,000 deterministic rounds. Asset tests load every required illustration/SFX and validate the scene's serialized settings/font references.
- **2 Play Mode tests passed.** Runs both predetermined faces through the complete animated flight and landing, then checks the main-menu → play → pause → resume → main-menu flow.
- Evidence: `Evidence/editmode-results.xml`, `Evidence/playmode-results.xml`, `Evidence/build-receipt.json`.

## Real-browser verification

Served the actual Unity build with Python HTTP on `http://127.0.0.1:8765`, then exercised it in installed Windows Chrome through Playwright. This is the WebAssembly build, not an HTML recreation or Unity Game-view mockup.

| Viewport | Result |
|---|---|
| Viewport | Match / result |
|---|---|
| 360 × 780 | iPhone 12 mini profile dimensions; passed |
| 375 × 667 | iPhone SE (2nd generation) dimensions; passed |
| 390 × 844 | iPhone 12/13 Pro dimensions; passed |
| 414 × 896 | iPhone 11 dimensions; passed |
| 428 × 926 | iPhone 12/13 Pro Max dimensions; passed |
| 360 × 640 | Short portrait; passed |
| 768 × 1024 | Tablet; passed |
| 1920 × 1080 | Desktop; passed |
| 1366 × 768 | Laptop; passed |
| 844 × 390 | Landscape reflow; passed |
| 390 × 844 at 2× pixel density | Same CSS layout and real touch taps; passed |

All visible buttons in the main menu, game, and pause menu stayed within each viewport. Touch taps, game controls, menu transitions, and the pause card were checked at the phone sizes.

The final browser suite passed all assertions:

- Repeated Heads/Tails rounds, including both outcomes, checked every balance delta, history entry and final coin face against the actual selection/wager.
- Fifteen rapid Flip taps resolve one round. Attempted side/wager input while airborne leaves them unchanged.
- Main menu and pause menu fit across the checked sizes. Pause stops an active flip; Resume continues it with the same outcome. Returning to the main menu mid-flip settles the stored round exactly once.
- Sampled the live animation: upward motion, horizontal squash and face changes; landing always restores full width and centered position.
- Real browser touchscreen events select a side and flip, at both 1× and 2× density.
- Orientation/viewport change during flight settles correctly.
- A naturally occurring losing all-in round reaches zero and the primary button restarts with 10 coins.
- Both independent volume sliders, mute and currency survive reload.
- Reloading in flight settles the saved outcome once; another reload produces no extra payout or history entry.
- The template's visibility callback pauses/resumes the active animation without changing its result. Full OS/browser process suspension was not profiled.
- Audio source playback was observed after actual user gestures. The final instrumented page observed 48 Web Audio source starts.
- No JavaScript page errors, `console.error` messages, or game exceptions occurred in the final interaction suite.

Round checks completed in roughly **1.6 seconds** against a configured animation duration of 1.55 seconds. Desktop browser frame samples varied and do not establish lower-end phone performance.

Evidence: `Evidence/browser-report.json`, `Evidence/viewport-*.png`, `Evidence/retina-390x844.png`, `Evidence/spin-frame.png`, `Evidence/zero-balance.png`, `Evidence/final-mobile.png`, `Evidence/final-desktop.png`.

## Problems discovered and fixed

- TMP essential resources require an asynchronous first import; bootstrap now waits for completion.
- A method named `Reset` collided with Unity's Editor lifecycle callback; renamed to `RestartRun`.
- Changing scenes during generation unloaded a settings asset. The builder now saves/reloads assets after the scene transition, checks required references, and has an automated regression test. Runtime also checks required configuration explicitly.
- Compact landscape title/result/history regions overlapped; the layout now reserves separate regions.
- The browser's Retina backing-buffer size stabilized after Unity's first layout. Layout now detects pixel-density changes independently of viewport dimensions.
- Browser persistence uses automatic WebGL filesystem synchronization, eliminating Unity's manual-sync deprecation notice.
- The project had no main or pause menu. Added a Play screen, a pause overlay with Resume/Sound settings/Main menu, and a pause-safe settlement path for leaving during a flip.
- Replaced the old celestial coin artwork with a simple Heads portrait and Tails feather mark.

## Unity iPhone profiles

Added Unity's `com.unity.device-simulator.devices` **1.0.1** package to the project. Its included profiles cover iPhone 11, iPhone 12/12 mini/12 Pro/12 Pro Max, and iPhone 13 Pro/Pro Max. In the Unity Editor, open `Assets/CoinFlip/Scenes/CoinFlip.unity`, select **Window → General → Device Simulator**, choose a device, and enter Play mode to check the simulated safe area, orientation, and touch layout. The project was checked at matching iPhone point sizes in Chrome; the Unity Simulator view itself was not run for this validation pass. Unity notes the Device Simulator does not reproduce the target device's graphics backend or performance characteristics ([Unity Device Simulator for iOS](https://docs.unity3d.com/2023.2/Documentation/Manual/ios-device-simulator.html)).

## Known limitations and console diagnostics

- Physical Android phones, iPhones/iPads and Safari were **not tested**. Browser viewport/touch emulation does not prove physical-device compatibility, notch behavior, audio output or frame rate. Unity's iOS build process requires Xcode on macOS to run an iOS app ([Unity iOS environment setup](https://docs.unity3d.com/2023.2/Documentation/Manual/ios-environment-setup.html)); this workstation is Windows, so a native iPhone runtime test was unavailable.
- Firefox automation could not start on this machine (`browserType.launch: spawn UNKNOWN`), including after installing the matching Playwright browser. Firefox runtime behavior remains **unverified**; see `Evidence/firefox-report.json`.
- Unity/driver startup diagnostics remain in Windows headless Chrome: `getInternalformatParameter: invalid internalformat` warnings, and log messages for unsupported unused `Hidden/CoreSRP/CoreCopy` / `Hidden/Universal/HDRDebugView` shaders. These did not prevent the rendered game or functional checks from passing. They were not suppressed.
- Unity's Web audio backend also logs sound-length queries while its compressed platform audio buffers are still decoding during startup. All required clips load and later Web Audio playback is verified; no missing audio references were found. Physical speaker/headphone listening and mobile audio behavior remain unverified.
- The complete diagnostic capture is retained at `Logs/browser-console.log`. The build is functional; this report does **not** claim a warning-free runtime console, bug-free software, or validation on every supported device.
- The linked video could not be fetched during development; implementation follows the supplied written brief and original art direction.

## Reproduce

```powershell
unity test . --mode EditMode --output Logs/test-results.xml
unity test . --mode PlayMode --output Logs/playmode-results.xml
.\Tools\build.ps1
.\Tools\serve.ps1
# In another terminal:
cd Tools
npm install
npm test
```

The local server is left running on port 8765 for review. The build is ready for static hosting; it has not been published to an external host.
