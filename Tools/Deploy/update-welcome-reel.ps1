param([ValidateSet('Verify','Build')][string]$Action='Verify')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$copyRoot=Join-Path $projectRoot 'Logs\SpriteValidation'
if(!(Test-Path -LiteralPath (Join-Path $copyRoot 'ProjectSettings\ProjectVersion.txt'))){throw 'The isolated deployment project is missing.'}
foreach($folder in @('Scripts','Editor')){
    foreach($file in Get-ChildItem -LiteralPath (Join-Path $projectRoot ('Assets\LittleDepths\'+$folder)) -File){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $copyRoot ('Assets\LittleDepths\'+$folder)) -Force}
}
foreach($relative in @('Assets\LittleDepths\Art\Provided\ref_pause.png','Assets\LittleDepths\Art\Provided\ref_pause.png.meta','Assets\LittleDepths\Data\FishingBalance.asset','Assets\LittleDepths\Data\FishingBalance.asset.meta','Assets\Scenes\LittleDepths.unity','Assets\Scenes\LittleDepths.unity.meta')){
    Copy-Item -LiteralPath (Join-Path $projectRoot $relative) -Destination (Join-Path $copyRoot $relative) -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Tools\Validation\WelcomeReelValidation.cs') -Destination (Join-Path $copyRoot 'Assets\LittleDepths\Editor') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BuildCloudflareWebGL.cs') -Destination (Join-Path $copyRoot 'Assets\LittleDepths\Editor') -Force
$method=if($Action -eq 'Verify'){'WelcomeReelValidation.Run'}else{'BuildCloudflareWebGL.Run'}
$arguments=@('-batchmode','-projectPath',('"'+$copyRoot+'"'),'-buildTarget','WebGL','-executeMethod',$method,'-logFile',('"'+(Join-Path $projectRoot ('Logs\WelcomeReel-'+$Action+'.log'))+'"'))
if($Action -eq 'Verify'){$arguments+='-littleDepthsSmoke'}else{$arguments+='-quit';$arguments+='-nographics'}
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$process=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0){throw ('Unity '+$Action+' failed. Check Logs/WelcomeReel-'+$Action+'.log')}
if($Action -eq 'Verify'){Get-Content -LiteralPath (Join-Path $projectRoot 'Logs\WelcomeReel-Validation.txt')}
else{Get-Content -LiteralPath (Join-Path $projectRoot 'Logs\CloudflareBuild.status')}
