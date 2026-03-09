[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$Configuration = "Release",
    [string]$OutputPath = (Join-Path $RepoRoot "nupkgs"),
    [string]$PackageVersion = "",
    [string]$Base = "",
    [string]$Head = "",
    [switch]$AffectedOnly,
    [switch]$PublishableOnly,
    [switch]$DryRun
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CatalogPath)) {
    throw "Package catalog not found at '$CatalogPath'. Run eng/Generate-PackageCatalog.ps1 first."
}

$catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json
$selectedProjects = @($catalog.projects | Where-Object { $_.packable })

if ($PublishableOnly) {
    $selectedProjects = @($selectedProjects | Where-Object { $_.publishable })
}

if ($AffectedOnly) {
    $resolveScript = Join-Path $PSScriptRoot "Resolve-AffectedProjects.ps1"
    $affectedJson = & $resolveScript -RepoRoot $RepoRoot -CatalogPath $CatalogPath -Base $Base -Head $Head -AsJson
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to resolve affected projects."
    }

    $affected = @((ConvertFrom-Json $affectedJson).projectPaths)
    $selectedProjects = @($selectedProjects | Where-Object { $affected -contains $_.projectPath })
}

if ($selectedProjects.Count -eq 0) {
    Write-Host "No public packages selected."
    return
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

foreach ($project in $selectedProjects | Sort-Object projectPath) {
    $projectPath = Join-Path $RepoRoot $project.projectPath
    $arguments = @(
        "pack",
        $projectPath,
        "--configuration", $Configuration,
        "--output", $OutputPath,
        "--no-restore",
        "-p:IsPackable=true"
    )

    if (-not [string]::IsNullOrWhiteSpace($PackageVersion)) {
        $arguments += @(
            "-p:Version=$PackageVersion",
            "-p:PackageVersion=$PackageVersion"
        )
    }

    Write-Host "Packing $($project.projectPath)"
    if ($DryRun) {
        Write-Host ("dotnet " + ($arguments -join " "))
        continue
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for '$($project.projectPath)' with exit code $LASTEXITCODE."
    }
}
