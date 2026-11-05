"""Run focused Unity test suites without requiring PowerShell script execution."""
import argparse
import json
from pathlib import Path
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--mode',choices=['all','EditMode','PlayMode'],default='all')
parser.add_argument('--editor',default=r'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe')
args=parser.parse_args()
results=[]
for mode in (['EditMode','PlayMode'] if args.mode=='all' else [args.mode]):
    assembly='ChickenRush.EditTests' if mode=='EditMode' else 'ChickenRush.PlayTests'
    report=ROOT/f'QA/{mode}-results.xml'
    log=ROOT/f'Logs/{mode}-tests.log'
    startup=subprocess.STARTUPINFO()
    startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW
    command=[args.editor,'-batchmode','-projectPath',str(ROOT),'-runTests','-testPlatform',mode,
        '-assemblyNames',assembly,'-testResults',str(report),'-logFile',str(log)]
    started=time.time()
    process=subprocess.Popen(command,cwd=ROOT,startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW)
    print(f'{mode} tests started (PID {process.pid}).',flush=True)
    code=process.wait()
    if report.exists() and report.stat().st_mtime>=started-1:
        run=ET.parse(report).getroot()
        result=dict(suite=mode,exitCode=code,**{key:run.get(key) for key in ['result','passed','failed','skipped','total']})
    else: result=dict(suite=mode,exitCode=code,result='No report produced')
    results.append(result)
    (ROOT/'QA/TestSummary.json').write_text(json.dumps(results,indent=2))
    print(json.dumps(result),flush=True)
raise SystemExit(0 if all(r['result']=='Passed' and r['exitCode']==0 for r in results) else 1)
