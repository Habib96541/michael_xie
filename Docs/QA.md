# Brick Rush verification

Status: **Ready with validation limitations.** The Unity project and HTTP-hosted WebGL release are playable in the tested desktop browsers and touch emulation. Physical Android/iPhone browser checks and mobile hardware performance remain unverified.

## Build and environment

- Unity **6000.3.10f1**, Universal 2D / URP 17.3.0, Input System, uGUI + TextMeshPro.
- Release WebGL build: **Succeeded**, **0 errors**, **0 build warnings**, 102.2 seconds.
- Distribution: **13.92 MB**, compared with the initial 59.35 MB build (about 77% smaller).
- Gzip + decompression fallback, Disk Size optimization, high stripping, smaller IL2CPP code generation, 64 MB initial / 512 MB maximum memory.
- Tested through **http://127.0.0.1:8187/**, never through `file://`.
- Chrome **156.0.8078.49**: **36 checks passed**.
- Edge **156.0.4314.50**: **36 checks passed**.
- **29 Editor checks passed**, including 1,000 direction samples, score thresholds, armor, duplicate destruction, all eight layouts, timed evolution, high-speed circle casts, corner normals, adjacent-brick seams and completion before transition.

## Acceptance evidence

| Criterion | Status | Evidence |
| --- | --- | --- |
| Complete menu / countdown / play / results / replay | Passed | Browser UI and state checks; inspected screenshots |
| 90-second timer / expiration | Passed | Full unaccelerated session, plus arranged expiry |
| Score / lives / combo / final life bonus | Passed | Rules and browser checks |
| Paddle and reliable ball collisions | Passed in tested environment | Mouse, keyboard, paddle return, swept corner/seam casts |
| All brick types and eight layouts | Passed | Rules, timed evolution and browser chain/wave scenarios |
| Seven powers, multiball cap, shield | Passed | Real catch logic with arranged drops; capped spawns and bottom-loss reflection |
| Wave transition with active powers | Passed | Wave clear continues play and retains refreshed wide-paddle effect |
| Simultaneous final-ball loss / life exhaustion | Passed | Arranged losses, one life decrement, 3 -> 2 -> 1 -> 0 |
| Pause / resume / background hook | Passed | Escape, resume button, frozen timer and dispatched blur hook |
| Saved best and preferences | Passed | Reload in the same browser origin |
| Independent music / SFX / audio unlock | Passed | Real UI gestures, separate volume values and running AudioContext |
| Offline play after loading | Passed | Browser network disabled while the timer continues |
| Desktop and portrait layout | Passed | 1440x960 and 390x844 screenshots, resize and no page overflow |
| Touch drag and interruption | Passed in emulation | Chromium mobile contexts, touch movement and cancellation |
| High-DPI touch below the arena | Passed in emulation | Device pixel ratio 3, canvas capped to 1.6x (624x1350), correct paddle mapping |
| Neon art, fonts, trails, VFX and UI | Visually inspected | Desktop play, menus/settings/help/results, portrait and touch captures |
| WebGL distribution / HTML loader / static hosting | Passed | Complete compressed distribution, HTTP load, licenses and deployment guide |
| Chrome Android / Safari iOS on physical devices | Not run | No attached physical devices or Safari environment |
| Mobile 30/60 FPS targets | Not established | Requires representative Android and iPhone hardware |

## Full-session timing and rendering sample

The natural-session test used automatic paddle steering to avoid premature life exhaustion. It did **not** accelerate time, inject score, clear waves or force timer expiry. The game ran with normal visual effects and audio enabled.

- Survived **90 gameplay seconds**; results appeared after **94.0 wall-clock seconds**, including countdown and test sampling.
- Gameplay points **3485**, final score **3785**, bricks **124**, waves completed **2**, lives **3**.
- Sampled **10663** browser animation frames after warm-up: **120.0 FPS average**, **8.4 ms p95**, **8.5 ms p99**.
- Renderer: `ANGLE (Intel, Intel(R) UHD Graphics 730 (0x00004692) Direct3D11 vs_5_0 ps_5_0, D3D11)`.

This is a measurement of this desktop automation environment, not proof of mobile hardware performance. A separate browser scenario exercised five balls and multiple simultaneous powers.

The timing sample preceded the final overlay-only cleanup (clearing a lingering celebration on results and resetting the replay score display). Playing-state physics, audio, VFX and timing code are identical. The sample build receipt is retained separately; functional browser checks were repeated on the final release.

## Limitations and diagnostics

No JavaScript or C# runtime exceptions were observed in the completed browser suites or full session. Unity reports startup WebGL `INVALID_ENUM` capability-query diagnostics and unsupported **unused** built-in CoreCopy/HDRDebugView shaders on the available graphics backend. The custom arena, trail, sprite and text materials render in inspected captures. Full diagnostic text is retained in the browser receipts; these messages are not hidden as a clean runtime-warning result.

Physical notches, orientation changes on real devices, thermal behavior, low-memory Safari behavior, audio mixing through actual speakers, OS interruptions and prolonged device-specific play remain to be checked. CSS safe-area insets, unified touch controls, cancellation handling and DPI limits are implemented.

The browser suite uses real pointer, keyboard and touch events for UI/input. Query-gated QA commands arrange power catches, shield and final-ball loss, late-session, wave-clear and timer-expiry scenarios. Those arrangements are distinguished from the full natural-time session.

## Reproduction

```powershell
python Tools/serve.py --port 8187
npm ci --prefix Tools
node Tools/browser-qa.cjs chrome
node Tools/browser-qa.cjs edge
node Tools/session-qa.cjs
node Tools/mobile-qa.cjs
python Tools/write_report.py
```

Run the browser performance sample after other automated browser suites finish. The Editor check method is `BrickRush.Editor.RuleValidation.Run()`. Build using **Brick Rush > 3. Build WebGL**. Machine-readable receipts are committed under `Docs/Validation/`; screenshots and additional local logs are in `TestResults/` and `Logs/`.
