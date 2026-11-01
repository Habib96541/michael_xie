param([int]$Port = 8080)
$buildPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Builds/WebGL'))
python -m http.server $Port --bind 127.0.0.1 --directory $buildPath
