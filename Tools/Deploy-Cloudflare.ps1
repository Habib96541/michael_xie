param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9][a-z0-9-]*$')]
    [string]$ProjectName
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$webRoot = Join-Path $projectRoot 'Builds\WebGL'
& (Join-Path $PSScriptRoot 'Package-Web.ps1')
Push-Location $projectRoot
try {
    & npx --yes wrangler@4.156.0 pages deploy $webRoot "--project-name=$ProjectName" --branch=main --commit-dirty=true
    if ($LASTEXITCODE -ne 0) { throw 'Cloudflare deployment failed. Check the Wrangler output above.' }
} finally {
    Pop-Location
}
