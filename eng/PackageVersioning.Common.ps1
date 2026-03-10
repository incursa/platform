[CmdletBinding()]
param()

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
    param(
        [string]$Base = "",
        [string]$Head = "",
        [string[]]$ChangedFiles = @(),
        [switch]$Staged
    )

    if ($ChangedFiles.Count -gt 0) {
        return @($ChangedFiles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Normalize-RelativePath $_ } | Sort-Object -Unique)
    }

    $gitArgs = @("diff", "--name-only", "--diff-filter=ACMRTUXB")
    if ($Staged) {
        $gitArgs += "--cached"
    }

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

function Get-PackageCatalogProjects {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CatalogPath,
        [switch]$PackableOnly,
        [switch]$PublishableOnly
    )

    if (-not (Test-Path -LiteralPath $CatalogPath)) {
        throw "Package catalog not found at '$CatalogPath'. Run eng/Generate-PackageCatalog.ps1 first."
    }

    $catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json
    $projects = @($catalog.projects)

    if ($PackableOnly) {
        $projects = @($projects | Where-Object { $_.packable })
    }

    if ($PublishableOnly) {
        $projects = @($projects | Where-Object { $_.publishable })
    }

    return $projects
}

function Get-PackageVersionManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "Package version manifest not found at '$ManifestPath'. Run eng/Initialize-PackageVersions.ps1 first."
    }

    return Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
}

function Get-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    [xml]$projectXml = Get-Content -LiteralPath (Join-Path $RepoRoot $ProjectPath) -Raw
    $versionNode = @($projectXml.SelectNodes("/*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='Version']") | Where-Object { -not [string]::IsNullOrWhiteSpace($_.InnerText) } | Select-Object -First 1)
    if ($versionNode.Count -gt 0) {
        return [string]$versionNode[0].InnerText
    }

    return "1.0.0"
}

function Get-PackageVersionMap {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Manifest
    )

    $map = @{}
    foreach ($package in @($Manifest.packages)) {
        $map[[string]$package.packageId] = [string]$package.version
    }

    return $map
}

function Get-ProjectReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
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

    return @($references)
}

function Get-ProjectDependencyGraph {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [object[]]$Projects
    )

    $projectPaths = @($Projects | ForEach-Object { [string]$_.projectPath })
    $referencesByProject = @{}
    $dependentsByProject = @{}

    foreach ($projectPath in $projectPaths) {
        $references = @(Get-ProjectReferences -RepoRoot $RepoRoot -ProjectPath $projectPath | Where-Object { $projectPaths -contains $_ })
        $referencesByProject[$projectPath] = $references

        foreach ($reference in $references) {
            if (-not $dependentsByProject.ContainsKey($reference)) {
                $dependentsByProject[$reference] = New-Object System.Collections.Generic.List[string]
            }

            [void]$dependentsByProject[$reference].Add($projectPath)
        }
    }

    return [ordered]@{
        referencesByProject = $referencesByProject
        dependentsByProject = $dependentsByProject
    }
}

function Get-GlobalImpactPatterns {
    return @(
        "^Directory\.Build\.(props|targets)$",
        "^global\.json$",
        "^stylecop\.json$",
        "^\.editorconfig$",
        "^Incursa\.Platform(\.CI)?\.slnx$",
        "^assets/nuget_logo\.png$"
    )
}

