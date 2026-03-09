Set-StrictMode -Version Latest

function Assert-DotNetAvailable {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet is required but was not found on PATH."
    }
}

function Get-QualityRepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $RepoRoot $Path)
}

function Initialize-ArtifactDirectory {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$Clean
    )

    if ($Clean -and (Test-Path $Path)) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -Path $Path -ItemType Directory -Force | Out-Null
    return (Resolve-Path $Path).Path
}

function Invoke-TestPrerequisites {
    param(
        [Parameter(Mandatory)]
        [string]$Solution,

        [Parameter(Mandatory)]
        [string]$Configuration,

        [switch]$NoRestore,

        [switch]$NoBuild
    )

    if (-not $NoRestore) {
        Write-Host "Restoring $Solution..." -ForegroundColor Cyan
        & dotnet restore $Solution
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }
    }

    if (-not $NoBuild) {
        $buildArgs = @(
            "build"
            $Solution
            "--configuration"
            $Configuration
            "--no-restore"
        )

        Write-Host "Building $Solution..." -ForegroundColor Cyan
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE."
        }
    }
}

function Get-TrxFiles {
    param(
        [Parameter(Mandatory)]
        [string]$ResultsDirectory
    )

    if (-not (Test-Path $ResultsDirectory)) {
        return @()
    }

    return @(
        Get-ChildItem -Path $ResultsDirectory -Filter *.trx -File -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName
    )
}

function Get-RelativeArtifactPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [Parameter(Mandatory)]
        [string]$Path
    )

    if ($Path.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($RepoRoot.Length).TrimStart('\', '/')
    }

    return $Path
}

function Write-TrxSummaryMarkdown {
    param(
        [Parameter(Mandatory)]
        [string]$Title,

        [Parameter(Mandatory)]
        [string]$ResultsDirectory,

        [Parameter(Mandatory)]
        [string]$SummaryPath,

        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [string]$EmptyMessage = "No .trx files were produced."
    )

    $relativeResultsDirectory = Get-RelativeArtifactPath -RepoRoot $RepoRoot -Path $ResultsDirectory

    $summaryLines = New-Object System.Collections.Generic.List[string]
    $summaryLines.Add("# $Title")
    $summaryLines.Add("")
    $summaryLines.Add(('- Results directory: `{0}`' -f $relativeResultsDirectory))

    $trxFiles = Get-TrxFiles -ResultsDirectory $ResultsDirectory

    if ($trxFiles.Count -eq 0) {
        $summaryLines.Add("- Outcome: $EmptyMessage")
        $summaryLines | Set-Content -Path $SummaryPath -Encoding UTF8

        return [pscustomobject]@{
            HasResults  = $false
            Total       = 0
            Passed      = 0
            Failed      = 0
            Skipped     = 0
            NotExecuted = 0
            SummaryPath = $SummaryPath
        }
    }

    $total = 0
    $passed = 0
    $failed = 0
    $skipped = 0
    $notExecuted = 0

    $summaryLines.Add("- TRX files: $($trxFiles.Count)")
    $summaryLines.Add("")
    $summaryLines.Add("| File | Outcome | Counts |")
    $summaryLines.Add("| --- | --- | --- |")

    foreach ($trxFile in $trxFiles) {
        $relativePath = Get-RelativeArtifactPath -RepoRoot $RepoRoot -Path $trxFile.FullName

        try {
            [xml]$trx = Get-Content -LiteralPath $trxFile.FullName
            $counters = $trx.TestRun.ResultSummary.Counters
            $outcome = [string]$trx.TestRun.ResultSummary.outcome

            $fileTotal = [int]$counters.total
            $filePassed = [int]$counters.passed
            $fileFailed = [int]$counters.failed
            $fileNotExecuted = [int]$counters.notExecuted

            $total += $fileTotal
            $passed += $filePassed
            $failed += $fileFailed
            $notExecuted += $fileNotExecuted

            $summaryLines.Add(('| `{0}` | {1} | total={2}, passed={3}, failed={4}, notExecuted={5} |' -f $relativePath, $outcome, $fileTotal, $filePassed, $fileFailed, $fileNotExecuted))
        } catch {
            $summaryLines.Add(('| `{0}` | unreadable | unable to parse .trx |' -f $relativePath))
        }
    }

    $summaryLines.Insert(3, "- Totals: total=$total, passed=$passed, failed=$failed, notExecuted=$notExecuted")
    $summaryLines | Set-Content -Path $SummaryPath -Encoding UTF8

    return [pscustomobject]@{
        HasResults  = $true
        Total       = $total
        Passed      = $passed
        Failed      = $failed
        Skipped     = $skipped
        NotExecuted = $notExecuted
        SummaryPath = $SummaryPath
    }
}

function Append-GitHubStepSummary {
    param(
        [Parameter(Mandatory)]
        [string]$SummaryPath
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY) -or -not (Test-Path $SummaryPath)) {
        return
    }

    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value (Get-Content -Path $SummaryPath -Raw)
    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value [Environment]::NewLine
}
