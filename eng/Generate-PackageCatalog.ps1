[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$OutputPath = (Join-Path $PSScriptRoot "package-catalog.json")
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

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FullPath
    )

    return Normalize-RelativePath ([System.IO.Path]::GetRelativePath($RepoRoot, $FullPath))
}

function Get-FirstPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$ProjectXml,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    $propertyGroups = $ProjectXml.SelectNodes("/*[local-name()='Project']/*[local-name()='PropertyGroup']")
    foreach ($propertyGroup in $propertyGroups) {
        $propertyNode = $propertyGroup.SelectSingleNode("*[local-name()='$PropertyName']")
        if ($null -ne $propertyNode -and -not [string]::IsNullOrWhiteSpace([string]$propertyNode.InnerText)) {
            return [string]$propertyNode.InnerText
        }
    }

    return $null
}

function Get-ProjectOrigin {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    switch -Wildcard ($ProjectPath) {
        "incubating/cloudflare/*" {
            return [ordered]@{
                kind = "imported-repo"
                sourcePath = "C:/src/incursa/integrations-cloudflare"
            }
        }
        "incubating/workos/*" {
            return [ordered]@{
                kind = "imported-repo"
                sourcePath = "C:/src/incursa/integrations-workos"
            }
        }
        "incubating/electronicnotary/*" {
            return [ordered]@{
                kind = "imported-repo"
                sourcePath = "C:/src/incursa/integrations-electronicnotary"
            }
        }
        "src/Incursa.Platform.Email*" {
            return [ordered]@{
                kind = "imported-repo"
                sourcePath = "C:/src/incursa/integrations-postmark"
            }
        }
        "tests/Incursa.Platform.Email.Tests/*" {
            return [ordered]@{
                kind = "imported-repo"
                sourcePath = "C:/src/incursa/integrations-postmark"
            }
        }
        default {
            return [ordered]@{
                kind = "existing-repo"
                sourcePath = "C:/src/incursa/platform"
            }
        }
    }
}

