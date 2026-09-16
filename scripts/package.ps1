[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$project = Join-Path $repoRoot 'src\CatosInventorySorter\CatosInventorySorter.csproj'
$dllPath = Join-Path $repoRoot 'src\CatosInventorySorter\bin\Release\net48\net48\CatosInventorySorter.dll'
$manifestPath = Join-Path $repoRoot 'thunderstore\manifest.json'
$iconPath = Join-Path $repoRoot 'thunderstore\icon.png'
$screenshotPath = Join-Path $repoRoot 'thunderstore\invsorter.png'
$readmePath = Join-Path $repoRoot 'thunderstore\README.md'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))

$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.name -ne 'CatosInventorySorter') { throw "Unexpected package name '$($manifest.name)'." }
if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') { throw 'Manifest version must use MAJOR.MINOR.PATCH format.' }
if ([string]::IsNullOrWhiteSpace($manifest.description) -or $manifest.description.Length -gt 250) {
    throw 'Manifest description must contain 1-250 characters.'
}
if ($manifest.dependencies -notcontains 'denikson-BepInExPack_Valheim-5.4.2350') {
    throw 'Manifest must declare BepInExPack Valheim 5.4.2350.'
}

foreach ($requiredPath in @($project, $manifestPath, $iconPath, $screenshotPath, $readmePath, $changelogPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required package file is missing: $requiredPath" }
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Thunderstore icon must be 256x256 pixels; found $($icon.Width)x$($icon.Height)."
    }
}
finally { $icon.Dispose() }

$screenshot = [System.Drawing.Image]::FromFile($screenshotPath)
$screenshot.Dispose()

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw "CatosInventorySorter build failed with exit code $LASTEXITCODE." }

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
$manifestVersion = [System.Version]::Parse($manifest.version_number)
if ($assemblyVersion.Major -ne $manifestVersion.Major -or
    $assemblyVersion.Minor -ne $manifestVersion.Minor -or
    $assemblyVersion.Build -ne $manifestVersion.Build) {
    throw "DLL version $assemblyVersion does not match manifest version $manifestVersion."
}

$packagePrefix = $artifactsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$packageRoot = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "package-$($manifest.version_number)"))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "CatosInventorySorter-$($manifest.version_number).zip"))
foreach ($path in @($packageRoot, $outputPath)) {
    if (-not $path.StartsWith($packagePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside artifacts directory: $path"
    }
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }
if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Force }
New-Item -ItemType Directory -Path $packageRoot | Out-Null

$files = @(
    @{ Source = $dllPath; Name = 'CatosInventorySorter.dll' },
    @{ Source = $manifestPath; Name = 'manifest.json' },
    @{ Source = $iconPath; Name = 'icon.png' },
    @{ Source = $readmePath; Name = 'README.md' },
    @{ Source = $changelogPath; Name = 'CHANGELOG.md' }
)
foreach ($file in $files) { Copy-Item -LiteralPath $file.Source -Destination (Join-Path $packageRoot $file.Name) }

$expected = @($files | ForEach-Object { $_.Name } | Sort-Object)
$actual = @(Get-ChildItem -LiteralPath $packageRoot -File | ForEach-Object { $_.Name } | Sort-Object)
if (@(Compare-Object -ReferenceObject $expected -DifferenceObject $actual).Count -ne 0) {
    throw 'Package staging directory contains unexpected files.'
}

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $outputPath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
Write-Host "Created $outputPath"
Write-Host "SHA256: $hash"
Write-Host "Contents: $($expected -join ', ')"
