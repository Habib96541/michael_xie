# CRASH — cash out before the crash

A polished Unity (6000.3) WebGL crash game. A rocket rides an exponential curve;
points grow faster the higher it climbs. Cash out to bank the round, or lose the
round's points when the rocket explodes.

## Play it

Two builds are produced, both work on any PC:

* **`CrashGame.html`** — the whole game inlined in a single file. Double-click it.
  No server, no install, works straight from `file://` or a USB stick.
* **`Build/WebGL/index.html`** — the normal Unity WebGL build. Host the
  `Build/WebGL` folder on any static web server (relative paths only, no
  `localhost` assumption), or open it through any local server.

## Rules

* The rocket climbs. **Round points = 1000 × (multiplier − 1)** and they grow
  faster the higher you go.
* **Tap / click / press Space** to cash out and bank the round.
* The background is a fixed trading table: the rocket crosses it left to right
  and can bust at **any moment**. If it survives to the **right edge** it always
  bursts there — the edge is the round's time limit (6.5 s, 7.90x), so cashing
  out a moment before it is the maximum payout.
* If it crashes first you lose the round's points and **one shield**.
* You start with **3 shields**; cash out **4 rounds in a row** to earn one back
  (max 3).
* With no shields left, the next crash **wipes your total** and ends the run.
  Your best score is saved in the browser.

## Controls

| Input | Action |
| --- | --- |
| Click / tap / Space | Cash out (or launch early during the countdown) |
| M | Mute / unmute |
| Esc | Close the how-to-play card |

## Project layout

| Path | What it is |
| --- | --- |
| `Assets/Scripts/Core/Rules.cs` | All game maths: multiplier curve, scoring, crash distribution |
| `Assets/Scripts/App/GameApp.cs` | State machine, input, scoring, UI construction |
| `Assets/Scripts/Visuals/` | Curve mesh, backdrop, rocket, particles |
| `Assets/Scripts/Core/Tex.cs` | Procedurally generated sprites (no image assets) |
| `Assets/Scripts/Core/Sfx.cs` | Procedurally synthesised audio (no audio assets) |
| `Assets/WebGLTemplates/Crash/index.html` | Custom web page + loading screen |
| `Tools/inline.js` | Packs the WebGL build into `CrashGame.html` |

Everything is generated at runtime: no textures, fonts, models or audio files.

## Rebuild

```bash
# headless WebGL build (Unity must be installed)
Unity.exe -batchmode -nographics -quit -projectPath . \
  -executeMethod Crash.EditorTools.BuildGame.BuildWeb

# pack the single-file build
node Tools/inline.js Build/WebGL CrashGame.html
```

Logic tests (multiplier maths, crash distribution, shield economy) run headless:

```bash
Unity.exe -batchmode -nographics -quit -projectPath . \
  -executeMethod Crash.EditorTools.BuildGame.RunSelfTest
```

The WebGL build uses uncompressed payloads on purpose so the same output can be
inlined into one file and opened directly from disk.
