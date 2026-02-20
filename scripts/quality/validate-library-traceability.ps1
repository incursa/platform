param(
    [string]$SpecsRoot = "specs/libraries",
    [string]$MatrixPath = "specs/libraries/library-conformance-matrix.md",
    [string]$SummaryPath = "artifacts/codex/library-traceability-summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$specsRootPath = Join-Path $repoRoot $SpecsRoot
$matrixFullPath = Join-Path $repoRoot $MatrixPath
$summaryFullPath = Join-Path $repoRoot $SummaryPath

if (-not (Test-Path $specsRootPath)) {
    Write-Error "Specs root was not found: $specsRootPath"
    exit 1
}

if (-not (Test-Path $matrixFullPath)) {
    Write-Error "Matrix file was not found: $matrixFullPath"
    exit 1
}

New-Item -ItemType Directory -Force -Path (Split-Path $summaryFullPath -Parent) | Out-Null

$specFiles = @(
    Get-ChildItem -Path $specsRootPath -Filter "*.md" -File |
        Where-Object { $_.Name -ne (Split-Path $MatrixPath -Leaf) } |
        Sort-Object Name
)

if (-not $specFiles -or $specFiles.Count -eq 0) {
    Write-Error "No library spec files were found under $specsRootPath"
    exit 1
}

$allSpecIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)

foreach ($specFile in $specFiles) {
    $content = Get-Content $specFile.FullName -Raw
    $matches = [regex]::Matches($content, 'LIB-[A-Z0-9]+-[A-Z0-9]+-\d{3}')

    foreach ($match in $matches) {
        [void]$allSpecIds.Add($match.Value)
    }
}

$matrixIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$duplicateIds = New-Object System.Collections.Generic.List[string]
$invalidPaths = New-Object System.Collections.Generic.List[string]

$lineNumber = 0
foreach ($line in Get-Content $matrixFullPath) {
    $lineNumber++
    if ($line -match '^\|\s*(LIB-[A-Z0-9]+-[A-Z0-9]+-\d{3})\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*(Covered|Missing|Deferred)\s*\|\s*(.*?)\s*\|$') {
        $scenarioId = $matches[1]
        $status = $matches[4]
        $mappedCell = $matches[5]

        if ($matrixIds.Contains($scenarioId)) {
            $duplicateIds.Add($scenarioId) | Out-Null
        } else {
            [void]$matrixIds.Add($scenarioId)
        }

        $paths = @([regex]::Matches($mappedCell, '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
        if ($status -eq "Covered") {
            if (-not $paths -or $paths.Count -eq 0) {
                $invalidPaths.Add("$scenarioId (line $lineNumber): Covered row has no mapped path.") | Out-Null
            } else {
                foreach ($path in $paths) {
                    $candidate = Join-Path $repoRoot $path
                    if (-not (Test-Path $candidate)) {
                        $invalidPaths.Add("$scenarioId (line $lineNumber): Path not found '$path'.") | Out-Null
                    }
                }
            }
        }
    }
}

$missingFromMatrix = @()
foreach ($id in $allSpecIds) {
    if (-not $matrixIds.Contains($id)) {
        $missingFromMatrix += $id
    }
}

$unknownInMatrix = @()
foreach ($id in $matrixIds) {
    if (-not $allSpecIds.Contains($id)) {
        $unknownInMatrix += $id
    }
}

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Library Traceability Summary")
$summary.Add("")
$summary.Add("| Check | Result |")
$summary.Add("| --- | --- |")
$summary.Add("| Spec files discovered | $($specFiles.Count) |")
$summary.Add("| Spec IDs discovered | $($allSpecIds.Count) |")
$summary.Add("| Matrix IDs discovered | $($matrixIds.Count) |")
$summary.Add("| Missing IDs in matrix | $($missingFromMatrix.Count) |")
$summary.Add("| Unknown IDs in matrix | $($unknownInMatrix.Count) |")
$summary.Add("| Duplicate matrix IDs | $($duplicateIds.Count) |")
$summary.Add("| Invalid mapped paths | $($invalidPaths.Count) |")
$summary.Add("")

if ($missingFromMatrix.Count -gt 0) {
    $summary.Add("## Missing IDs")
    foreach ($id in ($missingFromMatrix | Sort-Object)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($unknownInMatrix.Count -gt 0) {
    $summary.Add("## Unknown Matrix IDs")
    foreach ($id in ($unknownInMatrix | Sort-Object)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($duplicateIds.Count -gt 0) {
    $summary.Add("## Duplicate Matrix IDs")
    foreach ($id in ($duplicateIds | Sort-Object -Unique)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($invalidPaths.Count -gt 0) {
    $summary.Add("## Invalid Coverage Rows")
    foreach ($entry in $invalidPaths) {
        $summary.Add("- $entry")
    }
    $summary.Add("")
}

$summary | Set-Content -Path $summaryFullPath -Encoding UTF8
Write-Host "Traceability summary written to $summaryFullPath"

if ($missingFromMatrix.Count -gt 0 -or $unknownInMatrix.Count -gt 0 -or $duplicateIds.Count -gt 0 -or $invalidPaths.Count -gt 0) {
    exit 1
}
