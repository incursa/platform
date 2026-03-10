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
    [string]$OverridesPath = "",
    [switch]$AsJson
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

if (@("patch", "minor", "major") -notcontains $DefaultBump) {
    throw "DefaultBump must be one of: patch, minor, major."
}

$projects = @(Get-PackageCatalogProjects -CatalogPath $CatalogPath -PackableOnly)
$manifest = Get-PackageVersionManifest -ManifestPath $ManifestPath
$versionMap = Get-PackageVersionMap -Manifest $manifest
$changed = @(Get-ChangedFilesFromGit -Base $Base -Head $Head -ChangedFiles $ChangedFiles -Staged:$Staged)
$impact = Get-PublishImpact -RepoRoot $RepoRoot -Projects $projects -ChangedFiles $changed

$overrides = @{}
if (-not [string]::IsNullOrWhiteSpace($OverridesPath)) {
    $overrideDocument = Get-Content -LiteralPath $OverridesPath -Raw | ConvertFrom-Json
    foreach ($entry in @($overrideDocument.packages)) {
        $packageId = [string]$entry.packageId
        $bump = [string]$entry.bump
        if ([string]::IsNullOrWhiteSpace($packageId) -or [string]::IsNullOrWhiteSpace($bump)) {
            continue
        }

        $overrides[$packageId] = $bump
    }
}

$projectsByPath = @{}
foreach ($project in $projects) {
    $projectsByPath[[string]$project.projectPath] = $project
}

$publishPackages = @()
foreach ($projectPath in @($impact.publishProjectPaths | Sort-Object)) {
    $project = $projectsByPath[$projectPath]
    $packageId = [string]$project.packageId
    $currentVersion = $versionMap[$packageId]
    if ([string]::IsNullOrWhiteSpace($currentVersion)) {
        $currentVersion = Get-ProjectVersion -RepoRoot $RepoRoot -ProjectPath $projectPath
    }

    $bump = $DefaultBump
    if ($overrides.ContainsKey($packageId)) {
        $bump = Merge-VersionBump -Current $bump -Candidate $overrides[$packageId]
    }

    $reason = if ($impact.hasGlobalImpact) {
        "global-impact"
    }
    elseif ($impact.directlyChangedProjectPaths -contains $projectPath) {
        "changed"
    }
    else {
        "dependent"
    }

    $publishPackages += [ordered]@{
        packageId = $packageId
        projectPath = $projectPath
        currentVersion = $currentVersion
        bump = $bump
        nextVersion = Get-NextSemanticVersion -Version $currentVersion -Bump $bump
        reason = $reason
    }
}

$result = [ordered]@{
    changedFiles = $changed
    directlyChangedProjectPaths = @($impact.directlyChangedProjectPaths | Sort-Object)
    publishPackages = @($publishPackages | Sort-Object packageId)
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
}
else {
    $result.publishPackages
}
