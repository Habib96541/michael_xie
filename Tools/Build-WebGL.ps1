$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe' -batchmode -quit -projectPath $projectPath -executeMethod PegRushAuthoring.Build -logFile (Join-Path $projectPath 'Logs/PegRushBuild.log')
if ($LASTEXITCODE -ne 0) { throw 'Unity WebGL build failed. See Logs/PegRushBuild.log.' }