function Get-CatalogMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )

    if ($ProjectPath -like "tests/*") {
        if ($ProjectPath -match "/Incursa\.Platform\.Smoke\." -or $ProjectName -like "*Smoke*") {
            return [ordered]@{
                zone = "tests"
                category = "smoke"
                classification = "smoke/sample"
                packable = $false
                publishable = $false
                notes = "Smoke/sample coverage project preserved for hosted verification and demo flows."
            }
        }

        return [ordered]@{
            zone = "tests"
            category = "tests"
            classification = "test-only"
            packable = $false
            publishable = $false
            notes = "Automated test project."
        }
    }

    if ($ProjectPath -like "tools/*") {
        return [ordered]@{
            zone = "tools"
            category = "tools"
            classification = "tool-only"
            packable = $true
            publishable = $true
            notes = "Tooling/analyzer package intentionally shipped from this monorepo."
        }
    }

    if ($ProjectPath -like "incubating/*") {
        if ($ProjectPath -like "incubating/*/tests/*") {
            return [ordered]@{
                zone = "incubating"
                category = "tests"
                classification = "test-only"
                packable = $false
                publishable = $false
                notes = "Tests preserved with the incubating import."
            }
        }

        if ($ProjectName -like "*.KvProbe") {
            return [ordered]@{
                zone = "incubating"
                category = "smoke"
                classification = "smoke/sample"
                packable = $false
                publishable = $false
                notes = "Provider-specific probe/sample kept with the incubating Cloudflare import."
            }
        }

        $note = switch -Wildcard ($ProjectPath) {
            "incubating/cloudflare/src/Incursa.Integrations.Cloudflare/*" {
                "Mixed Cloudflare vendor surface spanning storage, custom hostnames, and load-balancing; retained in incubating until split by capability."
                break
            }
            "incubating/workos/src/*" {
                "Broad WorkOS auth, webhook, management, and widget surface retained in incubating until it is split into capability-specific packages."
                break
            }
            "incubating/electronicnotary/src/*" {
                "Electronic notary provider code retained in incubating because the current surface mixes provider APIs with workflow/healing behavior."
                break
            }
            default {
                "Preserved in incubating pending further boundary cleanup."
            }
        }

        return [ordered]@{
            zone = "incubating"
            category = "incubating"
            classification = "incubating"
            packable = $false
            publishable = $false
            notes = $note
        }
    }

    if ($ProjectPath -like "src/*") {
        if ($ProjectPath -eq "src/Incursa.Platform/Incursa.Platform.csproj") {
            return [ordered]@{
                zone = "src"
                category = "core"
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = "Core durable processing abstractions and orchestration."
            }
        }

        if ($ProjectName -in @("Incursa.Platform.SqlServer", "Incursa.Platform.Postgres", "Incursa.Platform.InMemory")) {
            return [ordered]@{
                zone = "src"
                category = "providers"
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = "Provider implementation package."
            }
        }

        if ($ProjectName -eq "Incursa.Integrations.Storage.Azure") {
            return [ordered]@{
                zone = "src"
                category = "integrations"
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = "Public Azure storage provider adapter for Incursa.Platform.Storage."
            }
        }

        if ($ProjectName -like "Incursa.Platform.Email*") {
            $category = switch -Regex ($ProjectName) {
                "\.AspNetCore$" { "hosting"; break }
                "\.Postmark$" { "integrations"; break }
                "\.(SqlServer|Postgres)$" { "providers"; break }
                default { "capabilities" }
            }

            $notes = switch -Regex ($ProjectName) {
                "\.AspNetCore$" { "Hosting adapter for the public email capability family imported from the Postmark/email integration repo."; break }
                "\.Postmark$" { "Postmark provider adapter imported from the Postmark/email integration repo and normalized into the platform monorepo."; break }
                "\.(SqlServer|Postgres)$" { "Database provider adapter for the public email capability family imported from the Postmark/email integration repo."; break }
                default { "Provider-agnostic email capability package imported from the Postmark/email integration repo." }
            }

            return [ordered]@{
                zone = "src"
                category = $category
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = $notes
            }
        }

        if ($ProjectName -eq "Incursa.Platform.Audit.WorkOS") {
            return [ordered]@{
                zone = "src"
                category = "integrations"
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = "Capability-specific WorkOS audit sink package retained in the public source surface."
            }
        }

        if ($ProjectName -match "\.AspNetCore$" -or $ProjectName -match "\.Razor$" -or $ProjectName -eq "Incursa.Platform.Metrics.HttpServer") {
            return [ordered]@{
                zone = "src"
                category = "hosting"
                classification = "public-packable"
                packable = $true
                publishable = $true
                notes = "Hosting adapter or ASP.NET Core integration package."
            }
        }

        return [ordered]@{
            zone = "src"
            category = "capabilities"
            classification = "public-packable"
            packable = $true
            publishable = $true
            notes = "Public capability package."
        }
    }

    throw "Unclassified project path '$ProjectPath'."
}

$excludedDirectoryPattern = [regex]"[\\/](artifacts|bin|obj|node_modules|StrykerOutput|\.git)[\\/]"

$projectFiles = Get-ChildItem -Path $RepoRoot -Recurse -File -Filter "*.csproj" |
    Where-Object { $excludedDirectoryPattern.IsMatch($_.FullName) -eq $false } |
    Sort-Object FullName

$catalogEntries = foreach ($projectFile in $projectFiles) {
    $projectPath = Get-RelativePath -FullPath $projectFile.FullName
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

    $projectName = $projectFile.BaseName
    $packageId = Get-FirstPropertyValue -ProjectXml $projectXml -PropertyName "PackageId"
    if ([string]::IsNullOrWhiteSpace($packageId)) {
        $assemblyName = Get-FirstPropertyValue -ProjectXml $projectXml -PropertyName "AssemblyName"
        $packageId = if ([string]::IsNullOrWhiteSpace($assemblyName)) { $projectName } else { $assemblyName }
    }

    $metadata = Get-CatalogMetadata -ProjectPath $projectPath -ProjectName $projectName
    $origin = Get-ProjectOrigin -ProjectPath $projectPath

    [ordered]@{
        projectPath = $projectPath
        packageId = $packageId
        zone = $metadata.zone
        category = $metadata.category
        classification = $metadata.classification
        packable = [bool]$metadata.packable
        publishable = [bool]$metadata.publishable
        origin = $origin
        notes = $metadata.notes
    }
}

$catalog = [ordered]@{
    generatedAt = (Get-Date).ToString("o")
    generatedBy = "eng/Generate-PackageCatalog.ps1"
    projects = $catalogEntries
}

$catalogDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $catalogDirectory)) {
    New-Item -ItemType Directory -Path $catalogDirectory | Out-Null
}

$catalog | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath
Write-Host "Wrote $OutputPath"
