param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe')
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runningEditor = Get-CimInstance Win32_Process -Filter "name='Unity.exe'" | Where-Object {
    $_.CommandLine -like "*$projectRoot*" -and $_.CommandLine -notlike '*AssetImportWorker*'
}
if ($runningEditor) {
    $resultPath = Join-Path $projectRoot 'Temp/BlitzResult.txt'
    if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
    Set-Content -LiteralPath (Join-Path $projectRoot 'Temp/BlitzCommand.txt') -Value 'build'
    Write-Output 'Build requested in the open Unity Editor. Keep the Editor open.'
    $deadline = (Get-Date).AddMinutes(30)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $resultPath) {
            $result = Get-Content -LiteralPath $resultPath -Raw
            if ($result.StartsWith('build OK')) { Write-Output $result; exit 0 }
            throw $result
        }
        Start-Sleep -Milliseconds 500
    }
    throw 'Build did not complete within 30 minutes. Inspect the Unity Console.'
}
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Builds') | Out-Null
& $UnityPath -batchmode -quit -projectPath $projectRoot -executeMethod BullseyeBlitz.Editor.BlitzSetup.Build -logFile (Join-Path $projectRoot 'Builds/WebBuild.log')
if ($LASTEXITCODE -ne 0) { throw "Unity build failed with exit code $LASTEXITCODE. See Builds/WebBuild.log." }
