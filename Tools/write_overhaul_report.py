from pathlib import Path
import hashlib
import json
import zipfile

root=Path(__file__).resolve().parents[1]
out=root/'QA/Overhaul'
browser=json.loads((out/'BrowserQA.json').read_text())
tests=json.loads((root/'QA/TestSummary.json').read_text())
build=json.loads((root/'QA/BuildReport.json').read_text())
artifacts=json.loads((root/'QA/BuildArtifacts.json').read_text())
assert len(browser)==7 and all(not c['consoleErrors'] and not c['httpErrors'] for c in browser)
assert all(t['result']=='Passed' for t in tests) and build['result']=='Succeeded'
unchanged=0
with zipfile.ZipFile(root/'Backups/PreOverhaulSource.zip') as archive:
    for path in (root/'Assets/ChickenRush/Art/Chicken').glob('*.png'):
        entry=path.relative_to(root).as_posix()
        assert hashlib.sha256(path.read_bytes()).digest()==hashlib.sha256(archive.read(entry)).digest(),path
        unchanged+=1
rows='\n'.join(f"| {c['resolution']} | {c['layout']} | {c['input']} | {c['dpr']} | {c['medianReportedFPS']} | Passed |" for c in browser)
passed=sum(int(t['passed']) for t in tests)
zip_bytes=(root/'Builds/ChickenRush-WebGL.zip').stat().st_size
near=max(c.get('nearMissesObserved',0) for c in browser)
refill=json.loads((out/'RefillQA.json').read_text())
assert refill['noTruncatedText'] and not refill['consoleErrors']
text=f'''# Chicken Rush 2.0 — overhaul report

Validated on 2026-10-31. **Status: ready with the device coverage limits below.**
The existing Unity project was updated in place. The original source/build are
preserved under `Backups/`.

## Major improvements

- Vertical lanes, traffic moving in both Y directions, and chicken hops along +X.
  Camera follows horizontally with the chicken around 32% of viewport width.
- 20 configurable lanes. Traffic pressure increases at lanes 4, 7 and 11 through
  greater speed, denser groups, varied headways, larger vans and express streams.
  Every lane retains a recovery gap; same-lane speed preserves vehicle spacing.
- Real swept collisions, protected safe islands, single-hop input gating,
  safe-only cash-out and single-payout state transitions.
- Faster 0.42-second hops, squash/stretch, directional approach warnings,
  passing/near-miss audio and particles, controlled shake and celebration.
- Four responsive layouts use independent camera and UI geometry. The HTML
  fills the viewport, supports safe-area insets and caps render DPR at 2.
- Separate lossless UI atlas, antialiased procedural icons/cards, explicit
  nine-slice borders, and sliced/tiled world shapes eliminate the earlier
  stretched corners and compressed UI edges.
- Compact two-line phone stats prevent truncated difficulty names. Important
  lane numbers, arrows and multiplier plates draw above traffic.

## Completed validation

**{passed}/{passed} Unity tests passed**, no failures/skips: 30 EditMode and
3 PlayMode. Evidence: `QA/TestSummary.json`, the two test XML reports and their
logs. Tests include reserved gaps and non-overlap across 1,920 seeded lane
patterns, independently sampled reachable launch windows, responsive regions,
actual nine-slice mesh corner widths, decimal rewards and Animator interruptions.

The compiled player was tested in Google Chrome {browser[0].get('browserVersion','')}
on this Windows workstation. All seven required viewports passed:

| Viewport | Layout | Input | DPR | Median reported FPS | Result |
| --- | --- | --- | --- | --- | --- |
{rows}

Each case exercised mouse/keyboard or actual emulated touch, horizontal hops,
vertical movement in both traffic directions, no overlapping vehicle bodies,
camera look-ahead, pause/resume, one accepted hop per input, mid-hop cash-out
rejection, payouts and celebration. Primary controls stay at least 44 CSS pixels.
No browser scrolling, overflowing/ellipsized UI text, missing resources or console
errors remained in the final pass. All seven full-resolution captures were
visually inspected. The overview is `Resolution-Matrix.png`.

The desktop run crossed **12 consecutive lanes** using real controls, reached
the high-pressure band, cashed out, restarted, deliberately encountered a real
vehicle collision and verified Hit held before restart. All five animation
states were observed. The vehicle pool stayed fixed at **115** objects. The
run observed **{near}** near-miss event(s). The phone run also rotated during a
live round and retained progress and currency.

An additional 360px touch test selected a 10-coin risk, lost to an actual vehicle,
verified the FREE COINS label/refill at zero, and restarted with a one-coin stake.
No text was truncated in those result/refill/restart states (`RefillQA.json`).

The sampled FPS numbers are sparse Unity frame-time reports from headless
workstation Chrome, including DPR 2 touch emulation and fractional DPR 1.25.
They are indicative, not certification of sustained 60 FPS on physical phones.

## Build / source evidence

- Unity 6000.3.10f1, URP 17.3.0; no new project or package dependencies.
- Release build: {build['result']}, {build['errors']} errors, {build['warnings']} warnings,
  {build['seconds']:.2f} seconds; {build['bytes']:,} bytes reported by Unity.
- Final folder: {artifacts['totalBytes']:,} bytes. ZIP: {zip_bytes:,} bytes.
- HTML, loader JS, framework JS, data, loader artwork and actual gzip-encoded
  WebAssembly were verified. `QA/BuildArtifacts.json` records SHA-256 hashes.
- All **{unchanged} existing chicken PNGs** match the pre-overhaul backup byte
  for byte. Their native resolution, transparency and foot pivots are preserved.
- Latest build log: `Logs/Overhaul-WebGL-Release.log`.
- Runtime: `ChickenRushGame`, `TrafficPattern`, `RoadWorld`, `RushLayout`,
  `RushHUD`, `RushEffects`, `ChickenAnimationController` and `RoundRules`.
- Scene/prefab/configuration changes were made using the existing Unity builder.
  Existing traffic tuning is retained when regenerating assets.

## Resolved findings during review

The initial visual pass found a 360px difficulty label using ellipsis and cars
covering multiplier plates. Phone header cards now use two lines, detection also
checks TMP's truncation flag, and lane labels render above traffic. Long restart
and refill labels use the full button width. A test-harness snapshot race was
fixed by waiting for Hopping before waiting for its result. Failed diagnostic
captures are preserved separately from the final resolution captures.

## Known limits

- Physical iPhone/Android performance, hardware notch behavior and phone audio
  output were not tested. Safari, Firefox and Edge were not separately exercised.
- The shipped course has 20 lanes, with course length/progression configurable
  in difficulty assets. It is not an unbounded procedural course.
- The original character drawings retain their roughly 130–140px native
  silhouette detail and minor pose-to-pose variation.
- The gap/overlap tests verify the shipped traffic configuration. Custom tuning
  should rerun those checks. No RTP or probabilistic fairness percentage is claimed.

## Reproduce

```text
python Tools/run_unity_tests.py
Unity.exe -batchmode -projectPath <project> -buildTarget WebGL -executeMethod ChickenRushEditor.RushBuilder.BuildPlayerOnly -logFile <project>/Logs/Overhaul-WebGL-Release.log
python Tools/serve.py
python Tools/overhaul_qa.py
python Tools/resolution_contact_sheet.py
python Tools/package_build.py
```

The playable output is `Builds/WebGL/`, packaged as
`Builds/ChickenRush-WebGL.zip`. Local URL: http://127.0.0.1:8783/.
'''
(out/'Report.md').write_text(text,encoding='utf-8')
(root/'QA/Validation.md').write_text('# Current validation\n\nThe latest completed overhaul report is [Overhaul/Report.md](Overhaul/Report.md).\n',encoding='utf-8')
print(out/'Report.md')
