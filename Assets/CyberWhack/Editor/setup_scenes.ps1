param()

$sourceScene = "Assets/Scenes/SampleScene.unity"

function Make-SceneCopy($destPath, $guidStr) {
    Copy-Item $sourceScene $destPath -Force
    $metaPath = "$destPath.meta"
    $metaContent = @"
fileFormatVersion: 2
guid: $guidStr
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -Path $metaPath -Value $metaContent -NoNewline
    Write-Output "Created scene: $destPath with GUID: $guidStr"
}

Make-SceneCopy "Assets/CyberWhackScene.unity" "a1c2e3f4a5b6c7d8e9f0123456789abc"
Make-SceneCopy "Assets/CyberWhack/CyberWhackScene.unity" "b2c3d4e5f6a7b8c9d0e123456789abcd"
Make-SceneCopy "Assets/Scenes/CyberWhackScene.unity" "c3d4e5f6a7b8c9d0e1f23456789abcde"

# Update EditorBuildSettings to include Assets/CyberWhackScene.unity as primary scene
$buildSettingsPath = "ProjectSettings/EditorBuildSettings.asset"
$buildSettingsContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/CyberWhackScene.unity
    guid: a1c2e3f4a5b6c7d8e9f0123456789abc
  - enabled: 1
    path: Assets/CyberWhack/CyberWhackScene.unity
    guid: b2c3d4e5f6a7b8c9d0e123456789abcd
  - enabled: 1
    path: Assets/Scenes/CyberWhackScene.unity
    guid: c3d4e5f6a7b8c9d0e1f23456789abcde
  - enabled: 1
    path: Assets/Scenes/SampleScene.unity
    guid: 8c9cfa26abfee488c85f1582747f6a02
  m_configObjects:
    com.unity.input.settings.actions: {fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}
"@

Set-Content -Path $buildSettingsPath -Value $buildSettingsContent -NoNewline
Write-Output "Updated EditorBuildSettings.asset with CyberWhackScene as primary scene!"
