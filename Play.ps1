param([int]$Port = 8765)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
Write-Host "Open http://localhost:$Port in your browser. Press Ctrl+C here to stop."
python Tools/serve.py $Port
