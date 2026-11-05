# Pocket Borough — development and validation report

**Status: Ready with limitations.** A real Unity WebGL release was generated, served through HTTP, and exercised through a complete two-minute match, results and replay. Physical mobile hardware, iOS Safari and Firefox were unavailable for device/browser validation.

## Deliverables

- Complete Unity project: `D:\30 games\Hole Arena`
- Main scene: `Assets/PocketBorough/Scenes/PocketBorough.unity`
- Complete deployable WebGL output: `Builds/WebGL/`
- Upload ZIP: `Builds/HoleArena_WebGL_Desktop_Mobile.zip`
- Local preview: http://127.0.0.1:8767/
- Exact server command: `python Tools/serve.py --port 8767`
- Rebuild and package: `Tools/Build-WebGL.ps1` after closing this project's Editor, or use the Pocket Borough editor menu with Web as the active platform.

## Completed gameplay and presentation

The original city-swallowing game has a main menu, animated countdown, real 120-second match, scoring, smooth growth, live eight-player rankings, results, local best-score persistence and immediate replay. Seven separately named and coloured bots independently seek valuable edible targets, avoid larger rivals, pursue smaller rivals, and respawn after being swallowed. Bot movement and mass gain are tuned below the player's rates for an approachable solo match.

The city starts with **1,025 pooled consumable objects** in residential, commercial, downtown, industrial and park districts. Roads, sidewalks, pedestrian crossings and parks are combined into one mesh. Spatial buckets handle local consumption and target selection; a throttled fallback finds remaining targets farther away. Street furniture and vehicles receive pooled restocks at **40 and 80 seconds elapsed**, away from active holes, so late matches retain edible targets. Buildings stay consumed until replay.

The hole uses an eight-opening ground shader, recessed mesh interior and sculpted animated lip. Object claims are exclusive, footprint limits are conservative, and objects rotate, sink, shrink and clip at ground level before rewarding score exactly once. Rival capture shrinks the defeated opening before respawn. Growth sparks and score popups are pooled. The camera follows smoothly, zooms with size and adapts to aspect ratio.

Menus and HUD use uGUI, TextMeshPro, rounded sliced panels and custom colours. There are four rim skins, separate music/SFX controls, instructions, safe-area anchoring, large touch buttons and a portrait rotation notice. Desktop controls are WASD, arrows and mouse drag; Escape pauses. Mobile uses touch drag, preserves the primary steering finger across multi-touch, releases on cancellation, and has a touch pause button. Browser scrolling and text selection are disabled on the game surface. Focus loss pauses gameplay.

Original synthesized audio includes a looping 110 BPM groove, small/large consumption, growth, level-up, click, countdown, victory, defeat and rival capture sounds. Music starts after user interaction. Preferences save locally.

## Original assets

**15 buildings:** gable cottage, garden duplex, balcony apartments, corner market, noodle house, terraced offices, sawtooth warehouse, foundry works, borough arcade, sunset hotel, stepped studios, skygarden residences, crown skyscraper, sail glass tower and borough clock hall.

**10 vehicles:** sedan, hatchback, SUV, pickup, taxi, police cruiser, ambulance, fire engine, city bus and parcel van. Designs include modelled wheel profiles, glazing, lamps, bumpers and identifying body features.

**20 props:** street lantern, traffic signal, cone, bin, hydrant, bench, tree, shrub, flower planter, fence, postbox, wayfinder, bus shelter, food cart, picnic table, sculpture, billboard, barrier, utility pole and fountain.

All models are original custom procedural meshes baked as reusable mesh assets and prefabs, with material regions, shared vertex-colour materials and box trigger colliders. There are no final Unity primitive GameObjects. Architecture includes windows, sills, doors, cornices, gabled or sawtooth roofs, balconies, marquees and landmark features. Every model was rendered from two angles; contact sheets are in `Reports/`. Exact geometry counts and fit/mass/point values are in `asset-inventory.csv`. Typography is Liberation Sans; its license is preserved in the project and upload package.

## Build evidence

Unity **6000.3.10f1**, URP **17.3.0**, Input System **1.18.0**, uGUI **2.0.0**, IL2CPP, WebGL 2, release build, no threads. Unity build result: **Succeeded**, **0 errors**, **0 warnings**, 125.5 seconds for the final incremental build.

The generated loader, data, framework and WebAssembly payloads are present. Gzip with Unity's JavaScript decompression fallback allows ordinary static hosting without special Content-Encoding headers. Initial heap is 128 MB with geometric growth up to 512 MB; that ceiling is a limit, not a measured usage claim. Managed stripping is Low to preserve runtime/UI behaviour.

Deployable files total **16.67 MB** on disk; ZIP **16.18 MB**. The full ZIP was read back and its CRC verified. `index.html` is at the ZIP root. Per-file SHA-256 hashes are in `package-manifest.json`.

