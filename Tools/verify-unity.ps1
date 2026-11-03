param([string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$runtimeRoot=Join-Path $projectRoot 'Library\CloudflareRuntimeProject'
$artifacts=Join-Path $projectRoot 'Artifacts'
if(!(Test-Path -LiteralPath (Join-Path $runtimeRoot 'Assets\Scenes\BackyardDojo.unity'))){throw 'Run Tools/build-web.ps1 first to prepare the export project.'}
foreach($script in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\FruitNinja\Scripts') -File){Copy-Item -LiteralPath $script.FullName -Destination (Join-Path $runtimeRoot 'Assets\FruitNinja\Scripts') -Force}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\FruitNinja\Editor\MobileVerification.cs') -Destination (Join-Path $runtimeRoot 'Assets\FruitNinja\Editor') -Force
$env:DOJO_VERIFY_ARTIFACTS=$artifacts
try {
    $arguments=@('-batchmode','-nographics','-projectPath',('"'+$runtimeRoot+'"'),'-buildTarget','WebGL','-executeMethod','BackyardDojo.Editor.MobileVerification.Run','-logFile',('"'+(Join-Path $artifacts 'mobile-verification.log')+'"'))
    $process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if($process.ExitCode -ne 0){throw ('Unity verification failed. See '+(Join-Path $artifacts 'mobile-verification.log'))}
    Get-Content -LiteralPath (Join-Path $artifacts 'mobile-result.txt')
} finally {Remove-Item -LiteralPath Env:DOJO_VERIFY_ARTIFACTS -ErrorAction SilentlyContinue}
