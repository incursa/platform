param(
    [string]$SourceRoot = "src",
    [string]$OutputPath = "artifacts/codex/public-api-surface.md"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $SourceRoot)) {
    throw "Source root not found: $SourceRoot"
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
New-Item -Path $outputDirectory -ItemType Directory -Force | Out-Null

$files = Get-ChildItem -Path $SourceRoot -Recurse -Filter *.cs -File |
    Sort-Object FullName

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Public API Surface")
$lines.Add("")
$lines.Add("Generated: $(Get-Date -Format o)")
$lines.Add("")

foreach ($file in $files) {
    $matches = Select-String -Path $file.FullName -Pattern '^\s*public\s+'
    if (-not $matches) {
        continue
    }

    $relativePath = [System.IO.Path]::GetRelativePath((Get-Location).Path, $file.FullName)
    $lines.Add("## $relativePath")
    $lines.Add("")
    $lines.Add("```csharp")
    foreach ($match in $matches) {
        $lines.Add($match.Line.Trim())
    }
    $lines.Add("```")
    $lines.Add("")
}

$lines | Set-Content -Path $OutputPath -Encoding ascii
Write-Host "Wrote public API surface to $OutputPath"
