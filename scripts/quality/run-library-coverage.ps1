param(
    [int]$LineThreshold = 20,
    [int]$BranchThreshold = 0,
    [string[]]$Targets = @("Core", "InMemory", "Audit", "Correlation", "Email", "EmailPostmark", "HealthProbe", "Observability", "Operations", "Webhooks", "WebhooksAspNetCore")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet is required but not found."
    exit 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$coverageRoot = Join-Path $repoRoot "artifacts\codex\coverage\libraries"
$summaryPath = Join-Path $repoRoot "artifacts\codex\library-coverage-summary.md"
New-Item -Path $coverageRoot -ItemType Directory -Force | Out-Null

$configuredTargets = @(
    @{ Name = "Core"; Project = "tests/Incursa.Platform.Tests/Incursa.Platform.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform]*" },
    @{ Name = "Audit"; Project = "tests/Incursa.Platform.Audit.Tests/Incursa.Platform.Audit.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Audit]*" },
    @{ Name = "Correlation"; Project = "tests/Incursa.Platform.Correlation.Tests/Incursa.Platform.Correlation.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Correlation]*" },
    @{ Name = "Email"; Project = "tests/Incursa.Platform.Email.Tests/Incursa.Platform.Email.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Email]*" },
    @{ Name = "EmailPostmark"; Project = "tests/Incursa.Platform.Email.Postmark.Tests/Incursa.Platform.Email.Postmark.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Email.Postmark]*" },
    @{ Name = "HealthProbe"; Project = "tests/Incursa.Platform.HealthProbe.Tests/Incursa.Platform.HealthProbe.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.HealthProbe]*" },
    @{ Name = "InMemory"; Project = "tests/Incursa.Platform.InMemory.Tests/Incursa.Platform.InMemory.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.InMemory]*" },
    @{ Name = "Observability"; Project = "tests/Incursa.Platform.Observability.Tests/Incursa.Platform.Observability.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Observability]*" },
    @{ Name = "Operations"; Project = "tests/Incursa.Platform.Operations.Tests/Incursa.Platform.Operations.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Operations]*" },
    @{ Name = "Postgres"; Project = "tests/Incursa.Platform.Postgres.Tests/Incursa.Platform.Postgres.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Postgres]*" },
    @{ Name = "SqlServer"; Project = "tests/Incursa.Platform.SqlServer.Tests/Incursa.Platform.SqlServer.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.SqlServer]*" },
    @{ Name = "Webhooks"; Project = "tests/Incursa.Platform.Webhooks.Tests/Incursa.Platform.Webhooks.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Webhooks]*" },
    @{ Name = "WebhooksAspNetCore"; Project = "tests/Incursa.Platform.Webhooks.AspNetCore.Tests/Incursa.Platform.Webhooks.AspNetCore.Tests.csproj"; Filter = 'Category!=Integration&RequiresDocker!=true'; Include = "[Incursa.Platform.Webhooks.AspNetCore]*" }
)

$selectedTargets = @()
foreach ($targetName in $Targets) {
    $matched = $configuredTargets | Where-Object { $_.Name -eq $targetName } | Select-Object -First 1
    if ($null -eq $matched) {
        Write-Error "Unknown coverage target '$targetName'."
        exit 1
    }

    $selectedTargets += $matched
}

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Library Coverage Summary")
$summary.Add("")
$summary.Add("| Target | Filter | Result |")
$summary.Add("| --- | --- | --- |")

$failures = 0
$threshold = "$LineThreshold"
$thresholdType = "line"

if ($BranchThreshold -gt 0) {
    $threshold = "$LineThreshold,$BranchThreshold"
    $thresholdType = "line,branch"
}

foreach ($target in $selectedTargets) {
    $name = $target.Name
    $project = $target.Project
    $filter = $target.Filter
    $include = $target.Include

    $coverageOutputPrefix = Join-Path $coverageRoot $name
    Write-Host "Running library coverage gate for $name..."
    try {
        $dotnetArgs = @(
            "test"
            $project
            "--configuration"
            "Release"
            "--no-build"
            "--filter"
            $filter
            "/p:CollectCoverage=true"
            "/p:CoverletOutput=$coverageOutputPrefix"
            "/p:CoverletOutputFormat=cobertura"
            "/p:Threshold=$threshold"
            "/p:ThresholdType=$thresholdType"
            "/p:ThresholdStat=total"
            "/p:Include=$include"
        )

        $output = dotnet @dotnetArgs 2>&1

        $noMatchingTests = $false
        foreach ($line in $output) {
            if ($line -match "No test matches the given testcase filter") {
                $noMatchingTests = $true
                break
            }
        }

        if ($noMatchingTests) {
            throw "No matching tests found for $name coverage filter '$filter'."
        }

        $coverageFiles = @(
            Get-ChildItem -Path $coverageRoot -Filter "$name*.cobertura.xml" -ErrorAction SilentlyContinue
        )
        if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
            throw "Coverage output file was not generated for ${name} under $coverageRoot"
        }

        if ($BranchThreshold -gt 0) {
            $summary.Add("| $name | $filter | Passed (line >= $LineThreshold, branch >= $BranchThreshold) |")
        } else {
            $summary.Add("| $name | $filter | Passed (line >= $LineThreshold) |")
        }
    } catch {
        $failures++
        $summary.Add("| $name | $filter | Failed coverage gate |")
        Write-Host "Coverage gate failed for ${name}: $($_.Exception.Message)"
    }
}

$summary | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Coverage summary written to $summaryPath"

if ($failures -gt 0) {
    exit 1
}
