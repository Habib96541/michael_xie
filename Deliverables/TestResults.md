# Geometry Dash — verification report

## What was verified, and how

The authoring environment had **no Unity install**, so the project is split deliberately:

- `Assets/Scripts/Core/` is pure C# with **no UnityEngine references**. It is compiled and executed
  headlessly by a .NET 8 test harness.
- `Assets/Scripts/Game/` is type-checked against real UnityEngine + UGUI reference assemblies.

That means the *rules* are genuinely tested and the *rendering/UI code* is genuinely compiled — but
no frame of the game has been rendered, played or listened to. See "Not verified" at the end.

| Check | Command | Result |
|---|---|---|
| Simulation, physics, seeding, scoring, records | `cd Tests/LogicTests && dotnet run -c Release` | **70 passed, 0 failed** |
| Playability sweep (autopilot plays real seeds) | same run | **300/300 seeds cleared, 100.0% average progress** |
| Frame-rate independence | same run | **312 vs 312 blocks** at 60 Hz vs 144 Hz |
| Layout integrity | same run | 0 duplicate tiles, 0 buried pickups, 0 unclosed sections across 120 seeds |
| Unity-layer type check | `cd Tests/CompileCheck && dotnet build` | **0 errors** (31 runtime scripts) |

### What the 70 checks cover

- **Seeding:** identical seeds produce byte-identical layouts; different seeds differ; 120 seeds
  give 60+ distinct layouts; every seed has obstacles across the whole course.
- **Cube physics:** jump peaks at the configured 2.60 blocks, ~0.51 s airtime, travels ~5.4 blocks,
  lands and resets rotation, rejects a second jump mid-air, holds to jump on landing.
- **Collisions:** spikes kill at the spike, block sides kill, block tops are landable, flipped
  gravity lands on the ceiling.
- **Modes and objects:** gravity portal flips and settles on the ceiling, ship portal switches mode
  and stays inside the playfield, cube portal returns to cube, yellow orb boosts higher than a
  jump, pads fire without input and launch further than a jump.
- **Scoring:** caps are exactly 0.1x / 5x / 10x at 0% / 50% / 100%; 20,000 random rolls all fall
  inside `[0.1, cap]`; midpoint roll at 50% is 2.55x; payout math incl. zero and negative distance.
- **Records:** per-seed best distance and holder, attempt counts, sorting, bounded size, name
  truncation.
- **Timing:** a flat course finishes at exactly 30.00 s and reports 100% completion.
- **Chunk library:** all 14 cube chunks, 4 ship chunks and 4 ceiling-spike chunks are clearable.

### The autopilot

The sweep uses a bot that knows nothing about the chunk library: it reads level geometry, jumps so
its arc clears the whole obstacle, taps orbs when close, and flies the ship with a proportional
controller. It clears **300 of 300 seeds at 100%**, which is the evidence that generated levels are
beatable rather than merely well-formed.

### Bugs the tests caught (all fixed)

1. **Gravity-exit portal was on the floor.** In flipped gravity the cube travels along the ceiling,
   so the portal never overlapped and the player stayed inverted. Moved to the ceiling row.
2. **Rolls could exceed the cap.** Rounding to two decimals could round *up* past the cap (a 0.105x
   cap produced 0.11x). Now clamped after rounding.
3. **The 30 second run finished at 29.86 s.** The player started 1.5 blocks in, so the course ended
   early. Start offset removed; the run is now exactly 30.00 s.
4. **Three ship chunks were impossible at entry** (a 3-block window and a full-height weave). They
   were replaced with 5- and 6-block corridors.
5. **The ship bot was the bug, not the game.** A bang-bang controller overshot into walls; replacing
   it with velocity-damped control showed the chunks were fine. (Worth noting because a bad test can
   implicate good code.)

## How close is this to real Geometry Dash?

### Faithful to the original

