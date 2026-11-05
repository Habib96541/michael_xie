"""Audit and package the actual compiled WebGL files."""
from pathlib import Path
import gzip
import hashlib
import json
import zipfile

root=Path(__file__).resolve().parents[1]
build=root/'Builds/WebGL'
wasm=build/'Build/WebGL.wasm.unityweb'
assert gzip.decompress(wasm.read_bytes())[:4]==b'\x00asm','Missing valid WebAssembly binary'
assert (build/'Build/WebGL.loader.js').stat().st_size>10000
assert (build/'Build/WebGL.framework.js.unityweb').stat().st_size>10000
assert (build/'Build/WebGL.data.unityweb').stat().st_size>10000
html=(build/'index.html').read_text()
assert '{{{' not in html,'Unexpanded Unity template'
assert 'safe-area-inset-top' in html
(build/'DEPLOY.txt').write_text('Chicken Rush: Cash Out! 3.0\n\nUpload this entire folder to static hosting.\nKeep Build/ and chicken.png next to index.html. The .unityweb files contain gzip\ndata and the loader includes a decompression fallback; custom Content-Encoding\nheaders are unnecessary. Do not double-compress .unityweb files.\n\nLocal play: python -m http.server 8783 --bind 127.0.0.1\nThen visit http://127.0.0.1:8783/ (not file://).\n\nSpace/Right: play/hop right. C: cash out. P/Escape/gear: pause. M: sound.\nMouse/touch buttons and road tapping are supported. Virtual coins only.\nEach hop has seeded odds. Input timing never changes those odds.\n',encoding='utf-8')
files=[]
for path in sorted(build.rglob('*')):
    if path.is_file(): files.append(dict(path=path.relative_to(build).as_posix(),bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(root/'QA/BuildArtifacts.json').write_text(json.dumps(dict(wasmMagicVerified=True,totalBytes=sum(f['bytes'] for f in files),files=files),indent=2))
destination=root/'Builds/ChickenRush-WebGL.zip'
with zipfile.ZipFile(destination,'w',compression=zipfile.ZIP_DEFLATED) as archive:
    for file in files:
        path=build/file['path'];archive.write(path,file['path'])
print(json.dumps(dict(zip=str(destination),bytes=destination.stat().st_size,files=len(files))))
