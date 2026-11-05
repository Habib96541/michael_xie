"""Reproducible current revision: optional asset generation, tests, WebGL build."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
EDITOR=r'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
parser=argparse.ArgumentParser();parser.add_argument('--prepare',action='store_true');parser.add_argument('--skip-tests',action='store_true');args=parser.parse_args()
def unity(method,log):
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW
    cmd=[EDITOR,'-batchmode','-projectPath',str(ROOT),'-buildTarget','WebGL','-executeMethod',method,'-logFile',str(ROOT/'Logs'/log)]
    process=subprocess.Popen(cmd,cwd=ROOT,startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW)
    print(f'{method} PID {process.pid}',flush=True)
    code=process.wait()
    if code:raise SystemExit(code)
if args.prepare:unity('ChickenRushEditor.RushBuilder.Prepare','Reference-Prepare.log')
if not args.skip_tests:
    code=subprocess.call([sys.executable,str(ROOT/'Tools/run_unity_tests.py')],cwd=ROOT)
    if code:raise SystemExit(code)
def inputs():
    return {str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
        for folder in ['Assets','ProjectSettings','Packages'] for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}
before=inputs()
(ROOT/'QA/ReferenceRevision/BuildInputs.json').write_text(json.dumps(before,indent=2))
unity('ChickenRushEditor.RushBuilder.BuildPlayerOnly','Reference-WebGL.log')
after=inputs()
if after!=before:raise RuntimeError('Unity inputs changed during compilation; rebuild the reviewed source before delivery.')
print('Reference revision WebGL build complete.',flush=True)