| Geometry Dash mechanic | Geometry Dash |
|---|---|
| Auto-run, one-button jump, no health | Yes |
| Fixed jump arc, ~2.6 blocks high, ~0.5 s airtime | Yes (2.60 blocks, 0.51 s) |
| 1x speed = 10.39 blocks/s | 10.4 blocks/s |
| Hitbox smaller than the sprite (forgiving spikes) | Yes (0.6 unit hitbox, 0.4 unit spike core) |
| Cube spins in the air and snaps flat on landing | Yes, with a snap lerp |
| Hold to buffer the next jump | Yes |
| Instant death, instant retry | Death is instant; retry is one key (`R`) |
| Gravity portals, jump pads, three orb colours | Yes |
| Ship mode (hold to climb, release to fall) | Yes |
| Blocks are landable platforms | Yes |
| Deterministic, knowledge-based trials | Yes — the whole simulation is fixed-step |

### Deliberately different

| Geometry Dash | Geometry Dash |
|---|---|
| Hand-authored levels on a music grid | Seeded levels assembled from authored chunks |
| Level ends with a percentage and stars | 30 second timed run with a random multiplier payout |
| Practice mode + checkpoints | Not implemented |
| Level editor, user levels, coins, demons | Not implemented |
| 7 movement modes (ball, UFO, robot, spider, swing) | Cube and ship |
| Speed portals, size portals, monsters, moving objects | Not implemented |
| Online leaderboards | Local per-seed records only |

**Verdict on similarity:** the *moment-to-moment feel* is modelled closely — the same speed, jump
height, forgiving hitbox, hold-to-buffer input and fixed-step determinism that give Geometry Dash its
character — plus gravity portals, pads, orbs and a ship section. The *content model* is deliberately
different: this is a 30-second seeded score-chase with a multiplier gamble, not a level-conquest game
with an editor. It plays like Geometry Dash, but it is not a Geometry Dash level pack.

## Not verified — needs Unity

The following are compiled but never executed, so no claim is made that they look or sound right:

- Rendering of the procedural meshes (track, blocks, spikes, portals, orbs, particles, trail).
- Every UI screen and its animation (tweens, multiplier reel, count-up, slide-ins).
- The synthesised chiptune loop and sound effects.
- Input handling (keyboard, mouse, touch), pause flow, record persistence through `PlayerPrefs`.
- The WebGL build itself, its size, and browser performance.
- Responsive layout at phone/tablet aspect ratios.

### To check them

1. Open `NeonDash/` with Unity 6000.6.7f1 (the scene creates itself; otherwise **Geometry Dash → Setup
   Scene**).
2. Run **Geometry Dash → Validate Game Rules** — it re-runs a subset of these checks inside Unity.
3. Press Play: play a seed, die, watch the reel, retry, switch seeds, check the records screen.
4. **Geometry Dash → Build WebGL**, then serve `Build/WebGL` over HTTP (`python3 -m http.server 8000`).

## Reproducing the checks

```bash
# 1) logic + physics + playability sweep
cd NeonDash/Tests/LogicTests
dotnet run -c Release

# 2) type check the Unity layer (needs UnityEngine reference assemblies)
cd NeonDash/Tests/CompileCheck
dotnet build
```

The compile check needs the reference DLLs in `Tests/CompileCheck/lib` (git-ignored). Restore them
with:

```bash
mkdir -p NeonDash/Tests/CompileCheck/lib && cd NeonDash/Tests/CompileCheck/lib
curl -sSL -o ue.nupkg https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
python3 -c "import zipfile,shutil,os;z=zipfile.ZipFile('ue.nupkg');[shutil.copyfileobj(z.open(n),open(os.path.basename(n),'wb')) for n in z.namelist() if n.startswith('lib/netstandard2.0/') and n.endswith('.dll')]"
```

## Environment

- .NET SDK **8.0.427** (installed locally for the tests).
- UnityEngine.Modules **2021.3.33** reference assemblies (API surface is a subset of Unity 6; all
  APIs used here are stable across both).
