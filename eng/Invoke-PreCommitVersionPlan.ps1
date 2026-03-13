[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json")
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

Push-Location $RepoRoot
try {
    $resolveArguments = @{
        RepoRoot = $RepoRoot
        CatalogPath = $CatalogPath
        ManifestPath = $ManifestPath
        Staged = $true
        AsJson = $true
    }

    $planJson = & (Join-Path $PSScriptRoot "Resolve-VersionPlan.ps1") @resolveArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to resolve the staged version plan."
    }

    $plan = $planJson | ConvertFrom-Json
    $publishPackages = @($plan.publishPackages)
    if ($publishPackages.Count -eq 0) {
        Write-Host "No staged packable changes require version updates."
        return
    }

    & (Join-Path $PSScriptRoot "Apply-VersionPlan.ps1") -RepoRoot $RepoRoot -CatalogPath $CatalogPath -ManifestPath $ManifestPath -Staged
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to apply staged package version updates."
    }

    $pathsToStage = @("eng/package-versions.json") + @($publishPackages | ForEach-Object { [string]$_.projectPath })
    $pathsToStage = @($pathsToStage | Sort-Object -Unique)

    $unstagedTouchedPaths = @(& git diff --name-only -- @($pathsToStage))
    if ($LASTEXITCODE -ne 0) {
        throw "git diff failed while checking unstaged changes."
    }

    if ($unstagedTouchedPaths.Count -gt 0) {
        Write-Warning ("Pre-commit is re-staging full file contents for version-managed files: {0}" -f ($unstagedTouchedPaths -join ", "))
    }

    foreach ($path in $pathsToStage) {
        & git add -- $path
        if ($LASTEXITCODE -ne 0) {
            throw "git add failed for '$path'."
        }
    }

    & (Join-Path $PSScriptRoot "Test-PackageVersionChanges.ps1") -RepoRoot $RepoRoot -CatalogPath $CatalogPath -ManifestPath $ManifestPath -Staged
    if ($LASTEXITCODE -ne 0) {
        throw "Staged package version validation failed after auto-applying updates."
    }

    Write-Host ("Applied and staged package version updates for {0} affected packable project(s)." -f $publishPackages.Count)
}
finally {
    Pop-Location
}
