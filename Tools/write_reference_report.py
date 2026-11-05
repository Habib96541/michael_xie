"""Verify final inputs/artifacts and package traceable client-review evidence."""
from pathlib import Path
import hashlib
import json
import statistics
import zipfile
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'QA/ReferenceRevision'
qa=json.loads((OUT/'BrowserQA.json').read_text());tests=json.loads((ROOT/'QA/TestSummary.json').read_text());build=json.loads((ROOT/'QA/BuildReport.json').read_text())
assert len(qa['cases'])==8 and all(c['status']=='Passed' for c in qa['cases'])
assert all(t['result']=='Passed' and t['exitCode']==0 for t in tests)
assert build['result']=='Succeeded' and build['errors']==0
inputs=json.loads((OUT/'BuildInputs.json').read_text())
for file,digest in inputs.items():assert hashlib.sha256((ROOT/file).read_bytes()).hexdigest()==digest,('Source changed after build',file)

with zipfile.ZipFile(ROOT/'Backups/PreReferenceRevision/Source.zip') as backup:
    original=sorted((ROOT/'Assets/ChickenRush/Art/Chicken').glob('*.png'));assert len(original)==54
    for path in original:assert path.read_bytes()==backup.read(path.relative_to(ROOT).as_posix()),path.name
quality=[]
for path in sorted((ROOT/'Assets/ChickenRush/Art/Arcade').glob('*.png')):
    im=Image.open(path).convert('RGBA')
    if path.stem!='RoadSurface':
        a=im.getchannel('A');assert a.getextrema()[0]==0
        for region in [(0,0,im.width,8),(0,im.height-8,im.width,im.height),(0,0,8,im.height),(im.width-8,0,im.width,im.height)]:assert a.crop(region).getextrema()==(0,0),(path.name,region)
    quality.append(dict(file=path.name,size=im.size))
(OUT/'AssetQuality.json').write_text(json.dumps({'chickenFramesUnchanged':54,'nativeArcadeAssets':quality,'transparentSpriteBordersVerified':True},indent=2))

font_path=Path('C:/Windows/Fonts/arialbd.ttf');font=ImageFont.truetype(str(font_path),24);small=ImageFont.truetype(str(font_path),18)
sheet=Image.new('RGB',(1800,1300),'#132234');draw=ImageDraw.Draw(sheet)
draw.text((28,18),'CHICKEN RUSH  /  REFERENCE REVISION',font=font,fill='#fff3dc')
def paste(file,box,label):
    im=Image.open(file).convert('RGB');im.thumbnail((box[2],box[3]),Image.Resampling.LANCZOS)
    sheet.paste(im,(box[0],box[1]));draw.text((box[0],box[1]+box[3]+7),label,font=small,fill='#a8c7d1')
paste(OUT/'1920x1080-Gameplay.png',(28,65,1160,653),'1920 x 1080 — side panel, broad road, centered landing pads')
paste(OUT/'390x844-Gameplay.png',(1215,65,250,550),'390 x 844')
paste(OUT/'360x800-Gameplay.png',(1510,65,250,550),'360 x 800')
paste(OUT/'844x390-Gameplay.png',(1215,675,545,252),'844 x 390 — dedicated side controls')
paste(OUT/'1366x768-Gameplay.png',(28,785,535,301),'1366 x 768 / fractional DPR')
paste(OUT/'1280x720-Gameplay.png',(590,785,535,301),'1280 x 720')
paste(OUT/'768x1024-Gameplay.png',(1215,980,210,280),'768 x 1024')
paste(OUT/'1600x900-Gameplay.png',(1450,990,310,174),'1600 x 900')
sheet.save(OUT/'Resolution-Matrix.jpg',quality=94)

comparison=Image.new('RGB',(1600,530),'#132234');d=ImageDraw.Draw(comparison)
for file,x,label in [(ROOT/'QA/Overhaul/1920x1080-Gameplay.png',20,'BEFORE: rejected presentation'),(OUT/'1920x1080-Gameplay.png',820,'AFTER: reference-inspired revision')]:
    im=Image.open(file).convert('RGB');im.thumbnail((760,428));comparison.paste(im,(x,55));d.text((x,15),label,font=font,fill='#fff3dc')