- Unity editor: **not available** in the authoring VM.
- Source: **31 runtime scripts, ~4,100 lines** plus editor tooling and tests.

---

## Round 2 — gameplay visuals, coaching, and a real WebGL build (Windows PC)

This round ran on the target machine (Windows, Unity **6000.6.7f1** with the WebGL module), so the
game was actually built and played in a browser rather than only compiled.

### Changes verified

- **Jump rotation is forward.** The cube now accumulates negative Z (`Rotation -= CubeSpinSpeed`)
  and the simulation snaps the accumulated spin to the nearest quarter turn on landing instead of
  resetting it to zero. Confirmed in a live browser run: the cube tilts clockwise through the arc
  and touches down flat instead of unwinding backwards.
- **Solid walls.** The track is now real masonry: three rows of offset bricks with bevelled lips, a
  glowing play-surface line and a bloom under it. A matching wall runs above the ceiling, so flipped
  gravity has a surface to run along; when gravity flips the camera lifts to 8.5 and frames it.
  The ceiling mesh is hidden on the lobby/records screens so the title art stays open.
- **Coaching cards.** A new HUD banner slides in with an accent bar whenever the run changes verb:
  ship ("HOLD TO FLY · RELEASE TO FALL"), cube ("TAP TO JUMP · HOLD TO CHAIN JUMPS") and gravity
  ("YOU FALL UP · TAP TO JUMP DOWN"). The run also opens with a seed card.
- **Score replaces payout** on the result screen, the lobby record line and the records header. The
  score now counts up inside its own glowing panel.
- **UI polish.** Lobby card glow, three rule chips with staggered entrances, result card glow,
  pulsing score panel, records row accents.

### Checks run

| Check | Result |
|---|---|
| `dotnet run -c Release` in `Tests/LogicTests` (after the physics change) | **70 passed, 0 failed**, autopilot still clears **300/300 seeds at 100%** |
| Unity batch-mode WebGL build | **succeeded**, 39.7 MB, no compile errors |
| Served over HTTP and played in Chromium | lobby, run start, jump arc, ground/ceiling walls, coaching card, death → result with SCORE |

### Follow-up (same day): full-height portals

A portal was a fixed 3-block gate, so a ship could fly over it and a cube dropping from a jump could
skip the switch entirely. Portals are now **full-height gates**: `PortalHeight = CeilingY`, every
portal is placed at the floor, the trigger covers the whole column, and the gate is drawn with rails,
rungs and a direction chevron from the floor to the ceiling.

- `dotnet run -c Release` → **70 passed, 0 failed**; autopilot still **300/300 at 100%**, which is the
  useful signal here: every mode switch now fires on every seed.
- WebGL rebuilt and played: the flipped-gravity run shows the cube upside-down on the ceiling wall,
  and the gate spans floor-to-ceiling as intended (`Deliverables/Screenshots/round2-result-gate.png`).

Also switched the canvas scaler from `MatchWidthOrHeight` to `Expand`, so a short, wide browser
window shrinks the layout instead of cropping the bottom of the result card.

### Still not verified visually

The ship section and the per-mode coaching cards for ship/gravity were code-reviewed and pass in the
headless suite, but the browser's frame flow was too erratic during this session to catch those
exact moments on screen.

---

## Round 3 - earned multiplier, gems, and the Geometry Dash rename

### The multiplier is no longer a dice roll

The old system rolled a random value in `[0.1, 10 x progress]` when the run ended. It is now earned
during the run:

```
start       = 1.00x
arc gem     = +0.16x   sits in the jump arc over a hazard
high gem    = +0.30x   floats above open ground; needs an optional jump
pink orb +0.18x   yellow orb +0.28x   blue orb (gravity flip) +0.45x
pink pad +0.22x   yellow pad +0.34x
chain       = +12% per pickup taken within 2.5s of the previous one (max +72%)
cap         = 10.00x
score       = distance_in_blocks x 100 x earned_multiplier
```

