"""Package a verified WebGL build with its own local launcher and integrity manifest."""
from pathlib import Path
import hashlib, shutil, zipfile, json
project = Path(__file__).resolve().parents[1]
build = project / 'Builds/WebGL'
assert (build / 'index.html').is_file(), 'Build is missing index.html'
template = (project / 'Assets/WebGLTemplates/HydraulicPress/index.html').read_text()
for token, pattern in {'DATA_FILENAME':'*.data.gz','FRAMEWORK_FILENAME':'*.framework.js.gz','CODE_FILENAME':'*.wasm.gz','LOADER_FILENAME':'*.loader.js'}.items():
    matches = list((build / 'Build').glob(pattern))
    assert len(matches) == 1, f'Expected one {pattern}'
    template = template.replace('{{{ ' + token + ' }}}', matches[0].name)
(build / 'index.html').write_text(template)
shutil.copy2(project / 'Tools/serve.py', build / 'serve.py')
shutil.copy2(project / 'Assets/WebGLTemplates/HydraulicPress/_headers', build / '_headers')
shutil.copy2(project / 'ASSET_CREDITS.md', build / 'ASSET_CREDITS.md')
shutil.copy2(project / 'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt', build / 'LiberationSans-OFL.txt')
(build / 'Play Hydraulic Press.cmd').write_text('@echo off\ncd /d "%~dp0"\npython serve.py --open\n')
(build / 'README.txt').write_text('HYDRAULIC PRESS\n\nRun Play Hydraulic Press.cmd (Python 3 required).\nOr run python serve.py and visit http://localhost:8088.\n\nChoose PLAY from the main menu. CRUSH to continue. CASH OUT to bank.\nTap pause or press Escape for RESUME / SETTINGS / MAIN MENU.\nPhone: portrait and landscape supported. Virtual points only.\n\nFor other hosting, serve .gz files with Content-Encoding: gzip and\noriginal MIME types: .wasm.gz = application/wasm, .js.gz = application/javascript.\nDo not open index.html directly via file://.\n')
manifest = {}
for path in sorted(build.rglob('*')):
    if path.is_file() and path.name != 'sha256.json':
        manifest[path.relative_to(build).as_posix()] = {'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
(build / 'sha256.json').write_text(json.dumps(manifest, indent=2))
archive = project / 'Builds/HydraulicPress-WebGL.zip'
with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as out:
    for path in sorted(build.rglob('*')):
        if path.is_file(): out.write(path, Path('HydraulicPress') / path.relative_to(build))
print(json.dumps({'archive':str(archive),'bytes':archive.stat().st_size,'files':len(manifest)}, indent=2))
