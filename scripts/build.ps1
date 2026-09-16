[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/CatosInventorySorter/CatosInventorySorter.csproj'
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "CatosInventorySorter build failed with exit code $LASTEXITCODE."
}
