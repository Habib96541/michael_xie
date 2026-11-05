# Chicken Rush: Cash Out! — 3.0

A virtual-coin, hop-by-hop risk game built in the existing Unity project.
Chicken moves right across raised landing pads; opposing traffic moves vertically.
Each hop has a seeded outcome. Waiting or watching traffic does not change its
odds. Cash out between hops to secure the displayed reward.

## Play and deploy

```powershell
python Tools/serve.py
```

Open http://127.0.0.1:8783/. Upload the entire `Builds/WebGL/` folder to static
hosting, or distribute `Builds/ChickenRush-WebGL.zip`. Gzip decompression fallback
is included; no custom Content-Encoding header is needed. Do not double-compress
`.unityweb` files or launch through `file://`.

- Space / Right / Up / HOP / road tap: start, one hop, restart.
- C / CASH OUT: bank after a successful landing.
- P / Escape / gear: pause/settings. M: sound.
- Change risk coins and Easy / Medium / Hard / Hardcore between rounds.
- Zero balance enables a free 10-coin refill. No purchases or real-money system.
- Existing browser-local coins, best multiplier, sound and progress are retained.

## Current revision

- Broad continuous painted road, restrained raised view, original shaded front
  and rear vehicle sprites: compact, taxi, van, truck, delivery.
- Raised landing grates, next-pad highlight, large multiplier badges, protective
  barriers, subtle foliage and paving, consistent upper-left lighting.
- Desktop side control panel; portrait bottom panel; landscape side controls.
  Camera and HUD adapt independently, including live rotation and safe areas.
- Seeded per-hop risk: rules reject duplicate starts/hops/payouts, cash-out during
  a hop, and presentation callbacks that contradict the planned result.
- Losing vehicles make actual swept geometric contact before the loss state.
  Successful traffic either brakes behind protection or passes with clearance.
  Near-miss feedback uses measured clearance; audio tension rises with progress.
- Twelve recycled road segments and 36 pooled cars extend the visible course.
  Existing 32-particle pool, unlit URP sprites and single-thread WebAssembly remain.
- Idle, Walk, Hop, Hit, Celebrate and a subtle Danger lean use the original 54
  registered PNGs. The danger reaction reuses idle drawings; it is not a new
  hand-drawn expression sequence. No production-master animation claim is made.
- Lossless RGBA UI atlas, correct nine-slice borders; ETC2 RGBA world atlases.
  No expensive post-processing, HDR or dynamic shadows. Browser DPR is capped at 2.

## Open, tune and build

Use Unity **6000.3.10f1** with Web Build Support. Open the existing scene
`Assets/ChickenRush/ChickenRush.unity`.

`Assets/ChickenRush/Config/` contains four difficulty assets. `NORMAL.asset`
retains its existing identity but now displays **MEDIUM**. Tune `successChances`,
`multipliers`, `hopSeconds` and `speedScale`. Arrays must have matching lengths;
each chance is greater than 0 and at most 1. Course lengths are 24 / 20 / 16 / 12,
with automatic banking at the final pad. The reward uses integer coin rounding
down, displayed consistently. Default table generation is documented in
`RushBuilder.cs`; this virtual game makes no exact RTP or certified fairness claim.

```powershell
python -m pip install -r Tools/requirements.txt
python Tools/reference_build.py
```

Close this project in the Editor before batch tests/builds. `reference_build.py`
runs EditMode/PlayMode tests, then builds existing assets. `--prepare` first
regenerates the scene, prefab, Animator and UI sources. Risk defaults migrate once
when `riskVersion < 3`; later custom values are preserved. `--skip-tests` rebuilds
current assets without rerunning tests. Generated asset edits belong in the
builder if they must survive regeneration. Original PNGs remain direct assets.

## Verification and artifacts

```powershell
python Tools/serve.py
python Tools/reference_qa.py
python Tools/package_build.py
python Tools/write_reference_report.py
```

Chrome is required for browser QA. Real mouse, keyboard and emulated touch inputs
exercise the compiled player. `?qa=1` publishes read-only observations, controls
and public chances; it exposes no future outcome and cannot modify currency.

Captures and the report are under `QA/ReferenceRevision/`. Tests cover risk
frequency/reproducibility, payout guards, configured math, physical outcome
presentation, fixed road/vehicle pools, layout geometry, nine-slice meshes and
actual Animator transitions. Browser checks include all seven requested sizes
plus 1600×900, DPR 1 / 1.25 / 2, live rotation, pause, loss, replay, cash-out,
lane 13, input guards and saved-balance reload.

## Reference, provenance and limits

The supplied YouTube reference was accessible and sampled at representative
timestamps. `Docs/AI/ReferenceRevisionPlan.md` records the comparison. This is
an original interpretation of its composition and core loop, not an exact copy.
No reference branding or artwork ships in the player.

The original 54 chicken PNGs retain byte-identical drawings, 192px canvases,
100 PPU and foot pivot (96,18). Native character silhouettes are about 130–140px
high; scaling does not create additional detail. New original AI artwork,
extraction metadata and prompts are retained under `SourceArt/ReferenceRevision/`.
Prepared final assets live under `Assets/ChickenRush/Art/Arcade/`.

Target is 60 rendered FPS on capable hardware; clips use authored timing at
30 Hz curve sampling, not 60 unique drawings per second. Headless Chrome and
emulated touch results do not certify physical phones. Physical iOS/Android,
Safari/Firefox, hardware notches and phone audio still need device testing.

Rollback source/build bundles are in `Backups/PreReferenceRevision/`. Older QA
under `QA/Overhaul/` is historical evidence. TMP Liberation Sans includes its
bundled license; generated sound effects are original to this project.
