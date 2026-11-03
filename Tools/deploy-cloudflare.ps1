param(
    [string]$ProjectName = 'fruit-ninja-backyard-dojo',
    [string]$Branch = 'main'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$webOutput = Join-Path $projectRoot 'Builds\WebGL'
if (!(Test-Path -LiteralPath (Join-Path $webOutput 'index.html'))) {
    throw 'Build the web player with ./Tools/build-web.ps1 in PowerShell 7 first.'
}
$oversize = Get-ChildItem -LiteralPath $webOutput -File -Recurse | Where-Object Length -gt 25MB
if ($oversize) { throw ('Cloudflare asset size limit exceeded: ' + ($oversize.Name -join ', ')) }
& npx.cmd --yes wrangler@4.156.2 pages deploy $webOutput --project-name $ProjectName --branch $Branch --commit-dirty=true
if ($LASTEXITCODE -ne 0) { throw 'Cloudflare deployment failed. Check the Wrangler output above.' }
