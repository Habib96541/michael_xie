# Bottle Flip

A one-tap arcade game made in Unity. Tap the screen and the bottle flips and lands back on its
base — almost every time. There is a **10% chance it topples over instead, and that ends the run**.
Your score is the number of clean landings you string together before gravity wins.

![Title screen](screenshots/title-04.png)

## Play

| | |
|---|---|
| **Tap / click** | Flip the bottle (anywhere on the play area) |
| **Space / Enter** | Flip the bottle (keyboard) |
| **Sound chip (top right)** | Mute / unmute |

- Every flip: 90% the bottle plants on its base, 10% it tips over and the run ends.
- One tap = one flip. Taps made while the bottle is still in the air are buffered, so fast
  tapping never loses an input.
- Your best streak is saved locally (`PlayerPrefs` → IndexedDB in the browser).

## Design notes

**Why the bottle stands by default.** The run is a survival streak: each tap is the player
choosing to push their luck one more time, and the topple is the only fail state. With a flat 10%
chance, the expected run is ~9 landings, which keeps a full loop inside ten seconds while still
allowing the rare 30–40 streak that feels like a story. The odds are stated plainly on the title
screen — no fake difficulty ramp, no hidden pity timer.

**Making one repeated action feel good.** Every successful landing pays out in four ways at once:
the counter pops and flashes amber, a `+1` floats up from the bottle, dust kicks off the stage and
a soft "tok" plays. Milestones at 10/25/50/100 add a toast so long streaks keep escalating. The
flip itself is one clean full turn, so the bottle is always side-on at the apex — readable at a
glance and never clipped by the HUD.

**Making the 10% land hard.** The topple is a different animation, not a variation: the bottle
lands on its edge, teeters, then collapses, all in slow motion with a heavy thud, dust, a coral
flash and camera shake. The game-over card slides up as a bottom sheet so the fallen bottle stays
visible above it.

**Layout.** Portrait 9:16 with a fixed reference resolution of 1080×1920. Score at the top, tap
hint at the bottom, bottle framed in the safe area between them. The browser template letterboxes
the canvas on desktop so the composition is identical everywhere.

![In-game HUD](screenshots/play-01.png)

## Technology

- **Unity 6000.3.27f2**, built-in render pipeline, uGUI + TextMeshPro. No URP/HDRP packages.
- **Everything is procedural — the project contains no imported art or audio.**
  - The bottle is a surface-of-revolution mesh generated at runtime from a profile curve
    (`Lathe.cs`), with a separately generated label texture, cap and neck ring.
  - Textures and UI sprites (rounded 9-slice pills, gradients, glows, vignette, confetti, label
    art) are generated in code (`Art.cs`).
  - A small additive shader (`AdditiveGlow.shader`) drives the halos, light pool, particles and
    shockwave rings with predictable intensity.
  - All sound effects are synthesised at runtime (`Sfx.cs`): noise-shaped whoosh, landing "tok",
    topple thud with a plastic rattle, UI click/pop and a rising arpeggio for a new best.
- **One bootstrap scene.** `GameBootstrap` builds the camera, lights, stage, bottle, effects,
  audio, UI and game manager in code, so there are no fragile prefab or inspector references.
- WebGL build is self-contained (compression disabled, no server headers required).

## Project layout

```
BottleFlipUnity/
  Assets/
    Editor/ProjectBuilder.cs        project setup + WebGL build entry point
    Plugins/WebGL/BottleFlipBridge.jslib   state bridge + screenshot upload (test tooling)
    Scenes/Main.unity               the only scene: one GameObject with GameBootstrap
    Scripts/
      Art/Art.cs, Lathe.cs          procedural meshes, textures, materials, palette
      Core/Ease.cs                  easing curves + coroutine tweens
      Game/BottleRig.cs             bottle construction and the flip/land/topple animation
      Game/GameBootstrap.cs         builds the whole game
      Game/GameCamera.cs            framing, focus zoom, shake
      Game/GameManager.cs           rules, input, scoring, game-over flow
      Game/Fx.cs, Sfx.cs            particles and synthesised audio
      UI/Ui.cs, UIRoot.cs           widget helpers and every screen
    Shaders/AdditiveGlow.shader
    WebGLTemplates/Portrait/        letterboxed 9:16 page with a matching loading screen
  Builds/WebGL/                     the web build (index.html + Build/)
tools/shot_server.py                receives screenshots during playtesting
screenshots/                        captured from the finished WebGL build
```

## Run and build

**In the editor:** open `BottleFlipUnity` with Unity 6000.3.27f2, open `Assets/Scenes/Main.unity`
and press Play. The scene only needs the `Bootstrap` GameObject.

**WebGL build (editor menu):** `Bottle Flip → Build WebGL`.

**WebGL build (command line):**

```bash
"/path/to/Unity" -batchmode -quit \
  -projectPath BottleFlipUnity \
  -executeMethod BottleFlip.EditorTools.ProjectBuilder.BuildWebGL \
  -logFile build.log
```

**Serve the build** (any static server works; compression is disabled so no special headers):

```bash
cd BottleFlipUnity/Builds/WebGL && python -m http.server 8123
# then open http://localhost:8123/
```

## Playtest hooks

The build exposes a small bridge so the game can be driven and inspected from the browser
console. It is inert outside WebGL and no gameplay code depends on it.

```js
window.__bottleFlipState
// { phase: "title" | "playing" | "over", flips: <landings>, best, last, t }

unityInstance.SendMessage("Game", "DebugForceOutcome", "fall");   // "stand" | "random"
unityInstance.SendMessage("Game", "DebugTap", "");
unityInstance.SendMessage("Game", "DebugPlayAgain", "");
unityInstance.SendMessage("Game", "DebugScreenshot", "shot-name");
```

`python tools/shot_server.py` listens on port 8124 and saves PNGs posted by `DebugScreenshot` into
`screenshots/` — useful when the browser cannot be captured directly.

## Verification

- WebGL build compiles and links cleanly; the page loads with no console errors.
- Title, in-game HUD and game-over sheet captured from the built WebGL player (see `screenshots/`).
- Forced outcome hook exercised for both the landing and the topple path; scoring, best-score
  persistence (`PlayerPrefs`), restart and menu transitions checked.
- One tap produces exactly one flip; taps during a flip are buffered rather than dropped.
