[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json"),
    [string]$Base = "",
    [string]$Head = "",
    [string[]]$ChangedFiles = @(),
    [switch]$Staged
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

$projects = @(Get-PackageCatalogProjects -CatalogPath $CatalogPath -PackableOnly)
$manifest = Get-PackageVersionManifest -ManifestPath $ManifestPath
$currentVersions = Get-PackageVersionMap -Manifest $manifest
$changed = @(Get-ChangedFilesFromGit -Base $Base -Head $Head -ChangedFiles $ChangedFiles -Staged:$Staged)
$impact = Get-PublishImpact -RepoRoot $RepoRoot -Projects $projects -ChangedFiles $changed

if (@($impact.publishProjectPaths).Count -eq 0) {
    Write-Host "No packable projects are affected by the current changes."
    return
}

$baseRef = $Base
if ($Staged -and [string]::IsNullOrWhiteSpace($baseRef)) {
    $baseRef = "HEAD"
}

if ([string]::IsNullOrWhiteSpace($baseRef)) {
    throw "A Base ref is required unless -Staged is used."
}

$relativeManifestPath = Normalize-RelativePath ([System.IO.Path]::GetRelativePath($RepoRoot, $ManifestPath))
$baseManifest = Read-JsonAtGitRef -Ref $baseRef -RelativePath $relativeManifestPath
if ($null -eq $baseManifest) {
    Write-Warning "Could not read '$ManifestPath' at '$baseRef'. Skipping version guard."
    return
}

$baseVersions = Get-PackageVersionMap -Manifest $baseManifest
$projectsByPath = @{}
foreach ($project in $projects) {
    $projectsByPath[[string]$project.projectPath] = $project
}

$missingBumps = New-Object System.Collections.Generic.List[object]
foreach ($projectPath in @($impact.publishProjectPaths | Sort-Object)) {
    $project = $projectsByPath[$projectPath]
    $packageId = [string]$project.packageId
    $currentVersion = $currentVersions[$packageId]
    $baseVersion = $baseVersions[$packageId]

    if ([string]::IsNullOrWhiteSpace($currentVersion) -or [string]::IsNullOrWhiteSpace($baseVersion)) {
        $missingBumps.Add([pscustomobject]@{
                packageId = $packageId
                projectPath = $projectPath
                currentVersion = $currentVersion
                baseVersion = $baseVersion
            }) | Out-Null
        continue
    }

    if ((Compare-SemanticVersions -Left $currentVersion -Right $baseVersion) -le 0) {
        $missingBumps.Add([pscustomobject]@{
                packageId = $packageId
                projectPath = $projectPath
                currentVersion = $currentVersion
                baseVersion = $baseVersion
            }) | Out-Null
    }
}

if ($missingBumps.Count -eq 0) {
    Write-Host "Validated package version bumps for $(@($impact.publishProjectPaths).Count) affected packable project(s)."
    return
}

Write-Error "Detected affected packable projects without a committed version bump. Run 'pwsh -File eng/Apply-VersionPlan.ps1' and commit the resulting manifest/project version updates."
foreach ($item in $missingBumps) {
    Write-Host ("- {0} ({1}): current '{2}', base '{3}'" -f $item.packageId, $item.projectPath, $item.currentVersion, $item.baseVersion)
}

exit 1
