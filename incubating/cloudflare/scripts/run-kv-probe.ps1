param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,
    [switch]$KeepKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

$projectPath = "src/Incursa.Integrations.Cloudflare.KvProbe/Incursa.Integrations.Cloudflare.KvProbe.csproj"
$args = @("--config", $ConfigPath)
if ($KeepKey) {
    $args += "--keep-key"
}

Write-Host "Running Cloudflare KV probe with config: $ConfigPath"
& dotnet run --project $projectPath -p:NuGetAudit=false -- @args
if ($LASTEXITCODE -ne 0) {
    throw "KV probe failed with exit code $LASTEXITCODE."
}
