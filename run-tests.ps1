# GitBackup Robot Framework Test Execution Script
# This script runs all Robot Framework tests locally

param(
    [string]$TestSuite = "all",
    [string]$OutputDir = "test_results",
    [switch]$Verbose,
    [switch]$DryRun
)

Write-Host "GitBackup Robot Framework Test Runner" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# Ensure we have Robot Framework installed
Write-Host "Checking Robot Framework installation..." -ForegroundColor Cyan
try {
    $rfVersion = & python -m robot.libdoc --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Robot Framework not found"
    }
    Write-Host "Robot Framework found: $rfVersion" -ForegroundColor Green
} catch {
    Write-Host "Robot Framework not found. Installing..." -ForegroundColor Yellow
    & pip install -r tests/requirements.txt
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install Robot Framework dependencies" -ForegroundColor Red
        exit 1
    }
}

# Build the application first
Write-Host "Building GitBackup application..." -ForegroundColor Cyan
& dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Ensure the executable is in PATH or copy it
$exePath = "bin/Release/net8.0/GitBackup.exe"
if (Test-Path $exePath) {
    $env:PATH = "$pwd/bin/Release/net8.0;$env:PATH"
    Write-Host "GitBackup executable found at: $exePath" -ForegroundColor Green
} else {
    Write-Host "GitBackup executable not found at: $exePath" -ForegroundColor Red
    exit 1
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Determine which tests to run
$testFiles = @()
switch ($TestSuite.ToLower()) {
    "all" { 
        $testFiles += "tests/gitbackup_tests.robot"
        if ($IsWindows) {
            $testFiles += "tests/windows_tests.robot"
        } elseif ($IsLinux) {
            $testFiles += "tests/linux_tests.robot" 
        }
    }
    "core" { $testFiles += "tests/gitbackup_tests.robot" }
    "windows" { $testFiles += "tests/windows_tests.robot" }
    "linux" { $testFiles += "tests/linux_tests.robot" }
    default { $testFiles += $TestSuite }
}

Write-Host "Test files to execute:" -ForegroundColor Cyan
foreach ($file in $testFiles) {
    Write-Host "  - $file" -ForegroundColor Yellow
}
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - Would execute the following command:" -ForegroundColor Yellow
    $robotCmd = "robot --outputdir $OutputDir --timestampoutputs"
    if ($Verbose) { $robotCmd += " --loglevel DEBUG" }
    $robotCmd += " " + ($testFiles -join " ")
    Write-Host $robotCmd -ForegroundColor Cyan
    exit 0
}

# Execute Robot Framework tests
Write-Host "Running Robot Framework tests..." -ForegroundColor Cyan
$robotArgs = @(
    "--outputdir", $OutputDir
    "--timestampoutputs"
    "--reporttitle", "GitBackup Test Report"
    "--logtitle", "GitBackup Test Log"
)

if ($Verbose) {
    $robotArgs += "--loglevel", "DEBUG"
}

$robotArgs += $testFiles

Write-Host "Executing: robot $($robotArgs -join ' ')" -ForegroundColor Gray
& python -m robot @robotArgs

$testResult = $LASTEXITCODE

# Display results
Write-Host ""
if ($testResult -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed (exit code: $testResult)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test results available in:" -ForegroundColor Cyan
Write-Host "  - HTML Report: $OutputDir/report.html" -ForegroundColor Yellow
Write-Host "  - Log File: $OutputDir/log.html" -ForegroundColor Yellow
Write-Host "  - XML Output: $OutputDir/output.xml" -ForegroundColor Yellow

exit $testResult
