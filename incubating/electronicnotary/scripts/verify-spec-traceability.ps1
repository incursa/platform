param(
    [string]$SpecsDirectory = "docs/specs",
    [string]$TraceabilityFile = "docs/testing/traceability.md"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $SpecsDirectory)) {
    throw "Specs directory not found: $SpecsDirectory"
}

if (-not (Test-Path -Path $TraceabilityFile)) {
    throw "Traceability file not found: $TraceabilityFile"
}

$scenarioPattern = 'PRF-[A-Z]+-\d{3}'
$specIds = New-Object System.Collections.Generic.HashSet[string]

Get-ChildItem -Path $SpecsDirectory -Filter *.md -File | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    foreach ($match in [regex]::Matches($content, $scenarioPattern)) {
        [void]$specIds.Add($match.Value)
    }
}

if ($specIds.Count -eq 0) {
    throw "No scenario IDs found in $SpecsDirectory."
}

$traceRows = @()
$lines = Get-Content -Path $TraceabilityFile
foreach ($line in $lines) {
    if ($line -notmatch '^\|\s*`?PRF-[A-Z]+-\d{3}`?\s*\|') {
        continue
    }

    $parts = $line.Trim('|').Split('|')
    if ($parts.Count -lt 4) {
        throw "Invalid traceability row format: $line"
    }

    $traceRows += [pscustomobject]@{
        ScenarioId = $parts[0].Trim().Trim('`')
        Status     = $parts[1].Trim().ToLowerInvariant()
        Test       = $parts[2].Trim().Trim('`')
        File       = $parts[3].Trim().Trim('`')
    }
}

if ($traceRows.Count -eq 0) {
    throw "No traceability rows found in $TraceabilityFile."
}

$traceIds = New-Object System.Collections.Generic.HashSet[string]
$errors = New-Object System.Collections.Generic.List[string]

foreach ($row in $traceRows) {
    [void]$traceIds.Add($row.ScenarioId)

    if ($row.Status -ne "covered" -and $row.Status -ne "planned") {
        $errors.Add("Scenario '$($row.ScenarioId)' has invalid status '$($row.Status)'.")
        continue
    }

    if ($row.Status -eq "planned") {
        continue
    }

    if ([string]::IsNullOrWhiteSpace($row.File) -or -not (Test-Path -Path $row.File)) {
        $errors.Add("Scenario '$($row.ScenarioId)' points to missing file '$($row.File)'.")
        continue
    }

    if ([string]::IsNullOrWhiteSpace($row.Test)) {
        $errors.Add("Scenario '$($row.ScenarioId)' must include test name when status=covered.")
        continue
    }

    $testPattern = [regex]::Escape($row.Test)
    $testFound = Select-String -Path $row.File -Pattern $testPattern -Quiet
    if (-not $testFound) {
        $errors.Add("Scenario '$($row.ScenarioId)' references test '$($row.Test)' not found in '$($row.File)'.")
    }
}

foreach ($id in $specIds) {
    if (-not $traceIds.Contains($id)) {
        $errors.Add("Scenario '$id' is documented in specs but missing from traceability matrix.")
    }
}

foreach ($id in $traceIds) {
    if (-not $specIds.Contains($id)) {
        $errors.Add("Scenario '$id' exists in traceability matrix but not in specs.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "Spec traceability verification failed."
}

Write-Host "Spec traceability verification passed. Scenarios: $($specIds.Count). Trace rows: $($traceRows.Count)."
