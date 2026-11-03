# Geometry Dash

A complete, from-scratch Geometry Dash style game for Unity: one-button auto-runner, seeded
levels, 30 second runs, a multiplier you earn by collecting gems, and a per-seed record book.

Everything is generated at runtime — meshes, particles, UI sprites and even the music. There are
no imported art or audio assets and no third-party packages beyond Unity's own UGUI module.

![status](https://img.shields.io/badge/build-unity%206000.6.7f1-informational) ![tests](https://img.shields.io/badge/logic%20checks-85%20passing-brightgreen)

---

## Play it

1. Open this folder (`NeonDash`) with **Unity 6000.6.7f1** (any Unity 6 / 2022.3+ should work).
2. The project creates its own scene on first open: `Assets/Scenes/NeonDash.unity`.
   If it does not, use **Geometry Dash → Setup Scene**.
3. Press **Play**.

### Controls

| Action | Input |
|---|---|
| Jump (cube) / thrust (ship) | `Space`, `↑`, `W`, left click, or tap |
| Pause | `Esc` or the `II` button |
| Retry after a run | `R` or `Enter` |
| Mute audio | `M` |
| Lobby / records | on-screen buttons |

Holding the jump button makes the cube jump again the moment it lands — the classic Geometry Dash
"hold to buffer jumps" trick.

### Build a WebGL player

**Geometry Dash → Build WebGL** writes a playable web build to `Build/WebGL` and patches the generated
page to fill the browser window on desktop and mobile.

Unity's WebGL player must be served over HTTP:

```bash
cd Build/WebGL
python3 -m http.server 8000
# open http://localhost:8000
```

Opening `index.html` via `file://` does not work — that is a Unity WebGL restriction, not a bug
(it fetches its own `.data`, `.framework.js` and `.wasm`, which browsers block from local files).

### Share it as one file (no server, ever)

`python Tools/make_single_file.py` packs the WebGL build into **one 16 MB HTML file**:
[`Deliverables/GeometryDash.html`](Deliverables/GeometryDash.html). Double-click it, or drag it into
any browser — it runs straight from disk, and you can email it, drop it in Drive/Discord, or open a
copy on a machine that has never seen this project.

The packer works around Unity's HTTP dependency rather than pretending it is not there:

| Normal WebGL build | Single file |
|---|---|
| `fetch('Build/WebGL.data')` | `.data` gzipped+base64 in the page, unpacked to a **Blob URL** and fetched from there |
| `<script src="Build/WebGL.framework.js">` | framework base64 → **Blob URL** script tag |
| `fetch('Build/WebGL.wasm')` then stream-compile | `.wasm` gzipped+base64, unpacks in ~1s and is handed to Unity through `Module.instantiateWasm` — no fetch, no MIME negotiation |
| `content-length`, `Content-Encoding` headers | none needed; progress is driven by the page's own unpacking stages |

Re-run the packer after any Unity rebuild; it reads `Build/WebGL/Build/*` and is byte-stable, so
repacking an unchanged build produces an identical file.

Verified from `file://` in Chrome (see [Deliverables/TestResults.md](Deliverables/TestResults.md)):
boots in ~2 s, plays, saves a record, and the record is still there after reloading the page.
Limits worth knowing: it needs a browser with `DecompressionStream` (Chrome/Edge 80+, Firefox 113+,
Safari 16.4+) and WebGL 2, and records live in that browser's storage for the file path they were
opened from — copy the file somewhere else and its record book starts empty.

---

## The game

### One run

A run is **30 seconds** and **312 blocks** long (10.4 blocks/second, which matches Geometry Dash's
1x speed). The cube runs forward on its own; you only control jumps. Touching a spike, a block, or
falling into a gap ends the run immediately.

The player has **cube** and **ship** modes, gravity portals, jump pads and three kinds of jump orb:

| Object | Effect |
|---|---|
| Gem (gold) | +0.16x, sits in the arc over a hazard; collect on contact |
| High gem (cyan) | +0.30x, floats above open ground and needs an extra jump |
| Yellow orb | Full-height boost, tap while airborne |
| Pink orb | Smaller boost |
| Blue orb | Flips gravity and gives a small boost |
| Yellow pad | Automatic big launch on contact |
| Pink pad | Automatic small launch |
| Gravity portal | Flips gravity up/down |
| Ship portal | Switches to ship (hold to climb, release to fall) |
| Cube portal | Returns to cube mode |

### Seeding

Enter any seed (digits) and the level is rebuilt from it. The same seed always produces exactly the
same layout, on any machine, which is what makes the record book meaningful. Levels are assembled
from hand-authored chunk patterns (single/double/triple spikes, block hops, stair steps, pad
launches, orb chains, ceiling-spike corridors, ship gates and windows) shuffled by the seed. Because
every chunk is authored to be clearable, any shuffle stays playable — but the order genuinely
changes the level.

### Scoring and the multiplier

The multiplier is **earned, never rolled**. Every run starts at **1.00x** and grows only from things
the player actually touches:

```
start        = 1.00x
gem (arc)    = +0.16x     gem in the jump arc over a hazard
high gem     = +0.30x     gem above open ground: needs an optional jump
pink orb     = +0.18x     yellow orb +0.28x     blue orb (gravity) +0.45x
pink pad     = +0.22x     yellow pad +0.34x
chain        = +12% per pickup taken within 2.5s of the last, up to +72%
cap          = 10.00x
score        = distance_in_blocks x 100 x earned_multiplier
```

Because nothing is random, the same inputs always earn the same multiplier — two players on one seed
can compare runs honestly. Gems float above the run line, so a careful run that only jumps over
hazards earns little, while a greedy line that grabs the high gems (each one an extra jump) can
reach several times more. The result screen counts the tally from 1.00x up to the earned value and
shows the breakdown: gems, orbs, pads and the best chain.

### Records

Each seed stores its own record: best distance, who set it, the multiplier and the score, plus
attempt counts. Records live in `PlayerPrefs` (browser storage in WebGL) and survive reloads. The
lobby shows the current seed's record; the records screen lists the best seeds.

---

## Interface

The UI is built entirely in code with UGUI and animated by a small tween engine:

- Lobby: title pulse and colour breathing, card slides up with an overshoot, buttons scale on hover.
- HUD: smoothly counting distance, live progress bar with a gold record marker, draining timer that
  turns magenta under 5 seconds, plus a coaching card that slides in whenever the run changes verb
  (cube, ship, flipped gravity).
- Result: card pop, spinning multiplier reel, glowing score count-up, pulsing "NEW BEST" badge.
- Records: staggered row slide-in.
- Pause: pop-in card.

---

## Project layout

```
Assets/Scripts/Core/       Pure C# simulation - no UnityEngine references
  GameConstants.cs         All tuning in one place
  LevelModel.cs            Tiles, portals, pads, orbs, level container
  ChunkLibrary.cs          Hand-authored level patterns
  LevelBuilder.cs          Seed -> level assembly
  DetRandom.cs             Deterministic xorshift RNG
  PlayerSim.cs             Fixed-step kinematic cube/ship physics
  RunSession.cs            30 second run, death/finish, result
  ScoreRules.cs            Progress, multiplier cap, roll, payout
  RecordTable.cs           Per-seed record book

Assets/Scripts/Game/       Unity presentation layer
  NeonDashApp.cs           Controller: phases, input, camera, flow
  LevelView.cs             Procedural level geometry + animated orbs/portals
  PlayerView.cs            Cube/ship visuals, spin, squash, trail
  BackgroundRig.cs         Gradient, parallax grids, dust
  Fx.cs                    Custom particle system + screen shake
  AudioDirector.cs         Procedural chiptune and sound effects
  UiKit.cs, Ui*.cs         Runtime-built animated UI
  Tween.cs, MeshTools.cs   Easing/tween engine and mesh helpers
  RecordStore.cs           PlayerPrefs persistence

Assets/Editor/             Scene creation, WebGL build, rule checks
Tests/LogicTests/          Headless simulation test suite (dotnet)
Tests/CompileCheck/        Compiles the Unity layer against UnityEngine assemblies
```

The split matters: `Core` never references Unity, so the entire game simulation is compiled and
tested headlessly on a build machine. `Game` only renders it and handles input.

---

## Tests

```bash
# Simulation rules, physics, seeding, scoring, and a playability sweep with an autopilot bot
cd Tests/LogicTests
dotnet run -c Release

# Type-check the Unity-facing scripts against real UnityEngine assemblies
cd Tests/CompileCheck
dotnet build
```

`Tests/LogicTests` includes an **autopilot** that plays levels with no knowledge of the chunk
library — it only reacts to geometry. It clears the full course on **300/300 seeds**, which is the
evidence that seeded levels are actually beatable. See
[Deliverables/TestResults.md](Deliverables/TestResults.md) for the full results and the comparison
against the real Geometry Dash.

`Tests/CompileCheck` needs the UnityEngine reference DLLs in `Tests/CompileCheck/lib` (they are
git-ignored); drop `UnityEngine.Modules` from NuGet there, or just build inside Unity.

---

## Design notes and limits

- **Deliberately no Rigidbody/PhysX.** Geometry Dash's feel comes from a fixed, deterministic arc.
  Physics engine collisions would drift with frame rate and produce inconsistent deaths.
- **No level editor / practice mode / user levels.** This is a seeded run-based game, not a
  Geometry Dash clone of the full editor ecosystem.
- **Constant speed.** Speed portals are not implemented; ship and gravity portals are.
- **Local records only.** No accounts or leaderboards.
- The authoring environment had no Unity install, so the *simulation* is verified by tests and the
  *rendering/UI* is verified by compilation only. In-editor playtesting is still required.
