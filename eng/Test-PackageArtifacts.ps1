[CmdletBinding()]
param(
    [string]$PackagePath = (Join-Path $PSScriptRoot "../nupkgs"),
    [string]$CatalogPath = (Join-Path $PSScriptRoot "package-catalog.json"),
    [string]$VersionsPath = (Join-Path $PSScriptRoot "package-versions.json"),
    [ValidateSet("platform", "test-support", "observability-tools", "prose-tools", "testdocs-tools")]
    [string]$ReleaseGroup = "",
    [switch]$RequireAllPublishable
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-ElementText {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Parent,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $node = $Parent.SelectSingleNode("*[local-name()='$Name']")
    if ($null -eq $node) {
        return ""
    }

    return ([string]$node.InnerText).Trim()
}

function Add-Failure {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]]$Failures,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $Failures.Add($Message)
}

if (-not (Test-Path -LiteralPath $CatalogPath)) {
    throw "Package catalog not found at '$CatalogPath'."
}

if (-not (Test-Path -LiteralPath $VersionsPath)) {
    throw "Package version manifest not found at '$VersionsPath'."
}

$catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json
$versions = Get-Content -LiteralPath $VersionsPath -Raw | ConvertFrom-Json
$publishableById = @{}
foreach ($project in @($catalog.projects | Where-Object { $_.publishable })) {
    $publishableById[[string]$project.packageId] = $project
}

$versionById = @{}
foreach ($package in @($versions.packages)) {
    $versionById[[string]$package.packageId] = [string]$package.version
}

$packageFiles = @(Get-ChildItem -LiteralPath $PackagePath -Filter "*.nupkg" -File -ErrorAction SilentlyContinue | Sort-Object Name)
if ($packageFiles.Count -eq 0) {
    Write-Host "No NuGet packages found at '$PackagePath'."
    return
}

$failures = [System.Collections.Generic.List[string]]::new()
$artifactsById = @{}

foreach ($packageFile in $packageFiles) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($packageFile.FullName)
    try {
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith(".nuspec", [System.StringComparison]::OrdinalIgnoreCase) })
        if ($nuspecEntries.Count -ne 1) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): expected exactly one .nuspec file; found $($nuspecEntries.Count)."
            continue
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($null -eq $metadata) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): package metadata was not found."
            continue
        }

        $packageId = Get-ElementText -Parent $metadata -Name "id"
        $packageVersion = Get-ElementText -Parent $metadata -Name "version"
        if ([string]::IsNullOrWhiteSpace($packageId)) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): package id is missing."
            continue
        }

        if (-not $publishableById.ContainsKey($packageId)) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): '$packageId' is not publishable in the package catalog."
        }
        elseif (-not [string]::IsNullOrWhiteSpace($ReleaseGroup) -and $publishableById[$packageId].releaseGroup -ne $ReleaseGroup) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): '$packageId' is not in release group '$ReleaseGroup'."
        }

        if ($artifactsById.ContainsKey($packageId)) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): duplicate artifact for '$packageId'."
        }
        else {
            $artifactsById[$packageId] = $packageVersion
        }

        foreach ($requiredElement in @("title", "authors", "description", "projectUrl", "tags", "icon", "readme")) {
            if ([string]::IsNullOrWhiteSpace((Get-ElementText -Parent $metadata -Name $requiredElement))) {
                Add-Failure -Failures $failures -Message "$($packageFile.Name): required metadata '$requiredElement' is missing."
            }
        }

        $license = $metadata.SelectSingleNode("*[local-name()='license']")
        if ($null -eq $license -or $license.GetAttribute("type") -ne "expression" -or [string]::IsNullOrWhiteSpace($license.InnerText)) {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): an SPDX license expression is required."
        }

        $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
        if ($null -eq $repository -or [string]::IsNullOrWhiteSpace($repository.GetAttribute("url")) -or $repository.GetAttribute("type") -ne "git") {
            Add-Failure -Failures $failures -Message "$($packageFile.Name): git repository metadata is missing or incomplete."
        }

        foreach ($assetName in @(
                (Get-ElementText -Parent $metadata -Name "icon"),
                (Get-ElementText -Parent $metadata -Name "readme"))) {
            if ([string]::IsNullOrWhiteSpace($assetName)) {
                continue
            }

            $normalizedAssetName = $assetName.Replace("\", "/").TrimStart("/")
            $assetEntry = $archive.Entries | Where-Object { $_.FullName.Equals($normalizedAssetName, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            if ($null -eq $assetEntry) {
                Add-Failure -Failures $failures -Message "$($packageFile.Name): declared asset '$assetName' is not embedded in the package."
            }
            elseif ($assetEntry.Length -eq 0) {
                Add-Failure -Failures $failures -Message "$($packageFile.Name): embedded asset '$assetName' is empty."
            }
            elseif ($normalizedAssetName.EndsWith(".png", [System.StringComparison]::OrdinalIgnoreCase) -and $assetEntry.Length -gt 1MB) {
                Add-Failure -Failures $failures -Message "$($packageFile.Name): package icon '$assetName' exceeds NuGet's 1 MB limit."
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

$expectedProjects = @()
if ($RequireAllPublishable) {
    $expectedProjects = @($catalog.projects | Where-Object { $_.publishable })
}
elseif (-not [string]::IsNullOrWhiteSpace($ReleaseGroup)) {
    $expectedProjects = @($catalog.projects | Where-Object { $_.publishable -and $_.releaseGroup -eq $ReleaseGroup })
}

foreach ($project in $expectedProjects) {
    $packageId = [string]$project.packageId
    if (-not $artifactsById.ContainsKey($packageId)) {
        Add-Failure -Failures $failures -Message "Expected artifact for '$packageId' was not found."
        continue
    }

    if (-not $versionById.ContainsKey($packageId)) {
        Add-Failure -Failures $failures -Message "Version manifest entry for '$packageId' was not found."
        continue
    }

    if ($artifactsById[$packageId] -ne $versionById[$packageId]) {
        Add-Failure -Failures $failures -Message "Artifact '$packageId' has version '$($artifactsById[$packageId])'; expected '$($versionById[$packageId])'."
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "NuGet package artifact validation failed with $($failures.Count) error(s)."
}

Write-Host "Validated $($packageFiles.Count) NuGet package artifact(s), including required metadata, icon, and README assets."
