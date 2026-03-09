param(
    [string]$Name = "Incursa.Sample.Library",
    [switch]$KeepTemp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("repomanager-selftest-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

Write-Host "Self-test workspace: $tempRoot"

# Copy repo to temp, excluding .git to keep the run isolated and fast.
Get-ChildItem -LiteralPath $repoRoot -Force |
    Where-Object { $_.Name -ne ".git" } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $tempRoot -Recurse -Force
    }

function Cleanup-Temp {
    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    } else {
        Write-Host "Kept temp workspace: $tempRoot"
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$Arguments = @()
    )

    Write-Host "Running: $Description"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

try {
    $initScript = Join-Path $tempRoot "Init-Repo.ps1"
    if (-not (Test-Path -LiteralPath $initScript)) {
        throw "Init-Repo.ps1 not found in temp workspace."
    }

    Push-Location $tempRoot
    try {
        Invoke-External -Description "Init-Repo (first run)" -FilePath "pwsh" -Arguments @("-NoProfile", "-File", $initScript, "-Name", $Name)
        Invoke-External -Description "Init-Repo (idempotency run)" -FilePath "pwsh" -Arguments @("-NoProfile", "-File", $initScript, "-Name", $Name)

        $solution = Get-ChildItem -Path . -File -Filter *.slnx | Select-Object -First 1
        if (-not $solution) {
            $solution = Get-ChildItem -Path . -File -Filter *.sln | Select-Object -First 1
        }
        if (-not $solution) {
            throw "No solution file found at repo root after initialization."
        }

        Write-Host "Using solution: $($solution.Name)"
        Invoke-External -Description "dotnet restore" -FilePath "dotnet" -Arguments @("restore", $solution.Name)
        Invoke-External -Description "dotnet build" -FilePath "dotnet" -Arguments @("build", $solution.Name, "-c", "Release", "--no-restore")
        Invoke-External -Description "dotnet test" -FilePath "dotnet" -Arguments @("test", "--solution", $solution.Name, "-c", "Release", "--no-build", "--filter", "Category!=Integration&RequiresDocker!=true")

        $needleA = "Incursa" + ".Template"
        $needleB = "Class" + "1"
        $searchPattern = [regex]::Escape($needleA) + "|" + [regex]::Escape($needleB)

        $excludedDirs = @(".git", "bin", "obj", "artifacts", "nupkgs")
        $pathLeftovers = Get-ChildItem -Recurse -Force |
            Where-Object {
                $full = $_.FullName
                foreach ($seg in $excludedDirs) {
                    if ($full -match "(^|[\\/])$([regex]::Escape($seg))([\\/]|$)") {
                        return $false
                    }
                }
                return $true
            } |
            Where-Object { $_.FullName -match $searchPattern }

        if ($pathLeftovers) {
            $sample = $pathLeftovers | Select-Object -First 20 -ExpandProperty FullName
            throw "Found placeholder(s) in file/directory names:`n$($sample -join "`n")"
        }

        if (Get-Command rg -ErrorAction SilentlyContinue) {
            $rgOutput = & rg -n --hidden --glob "!.git/**" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!artifacts/**" --glob "!nupkgs/**" $searchPattern .
            if ($LASTEXITCODE -eq 0) {
                throw "Found placeholder(s) in file contents:`n$rgOutput"
            }
            if ($LASTEXITCODE -gt 1) {
                throw "rg failed while scanning for placeholders."
            }
        } else {
            $contentHits = Select-String -Path (Get-ChildItem -Recurse -File | ForEach-Object { $_.FullName }) -Pattern $searchPattern -SimpleMatch -ErrorAction SilentlyContinue
            if ($contentHits) {
                $sample = $contentHits | Select-Object -First 20 | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line)" }
                throw "Found placeholder(s) in file contents:`n$($sample -join "`n")"
            }
        }

        Write-Host "Self-test passed."
    } finally {
        Pop-Location
    }
} finally {
    Cleanup-Temp
}
