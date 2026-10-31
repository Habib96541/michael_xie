param([int]$Port = 8080, [string]$Root = (Join-Path $PSScriptRoot '..\Builds\WebGL'))
$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
if (!(Test-Path -LiteralPath (Join-Path $rootPath 'index.html'))) { throw 'Build the game first using Drop Stack > Build WebGL in Unity.' }
$listener = [Net.HttpListener]::new()
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "Drop Stack is ready at http://localhost:$Port/ (Ctrl+C to stop)"
$mime = @{ '.html'='text/html; charset=utf-8'; '.js'='application/javascript'; '.wasm'='application/wasm'; '.css'='text/css'; '.json'='application/json'; '.png'='image/png'; '.svg'='image/svg+xml'; '.ico'='image/x-icon'; '.unityweb'='application/octet-stream'; '.data'='application/octet-stream' }
try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        try {
            $relative = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath).TrimStart('/')
            if (!$relative) { $relative = 'index.html' }
            $path = [IO.Path]::GetFullPath((Join-Path $rootPath $relative))
            if (!$path.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { $context.Response.StatusCode = 403 }
            elseif (!(Test-Path -LiteralPath $path -PathType Leaf)) { $context.Response.StatusCode = 404 }
            else {
                $type = $mime[[IO.Path]::GetExtension($path)]
                $context.Response.ContentType = $(if ($type) { $type } else { 'application/octet-stream' })
                $context.Response.AddHeader('Cross-Origin-Opener-Policy','same-origin')
                $context.Response.AddHeader('Cross-Origin-Embedder-Policy','require-corp')
                $context.Response.ContentLength64 = (Get-Item -LiteralPath $path).Length
                if ($context.Request.HttpMethod -ne 'HEAD') {
                    $stream = [IO.File]::OpenRead($path)
                    try { $stream.CopyTo($context.Response.OutputStream) } finally { $stream.Dispose() }
                }
            }
        } catch { $context.Response.StatusCode = 500; Write-Warning $_.Exception.Message }
        finally { $context.Response.Close() }
    }
} finally { $listener.Stop(); $listener.Close() }
