"""Package and hash the complete Unity WebGL output without changing its files."""
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
build = root / "Builds" / "WebGL"
assert (build / "index.html").is_file(), "Unity index.html is missing"
payloads = [p.name for p in (build / "Build").iterdir() if p.is_file()]
for suffix in (".loader.js", ".data", ".framework.js", ".wasm"):
    assert any(suffix in name for name in payloads), f"Unity output missing {suffix}"
licenses = build / "Licenses"
licenses.mkdir(exist_ok=True)
shutil.copy2(root / "ASSET_CREDITS.md", licenses / "ASSET_CREDITS.md")
shutil.copy2(root / "Assets" / "TextMesh Pro" / "Fonts" / "LiberationSans - OFL.txt", licenses / "LiberationSans-OFL.txt")
files = sorted(p for p in build.rglob("*") if p.is_file())
archive = root / "Builds" / "HoleArena_WebGL_Desktop_Mobile.zip"
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as package:
    for item in files:
        package.write(item, item.relative_to(build).as_posix())
with zipfile.ZipFile(archive) as package:
    assert package.testzip() is None, "ZIP integrity failure"
    assert "index.html" in package.namelist(), "index.html must be at the ZIP root"
manifest = {"archive": str(archive), "archiveBytes": archive.stat().st_size,
            "archiveSha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
            "uncompressedBytes": sum(p.stat().st_size for p in files),
            "files": [{"path": p.relative_to(build).as_posix(), "bytes": p.stat().st_size,
                       "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in files]}
(root / "Reports" / "package-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps({k: v for k, v in manifest.items() if k != "files"}, indent=2))
