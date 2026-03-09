param(
    [string]$Configuration = "Release",
    [string]$Version,
    [switch]$SkipRestore,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

$solution = Get-ChildItem -Path . -File -Filter *.slnx | Select-Object -First 1
if (-not $solution) {
    $solution = Get-ChildItem -Path . -File -Filter *.sln | Select-Object -First 1
}
if (-not $solution) {
    throw "No .slnx or .sln file found at repo root."
}

$outputDir = Join-Path $repoRoot "artifacts\packages"
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$packArgs = @(
    "pack", $solution.Name,
    "-c", $Configuration,
    "-o", $outputDir,
    "-p:ContinuousIntegrationBuild=true",
    "-p:Deterministic=true",
    "-p:EmbedUntrackedSources=true",
    "-p:IncludeSymbols=true",
    "-p:SymbolPackageFormat=snupkg"
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $packArgs += "-p:PackageVersion=$Version"
}

Write-Host "Packing solution: $($solution.Name)"
Write-Host "Output directory: $outputDir"
if ($Version) {
    Write-Host "Package version override: $Version"
}

if (-not $SkipRestore) {
    & dotnet restore $solution.Name
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }
}

if (-not $SkipBuild) {
    & dotnet build $solution.Name -c $Configuration --no-restore -p:ContinuousIntegrationBuild=true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}

& dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed with exit code $LASTEXITCODE."
}

Write-Host "Pack completed."
