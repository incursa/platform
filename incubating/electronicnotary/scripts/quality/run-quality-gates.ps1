param(
    [string]$Solution,
    [string]$Configuration = "Release",
    [int]$LineThreshold = 45,
    [int]$BranchThreshold = 30,
    [switch]$SkipFormat
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

pwsh -File scripts/verify-spec-traceability.ps1
if ($LASTEXITCODE -ne 0) {
    throw "Spec traceability verification failed."
}

if (-not $SkipFormat.IsPresent) {
    dotnet format $Solution --verify-no-changes
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet format verification failed."
    }
}

dotnet restore $Solution
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed."
}

dotnet build $Solution -c $Configuration --no-restore -p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

pwsh -File scripts/quality/run-coverage.ps1 `
    -Solution $Solution `
    -Configuration $Configuration `
    -LineThreshold $LineThreshold `
    -BranchThreshold $BranchThreshold `
    -NoBuild

if ($LASTEXITCODE -ne 0) {
    throw "Coverage gate failed."
}

Write-Host "Quality gates passed."
