[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$Base = "",
    [string]$Head = "",
    [string[]]$ChangedFiles = @(),
    [switch]$AsJson
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

function Normalize-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalized = $Path -replace "\\", "/"
    if ($normalized.StartsWith("./", [System.StringComparison]::Ordinal) -or $normalized.StartsWith(".\", [System.StringComparison]::Ordinal)) {
        return $normalized.Substring(2)
    }

    return $normalized
}

function Get-ChangedFilesFromGit {
    if ($ChangedFiles.Count -gt 0) {
        return $ChangedFiles | ForEach-Object { Normalize-RelativePath $_ } | Sort-Object -Unique
    }

    $gitArgs = @("diff", "--name-only", "--diff-filter=ACMRTUXB")
    if (-not [string]::IsNullOrWhiteSpace($Base)) {
        $gitArgs += $Base
    }
    if (-not [string]::IsNullOrWhiteSpace($Head)) {
        $gitArgs += $Head
    }

    $diffOutput = & git @gitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git diff failed with exit code $LASTEXITCODE."
    }

    return @($diffOutput | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Normalize-RelativePath $_ } | Sort-Object -Unique)
}

function Get-ProjectReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    [xml]$projectXml = Get-Content -LiteralPath (Join-Path $RepoRoot $ProjectPath) -Raw
    $projectDirectory = Split-Path -Parent (Join-Path $RepoRoot $ProjectPath)

    $references = New-Object System.Collections.Generic.List[string]
    $projectReferenceNodes = $projectXml.SelectNodes("/*[local-name()='Project']/*[local-name()='ItemGroup']/*[local-name()='ProjectReference']")
    foreach ($projectReference in $projectReferenceNodes) {
        $include = $projectReference.GetAttribute("Include")
        if ([string]::IsNullOrWhiteSpace($include)) {
            continue
        }

        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory $include))
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $relativePath = Normalize-RelativePath ([System.IO.Path]::GetRelativePath($RepoRoot, $fullPath))
        [void]$references.Add($relativePath)
    }

    return $references
}

if (-not (Test-Path -LiteralPath $CatalogPath)) {
    throw "Package catalog not found at '$CatalogPath'. Run eng/Generate-PackageCatalog.ps1 first."
}

$catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json
$projects = @($catalog.projects)
$projectPaths = $projects.projectPath

$referencesByProject = @{}
$dependentsByProject = @{}
foreach ($projectPath in $projectPaths) {
    $references = @(Get-ProjectReferences -ProjectPath $projectPath | Where-Object { $projectPaths -contains $_ })
    $referencesByProject[$projectPath] = $references

    foreach ($reference in $references) {
        if (-not $dependentsByProject.ContainsKey($reference)) {
            $dependentsByProject[$reference] = New-Object System.Collections.Generic.List[string]
        }

        [void]$dependentsByProject[$reference].Add($projectPath)
    }
}

$changed = @(Get-ChangedFilesFromGit)

$globalImpactPatterns = @(
    "^Directory\.Build\.(props|targets)$",
    "^global\.json$",
    "^stylecop\.json$",
    "^\.editorconfig$",
    "^assets/nuget_logo\.png$"
)

$affected = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

if ($changed | Where-Object {
        $path = $_
        $globalImpactPatterns | Where-Object { $path -match $_ }
    }) {
    foreach ($projectPath in $projectPaths) {
        [void]$affected.Add($projectPath)
    }
}
else {
    foreach ($changedPath in $changed) {
        foreach ($project in $projects) {
            $projectPath = [string]$project.projectPath
            $projectDirectory = Normalize-RelativePath (Split-Path -Parent $projectPath)
            if ($changedPath -eq $projectPath -or $changedPath -like "$projectDirectory/*") {
                [void]$affected.Add($projectPath)
            }
        }
    }

    $pending = New-Object System.Collections.Generic.Queue[string]
    foreach ($projectPath in $affected) {
        $pending.Enqueue($projectPath)
    }

    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        foreach ($dependent in @($dependentsByProject[$current])) {
            if ($affected.Add($dependent)) {
                $pending.Enqueue($dependent)
            }
        }
    }
}

$result = [ordered]@{
    changedFiles = $changed
    projectPaths = @($affected | Sort-Object)
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 4
}
else {
    $result.projectPaths
}
