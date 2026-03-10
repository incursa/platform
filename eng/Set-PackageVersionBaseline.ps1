[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json")
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

$manifest = Get-PackageVersionManifest -ManifestPath $ManifestPath

foreach ($package in @($manifest.packages)) {
    $package.version = $Version
}

$manifest.packages = @($manifest.packages | Sort-Object packageId)
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ManifestPath -Encoding utf8

foreach ($package in @($manifest.packages)) {
    Set-ProjectVersion -RepoRoot $RepoRoot -ProjectPath ([string]$package.projectPath) -Version $Version
}

Write-Host "Set $(@($manifest.packages).Count) packable package versions to '$Version'."
