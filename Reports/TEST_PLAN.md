# Acceptance test plan

Baseline: empty workspace; no existing project, tests, scenes or game artifacts. Created with Unity 6000.3.10f1 and the installed URP template. Build target: WebGL 2 / IL2CPP, release, no threads. Unity CLI controls a resident batch Editor; actual browser rendering validates the final user interface.

| Area | Verification |
|---|---|
| Movement | Browser keyboard, arrows, mouse drag; position telemetry |
| Touch | Chromium mobile emulation, drag, cancellation, pause; screenshots |
| Size restrictions | EditMode footprint and rival size boundary tests |
| Consumption | Duplicate claim/reward prevention, complete fall, score and growth tests |
| AI | Seven independent scores and autonomous growth during live match |
| Growth | Monotonic bounded radius, size levels, render inspection |
| Match | Countdown, real two-minute clock, final ranking and results |
| Pause | Timer and position remain fixed; resume continues |
| Replay | Full object pool reset, zero scores, original radius and 120-second clock |
| Assets | All 45 meshes checked for finite geometry, pivots and normals; two-angle contact sheets |
| UI | Menu, instructions, settings, HUD, results and safe-area anchoring |
| Layout | Desktop, 16:9, 19.5:9, 20:9 and portrait rotation notice |
| Camera | Follow and smooth growth zoom in live match |
| Audio | Original music and nine synthesized effects, separate volume preferences |
| Browser loading | Actual Unity loader/data/framework/WebAssembly over HTTP |
| Runtime errors | Browser console/page exceptions and Unity Console |
| Performance | Browser frame-rate and managed heap samples throughout a match; build size |
| Packaging | ZIP structure, content hashes, local server and hosting instructions |

Physical Android, iPhone Safari and Firefox remain separate device/browser coverage when unavailable. Runtime tests and screenshots do not establish those platforms' performance.
