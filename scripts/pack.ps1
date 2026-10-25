param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\HaldorIdols\HaldorIdols.csproj'
$manifestPath = Join-Path $root 'package\manifest.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.version_number

Write-Host "Building Haldor Idols $version..."
dotnet build $project -c $Configuration | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "src\HaldorIdols\bin\$Configuration\HaldorIdols.dll"
if (-not (Test-Path $dll)) { throw "Built assembly not found: $dll" }

$required = @(
    $manifestPath,
    (Join-Path $root 'package\icon.png'),
    (Join-Path $root 'README.md'),
    (Join-Path $root 'CHANGELOG.md')
)
foreach ($file in $required) {
    if (-not (Test-Path $file)) { throw "Missing package file: $file" }
}

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'stage'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Copy-Item $manifestPath $stage
Copy-Item (Join-Path $root 'package\icon.png') $stage
Copy-Item (Join-Path $root 'README.md') $stage
Copy-Item (Join-Path $root 'CHANGELOG.md') $stage

$pluginDir = Join-Path $stage 'BepInEx\plugins\HaldorIdols'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
Copy-Item $dll $pluginDir

$zip = Join-Path $dist "HaldorIdols-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Add-ZipEntry($archive, [string]$sourcePath, [string]$entryName) {
    $entry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = (Get-Item $sourcePath).LastWriteTime
    $input = [System.IO.File]::OpenRead($sourcePath)
    $output = $entry.Open()
    try { $input.CopyTo($output) } finally { $output.Dispose(); $input.Dispose() }
}

$stream = [System.IO.File]::Open($zip, [System.IO.FileMode]::Create)
$archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Add-ZipEntry $archive (Join-Path $stage 'manifest.json') 'manifest.json'
    Add-ZipEntry $archive (Join-Path $stage 'icon.png') 'icon.png'
    Add-ZipEntry $archive (Join-Path $stage 'README.md') 'README.md'
    Add-ZipEntry $archive (Join-Path $stage 'CHANGELOG.md') 'CHANGELOG.md'
    Add-ZipEntry $archive $dll 'BepInEx/plugins/HaldorIdols/HaldorIdols.dll'
} finally {
    $archive.Dispose()
    $stream.Dispose()
}

Remove-Item $stage -Recurse -Force

Write-Host ''
Write-Host "Package: $zip"
Get-Item $zip | Select-Object Name, Length | Format-Table | Out-Host
