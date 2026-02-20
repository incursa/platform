param(
    [switch]$FailOnThreshold
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet is required but not found."
    exit 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$summaryPath = Join-Path $repoRoot "artifacts\codex\library-mutation-summary.md"
New-Item -Path (Split-Path $summaryPath -Parent) -ItemType Directory -Force | Out-Null

try {
    dotnet tool restore | Out-Null
    $toolCheck = dotnet tool run dotnet-stryker -- --help 2>&1
    $toolCheckExitCode = $LASTEXITCODE
    $toolCheckText = $toolCheck -join [Environment]::NewLine
    if ($toolCheckExitCode -ne 0 -or $toolCheckText -match 'make the "dotnet-stryker" command available') {
        throw "dotnet-stryker is not restored."
    }
} catch {
    Write-Error "dotnet-stryker is not available. Install it with: dotnet tool install dotnet-stryker --local"
    exit 1
}

$configs = @(
    @{ Name = "SqlServer"; Required = $true; Path = "scripts/quality/stryker/sqlserver.stryker-config.json" },
    @{ Name = "Postgres"; Required = $true; Path = "scripts/quality/stryker/postgres.stryker-config.json" },
    @{ Name = "InMemory"; Required = $true; Path = "scripts/quality/stryker/inmemory.stryker-config.json" },
    @{ Name = "Operations"; Required = $false; Path = "scripts/quality/stryker/operations.stryker-config.json" },
    @{ Name = "Webhooks"; Required = $false; Path = "scripts/quality/stryker/webhooks.stryker-config.json" },
    @{ Name = "Observability"; Required = $false; Path = "scripts/quality/stryker/observability.stryker-config.json" }
)

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Library Mutation Summary")
$summary.Add("")
$summary.Add("Executed with dotnet-stryker using required and deferred library configs.")
$summary.Add("")
$summary.Add("| Target | Config | Result |")
$summary.Add("| --- | --- | --- |")

$failures = 0
foreach ($config in $configs) {
    $targetName = $config.Name
    $path = $config.Path
    $required = [bool]$config.Required
    $fullPath = Join-Path $repoRoot $path

    if (-not (Test-Path $fullPath)) {
        if ($required) {
            $failures++
            $summary.Add("| $targetName | $path | Failed (required config missing) |")
        } else {
            $summary.Add("| $targetName | $path | Deferred (optional config missing) |")
        }

        continue
    }

    Write-Host "Running mutation tests for $targetName using $path"
    try {
        $output = dotnet tool run dotnet-stryker -- --config-file $path 2>&1
        $runExitCode = $LASTEXITCODE
        $outputText = $output -join [Environment]::NewLine

        if ($runExitCode -ne 0 -or $outputText -match 'make the "dotnet-stryker" command available') {
            throw "dotnet-stryker failed for $targetName."
        }

        $summary.Add("| $targetName | $path | Passed |")
    } catch {
        if ($required) {
            $failures++
            $summary.Add("| $targetName | $path | Failed |")
        } else {
            $summary.Add("| $targetName | $path | Deferred (run failed; non-blocking) |")
        }
    }
}

$summary | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Mutation summary written to $summaryPath"

if ($FailOnThreshold -and $failures -gt 0) {
    exit 1
}
