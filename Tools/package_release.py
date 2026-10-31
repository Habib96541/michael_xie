"""Verify and zip the complete static distribution with hashes and license notices."""
from pathlib import Path
import gzip,hashlib,json,re,zipfile

root=Path(__file__).resolve().parents[1]
dist=root/'Builds/BrickRush_WebGL'
build=json.loads((root/'TestResults/build.json').read_text(encoding='utf-8-sig'))
assert build['result']=='Succeeded' and build['errors']==0
html=(dist/'index.html').read_text()
assert '{{{' not in html and 'file://' not in html
for filename in re.findall(r"Build/([A-Za-z0-9_.-]+)",html):assert (dist/'Build'/filename).is_file(),filename
wasm=next((dist/'Build').glob('*.wasm.unityweb'))
assert gzip.decompress(wasm.read_bytes())[:4]==b'\0asm','Invalid WebAssembly payload'
for name in ('Rajdhani-LICENSE.txt','Barlow-LICENSE.txt','LiberationSans-LICENSE.txt','EmojiOne-Attribution.txt','NOTICE.txt'):assert (dist/name).is_file()
files=[p for p in dist.rglob('*') if p.is_file() and p.name!='manifest.json']
manifest={'product':'Brick Rush','version':'1.0.0','unity':build['unity'],'compression':'gzip with Unity decompression fallback','files':[{'path':p.relative_to(dist).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(files)]}
(dist/'manifest.json').write_text(json.dumps(manifest,indent=2))
target=root/'Builds/BrickRush_WebGL.zip'
with zipfile.ZipFile(target,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
    for p in sorted(dist.rglob('*')):
        if p.is_file():archive.write(p,p.relative_to(dist).as_posix())
with zipfile.ZipFile(target) as archive:assert archive.testzip() is None and 'index.html' in archive.namelist()
print(f'Verified WebAssembly and {len(files)} distribution files; wrote {target} ({target.stat().st_size/1_000_000:.2f} MB).')
