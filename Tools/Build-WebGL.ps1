param([string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $Editor)) { throw 'Unity 6000.3.10f1 and Web Build Support are required. Pass -Editor with its executable path.' }
$buildLog = Join-Path $projectRoot 'Reports\batch-build.log'
& $Editor -batchmode -quit -projectPath $projectRoot -buildTarget WebGL -executeMethod BoroughBuilder.BuildWeb -logFile $buildLog
if ($LASTEXITCODE -ne 0) { throw "Unity failed. Read $buildLog" }
python (Join-Path $PSScriptRoot 'package_build.py')
if ($LASTEXITCODE -ne 0) { throw 'Package verification failed.' }
