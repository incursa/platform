param(
    [string]$Solution,
    [string]$Configuration = "Release",
    [int]$LineThreshold = 45,
    [int]$BranchThreshold = 30,
    [string]$TestFilter = "Category!=Integration&RequiresDocker!=true",
    [switch]$NoBuild = $true
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Solution)) {
    $sln = Get-ChildItem -Path . -Filter *.slnx -File | Select-Object -First 1
    if (-not $sln) {
        $sln = Get-ChildItem -Path . -Filter *.sln -File | Select-Object -First 1
    }

    if (-not $sln) {
        throw "No solution file found at repository root."
    }

    $Solution = $sln.Name
}

$coverageOutDir = Join-Path (Get-Location) "artifacts/codex/coverage"
New-Item -Path $coverageOutDir -ItemType Directory -Force | Out-Null

$testArgs = @(
    "--solution", $Solution,
    "-c", $Configuration,
    "--filter", $TestFilter,
    "--",
    "--coverage",
    "--coverage-output-format", "cobertura"
)

if ($NoBuild.IsPresent) {
    $testArgs = @(
        "--solution", $Solution,
        "-c", $Configuration,
        "--no-build",
        "--filter", $TestFilter,
        "--",
        "--coverage",
        "--coverage-output-format", "cobertura"
    )
}

& dotnet test @testArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed while collecting coverage."
}

$coverageFile = Get-ChildItem -Path tests -Recurse -Filter *.cobertura.xml -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $coverageFile) {
    throw "No cobertura coverage file was produced."
}

$targetCoverageFile = Join-Path $coverageOutDir "coverage.cobertura.xml"
Copy-Item -Path $coverageFile.FullName -Destination $targetCoverageFile -Force

[xml]$coverageXml = Get-Content -Path $coverageFile.FullName
$lineRate = [double]$coverageXml.coverage.'line-rate'
$branchRate = [double]$coverageXml.coverage.'branch-rate'

$linePercent = [math]::Round($lineRate * 100, 2)
$branchPercent = [math]::Round($branchRate * 100, 2)

$summary = @(
    "# Coverage Summary",
    "",
    "Coverage File: $targetCoverageFile",
    "Line Coverage: $linePercent%",
    "Branch Coverage: $branchPercent%",
    "Line Threshold: $LineThreshold%",
    "Branch Threshold: $BranchThreshold%"
)
$summaryPath = Join-Path $coverageOutDir "coverage-summary.txt"
$summary | Set-Content -Path $summaryPath -Encoding ascii

Write-Host "Line coverage: $linePercent% (threshold: $LineThreshold%)"
Write-Host "Branch coverage: $branchPercent% (threshold: $BranchThreshold%)"

$failed = $false
if ($linePercent -lt $LineThreshold) {
    Write-Error "Line coverage threshold failed: $linePercent% < $LineThreshold%."
    $failed = $true
}

if ($branchPercent -lt $BranchThreshold) {
    Write-Error "Branch coverage threshold failed: $branchPercent% < $BranchThreshold%."
    $failed = $true
}

if ($failed) {
    throw "Coverage thresholds failed."
}

Write-Host "Coverage thresholds passed."