function Get-PublishImpact {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [object[]]$Projects,
        [Parameter(Mandatory = $true)]
        [string[]]$ChangedFiles
    )

    $projectPaths = @($Projects | ForEach-Object { [string]$_.projectPath })
    $graph = Get-ProjectDependencyGraph -RepoRoot $RepoRoot -Projects $Projects
    $directlyChanged = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
    $publishSet = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

    $hasGlobalImpact = $false
    foreach ($changedPath in $ChangedFiles) {
        foreach ($pattern in Get-GlobalImpactPatterns) {
            if ($changedPath -match $pattern) {
                $hasGlobalImpact = $true
                break
            }
        }

        if ($hasGlobalImpact) {
            break
        }
    }

    if ($hasGlobalImpact) {
        foreach ($projectPath in $projectPaths) {
            [void]$directlyChanged.Add($projectPath)
            [void]$publishSet.Add($projectPath)
        }
    }
    else {
        foreach ($changedPath in $ChangedFiles) {
            foreach ($project in $Projects) {
                $projectPath = [string]$project.projectPath
                $projectDirectory = Normalize-RelativePath (Split-Path -Parent $projectPath)
                if ($changedPath -eq $projectPath -or $changedPath -like "$projectDirectory/*") {
                    [void]$directlyChanged.Add($projectPath)
                    [void]$publishSet.Add($projectPath)
                }
            }
        }

        $pending = New-Object System.Collections.Generic.Queue[string]
        foreach ($projectPath in $publishSet) {
            $pending.Enqueue($projectPath)
        }

        while ($pending.Count -gt 0) {
            $current = $pending.Dequeue()
            foreach ($dependent in @($graph.dependentsByProject[$current])) {
                if ([string]::IsNullOrWhiteSpace([string]$dependent)) {
                    continue
                }

                if ($publishSet.Add($dependent)) {
                    $pending.Enqueue($dependent)
                }
            }
        }
    }

    return [ordered]@{
        hasGlobalImpact = $hasGlobalImpact
        directlyChangedProjectPaths = @($directlyChanged | Sort-Object)
        publishProjectPaths = @($publishSet | Sort-Object)
    }
}

function Merge-VersionBump {
    param(
        [string]$Current = "patch",
        [string]$Candidate = "patch"
    )

    $priority = @{
        patch = 1
        minor = 2
        major = 3
    }

    if (-not $priority.ContainsKey($Current)) {
        throw "Unknown version bump '$Current'."
    }

    if (-not $priority.ContainsKey($Candidate)) {
        throw "Unknown version bump '$Candidate'."
    }

    if ($priority[$Candidate] -gt $priority[$Current]) {
        return $Candidate
    }

    return $Current
}

function Get-NextSemanticVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [ValidateSet("patch", "minor", "major")]
        [string]$Bump
    )

    $match = [System.Text.RegularExpressions.Regex]::Match($Version, "^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:[-+].*)?$")
    if (-not $match.Success) {
        throw "Version '$Version' is not a supported semantic version."
    }

    $major = [int]$match.Groups["major"].Value
    $minor = [int]$match.Groups["minor"].Value
    $patch = [int]$match.Groups["patch"].Value

    switch ($Bump) {
        "major" { return "{0}.0.0" -f ($major + 1) }
        "minor" { return "{0}.{1}.0" -f $major, ($minor + 1) }
        default { return "{0}.{1}.{2}" -f $major, $minor, ($patch + 1) }
    }
}

function Compare-SemanticVersions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Left,
        [Parameter(Mandatory = $true)]
        [string]$Right
    )

    $leftVersion = [System.Management.Automation.SemanticVersion]::Parse($Left)
    $rightVersion = [System.Management.Automation.SemanticVersion]::Parse($Right)

    return $leftVersion.CompareTo($rightVersion)
}

function Read-JsonAtGitRef {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Ref,
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $spec = "{0}:{1}" -f $Ref, (Normalize-RelativePath $RelativePath)
    $content = & git show $spec 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return ($content -join [Environment]::NewLine) | ConvertFrom-Json
}

function Set-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $fullPath = Join-Path $RepoRoot $ProjectPath
    [xml]$projectXml = Get-Content -LiteralPath $fullPath -Raw

    $propertyGroups = @($projectXml.SelectNodes("/*[local-name()='Project']/*[local-name()='PropertyGroup']"))
    if ($propertyGroups.Count -eq 0) {
        $propertyGroup = $projectXml.CreateElement("PropertyGroup", $projectXml.Project.NamespaceURI)
        [void]$projectXml.Project.AppendChild($propertyGroup)
        $propertyGroups = @($propertyGroup)
    }

    $versionNode = @($projectXml.SelectNodes("/*[local-name()='Project']/*[local-name()='PropertyGroup']/*[local-name()='Version']") | Select-Object -First 1)
    if ($versionNode.Count -gt 0) {
        $versionNode[0].InnerText = $Version
    }
    else {
        $newVersionNode = $projectXml.CreateElement("Version", $projectXml.Project.NamespaceURI)
        $newVersionNode.InnerText = $Version
        [void]$propertyGroups[0].AppendChild($newVersionNode)
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.NewLineChars = "`r`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    $settings.OmitXmlDeclaration = $true

    $writer = [System.Xml.XmlWriter]::Create($fullPath, $settings)
    try {
        $projectXml.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}
