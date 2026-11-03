param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$runtimeRoot=Join-Path $projectRoot 'Library\CloudflareRuntimeProject'
$artifacts=Join-Path $projectRoot 'Artifacts'
$output=Join-Path $projectRoot 'Builds\WebGL'

# This game renders with unlit Unity UI and uses its own fruit physics. Export
# the same runtime without unused animation, tilemap, render-pipeline or editor
# packages, which also keeps platform-specific package errors out of web builds.
$directories=@('Assets\FruitNinja\Scripts','Assets\FruitNinja\Resources','Assets\FruitNinja\Editor','Assets\Scenes','Assets\WebGLTemplates','Packages','ProjectSettings')
foreach($directory in $directories){New-Item -ItemType Directory -Force -Path (Join-Path $runtimeRoot $directory) | Out-Null}
New-Item -ItemType Directory -Force -Path $artifacts,$output | Out-Null
foreach($source in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\FruitNinja\Scripts') -File){Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $runtimeRoot 'Assets\FruitNinja\Scripts') -Force}
foreach($source in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\FruitNinja\Resources')){Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $runtimeRoot 'Assets\FruitNinja\Resources') -Recurse -Force}
foreach($source in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\WebGLTemplates')){Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $runtimeRoot 'Assets\WebGLTemplates') -Recurse -Force}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\FruitNinja\Editor\CloudflareWebBuild.cs') -Destination (Join-Path $runtimeRoot 'Assets\FruitNinja\Editor') -Force
foreach($scene in @('BackyardDojo.unity','BackyardDojo.unity.meta')){Copy-Item -LiteralPath (Join-Path $projectRoot ('Assets\Scenes\'+$scene)) -Destination (Join-Path $runtimeRoot 'Assets\Scenes') -Force}
foreach($settings in @('ProjectSettings.asset','ProjectVersion.txt')){Copy-Item -LiteralPath (Join-Path $projectRoot ('ProjectSettings\'+$settings)) -Destination (Join-Path $runtimeRoot 'ProjectSettings') -Force}
$manifest=@{
    dependencies=@{
        'com.unity.inputsystem'='1.18.0';'com.unity.ugui'='2.0.0';
        'com.unity.modules.audio'='1.0.0';'com.unity.modules.imageconversion'='1.0.0';
        'com.unity.modules.ui'='1.0.0';'com.unity.modules.uielements'='1.0.0';
        'com.unity.modules.jsonserialize'='1.0.0';'com.unity.modules.unitywebrequest'='1.0.0'
        'com.unity.modules.screencapture'='1.0.0'
    }
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runtimeRoot 'Packages\manifest.json') -Encoding utf8
$env:DOJO_WEB_OUTPUT=$output
$env:DOJO_WEB_ARTIFACTS=$artifacts
try {
    $arguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$runtimeRoot+'"'),'-buildTarget','WebGL','-executeMethod','BackyardDojo.Editor.CloudflareWebBuild.Build','-logFile',('"'+(Join-Path $artifacts 'web-runtime-build.log')+'"'))
    $process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -ne 0){throw ('Web build failed. See '+(Join-Path $artifacts 'web-runtime-build.log'))}
    Get-Content -LiteralPath (Join-Path $artifacts 'web-build-result.txt')
} finally {
    Remove-Item -LiteralPath Env:DOJO_WEB_OUTPUT,Env:DOJO_WEB_ARTIFACTS -ErrorAction SilentlyContinue
}
