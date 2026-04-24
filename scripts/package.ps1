param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = "dotnet"
$localDotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (Test-Path $localDotnet) {
    $dotnet = $localDotnet
}

& $dotnet publish (Join-Path $root "Jellyfin.Plugin.RatingSort\Jellyfin.Plugin.RatingSort.csproj") -c $Configuration --no-self-contained

$publishDir = Join-Path $root "Jellyfin.Plugin.RatingSort\bin\$Configuration\net9.0\publish"
$distDir = Join-Path $root "dist"
$zipPath = Join-Path $distDir "RatingSort_$Version.zip"

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath
}

$files = @(
    "Jellyfin.Plugin.RatingSort.dll",
    "Jellyfin.Plugin.RatingSort.deps.json"
)

$missing = $files | Where-Object { -not (Test-Path (Join-Path $publishDir $_)) }
if ($missing.Count -gt 0) {
    throw "Missing publish artifacts: $($missing -join ', ')"
}

Compress-Archive -Path ($files | ForEach-Object { Join-Path $publishDir $_ }) -DestinationPath $zipPath -CompressionLevel Optimal

$md5 = (Get-FileHash -Algorithm MD5 -LiteralPath $zipPath).Hash.ToLowerInvariant()
Write-Host "Created $zipPath"
Write-Host "MD5 $md5"
