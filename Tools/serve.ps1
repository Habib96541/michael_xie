param([int]$Port = 8765)
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskBuild = Join-Path $taskRoot 'Builds\WebGL'
if (!(Test-Path -LiteralPath (Join-Path $taskBuild 'index.html'))) { throw 'Build missing. Run Coin Flip > Build WebGL in Unity first.' }
Write-Host "Coinflip: http://localhost:$Port"
python -m http.server $Port --bind 127.0.0.1 --directory $taskBuild
