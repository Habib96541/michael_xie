"""Extract the installed uGUI package's TMP essentials without opening an import dialog."""
from pathlib import Path
import tarfile
root=Path(__file__).resolve().parents[1]
packages=list((root/'Library/PackageCache').glob('com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage'))
if not packages: raise SystemExit('Open/import the Unity project once to resolve uGUI first.')
count=0
with tarfile.open(packages[0],'r:gz') as archive:
    entries={m.name:m for m in archive.getmembers()}
    for name,member in entries.items():
        if not name.endswith('/pathname'): continue
        path=archive.extractfile(member).read().decode('utf-8').strip()
        target=(root/path).resolve()
        if not target.is_relative_to(root/'Assets'): raise ValueError(path)
        prefix=name.rsplit('/',1)[0]
        if prefix+'/asset' in entries:
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes(archive.extractfile(entries[prefix+'/asset']).read());count+=1
        else: target.mkdir(parents=True,exist_ok=True)
        if prefix+'/asset.meta' in entries:
            Path(str(target)+'.meta').write_bytes(archive.extractfile(entries[prefix+'/asset.meta']).read())
print(f'Imported {count} TMP essentials files from installed Unity package.')
