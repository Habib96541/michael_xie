param([string]$EditorVersion = '6000.3.10f1')
$taskRoot = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'Logs') | Out-Null
unity build $taskRoot --editor-version $EditorVersion --target WebGL --execute-method CoinFlipEditor.ProjectBuilder.BuildWebGL --log-file (Join-Path $taskRoot 'Logs\build.log')
if ($LASTEXITCODE -ne 0) { throw "Unity build failed ($LASTEXITCODE). See Logs/build.log." }