Archive SHA-256: `c951f7f3489e4b666ca0cd37e8073c0579c39066fea002902226389aedd5dc8a`.

## Executed tests

- EditMode: **6/6 passed**, 0 failures. Footprint restrictions, monotonic growth and cap, rival size threshold, all 45 models' finite geometry/normals/pivots, catalog references, duplicate claims/rewards, restock exclusivity and full pool reset.
- PlayMode: **1/1 passed**, 0 failures. Scene startup, countdown, autonomous bot scoring, player consumption/growth, exact paused time/position, resume, results, saved best score and reset of all objects, scores, radii and match time.
- Browser release: **19 passing checks**, **0 browser console/page errors**. Complete 120-second Chrome match plus results, replay, mouse steering, mobile touch/multi-touch/cancellation/pause, viewport/orientation checks and Edge startup/arrow movement.

| Browser check | Result |
|---|---|
| Chrome desktop loads over HTTP | Passed |
| Countdown enters live gameplay | Passed |
| WASD moves player | Passed |
| Pause freezes timer and player | Passed |
| Complete 120-second match reaches results | Passed |
| Consumption awards score and growth | Passed |
| Seven AI rivals score independently | Passed |
| Two street drops sustain late match targets | Passed |
| Replay resets score timer radius and entire city | Passed |
| Mouse drag steers on replay | Passed |
| Android Chrome emulation loads over HTTP | Passed |
| Touch drag moves without keyboard | Passed |
| Touch cancellation releases steering | Passed |
| Second touch does not take over steering | Passed |
| Touch pause works without moving player | Passed |
| Portrait layout has no page scrolling | Passed |
| Edge desktop loads over HTTP | Passed |
| Edge WebGL loads and arrow keys move | Passed |
| No browser runtime errors | Passed |

The Chrome input-driven test finished with **11,958 points**, **384 objects**, rank **5/8**, final radius **8.97 m** and **2 street drops**. The test drove keyboard input toward edible targets; it did not invoke an autoplay cheat or skip the 120-second browser match. The short PlayMode test accelerates only its end-of-match check.

## Measured performance and limits

Test host: **Intel Core i5-12400 (6 cores/12 threads), Intel UHD Graphics 730, approximately 32 GB RAM**, Windows. Browsers: `{"chrome": "156.0.8078.49", "msedge": "156.0.4314.50"}`. Browser automation used installed Chrome and Edge in headless mode with WebGL enabled.

During the complete desktop match at 1280×720, periodic in-game frame samples ranged **60.0–61.0 FPS**, median **60.0 FPS**. Managed heap samples ranged **4.33–5.57 MiB**. These are sampled measurements, not per-frame minimums or total browser/GPU memory measurements. Loading was tested over localhost and does not predict Internet download time.

Mobile preliminary checks used Chromium touch emulation at 844×390 and layout captures at 780×360 (19.5:9), 800×360 (20:9), 1280×720 (16:9) and 390×844 portrait. Responsive menus, touch targets and the rotation notice were visually inspected. Emulated mobile rendering uses this desktop GPU: **physical Android/iPhone frame rate, thermal behaviour, memory pressure, Safari compatibility and display-cutout behaviour remain unverified**. Firefox was not available for an actual runtime check.

Desktop and mobile URP assets use shared materials, instanced reusable meshes, frustum culling, one main light, limited shadow casters and a single combined ground mesh. Mobile uses lower render scale, smaller shadow maps, hard shadows and disabled post-processing. Adaptive scale can reduce to 0.65 when frame rate falls below the threshold. Gameplay uses no rigidbody simulation and replay reuses the same city object pool. No platform SDK or backend is required.

## Corrections and remaining work

Validation found and corrected compile/reference issues during initial construction, a missing sliced UI sprite, a clipped button label, an edge-target reachability issue, floating-label coordinate conversion, overly sparse late matches and Unity's non-cancellable touch-cancel preventDefault warning. Earlier failure evidence is preserved in `browser-first-pass.json` and the Editor log. The final build/test receipts are separate from that history. The final Unity Console has no active compile errors or warnings.

Before advertising support on specific mobile devices, run the packaged release on representative Android Chrome and iPhone Safari hardware. Test Firefox as an additional desktop browser. The verified static build is now published on Cloudflare at https://hole-arena-pocket-borough.am-ome.workers.dev/ (see cloudflare-deployment.json for the live browser check). Keep all build files together and serve through HTTP/HTTPS.

## iPhone follow-up — November 5, 2026

Four iPhone viewport/touch profiles passed main-menu, pause/resume, portrait auto-pause and safe-area checks. The HTML wrapper now pauses on portrait rotation and reserves browser-reported cutout margins. See iphone/REPORT.md and iphone/results.json for coverage and measured limits. The upload ZIP and manifest include these fixes.
