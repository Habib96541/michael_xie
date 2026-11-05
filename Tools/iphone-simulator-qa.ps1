$ErrorActionPreference='Stop'
$projectPath=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Invoke-Editor([string]$code) {
    $raw=& unity command script eval --project-path $projectPath --caller plugin --skill unity-build-validation --code $code --format json
    $reply=$raw | ConvertFrom-Json
    if(-not $reply.success){throw ($reply.errors | ConvertTo-Json -Compress)}
    return $reply.data.result.result
}
$devices=@('Apple iPhone SE (2nd generation)','Apple iPhone 12 mini','Apple iPhone 12','Apple iPhone 12 Pro Max')
$checks=@()
foreach($device in $devices) {
    foreach($landscape in @($false,$true)) {
        $orientation=if($landscape){'landscape'}else{'portrait'}
        $key=($device -replace '[^a-zA-Z0-9]+','-')+'-'+$orientation
        $bool=$landscape.ToString().ToLowerInvariant()
        Invoke-Editor "UnityEngine.Object.FindFirstObjectByType<BrickRush.GameManager>().ShowMenu();BrickRush.Editor.IPhoneValidation.Select(`"$device`",$bool);return `"selected`";" | Out-Null
        Start-Sleep -Milliseconds 500
        Invoke-Editor "return BrickRush.Editor.IPhoneValidation.Audit(`"$key-menu`");" | Out-Null
        Invoke-Editor "BrickRush.Editor.IPhoneValidation.Press(`"LET'S RUSH   >`");UnityEngine.Object.FindFirstObjectByType<BrickRush.GameManager>().TogglePause();return `"paused`";" | Out-Null
        Start-Sleep -Milliseconds 300
        $remaining=Invoke-Editor "return UnityEngine.Object.FindFirstObjectByType<BrickRush.GameManager>().Remaining;"
        Invoke-Editor "return BrickRush.Editor.IPhoneValidation.Audit(`"$key-pause`");" | Out-Null
        $still=Invoke-Editor "return UnityEngine.Object.FindFirstObjectByType<BrickRush.GameManager>().Remaining;"
        if([Math]::Abs($remaining-$still) -gt .001){throw 'Pause timer moved'}
        Invoke-Editor "BrickRush.Editor.IPhoneValidation.Press(`"SETTINGS`");return `"settings`";" | Out-Null
        Start-Sleep -Milliseconds 200
        Invoke-Editor "return BrickRush.Editor.IPhoneValidation.Audit(`"$key-settings`");" | Out-Null
        Invoke-Editor "BrickRush.Editor.IPhoneValidation.Press(`"ALL SET   >`");BrickRush.Editor.IPhoneValidation.Press(`"MAIN MENU`");BrickRush.Editor.IPhoneValidation.Press(`"HOW TO PLAY`");return `"help`";" | Out-Null
        Start-Sleep -Milliseconds 200
        Invoke-Editor "return BrickRush.Editor.IPhoneValidation.Audit(`"$key-help`");" | Out-Null
        Invoke-Editor "BrickRush.Editor.IPhoneValidation.Press(`"GOT IT   >`");return `"menu restored`";" | Out-Null
        $checks+=[pscustomobject]@{device=$device;orientation=$orientation;passed=$true;pages=@('Main menu','Pause','Settings','How to play');pausedTimer=$remaining}
        Write-Host "PASS $device ${orientation}: menu, pause, settings, help, safe area and arena aspect"
    }
}
$checks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $projectPath 'TestResults/iPhone/unity-simulator.json')