Nothing is random any more, so two runs on one seed are directly comparable. `MultiplierLedger`
in the core owns the rules, and because it is pure C# the whole earning curve is unit tested.

### Gems

Levels were too sparse for an earned multiplier (about 2 pickups per level), so `LevelBuilder` now
scatters **gems**: ~17 per level, spaced 9-15 blocks apart, each one placed in clear air and never
inside a block. Gems never touch the physics - they are collected on contact - which is what makes
the risk voluntary. Arc gems sit in the arc you already fly through when clearing a spike; high gems
sit above clear ground and cost an extra jump. Both mirror to the ceiling side inside flipped
sections.

### UI

- HUD chip under the seed showing the live multiplier, with a `+0.30 HIGH GEM` flash on each pickup
  and a `CHAIN xN` counter while a chain is alive.
- Result screen replaced the spinning reel with a deterministic tally from 1.00x to the earned
  value, plus a breakdown line: `4 GEMS · 1 ORBS · 0 PADS · CHAIN x3`.
- Records storage key bumped to `v2`, so records saved under rolled multipliers (not comparable)
  start fresh; player name, seed and mute setting are kept.

### Checks run

| Check | Result |
|---|---|
| `dotnet run -c Release` after the rework | **74 passed, 0 failed** |
| Playability sweep | **300/300 seeds at 100%** - gems never break a level |
| Earned multiplier in real play | autopilot earns **x2.90** from 7.2 pickups/run; gems alone give a floor of 1.00x |
| Layout integrity | gems never sit inside a block or outside the playfield |
| WebGL build (Unity 6000.6.7f1) | succeeded, 39.8 MB |
| In-browser | lobby, live chip (`x1.94`, `+0.34 HIGH GEM`, `CHAIN x2`), gem rendering, gravity coaching card, result tally with breakdown |

### Rename

Player-facing name is now **Geometry Dash**: lobby title, page title, product name, window name,
audio clip name, editor menus and docs. The folder and C# namespaces stay `NeonDash` (paths, saved
settings and every doc link depend on them); renaming those is a mechanical follow-up if wanted.

## Round 4 — single-file (offline) build

Unity's WebGL player needs HTTP: the stock loader fetches `WebGL.data`, injects
`WebGL.framework.js` as `<script src>`, and stream-compiles `WebGL.wasm`. A `file://` page cannot
fetch local files, so `Tools/make_single_file.py` repacks the build so nothing is fetched at all:
`.data` and `.wasm` are gzipped + base64 inside the HTML, unpacked in the page, and handed to Unity
as a Blob URL (`dataUrl`) and through `Module.instantiateWasm`; the framework is rebuilt as a Blob
URL script.

