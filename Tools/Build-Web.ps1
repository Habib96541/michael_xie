param([switch]$Setup)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$methodName = if ($Setup) { 'ShatterLeap.Editor.SprintWorkflow.Setup' } else { 'ShatterLeap.Editor.SprintWorkflow.BuildWeb' }
$editorProcess = Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object {
    $_.CommandLine -like "*$projectPath*" -and $_.CommandLine -notlike '*-adb2*'
}
if ($editorProcess) {
    $commandName = if ($Setup) { 'setup' } else { 'build' }
    Set-Content -LiteralPath (Join-Path $PSScriptRoot 'editor-command.txt') -Value $commandName
    Write-Output "Queued $commandName in the open editor. See Tools/editor-result.txt and SprintDoc/BuildReport.txt."
} else {
    $logPath = Join-Path $projectPath 'Logs/SprintBuild.log'
    $unityArguments = @('-batchmode', '-quit', '-projectPath', ('"' + $projectPath + '"'), '-executeMethod', $methodName, '-logFile', ('"' + $logPath + '"'))
    $buildProcess = Start-Process -FilePath $unityPath -ArgumentList $unityArguments -PassThru -Wait -WindowStyle Hidden
    if ($buildProcess.ExitCode -ne 0) { throw "Unity failed. See $logPath" }
}
