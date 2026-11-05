$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$webRoot = Join-Path $projectRoot 'Builds\WebGL'
if (!(Test-Path -LiteralPath (Join-Path $webRoot 'index.html'))) { throw 'Complete a WebGL build before packaging.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\WebGLTemplates\DropStack\_headers') -Destination $webRoot
foreach ($webAsset in @('logo.svg', 'favicon.svg', 'viewport.js')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "Assets\WebGLTemplates\DropStack\$webAsset") -Destination $webRoot
}
$licenses = Join-Path $webRoot 'Licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\Fonts\Oxanium-LICENSE.txt') -Destination $licenses
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\Fonts\Nunito-LICENSE.txt') -Destination $licenses
$zipPath = Join-Path $projectRoot 'Builds\DropStack-WebGL.zip'
Compress-Archive -Path (Join-Path $webRoot '*') -DestinationPath $zipPath -Force
Write-Host "Upload-ready ZIP: $zipPath"
