# GitBackup Build Test Script
# This script demonstrates local version building similar to GitHub Actions

param(
    [int]$BuildNumber = 1,
    [string]$VersionPrefix = "1.0.0",
    [string]$VersionSuffix = ""
)

Write-Host "GitBackup Local Build Test" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green
Write-Host ""
Write-Host "Version Prefix: $VersionPrefix" -ForegroundColor Yellow
Write-Host "Version Suffix: $VersionSuffix" -ForegroundColor Yellow
Write-Host "Build Number: $BuildNumber" -ForegroundColor Yellow
Write-Host ""

# Clean previous build
Write-Host "Cleaning previous build..." -ForegroundColor Cyan
dotnet clean --verbosity quiet

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Cyan
dotnet restore --verbosity quiet

# Build with version information
Write-Host "Building with version information..." -ForegroundColor Cyan
$buildArgs = @(
    "build"
    "--configuration", "Release"
    "--verbosity", "minimal"
    "-p:VersionPrefix=$VersionPrefix"
    "-p:BuildNumber=$BuildNumber"
    "-p:AssemblyVersion=$VersionPrefix.$BuildNumber"
    "-p:FileVersion=$VersionPrefix.$BuildNumber"
)

if ($VersionSuffix) {
    $buildArgs += "-p:VersionSuffix=$VersionSuffix"
}

& dotnet @buildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
    
    # Test the version display
    Write-Host "Testing version display:" -ForegroundColor Cyan
    dotnet run --configuration Release -- --version
    
    Write-Host ""
    Write-Host "Build test completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
