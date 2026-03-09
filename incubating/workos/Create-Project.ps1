param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$Directory
)

# Go to the folder where the script (and solution) live
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptRoot

# Find the first solution in this directory
$solution = Get-ChildItem -Path $scriptRoot -Filter *.slnx | Select-Object -First 1
if (-not $solution) {
    $solution = Get-ChildItem -Path $scriptRoot -Filter *.sln | Select-Object -First 1
}
if (-not $solution) {
    Write-Error "No solution file found in $scriptRoot"
    exit 1
}

# Ensure target directory exists (relative to solution folder)
$targetRoot = Join-Path $scriptRoot $Directory
New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

$projectName     = $Name
$testProjectName = "$Name.Tests"

$projectDir      = Join-Path $targetRoot $projectName
$testProjectDir  = Join-Path $targetRoot $testProjectName

# Create class library
& dotnet new classlib -n $projectName -o $projectDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Create test project (MSTest.Sdk)
& dotnet new mstest -n $testProjectName -o $testProjectDir --sdk true --test-runner mstest --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Add both to the solution
$projectCsproj     = Join-Path $projectDir "$projectName.csproj"
$testProjectCsproj = Join-Path $testProjectDir "$testProjectName.csproj"

& dotnet sln $solution.Name add $projectCsproj
& dotnet sln $solution.Name add $testProjectCsproj

Write-Host "Created '$projectName' and '$testProjectName' in '$Directory' and added them to '$($solution.Name)'."
