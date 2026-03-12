[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Message,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package-versions.json"),
    [ValidateSet("worktree", "staged")]
    [string]$Source = "worktree",
    [ValidateSet("patch", "minor", "major")]
    [string]$DefaultBump = "patch",
    [string]$OverridesPath = "",
    [switch]$Push,
    [switch]$SkipCommit
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "PackageVersioning.Common.ps1")

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-StagedFiles {
    $staged = & git diff --cached --name-only --diff-filter=ACMRTUXB
    if ($LASTEXITCODE -ne 0) {
        throw "git diff --cached --name-only failed with exit code $LASTEXITCODE."
    }

    return @(
        $staged |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { Normalize-RelativePath $_ } |
            Sort-Object -Unique
    )
}

$resolveArguments = @{
    RepoRoot = $RepoRoot
    CatalogPath = $CatalogPath
    ManifestPath = $ManifestPath
    DefaultBump = $DefaultBump
    Staged = $true
    AsJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($OverridesPath)) {
    $resolveArguments.OverridesPath = $OverridesPath
}

$applyArguments = @{
    RepoRoot = $RepoRoot
    CatalogPath = $CatalogPath
    ManifestPath = $ManifestPath
    DefaultBump = $DefaultBump
    Staged = $true
}

if (-not [string]::IsNullOrWhiteSpace($OverridesPath)) {
    $applyArguments.OverridesPath = $OverridesPath
}

Push-Location $RepoRoot
try {
    if ($Source -eq "worktree") {
        Invoke-Git -Arguments @("add", "--all", "--", ".")
    }

    $stagedFiles = @(Get-StagedFiles)
    if ($stagedFiles.Count -eq 0) {
        throw "No staged changes were found. Stage changes first or use -Source worktree."
    }

    $planJson = & (Join-Path $PSScriptRoot "Resolve-VersionPlan.ps1") @resolveArguments
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Failed to resolve version plan."
    }

    $plan = $planJson | ConvertFrom-Json

    & (Join-Path $PSScriptRoot "Apply-VersionPlan.ps1") @applyArguments
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Failed to apply version plan."
    }

    $pathsToStage = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
    $relativeManifestPath = Normalize-RelativePath ([System.IO.Path]::GetRelativePath($RepoRoot, $ManifestPath))
    [void]$pathsToStage.Add($relativeManifestPath)

    foreach ($package in @($plan.publishPackages)) {
        [void]$pathsToStage.Add([string]$package.projectPath)
    }

    if ($pathsToStage.Count -gt 0) {
        Invoke-Git -Arguments (@("add", "--") + @($pathsToStage | Sort-Object))
    }

    & (Join-Path $PSScriptRoot "Test-PackageVersionChanges.ps1") -RepoRoot $RepoRoot -CatalogPath $CatalogPath -ManifestPath $ManifestPath -Staged
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Version validation failed after applying the version plan."
    }

    if ($SkipCommit) {
        Write-Host "Prepared staged changes and validated package version bumps. Skipped commit."
        return
    }

    Invoke-Git -Arguments @("commit", "--no-verify", "-m", $Message)

    if ($Push) {
        Invoke-Git -Arguments @("push")
    }
}
finally {
    Pop-Location
}
