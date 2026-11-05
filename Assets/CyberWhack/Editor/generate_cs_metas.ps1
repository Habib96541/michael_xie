param()

$csFiles = Get-ChildItem -Path "Assets/CyberWhack" -Filter "*.cs" -Recurse

function Get-DeterministicGuid($name) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes("CyberWhack_CS_$name")
    $hash = (New-Object System.Security.Cryptography.MD5CryptoServiceProvider).ComputeHash($bytes)
    return [BitConverter]::ToString($hash).Replace("-", "").ToLower()
}

foreach ($f in $csFiles) {
    $metaPath = "$($f.FullName).meta"
    if (Test-Path $metaPath) { continue }
    $guid = Get-DeterministicGuid $f.Name

    $metaContent = @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -Path $metaPath -Value $metaContent -NoNewline
    Write-Output "Created meta for $($f.Name): $guid"
}
