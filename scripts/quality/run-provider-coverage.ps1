param(
    [int]$LineThreshold = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet is required but not found."
    exit 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$coverageRoot = Join-Path $repoRoot "artifacts\codex\coverage"
$summaryPath = Join-Path $repoRoot "artifacts\codex\provider-coverage-summary.md"
New-Item -Path $coverageRoot -ItemType Directory -Force | Out-Null

$targets = @(
    @{
        Name = "SqlServer";
        Project = "tests/Incursa.Platform.SqlServer.Tests/Incursa.Platform.SqlServer.Tests.csproj";
        Filter = 'Category=Unit';
        Include = "[Incursa.Platform.SqlServer]*";
    },
    @{
        Name = "Postgres";
        Project = "tests/Incursa.Platform.Postgres.Tests/Incursa.Platform.Postgres.Tests.csproj";
        Filter = 'Category=Unit';
        Include = "[Incursa.Platform.Postgres]*";
    },
    @{
        Name = "InMemory";
        Project = "tests/Incursa.Platform.InMemory.Tests/Incursa.Platform.InMemory.Tests.csproj";
        Filter = 'Category=Unit';
        Include = "[Incursa.Platform.InMemory]*";
    }
)

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Provider Coverage Summary")
$summary.Add("")
$summary.Add("| Target | Filter | Result |")
$summary.Add("| --- | --- | --- |")

$failures = 0
$threshold = "$LineThreshold"
$thresholdType = "line"

foreach ($target in $targets) {
    $name = $target.Name
    $project = $target.Project
    $filter = $target.Filter
    $include = $target.Include

    $coverageOutputPrefix = Join-Path $coverageRoot $name
    Write-Host "Running coverage gate for $name..."
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
            Write-Warning "No matching tests found for $name. Skipping coverage gate."
            $summary.Add("| $name | $filter | Skipped (no matching tests) |")
            continue
        }

        $coverageFiles = Get-ChildItem -Path $coverageRoot -Filter "$name*.cobertura.xml" -ErrorAction SilentlyContinue
        if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
            throw "Coverage output file was not generated for ${name} under $coverageRoot"
        }

        $summary.Add("| $name | $filter | Passed (line >= $LineThreshold) |")
    } catch {
        $failures++
        $summary.Add("| $name | $filter | Failed coverage gate |")
        Write-Error "Coverage gate failed for $name."
    }
}

$summary | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Coverage summary written to $summaryPath"

if ($failures -gt 0) {
    exit 1
}
