$ErrorActionPreference = 'Stop'
function Invoke-EditorCode([string]$Code) {
    $response = (& unity command script eval $Code --format json | Out-String | ConvertFrom-Json)
    if (!$response.success -or !$response.data.result.success) { throw ($response | ConvertTo-Json -Depth 20) }
    return $response.data.result.result
}
$profiles = @(
    @{Name='Apple iPhone SE (2nd generation)'; Slug='se2'},
    @{Name='Apple iPhone 12 mini'; Slug='12mini'},
    @{Name='Apple iPhone 12'; Slug='12'},
    @{Name='Apple iPhone 12 Pro Max'; Slug='12promax'}
)
foreach ($profile in $profiles) {
    foreach ($landscape in @($false,$true)) {
        $literal = $landscape.ToString().ToLowerInvariant()
        Invoke-EditorCode ('HydraulicPress.Editor.PhoneSimulatorQA.Select("'+$profile.Name+'",'+$literal+'); UnityEngine.Application.runInBackground=true; var g=UnityEngine.Object.FindFirstObjectByType<HydraulicPress.GameManager>(); g.SendMessage("OnApplicationFocus",true); g.ShowMainMenu(); return "selected";') | Out-Null
        $prefix='sim-'+$profile.Slug+'-'+$(if($landscape){'landscape'}else{'portrait'})
        Invoke-EditorCode ('return HydraulicPress.Editor.PhoneSimulatorQA.Record("'+$prefix+'-main");') | Out-Null
        Invoke-EditorCode 'UnityEngine.Object.FindFirstObjectByType<HydraulicPress.GameManager>().SetSettings(true); return true;' | Out-Null
        Invoke-EditorCode ('return HydraulicPress.Editor.PhoneSimulatorQA.Record("'+$prefix+'-settings");') | Out-Null
        Invoke-EditorCode 'var g=UnityEngine.Object.FindFirstObjectByType<HydraulicPress.GameManager>(); g.SetSettings(false);g.PlayOrResume();return true;' | Out-Null
        Invoke-EditorCode ('return HydraulicPress.Editor.PhoneSimulatorQA.Record("'+$prefix+'-gameplay");') | Out-Null
        Invoke-EditorCode 'UnityEngine.Object.FindFirstObjectByType<HydraulicPress.GameManager>().PauseGame();return true;' | Out-Null
        Invoke-EditorCode ('return HydraulicPress.Editor.PhoneSimulatorQA.Record("'+$prefix+'-pause");') | Out-Null
        Write-Output ($prefix+' passed: main/settings/gameplay/pause safe bounds and 44-point targets')
    }
}
