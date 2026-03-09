param(
    [string]$ConfigFile = "stryker-config.json",
    [string]$OutputDirectory = "artifacts/codex/mutation"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $ConfigFile)) {
    throw "Stryker config file not found: $ConfigFile"
}

$toolPath = Join-Path (Get-Location) "artifacts/codex/tools"
New-Item -Path $toolPath -ItemType Directory -Force | Out-Null
New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null

$strykerExe = Join-Path $toolPath "dotnet-stryker.exe"
if (-not (Test-Path -Path $strykerExe)) {
    dotnet tool install --tool-path $toolPath dotnet-stryker
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install dotnet-stryker."
    }
}

& $strykerExe --config-file $ConfigFile --output $OutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Mutation testing failed."
}

Write-Host "Mutation testing completed. Output: $OutputDirectory"
