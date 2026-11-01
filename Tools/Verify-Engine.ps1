$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe' -batchmode -projectPath $projectPath -executeMethod PegRushEngineChecks.Run -logFile (Join-Path $projectPath 'Logs/PegRushEngineChecks.log')
if ($LASTEXITCODE -ne 0) { throw 'Engine verification failed. See SprintDoc/EngineTests.txt and Logs/PegRushEngineChecks.log.' }
