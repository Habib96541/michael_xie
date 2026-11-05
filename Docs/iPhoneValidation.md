# iPhone responsiveness validation

Validated **2026-11-05**, Unity **6000.3.10f1**, Windows, current WebGL release. Main menu and pause menu are present and functional. Main menu provides Play, How to Play and Settings. Pause provides Resume, Settings and Main Menu; the session timer stays frozen while paused.

## Bugs fixed

- The HTML fullscreen control covered Pause on short portrait phones. On touch screens it now sits at the top safe-area edge.
- Landscape phones selected the desktop presentation and made menus and controls too small. Touch-aware presentation now uses two-column landscape menus, larger portrait buttons, and a separate landscape Pause control.
- Rotating Unity's Device Simulator could leave the camera viewport at the previous Canvas size and stretch the arena. Layout now follows simulated and native render dimensions, safe area and Canvas geometry; the camera follows the arena using a reused corner buffer. Arena proportions remain 9.9:14.6.
- Modal pages now block underlying UI, and Pause is hidden while menus are open.

## Unity Device Simulator

The installed Editor includes these four iPhone profiles. All passed in **portrait and landscape**:

| Built-in profile | Orientations | Checks |
| --- | --- | --- |
| Apple iPhone SE (2nd generation) | Both | Passed |
| Apple iPhone 12 mini | Both | Passed |
| Apple iPhone 12 | Both | Passed |
| Apple iPhone 12 Pro Max | Both | Passed |

Eight combinations produced **32 page audits** covering Main Menu, Pause, Settings and How to Play. Assertions check that buttons remain inside the simulated safe area, mobile presentation is active, the arena retains its proportions, and Pause freezes the timer. Settings returns to Pause, and Main Menu/Help navigation returns correctly. The iPhone 12 landscape profile, for example, reports a 2532x1170 display with a safe area of x=141, y=63, width=2250, height=1107.

Simulator navigation checks invoke the actual uGUI button callbacks. A separate native Simulator click also started the game. Browser checks below use actual emulated touchscreen events throughout navigation and paddle movement.

Only the Simulator should be open as the active game view during these checks. An active Game view can disable simulation; this was a test setup issue encountered during validation. The Editor-only reflection adapter is pinned to this Unity version and is excluded from the player.

## WebGL touch and layout checks

Chrome **156.0.8078.80**, iPhone user agent/touch context, device pixel ratios 2 or 3. **14 of 14 size/orientation cases passed**:

| Viewport label | Portrait CSS size | Device pixel ratio |
| --- | --- | --- |
| Small SE-size screen | 320x568 | 2 |
| SE (2nd generation) | 375x667 | 2 |
| 12 mini | 375x812 | 3 |
| 12 | 390x844 | 3 |
| 12 Pro Max | 428x926 | 3 |
| 14 Pro | 393x852 | 3 |
| 15 Pro Max | 430x932 | 3 |

Each viewport also runs with width/height exchanged. Checks cover real touch Play, Pause during countdown, frozen timer, Settings/return, Resume into gameplay, touch drag, Pause/Main Menu, Help/return, and Results/Main Menu. Results are arranged with query-gated timer expiry. Every visible game button is checked for safe-area containment, at least 43.5 CSS pixels of height (44 with fractional rounding tolerance), no overlap with the HTML fullscreen control, and correct arena proportions. Screenshots include all five pages in both orientations.

The labels above describe viewport dimensions; Chromium emulation does not reproduce an iPhone's Safari engine or hardware. Unity's simulator supplies the notch/safe-area checks; Chromium's emulated CSS environment does not reproduce all physical cutouts.

The complete gameplay regression suite also passed **36 checks in Chrome** and **36 in Edge 156.0.4314.58**, including touch cancellation, pointer/keyboard input, powers, waves, scoring, life exhaustion, persistence, pause/resume and results. **29 Editor rule/collision checks passed**. The rebuilt release reports **0 errors and 0 build warnings**.

## Smoothness sample

A separate **20-second** stress scenario runs the current WebGL build at iPhone 13 viewport dimensions, DPR 3, with automatic paddle steering. It repeatedly grants multiball plus wide paddle, fireball, lasers and shield; five-ball samples were observed. The build caps rendering density at 1.6, yielding a 624-pixel-wide canvas for the 390 CSS-pixel viewport. Other browser suites had finished before sampling.

| Measurement | Result |
| --- | --- |
| Browser animation frames after warm-up | 1,934 |
| Average browser frame rate | 101.3 FPS |
| p95 frame interval | 10.1 ms |
| p99 frame interval | 23.6 ms |
| JavaScript / Unity runtime errors | 0 |
| Renderer | Intel UHD Graphics 730 through ANGLE / D3D11 |

This measures browser frame cadence on this Windows PC. It is not a Unity Profiler CPU/GPU capture or an iPhone hardware benchmark. The earlier unaccelerated 90-second run remains documented separately in [QA](QA.md).

## Reproduce and inspect evidence

With this project open in Unity, enter Play Mode with Device Simulator as the sole active game view, then run:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/iphone-simulator-qa.ps1
```

With the release served locally, run the functional checks first and the performance sample separately:

```powershell
python Tools/serve.py --port 8187
npm ci --prefix Tools
node Tools/iphone-responsive-qa.cjs
node Tools/browser-qa.cjs chrome
node Tools/browser-qa.cjs edge
node Tools/iphone-smoothness-qa.cjs
```

Committed receipts and representative captures are under [Validation/iPhone](Validation/iPhone/). Full local captures remain under `TestResults/iPhone/`; the build receipt and browser regression receipts are under `Docs/Validation/`.

Physical iPhone/Safari, thermal behavior, low-memory reloads, OS interruptions and sustained mobile hardware frame rate remain unverified. Windows does not supply Apple's iOS Simulator. Unity's Device Simulator is suitable for safe areas, rotation and touch layout; it does not simulate real device performance. See [Unity's Device Simulator documentation](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/cross-platform-features/device-simulator/introduction).
