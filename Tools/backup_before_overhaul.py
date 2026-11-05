from pathlib import Path
import shutil
import zipfile

root=Path(__file__).resolve().parents[1]
folder=root/'Backups'
folder.mkdir(exist_ok=True)
destination=folder/'PreOverhaulSource.zip'
if not destination.exists():
    with zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED) as archive:
        for name in ['Assets/ChickenRush','Assets/WebGLTemplates/ChickenRush','Assets/Plugins','Tools','Docs','ProjectSettings']:
            for path in (root/name).rglob('*'):
                if path.is_file():archive.write(path,path.relative_to(root))
        archive.write(root/'README.md','README.md')
    shutil.copy2(root/'Builds/ChickenRush-WebGL.zip',folder/'ChickenRush-v1-WebGL.zip')
print(destination)
