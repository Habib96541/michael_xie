param([int]$Port = 8767, [switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildIndex = Join-Path $projectRoot 'Builds\WebGL\index.html'
if (-not (Test-Path -LiteralPath $buildIndex)) { throw "WebGL build missing: $buildIndex" }
$url = "http://127.0.0.1:$Port/"
function Get-Preview {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2
    } catch { return $false }
    if ($response.StatusCode -eq 200 -and $response.Content -match 'Pocket Borough') { return $true }
    throw "Port $Port is serving another application."
}
$startedProcess = $null
if (-not (Get-Preview)) {
    $python = (Get-Command python -ErrorAction Stop).Source
    $logDirectory = Join-Path $projectRoot 'Logs'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    $serveScript = Join-Path $PSScriptRoot 'serve.py'
    $startedProcess = Start-Process -FilePath $python -ArgumentList @(('"' + $serveScript + '"'), '--port', $Port) -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory 'preview-server.log') -RedirectStandardError (Join-Path $logDirectory 'preview-server-error.log')
    $deadline = (Get-Date).AddSeconds(12)
    do {
        if ($startedProcess.HasExited) { throw "Preview server exited. Read Logs\preview-server-error.log." }
        Start-Sleep -Milliseconds 200
        $ready = Get-Preview
    } while (-not $ready -and (Get-Date) -lt $deadline)
    if (-not $ready) { throw "Preview did not become ready at $url. Read Logs\preview-server-error.log." }
}
if (-not $NoBrowser) { Start-Process $url }
Write-Output "Pocket Borough is running at $url"
