from pathlib import Path
import shutil
import zipfile

root=Path(__file__).resolve().parents[1]
backup=root/'Backups/PreReferenceRevision'
backup.mkdir(parents=True,exist_ok=True)
target=backup/'Source.zip'
if not target.exists():
    with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED) as archive:
        for folder in ['Assets','Packages','ProjectSettings','Tools','Docs','QA/Overhaul']:
            for file in (root/folder).rglob('*'):
                if file.is_file():archive.write(file,file.relative_to(root))
        archive.write(root/'README.md','README.md')
    shutil.copy2(root/'Builds/ChickenRush-WebGL.zip',backup/'ChickenRush-v2-WebGL.zip')
print(target)
