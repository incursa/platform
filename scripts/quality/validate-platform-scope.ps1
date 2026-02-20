param(
    [string]$RulesPath = "scripts/quality/platform-scope.rules.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $RulesPath)) {
    throw "Scope rules file not found: $RulesPath"
}

$rules = Get-Content -LiteralPath $RulesPath -Raw | ConvertFrom-Json

$approved = @($rules.approvedProviderAdapters)
$disallowed = @($rules.disallowedProviderCandidates)
$roots = @($rules.scanRoots)

if ($roots.Count -eq 0) {
    throw "No scanRoots configured in $RulesPath"
}

$violations = New-Object System.Collections.Generic.List[string]

$projectFiles = foreach ($root in $roots) {
    if (Test-Path -LiteralPath $root) {
        Get-ChildItem -LiteralPath $root -Recurse -Filter *.csproj -File
    }
}

foreach ($project in $projectFiles) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)

    foreach ($token in $disallowed) {
        if (-not $token) {
            continue
        }

        if ($name -notmatch [Regex]::Escape($token)) {
            continue
        }

        if ($approved -contains $token) {
            continue
        }

        $violations.Add("Unapproved provider token '$token' found in project '$($project.FullName)'.")
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Platform scope validation failed."
    Write-Host ""
    $violations | Sort-Object -Unique | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "Platform scope validation passed."
exit 0
