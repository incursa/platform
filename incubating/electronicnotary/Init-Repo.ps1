param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Name
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptRoot

$templateName = ("Incursa" + ".Template")
$templateTestsName = "$templateName.Tests"
$templateClassName = ("Class" + "1")

$newName = $Name.Trim()
$newTestsName = "$newName.Tests"

$newClassName = [regex]::Replace(($newName.Split(".") | Select-Object -Last 1), "[^A-Za-z0-9_]", "")
if ([string]::IsNullOrWhiteSpace($newClassName)) {
    $newClassName = "Library"
}
if ($newClassName -match "^[0-9]") {
    $newClassName = "_$newClassName"
}
$newClassName = "${newClassName}Root"

$excludedDirs = @(
    ".git",
    "bin",
    "obj",
    "artifacts",
    "nupkgs",
    "ref",
    "refs",
    "reference",
    "references",
    ".reference",
    ".references"
)

function Test-IsExcludedPath {
    param([string]$Path)

    $full = [System.IO.Path]::GetFullPath($Path)
    foreach ($segment in $excludedDirs) {
        $pattern = "(^|[\\/])" + [regex]::Escape($segment) + "([\\/]|$)"
        if ($full -match $pattern) {
            return $true
        }
    }
    return $false
}

function Rename-IfNeeded {
    param(
        [string]$From,
        [string]$To
    )

    if ($From -eq $To) {
        return
    }

    if (-not (Test-Path -LiteralPath $From)) {
        return
    }

    if (Test-Path -LiteralPath $To) {
        return
    }

    Rename-Item -LiteralPath $From -NewName (Split-Path -Leaf $To)
    Write-Host "Renamed: $From -> $To"
}

Write-Host "Initializing template: '$templateName' -> '$newName'"

# First-pass canonical renames for known template structure.
Rename-IfNeeded -From "$templateName.slnx" -To "$newName.slnx"
Rename-IfNeeded -From "$templateName.sln" -To "$newName.sln"
Rename-IfNeeded -From (Join-Path "src" $templateName) -To (Join-Path "src" $newName)
Rename-IfNeeded -From (Join-Path "tests" $templateTestsName) -To (Join-Path "tests" $newTestsName)
Rename-IfNeeded -From (Join-Path "src\$newName" "$templateName.csproj") -To (Join-Path "src\$newName" "$newName.csproj")
Rename-IfNeeded -From (Join-Path "tests\$newTestsName" "$templateTestsName.csproj") -To (Join-Path "tests\$newTestsName" "$newTestsName.csproj")
Rename-IfNeeded -From (Join-Path "src\$newName" "$templateClassName.cs") -To (Join-Path "src\$newName" "$newClassName.cs")

# Second-pass broad rename for any remaining file/folder names containing template tokens.
$renameRules = @(
    @{ From = $templateTestsName; To = $newTestsName },
    @{ From = $templateName; To = $newName },
    @{ From = $templateClassName; To = $newClassName }
)

$allItems = Get-ChildItem -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending
foreach ($item in $allItems) {
    if (Test-IsExcludedPath -Path $item.FullName) {
        continue
    }

    $newLeafName = $item.Name
    foreach ($rule in $renameRules) {
        if ($newLeafName.Contains($rule.From)) {
            $newLeafName = $newLeafName.Replace($rule.From, $rule.To)
        }
    }

    if ($newLeafName -eq $item.Name) {
        continue
    }

    $targetPath = Join-Path $item.DirectoryName $newLeafName
    if (Test-Path -LiteralPath $targetPath) {
        continue
    }

    Rename-Item -LiteralPath $item.FullName -NewName $newLeafName
    Write-Host "Renamed: $($item.FullName) -> $targetPath"
}

# Replace tokens in text files.
$textExtensions = @(
    ".cs", ".csproj", ".props", ".targets", ".sln", ".slnx",
    ".md", ".txt", ".yml", ".yaml", ".json", ".xml",
    ".ps1", ".sh", ".cmd", ".bat", ".editorconfig",
    ".gitattributes", ".gitignore", ".baseline", ".config", ".nuspec"
)

$contentRules = @(
    @{ From = $templateTestsName; To = $newTestsName },
    @{ From = $templateName; To = $newName },
    @{ From = $templateClassName; To = $newClassName }
)

$changedFiles = 0
$files = Get-ChildItem -Recurse -File -Force
foreach ($file in $files) {
    if (Test-IsExcludedPath -Path $file.FullName) {
        continue
    }

    $ext = [System.IO.Path]::GetExtension($file.Name)
    if (-not $textExtensions.Contains($ext)) {
        continue
    }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    $updated = $content
    foreach ($rule in $contentRules) {
        $updated = $updated.Replace($rule.From, $rule.To)
    }

    if ($updated -ne $content) {
        $normalized = $updated.TrimEnd("`r", "`n") + "`n"
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($file.FullName, $normalized, $utf8NoBom)
        $changedFiles++
    }
}

$solution = Get-ChildItem -Path . -File -Filter *.slnx | Select-Object -First 1
if (-not $solution) {
    $solution = Get-ChildItem -Path . -File -Filter *.sln | Select-Object -First 1
}

Write-Host "Done. Updated $changedFiles file(s)."
if ($solution) {
    Write-Host "Next:"
    Write-Host "  dotnet build $($solution.Name) -c Release"
    Write-Host "  dotnet test  $($solution.Name) -c Release"
} else {
    Write-Host "No solution file found at repo root after rename."
}
