[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json"),
    [string]$Base = "",
    [string]$Head = "",
    [string[]]$ChangedFiles = @(),
    [switch]$Staged,
    [string]$DefaultBump = "patch",
    [string]$OverridesPath = ""
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

$resolveArguments = @{
    RepoRoot = $RepoRoot
    CatalogPath = $CatalogPath
    ManifestPath = $ManifestPath
    DefaultBump = $DefaultBump
    AsJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($Base)) {
    $resolveArguments.Base = $Base
}

if (-not [string]::IsNullOrWhiteSpace($Head)) {
    $resolveArguments.Head = $Head
}

if (-not [string]::IsNullOrWhiteSpace($OverridesPath)) {
    $resolveArguments.OverridesPath = $OverridesPath
}

if ($Staged) {
    $resolveArguments.Staged = $true
}

if ($ChangedFiles.Count -gt 0) {
    $resolveArguments.ChangedFiles = $ChangedFiles
}

$planJson = & (Join-Path $PSScriptRoot "Resolve-VersionPlan.ps1") @resolveArguments
if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    throw "Failed to resolve version plan."
}

$plan = $planJson | ConvertFrom-Json
$manifest = Get-PackageVersionManifest -ManifestPath $ManifestPath
$packagesById = @{}
foreach ($package in @($manifest.packages)) {
    $packagesById[[string]$package.packageId] = $package
}

foreach ($planPackage in @($plan.publishPackages)) {
    $packageId = [string]$planPackage.packageId
    if (-not $packagesById.ContainsKey($packageId)) {
        throw "Package '$packageId' was not found in '$ManifestPath'."
    }

    $packagesById[$packageId].version = [string]$planPackage.nextVersion
}

$manifest.packages = @($manifest.packages | Sort-Object packageId)
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ManifestPath -Encoding utf8

foreach ($package in @($manifest.packages)) {
    Set-ProjectVersion -RepoRoot $RepoRoot -ProjectPath ([string]$package.projectPath) -Version ([string]$package.version)
}

if (@($plan.publishPackages).Count -eq 0) {
    Write-Host "No packable projects require a version bump."
    return
}

Write-Host "Applied version updates:"
foreach ($package in @($plan.publishPackages | Sort-Object packageId)) {
    Write-Host ("- {0}: {1} -> {2} ({3})" -f $package.packageId, $package.currentVersion, $package.nextVersion, $package.reason)
}
