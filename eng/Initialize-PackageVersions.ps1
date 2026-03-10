[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json"),
    [switch]$PublishableOnly
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

$projects = @(Get-PackageCatalogProjects -CatalogPath $CatalogPath -PackableOnly -PublishableOnly:$PublishableOnly)

$manifest = [ordered]@{
    packages = @(
        $projects |
            Sort-Object packageId |
            ForEach-Object {
                [ordered]@{
                    packageId = [string]$_.packageId
                    projectPath = [string]$_.projectPath
                    version = Get-ProjectVersion -RepoRoot $RepoRoot -ProjectPath ([string]$_.projectPath)
                }
            }
    )
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ManifestPath -Encoding utf8

foreach ($package in @($manifest.packages)) {
    Set-ProjectVersion -RepoRoot $RepoRoot -ProjectPath ([string]$package.projectPath) -Version ([string]$package.version)
}

Write-Host "Wrote package version manifest to '$ManifestPath'."
Write-Host "Synchronized explicit <Version> values for $($manifest.packages.Count) packable projects."
