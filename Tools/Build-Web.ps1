param([string]$EditorPath)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionLine = Get-Content (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') | Select-Object -First 1
$version = ($versionLine -split ': ')[1]
if (!$EditorPath) {
    $candidates = @(
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"),
        (Join-Path $env:LOCALAPPDATA "DropStackBuildTools\Unity-$version\Editor\Unity.exe")
    )
    foreach ($candidate in $candidates) {
        $module = Join-Path (Split-Path $candidate) 'Data\PlaybackEngines\WebGLSupport\UnityEditor.WebGL.Extensions.dll'
        if ((Test-Path -LiteralPath $candidate) -and (Test-Path -LiteralPath $module)) { $EditorPath = $candidate; break }
    }
}
if (!$EditorPath -or !(Test-Path -LiteralPath $EditorPath)) { throw "Install Unity $version with WebGL Build Support, or supply -EditorPath." }
$log = Join-Path $projectRoot 'Logs\WebGL-Build.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
$arguments = "-batchmode -buildTarget WebGL -projectPath `"$projectRoot`" -executeMethod DropStack.Editor.DropStackWebBuild.Build -quit -logFile `"$log`""
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity build failed. See $log" }
& (Join-Path $PSScriptRoot 'Package-Web.ps1')
Write-Host "Build ready: $(Join-Path $projectRoot 'Builds\WebGL\index.html')"
Write-Host 'Preview with: .\Tools\Serve-Web.ps1'
