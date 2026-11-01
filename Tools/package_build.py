"""Package the static build, licenses, checksums and evidence report after browser validation."""
from pathlib import Path
from datetime import datetime, timezone, timedelta
import hashlib, json, gzip, zipfile, shutil, xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
build=root/'WebGL_Build'
validation=root/'Validation'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
required=['index.html','Build/WebGL_Build.loader.js','Build/WebGL_Build.framework.js.unityweb','Build/WebGL_Build.data.unityweb','Build/WebGL_Build.wasm.unityweb']
for name in required:
    p=build/name
    if not p.is_file() or not p.stat().st_size: raise RuntimeError('Missing build file: '+name)
    if name.endswith('.unityweb'):
        with gzip.open(p,'rb') as stream:
            while stream.read(1024*1024): pass
assert '{{{' not in (build/'index.html').read_text(encoding='utf-8')
notices='HI / LO — Third-party font notices\n\nCard art, icons and synthesized audio were created for this project.\n\nOUTFIT\n\n'+(root/'Assets/HiLo/Resources/Fonts/OFL.txt').read_text(encoding='utf-8')+'\n\nLIBERATION SANS (TextMeshPro fallback)\n\n'+(root/'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt').read_text(encoding='utf-8-sig')
(root/'THIRD_PARTY_NOTICES.txt').write_text(notices,encoding='utf-8')
shutil.copy2(root/'THIRD_PARTY_NOTICES.txt',build/'THIRD_PARTY_NOTICES.txt')
files=[{'path':p.relative_to(build).as_posix(),'bytes':p.stat().st_size,'sha256':digest(p)} for p in sorted(build.rglob('*')) if p.is_file()]
wasm_hash=digest(build/required[-1])
manifest={'createdUtc':datetime.now(timezone.utc).isoformat(),'unity':'6000.3.10f1','wasmSha256':wasm_hash,'files':files}
(validation/'build-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
archive=root/'HiLo-WebGL.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for entry in files: z.write(build/entry['path'],entry['path'])
with zipfile.ZipFile(archive) as z: assert z.testzip() is None
reports={name:json.loads((validation/f'browser-{name}.json').read_text()) for name in ['edge','chrome']}
density={name:json.loads((validation/f'density-{name}.json').read_text()) for name in reports}
refill={name:json.loads((validation/f'refill-{name}.json').read_text()) for name in reports}
for name,r in reports.items():
    assert r['buildHash']==wasm_hash, 'Browser report does not match the final WASM'
    assert not r['errors'] and all(c['passed'] for c in r['checks'])
    assert not density[name]['errors'] and all(c['passed'] for c in density[name]['checks'])
    assert refill[name]['passed']
    assert density[name]['buildHash']==wasm_hash and refill[name]['buildHash']==wasm_hash
tests=ET.parse(validation/'editmode.xml').getroot()
assert tests.attrib['result']=='Passed'
summary=(validation/'build-report.txt').read_text()
assert 'Result: Succeeded' in summary and 'Errors: 0' in summary
time=datetime.now(timezone(timedelta(hours=6))).strftime('%Y-%m-%d %H:%M (UTC+06:00)')
rows='\n'.join(f"| {c['width']} × {c['height']} | Passed | Passed |" for c in reports['edge']['captures'])
versions=', '.join(f"{r['browser']} {r['version']}" for r in reports.values())
count=sum(len(r['checks']) for r in reports.values())
report=f'''# HI / LO — validation report

Recorded {time}. **Ready with limitations.**

The release WebGL build loads and is playable through the local HTTP server. The source workspace began empty; this is a new Unity project.

## Build and artifacts

- Unity 6000.3.10f1, URP 17.3, WebGL 2 / IL2CPP, release build.
- Build result: succeeded, 0 build errors and 0 build warnings. See [build-report.txt](build-report.txt) and the local `webgl-build.log`.
- Static build: `../WebGL_Build`; deployment archive: `../HiLo-WebGL.zip`.
- {len(files)} files; {sum(f['bytes'] for f in files)/1048576:.2f} MiB on disk; ZIP {archive.stat().st_size/1048576:.2f} MiB.
- Required files, gzip integrity, ZIP CRCs, and absence of unexpanded template tokens verified.
- SHA-256 checksums: [build-manifest.json](build-manifest.json). Both full browser reports match this final WASM hash: `{wasm_hash}`.
- Preview: [localhost:8765](http://localhost:8765). Keep the local server running, or run `Play.ps1` again.

## Gameplay and persistence

- **{tests.attrib['passed']}/{tests.attrib['total']} EditMode tests passed**, no skipped tests. [NUnit results](editmode.xml).
- Coverage includes deck uniqueness, seeded shuffle reproducibility, exhaustion, fresh-deck continuation, higher/lower, pushes, an all-equal deck remainder, Ace-high ranking, impossible choices, probability-based gains, guaranteed-choice growth, cash out, wrong answers, timeout, double-input rejection, restart, free refills, and exact 20-stake depletion from 200 coins.
- A 10,000-prediction test remained playable without a streak cap. A 100,000-multiplication test retained finite significands; arbitrary-size exponents and scientific formatting were checked.
- **{count} browser assertions passed** across {versions}. [Edge results](browser-edge.json), [Chrome results](browser-chrome.json).
- Production-card draws yielded correct, wrong and equal outcomes. Actual UI input exercised mouse predictions, phone touch predictions and cash out, keyboard cash out, settings, saved balance/preferences after reload, and rapid clicks accepting exactly one prediction.
- Timed stages were reached using the production shuffle. Settings held a live decision window paused for 18 seconds; closing settings allowed it to expire and end the round without predicting.
- Additional keyboard/refill runs exhausted exactly 20 stakes, reached a zero balance, added 100 free coins, and restarted with 90 coins. [Edge refill](refill-edge.json), [Chrome refill](refill-chrome.json). Keyboard Up/Down was used for each prediction.
- Browser tests use isolated profiles and UI input. They do not replace the shuffle, inject streaks, or edit balances.

## Responsive checks

The same build was resized during play. Canvas dimensions followed the viewport, with no horizontal document overflow. The eight Edge layouts were visually inspected; equivalent Chrome captures were also recorded.

| CSS viewport | Edge | Chrome |
|---|---|---|
{rows}

Additional touch checks passed at pixel ratio 2 on 844×390, 390×844, 768×1024 and 1024×768; and at pixel ratio 3 on both phone orientations. The Unity render target stayed at or below the configured ratio cap of 2. These checks caught and verified the fix for selecting the phone landscape layout using CSS height rather than render-buffer pixels. [Edge density](density-edge.json), [Chrome density](density-chrome.json).

All captures are in [screenshots](screenshots), including phone results, settings, timed play, timeout, bankruptcy and refill.

## Limits and observed warnings

- Physical Android, iPhone and iPad devices were unavailable. Emulated touch and viewport/density checks do not establish device performance, Safari behavior, notch handling, or browser-chrome behavior on those devices.
- Firefox 157 was downloaded through Playwright, but Windows refused to spawn its executable (`spawn UNKNOWN`). No Firefox runtime tests are claimed. Safari was unavailable.
- Both Chromium browsers emitted `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat` during initialization. This remains an observed graphics-format query warning. The final tested flows had no JavaScript exceptions, failed network requests or console errors.
- Local loading plus the test's fixed three-second stabilization wait took {reports['edge']['loadMs']/1000:.2f}s in Edge and {reports['chrome']['loadMs']/1000:.2f}s in Chrome. This is not an internet download benchmark.
- `frameStats` records browser requestAnimationFrame cadence, not Unity FPS. The game targets 60 FPS, but a universal 60 FPS claim, full WebAssembly/GPU memory profile, and low-end mobile benchmark were not established.
- Later difficulty values and long streaks were verified in logic tests. The browser runs reached the first timed stage; no manual 40-win browser streak is claimed.
- Sound playback code ran after user input and settings persisted. Subjective audio quality and physical speaker behavior were not separately verified. Browser fullscreen was not exhaustively tested across platforms.

## Reproduce

Use the project and browser commands in [README](../README.md). Run `node Tools/refill_test.cjs` for the bankruptcy flow and `node Tools/density_test.cjs` for high-density touch layouts. After successful tests, run `python Tools/package_build.py` to rebuild the archive, notices, manifest and this report.
'''
(validation/'TEST_REPORT.md').write_text(report,encoding='utf-8')
print(json.dumps({'zip':str(archive),'bytes':archive.stat().st_size,'files':len(files),'editmodePassed':tests.attrib['passed'],'browserAssertions':count,'wasmSha256':wasm_hash},indent=2))
