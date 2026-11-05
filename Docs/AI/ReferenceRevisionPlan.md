# Reference revision — implementation contract

Modify the existing Unity 6000.3.10f1 project. Preserve the chicken drawings,
foot registration, Animator, currency persistence, input, audio/effects pools,
uGUI nine-slice system and WebGL template. Rollback: Backups/PreReferenceRevision.

## Reference evidence

- Supplied video: https://www.youtube.com/watch?v=KQrMVcBXHFw
- Accessible and inspected through the browser at 0:00, 0:29, 0:58 and 1:56.
  Representative screenshots were captured in the working session. This is
  sampled visual inspection, not a claim that all 4:51 were watched.
- Official description/UI: https://stake.com/casino/games/chicken
- Local Downloads/Classic Crossy Roads.mp4 is an 18.63-second recording of the
  rejected previous build, not the original Stake reference. Extracted baseline
  frames are under QA/ReferenceRevision/Reference/.

## Observed discrepancies and corrective implementation

| Area | Rejected baseline | Reference observation / correction |
|---|---|---|
| Composition | Tiny characters, many narrow green strips | Broad continuous dark road, fewer larger visible lanes |
| Perspective | Flat toy cars versus shaded chicken | Restrained raised front/rear vehicle views; no dramatic isometric rotation |
| Safe positions | Chicken stands on divider strips | Raised oval pads on lane centers, protection barriers after a safe hop |
| Multipliers | Small labels at bottom of screen | Large badges directly below pads, next pad highlighted |
| Controls | Single wide bottom dock | Desktop side panel with large current multiplier and stacked actions |
| Risk | Timed collision avoidance | Seeded independent hop probabilities; waiting cannot improve odds |
| Artwork | Procedural flat shapes | Original shaded vehicles, asphalt, pad, barrier, sidewalk and foliage |

## Acceptance checklist

- [x] Preserve project / backup existing source and build.
- [x] Inspect source video and rejected baseline.
- [x] Four configurable modes: Easy, Medium, Hard, Hardcore.
- [x] Seeded risk / rule guards / no mid-hop or duplicate payout.
- [x] Traffic choreography physically contacts on loss, stays clear on success.
- [x] Original cohesive 2.5D art; separate front and rear vehicles.
- [x] Dynamic road segment reuse with a fixed 36-vehicle pool.
- [x] Large centered landing multipliers and protective barriers.
- [x] Desktop side panel, portrait bottom panel, landscape side controls.
- [x] Danger reaction plus original five animation states.
- [x] Validate compiled player at all eight resolutions, live rotation, mouse,
  keyboard and emulated touch; inspect native captures.
- [x] Final visual comparison, build/ZIP/hash audit, updated report/docs.

No exact reference fidelity, certified fairness, exact RTP or physical-phone
60 FPS claim is implied. Virtual coins only; no accounts, purchases or payouts.