comparison.save(OUT/'Before-After.jpg',quality=94)
passed=sum(int(t['passed']) for t in tests);fps=statistics.median(c['fpsMedian'] for c in qa['cases'])
rows='\n'.join(f"| {c['width']}×{c['height']} | {c['mode']} | {c['dpr']} | {'Touch emulation' if c['touch'] else 'Mouse / keys'} | Passed | {c['fpsMedian']} | {c.get('browserRafHz','Not sampled')} |" for c in qa['cases'])
text=f'''# Chicken Rush 3.0 — implementation and testing report

Status: ready for client visual review, with physical-device/browser-release
coverage outstanding. Existing Unity project modified; no replacement project.

## Reference comparison

The supplied [video](https://www.youtube.com/watch?v=KQrMVcBXHFw) was accessible
through the browser. Representative gameplay views were inspected and captured
in the working session at0:29,0:58 and1:56, with the opening at0:00. This was
sampled inspection, not a claim to have watched all4:51. The
[official reference description](https://stake.com/casino/games/chicken) was
also inspected. No reference artwork or branding is included in the game.

The local18.63-second Classic Crossy Roads.mp4 is a recording of the rejected
previous build. Six baseline frames are under Reference/; it is not mislabeled
as original Stake gameplay. Browser-session reference captures were not bundled
as a downloaded video. Exact visual fidelity is not claimed.

| Comparison | Previous implementation | Corrective result |
|---|---|---|
| Road | Thin repetitive green strips | Broad continuous quiet dark asphalt, sparse markings |
| Safe position | Divider strip | Raised oval pad on each lane center; protective barrier |
| Scale | Tiny chicken / decorative flat cars | Prominent original chicken and larger dimensional vehicles |
| View | Flat top-down vehicle symbols | Restrained raised front/rear views without dramatic isometric rotation |
| Multipliers | Small labels at screen edge | Large badges immediately beneath pads; next pad highlighted |
| Desktop controls | Wide bottom dock | Large multiplier and stacked actions in a side panel |
| Core risk | Timed traffic avoidance | Seeded per-hop outcomes, explicit chance and potential coin reward |

![Before and after](Before-After.jpg)

## Implemented gameplay and presentation

Easy/Medium/Hard/Hardcore have configurable survival arrays, increasing rewards,
and24/20/16/12 lanes. Default hop time is0.62s. One accepted input commits one
outcome; input timing and cosmetic traffic never consume randomness. SeededRisk
uses a reproducible counter hash, not a certified or cryptographic fairness
service. No real-money mechanism, account system, purchase or withdrawal exists.

RoundRules remains currency/state authority. No duplicate stake deduction,
lane skip, mid-hop cash-out, duplicate payout or contradictory Land/Hit callback
is accepted. The current multiplier, next multiplier, next payout, success chance,
selected risk and secured result are visible. Integer coin rewards round down
consistently. Defaults use cumulative survival to construct the reward table;
tests verify that relationship. No exact RTP is advertised.

Traffic visibly stages each crossing. A losing vehicle makes swept geometric
contact before Hit; successful traffic brakes behind protection or passes clear.
Near-miss effects require positive measured body clearance. Cleared lanes gain
barriers; opposing lanes use separate front/rear artwork. Twelve recycled road
segments and36 cars extend the visible course without growing their pools.

The original54 chicken PNGs are byte-identical to the rollback source. Idle,
Walk, Hop, Hit and Celebrate remain; Danger adds a subtle lean using existing
idle drawings. Hit and Celebrate hold until reset. Anticipation, flight, landing,
collision effects, reward feedback, passing audio and a low progress pulse are
connected to the actual state. No new hand-drawn danger sequence is claimed.

## Original art and rendering

Ten original shaded vehicle sprites cover compact/taxi/van/truck/delivery in
both directions. New grates, barriers, foliage, paving and quiet painted asphalt
share rounded forms, upper-left highlights and restrained shadows. The coarse
first asphalt pass and barrier-over-vehicle composition were rejected and fixed
after actual-player visual review.

Native cutouts retain proportions and resolution. Low-alpha noise below24/255
was removed during inspected extraction;8px fully transparent borders are
verified. Exact cuts and prompts are in SourceArt/ReferenceRevision/. Final
assets are under Assets/ChickenRush/Art/Arcade/. The chicken remains a roughly
130–140px native silhouette; scaling does not create additional detail. Small
pose differences from the supplied sheet remain. This is a client-review build,
not a claim of a hand-authored production animation master.

UI retains lossless RGBA32 atlas sources and9-slice card borders. World atlases
use ETC2 RGBA, bilinear filtering, no mipmaps, padded packing and no rotation.
The existing URP unlit path, no HDR/post-processing/shadows, pooled particles,
single-thread WebAssembly,64MiB initial heap and512MiB maximum remain. DPR is
capped at2. Gzip fallback works on ordinary static hosting.

## Validation

Unity6000.3.10f1: **{passed} tests passed**, no skipped or failed tests. EditMode
tests include seed frequency/reproducibility, chance/reward math, rule guards,
next-reward rounding, physical winning/losing traffic choreography across all
four modes and configured lanes, pool reuse, geometry, source pivots and actual
nine-slice mesh corners. Four PlayMode tests exercise the real Animator,
including Danger→Hop and terminal interruption guards.

Final WebGL build: **{build['result']}**, {build['errors']} errors,
{build['warnings']} warnings, {build['bytes']:,} bytes reported by Unity,
{build['seconds']:.2f}s. BuildInputs.json hashes all Assets/Packages/ProjectSettings
inputs before compilation; report generation verifies they still match.
BuildArtifacts.json hashes the delivered files; gzip/WASM magic and ZIP integrity
are audited by the packaging step. No unrelated project was changed.

Browser: **{qa['browser']}**, Windows headless, normal browser autoplay policy.
All inputs are real mouse/key/touch-emulation events. No seed/result/currency
is injected. Opt-in read-only QA telemetry exposes public chances and current
state, not future outcomes.

| Viewport | Layout | DPR | Input | Result | Sparse Unity delta estimate | Browser callback Hz |
|---|---|---|---|---|---|---|
{rows}

All cases verify full viewport/no scroll, no clipped/ellipsized UI text, separate
gameplay/control regions, >=44px main targets, both traffic directions, one hop
per input, cash-out blocking during a hop, no duplicate payout, win/lose states,
pause/resume, replay and persisted-balance reload. Desktop reaches lane13 and
checks all four difficulty choices. Portrait390×844 rotates live to844×390 and
back without changing the round or balance. A label reformatting bug found in
that test was corrected and retested.

The median of per-case sparse Unity delta estimates is {fps:.1f}. Those reciprocal
delta-time samples are not a sustained rendered-FPS measurement. Browser callback
cadence is sampled independently over1.8 seconds per case. Both are indicative
workstation observations, not a physical-device GPU performance certification.
WASM heap readings, when exposed by the player, are recorded in BrowserQA.json;
they are not total browser-process memory. Target is60 rendered FPS on capable
devices with appropriate authored sprite timings, not60 unique drawings/sec.

![Resolution review](Resolution-Matrix.jpg)

## Remaining limits

Physical iOS/Android, Safari/Firefox, hardware-notch handling and phone audio
output have not been separately tested. Emulated touch and safe-area-aware code
do not replace those checks. The camera composition is compared with sampled
reference views; original artwork/UI styling and the disclosed virtual risk
tables intentionally remain this project's own implementation.

## Delivery and reproduction

- Existing updated source: D:/30 games/Classic Crossy Roads
- Player: Builds/WebGL/; archive: Builds/ChickenRush-WebGL.zip
- Source archive: Builds/ChickenRush-UnitySource.zip
- Run: python Tools/serve.py → http://127.0.0.1:8783/
- Validate/build: python Tools/reference_build.py
- Browser QA: python Tools/reference_qa.py
- Package/report: python Tools/package_build.py; python Tools/write_reference_report.py
- Rollback: Backups/PreReferenceRevision/Source.zip and ChickenRush-v2-WebGL.zip

Diagnostics from the initial visual/rotation pass are retained separately;
current screenshots and BrowserQA.json refer to the final compiled player.
'''
for original,replacement in [('Unity6000','Unity 6000'),('at0:29','at 0:29'),('all4:51','all 4:51'),('local18.63','local 18.63'),('original54','original 54'),('and36','and 36'),('segments and36','segments and 36'),('and24/','and 24/'),('is0.62s','is 0.62s'),('below24/','below 24/'),(';8px','; 8px'),('at2.','at 2.'),('over1.8','over 1.8'),('target is60','target is 60')]:text=text.replace(original,replacement)
(OUT/'Report.md').write_text(text,encoding='utf-8')
(ROOT/'QA/Validation.md').write_text('# Current validation\n\nSee [Reference revision report](ReferenceRevision/Report.md) for the current3.0 build. QA/Overhaul is historical2.0 evidence.\n',encoding='utf-8')
with zipfile.ZipFile(ROOT/'Builds/ChickenRush-WebGL.zip') as archive:assert archive.testzip() is None
source_zip=ROOT/'Builds/ChickenRush-UnitySource.zip'
with zipfile.ZipFile(source_zip,'w',zipfile.ZIP_DEFLATED) as archive:
    for folder in ['Assets','Packages','ProjectSettings','Tools','Docs','SourceArt']:
        for path in sorted((ROOT/folder).rglob('*')):
            if path.is_file() and '__pycache__' not in path.parts:archive.write(path,path.relative_to(ROOT))
    archive.write(ROOT/'README.md','README.md')
    for path in sorted(OUT.rglob('*')):
        if path.is_file() and 'Diagnostics' not in path.parts:archive.write(path,path.relative_to(ROOT))
    for path in [ROOT/'QA/TestSummary.json',ROOT/'QA/BuildReport.json',ROOT/'QA/BuildArtifacts.json',ROOT/'QA/EditMode-results.xml',ROOT/'QA/PlayMode-results.xml']:
        archive.write(path,path.relative_to(ROOT))
summary={'status':'Ready for client review; physical-device release testing outstanding','testsPassed':passed,'browserCasesPassed':len(qa['cases']),'buildErrors':build['errors'],'buildWarnings':build['warnings'],'sourceFilesVerified':len(inputs),'chickenFramesUnchanged':54,'sourceZipBytes':source_zip.stat().st_size}
(OUT/'DeliverySummary.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
