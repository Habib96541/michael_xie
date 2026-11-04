"""Verify required build files, HTTP headers, source artifacts, and screenshots."""
import hashlib
from datetime import datetime, timedelta, timezone
import json
import urllib.request
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
build = root / 'Builds' / 'Web'
required = ['index.html','Build/Web.loader.js','Build/Web.framework.js','Build/Web.data','Build/Web.wasm']
manifest = {'unity':'6000.3.10f1','date':datetime.now(timezone(timedelta(hours=6))).date().isoformat(),'compression':'Disabled','files':[],'screenshots':[]}
for name in required:
    path = build / name
    assert path.is_file() and path.stat().st_size > 0, name
    request = urllib.request.Request('http://127.0.0.1:8765/' + name,method='HEAD')
    with urllib.request.urlopen(request) as response:
        assert response.status == 200
        headers = {'contentType':response.headers.get('Content-Type'),'contentLength':response.headers.get('Content-Length')}
    manifest['files'].append({'path':name,'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'http':headers})
assert manifest['files'][-1]['http']['contentType'].startswith('application/wasm')
for width,height in [(360,800),(720,1280),(768,1024),(1280,720),(1600,900)]:
    for kind in ['home','gameplay']:
        path = root / 'SprintDoc' / 'Tests' / f'{kind}-{width}x{height}.png'
        with Image.open(path) as image:
            assert image.size == (width,height)
        manifest['screenshots'].append(path.name)
for name in ['Assets/Bullseye/Scenes/BullseyeBlitz.unity','Assets/Bullseye/BlitzConfig.asset','Assets/Bullseye/Prefabs/BoothTarget.prefab','Assets/Bullseye/Prefabs/BoothDecoration.prefab','SprintDoc/Phase1Sprint1.txt']:
    assert (root / name).is_file(),name
(root / 'SprintDoc' / 'Tests' / 'DeliveryManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('PASS: Five build files and HTTP responses, WASM MIME type, ten required screenshots, scene/config/prefabs/sprint document.')
print('Build file bytes:',sum(item['bytes'] for item in manifest['files']))