Output: `Deliverables/GeometryDash.html`, **15.9 MB**, no external references (verified: 0 `src="`
attributes, the only `https://` string is an error message inside Unity's own loader).

### Browser probe (what `file://` actually allows)

| Capability | `file://` result |
|---|---|
| `fetch(blobURL)` | works, with body + Content-Length |
| `fetch(dataURL)` | works |
| `<script src="blob:...">` | loads |
| `DecompressionStream('gzip')` | available |
| `localStorage` | works |
| `indexedDB.open` | hangs (never fires success or error) — so the packer must not depend on it |

### End-to-end checks, headless Chrome, page opened as `file:///.../GeometryDash.html`

| Check | Result |
|---|---|
| Single file pack | 15.9 MB, wasm 32.7 MB -> 9.4 MB gzip, data 6.6 MB -> 2.1 MB gzip |
| Cold boot from disk | **~2 s** to interactive (progress: unpack framework, wasm, assets, engine) |
| Runs with no server | lobby rendered, run started, cube died at 28 m, result card + score shown |
| Input | click and Space both reach the canvas (pointer move must precede the press) |
| Record save | run saved as `BEST 28 m BY PLAYER / 1.00x / SCORE 2,800` |
| Record after reload | still there on a fresh document (verified the reload, not a cached view) |
| Portability | copied to an unrelated folder, byte-identical, boots in ~1 s and plays |

### Limits

- Requires `DecompressionStream` (Chrome/Edge 80+, Firefox 113+, Safari 16.4+) and WebGL 2.
- Records are stored by the browser for the document's origin (a local file path); copying the HTML
  to a new location starts a new record book. Opening via HTTP keeps the usual behaviour.
- Only Chrome/Edge on Windows was exercised here; Firefox and Safari were not available to test.
- The 15.9 MB file is slow to email; Drive/Discord/USB are fine.

## Round 5 — pickup visibility and input latency

### What was wrong

- `SimEvents` held a single gem index. One 120 Hz step can overlap several gems, so only the
  last one was reported: the multiplier counted the others, but their gems stayed on screen.
- `RunSession.Advance` runs several fixed steps per frame, and the view only read the events of
  the final step. A gem collected in an earlier sub-step was banked but never hidden — the
  reported symptom exactly.
- Orbs and pads are single-use in the simulation but were never removed from the view, so a used
  yellow orb kept sitting in the air.
- A tap that arrived in a frame that ran no simulation step was dropped outright. At 144 Hz a
  frame is shorter than one 120 Hz step, so that happens constantly.

### What changed

| Area | Fix |
|---|---|
| `PlayerSim` | `GemsThisStep` reports every gem of the step; `SimEvents` gains `OrbIndex` / `PadIndex` |
| `RunSession` | `FrameEvents` + `FrameGems` merge every sub-step of a frame; each gem is counted once |
| `RunSession` | a tap with no step to run it is buffered and applied on the next step |
| `LevelView` | gems, orbs and pads share one pop animation (flare, spin, shrink, deactivate) |
| `NeonDashApp` | pops each collected gem, each used orb and each triggered pad by index |

### Checks run

| Check | Result |
|---|---|
| `dotnet run -c Release` | **85 passed, 0 failed** |
| two gems inside one step | both reported (2 of 2) |
| gem collected in an early sub-step | reaches `FrameGems`, and is counted once |
| next frame | does not replay the same gem |
| orb index | the frame reports which orb fired |
| 144 Hz tap in an empty frame | buffered, then jumps on the next step |
| playability sweep | still 300/300 seeds at 100% |
| Unity WebGL build | succeeded, 0 compile errors |
| single-file rebuild | 15.9 MB, boots from `file://` and runs |

Not verified: the pop animation was not filmed frame by frame in a browser. Capturing it needs a
pickup to happen at a known moment, and the scripted runs kept dying on the first spike row before
reaching a gem.

## Round 6 — orbs removed

Orbs are no longer part of the game. The only two patterns that placed one (`orb-chain`, `pink-orb-spikes`)
now keep their exact rows and heights with the pickup cell empty, so every seed builds the same
course shape as before, minus the orb. No chunk contains an orb glyph any more.

| Check | Result |
|---|---|
| `dotnet run -c Release` | **85 passed, 0 failed** |
| playability sweep | **300/300 seeds at 100%** |
| autopilot pickups per run | 6.0 (was 7.2 — the removed orbs) |
| Unity WebGL build | succeeded, 0 compile errors |
| single-file rebuild | 15.9 MB |

The orb mechanic itself (blue gravity orb, orb events, multiplier rows) is still in the code but
nothing in any level uses it, so it cannot appear in play.

## Round 7 — centred multiplier popups

The "+0.34 HIGH GEM" gain label moved from the top-left corner (under the multiplier chip) to the
centre of the screen, just above the middle so it never covers the cube. It now punches in at 1.4x
scale, settles back to 1x, drifts up 34 px and fades out over 0.9 s. `ResetForRun` restores its
position and scale so a new attempt never inherits a leftover pop.

| Check | Result |
|---|---|
| Unity WebGL build | succeeded, 0 compile errors |
| single-file rebuild | 15.9 MB |

Not verified visually this round: seeing the popup requires an actual pickup, and the scripted
runs still die on the first spike row before reaching a gem.
