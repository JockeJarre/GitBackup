# Robot Framework Testing for GitBackup

This document describes the Robot Framework test automation setup for GitBackup.

## Overview

GitBackup uses **Robot Framework** for comprehensive end-to-end testing across multiple platforms. Tests are automatically executed on every commit via GitHub Actions and can also be run locally during development.

## Test Structure

```
tests/
├── __init__.robot                 # Test suite configuration
├── gitbackup_tests.robot         # Core functionality tests
├── windows_tests.robot           # Windows-specific tests
├── linux_tests.robot             # Linux-specific tests
├── robot.args                    # Robot Framework configuration
├── requirements.txt              # Python dependencies
└── _listeners/
    └── TestListener.py           # Custom test listener
```

## Test Coverage

### Core Functionality (`gitbackup_tests.robot`)
- ✅ **Command-line interface** validation (help, version, invalid args)
- ✅ **Configuration management** (create, load, validate)
- ✅ **Error handling** for missing files and invalid configs
- ✅ **Dry-run mode** functionality
- ✅ **Verbose output** verification
- ✅ **Backup operations** (create, commit to git)
- ✅ **Force mode** for repeat backups

### Windows-Specific Tests (`windows_tests.robot`)
- ✅ **Windows path handling** (C:\ style paths)
- ✅ **Long path support** (>260 characters)
- ✅ **File permissions** and attributes
- ✅ **Windows-specific exclusions** (Thumbs.db, desktop.ini)

### Linux-Specific Tests (`linux_tests.robot`)
- ✅ **Unix path handling** (/tmp style paths)
- ✅ **File permissions** (chmod, executable files)
- ✅ **Hidden files** (dot files)
- ✅ **Symbolic links** handling
- ✅ **Case sensitivity** verification
- ✅ **Linux-specific exclusions** (core dumps, .DS_Store)

## Running Tests

### Local Execution

**Windows (PowerShell):**
```powershell
# Install dependencies
pip install -r tests/requirements.txt

# Run all tests
.\run-tests.ps1

# Run specific test suite
.\run-tests.ps1 -TestSuite core

# Verbose dry run
.\run-tests.ps1 -Verbose -DryRun

# Custom output directory
.\run-tests.ps1 -OutputDir my_results
```

**Linux/macOS (Bash):**
```bash
# Install dependencies  
pip3 install -r tests/requirements.txt

# Run all tests
./run-tests.sh

# Run specific test suite
./run-tests.sh -s linux

# Verbose dry run
./run-tests.sh -v --dry-run

# Custom output directory
./run-tests.sh -o my_results
```

**Direct Robot Framework:**
```bash
# Basic execution
robot --outputdir test_results tests/

# Specific test file
robot --outputdir test_results tests/gitbackup_tests.robot

# With custom variables
robot --variable GITBACKUP_EXE:./bin/Release/net8.0/GitBackup tests/

# Using argument file
robot --argumentfile tests/robot.args tests/
```

### GitHub Actions Integration

Tests automatically run on:
- **Push** to main/master/develop branches
- **Pull requests** to main/master branches

**Workflow Matrix:**
- **Ubuntu Latest** → Runs core + Linux tests
- **Windows Latest** → Runs core + Windows tests

**Artifacts:**
- Test reports uploaded as `test-results-linux` and `test-results-windows`
- HTML reports, logs, and XML output included
- 30-day retention period

## Test Reports

Robot Framework generates comprehensive reports:

- **`report.html`** → Visual test execution report
- **`log.html`** → Detailed execution log with expandable sections  
- **`output.xml`** → Machine-readable results for CI integration
- **`listener.log`** → Custom logging from TestListener

## Writing New Tests

### Test Case Template
```robot
*** Test Cases ***
New Feature Should Work
    [Documentation]    Verify new feature functionality
    [Tags]             smoke    feature-x
    [Setup]            Setup Feature Test Environment
    ${result} =        Run GitBackup    --new-feature
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain     ${result.stdout}    Expected output
    [Teardown]         Cleanup Feature Test Environment
```

### Best Practices

1. **Use descriptive test names** that explain what is being tested
2. **Include [Documentation]** for test purpose
3. **Tag tests appropriately** (smoke, regression, platform-specific)
4. **Setup and teardown** test environments properly
5. **Use meaningful variable names**
6. **Verify both exit codes and output content**
7. **Handle platform differences** in separate test files

### Common Keywords

- `Run GitBackup` → Execute GitBackup with arguments
- `Setup Test Environment` → Create test directories and files
- `Create Valid Config File` → Generate working configuration
- `Should Contain Any` → Check for multiple possible outputs

## CI/CD Integration

### Pre-commit Hooks
Consider adding Robot Framework tests to pre-commit hooks:

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: robot-tests
        name: Robot Framework Tests
        entry: python -m robot
        language: system
        args: ["--outputdir", "pre-commit-results", "tests/gitbackup_tests.robot"]
        pass_filenames: false
```

### Quality Gates
Tests serve as quality gates in the CI pipeline:
- **All tests must pass** before builds are created
- **Test artifacts** are uploaded for debugging failures
- **Test metrics** can be tracked over time

## Debugging Failed Tests

### Local Debugging
1. **Run with verbose logging**: `--loglevel DEBUG`
2. **Use dry-run mode** to understand test setup
3. **Check listener.log** for detailed execution flow
4. **Examine test artifacts** in output directory

### CI Debugging
1. **Download test artifacts** from GitHub Actions
2. **Check workflow logs** for Python/Robot Framework errors
3. **Verify platform-specific issues** in matrix builds
4. **Review test report HTML** for failure details

## Extending Tests

### Adding New Platforms
1. Create `macos_tests.robot` following Linux pattern
2. Add to GitHub Actions matrix
3. Update run scripts for macOS detection

### Adding Performance Tests
1. Create `performance_tests.robot`
2. Use Robot Framework timing keywords
3. Add performance thresholds and reporting

### Adding Integration Tests
1. Create real git repositories for testing
2. Test with various git configurations
3. Verify backup integrity and restoration

This Robot Framework setup provides comprehensive, maintainable, and scalable test automation for GitBackup across all supported platforms.
