"""Validate the current source, then compile the final WebGL player."""
from pathlib import Path
import subprocess
import sys

root=Path(__file__).resolve().parents[1]
result=subprocess.run([sys.executable,str(root/'Tools/run_unity_tests.py')],cwd=root)
if result.returncode:raise SystemExit(result.returncode)
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW
command=[r'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe','-batchmode','-projectPath',str(root),
    '-buildTarget','WebGL','-executeMethod','ChickenRushEditor.RushBuilder.BuildPlayerOnly','-logFile',str(root/'Logs/Overhaul-WebGL-Release.log')]
process=subprocess.Popen(command,cwd=root,startupinfo=startup,creationflags=subprocess.CREATE_NO_WINDOW)
print(f'Final WebGL build started (PID {process.pid}).',flush=True)
code=process.wait()
print(f'Final WebGL build exit code: {code}',flush=True)
raise SystemExit(code)
