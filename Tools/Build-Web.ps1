param([string]$EditorPath)
$ErrorActionPreference = 'Stop'
$gardenProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gardenVersion = ((Get-Content (Join-Path $gardenProject 'ProjectSettings\ProjectVersion.txt') -First 1) -split ': ')[1]
if (!$EditorPath) {
    $gardenCandidates = @((Join-Path $env:LOCALAPPDATA "DropStackBuildTools\Unity-$gardenVersion\Editor\Unity.exe"),(Join-Path $env:ProgramFiles "Unity\Hub\Editor\$gardenVersion\Editor\Unity.exe"))
    foreach ($gardenCandidate in $gardenCandidates) {
        if ((Test-Path -LiteralPath $gardenCandidate) -and (Test-Path -LiteralPath (Join-Path (Split-Path $gardenCandidate) 'Data\PlaybackEngines\WebGLSupport\UnityEditor.WebGL.Extensions.dll'))) { $EditorPath = $gardenCandidate; break }
    }
}
if (!$EditorPath) { throw "Install Unity $gardenVersion with WebGL Build Support." }
$gardenLog = Join-Path $gardenProject 'Logs\WebGL-Build.log'
New-Item -ItemType Directory -Path (Split-Path $gardenLog) -Force | Out-Null
$gardenArguments = "-batchmode -nographics -buildTarget WebGL -projectPath `"$gardenProject`" -executeMethod BloomDoom.Editor.BloomDoomBuild.BuildWeb -quit -logFile `"$gardenLog`""
$gardenBuild = Start-Process -FilePath $EditorPath -ArgumentList $gardenArguments -WindowStyle Hidden -Wait -PassThru
if ($gardenBuild.ExitCode -ne 0) { throw "Unity build failed. Read $gardenLog" }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Web-Headers.txt') -Destination (Join-Path $gardenProject 'Builds\WebGL\_headers') -Force
Write-Host "Unity WebGL build ready at $gardenProject\Builds\WebGL"
