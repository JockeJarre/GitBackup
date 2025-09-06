*** Settings ***
Documentation     GitBackup Application Tests
Resource          gitbackup_keywords.robot
Suite Setup       Setup Test Environment
Suite Teardown    Cleanup Test Environment
Test Timeout      30 seconds

*** Test Cases ***
GitBackup Should Display Help
    [Documentation]    Verify that GitBackup displays help information
    ${result} =    Run GitBackup    --help
    # CommandLineParser may return 0 or 1 for help - both are acceptable
    Should Be True    ${result.rc} == 0 or ${result.rc} == 1    Help should return exit code 0 or 1, got ${result.rc}
    # Help output may go to stdout or stderr depending on CommandLineParser version
    ${output} =    Catenate    ${result.stdout}    ${result.stderr}
    Should Contain    ${output}    GitBackup
    Should Contain    ${output}    --config
    Should Contain    ${output}    --help

GitBackup Should Display Version
    [Documentation]    Verify that GitBackup displays version information
    ${result} =    Run GitBackup    --version
    # CommandLineParser may return 0 or 1 for version - both are acceptable
    Should Be True    ${result.rc} == 0 or ${result.rc} == 1    Version should return exit code 0 or 1, got ${result.rc}
    # Version output may go to stdout or stderr
    ${output} =    Catenate    ${result.stdout}    ${result.stderr}
    Should Contain    ${output}    GitBackup
    Should Contain    ${result.stdout}    GitBackup version
    Should Contain    ${result.stdout}    Build:
    Should Contain    ${result.stdout}    .NET Runtime:

GitBackup Should Create Sample Configuration
    [Documentation]    Verify that GitBackup can create a sample configuration file
    ${config_file} =    Set Variable    ${CURDIR}${/}test_data${/}created_config.ini
    Remove File    ${config_file}    # Ensure file doesn't exist
    ${result} =    Run GitBackup    --create-config    ${config_file}
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    Sample configuration file created
    File Should Exist    ${config_file}
    ${config_content} =    Get File    ${config_file}
    Should Contain    ${config_content}    [GitBackup]
    Should Contain    ${config_content}    RootDir=
    Should Contain    ${config_content}    BackupDir=
    Remove File    ${config_file}    # Cleanup

GitBackup Should Handle Invalid Arguments
    [Documentation]    Verify that GitBackup handles invalid arguments gracefully
    ${result} =    Run GitBackup    --invalid-option
    Should Be Equal As Integers    ${result.rc}    1
    Should Contain    ${result.stdout}    Option 'invalid-option' is unknown

GitBackup Should Require Configuration File
    [Documentation]    Verify that GitBackup requires a configuration file
    ${result} =    Run GitBackup    --config    non_existent_config.ini
    Should Be Equal As Integers    ${result.rc}    1
    Should Contain    ${result.stdout}    Configuration file not found

GitBackup Should Validate Configuration
    [Documentation]    Verify that GitBackup validates configuration content
    Create Invalid Config File
    ${result} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}
    Should Be Equal As Integers    ${result.rc}    1
    Should Contain Any    ${result.stdout}    RootDir cannot be empty    RootDir does not exist

GitBackup Should Perform Dry Run
    [Documentation]    Verify that GitBackup dry run mode works
    [Setup]    Setup Valid Test Environment
    ${result} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}    --dry-run    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    DRY RUN MODE
    Should Contain    ${result.stdout}    Configuration:
    [Teardown]    Cleanup Valid Test Environment

GitBackup Should Perform Real Backup
    [Documentation]    Verify that GitBackup performs actual backup operations
    [Setup]    Setup Valid Test Environment
    Create Test Files
    ${result} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    Backup completed successfully
    Directory Should Exist    ${TEST_BACKUP_DIR}
    Directory Should Exist    ${TEST_BACKUP_DIR}${/}.git
    [Teardown]    Cleanup Valid Test Environment

GitBackup Should Handle Force Option
    [Documentation]    Verify that GitBackup force option works
    [Setup]    Setup Valid Test Environment
    Create Test Files
    # First backup
    ${result1} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}
    Should Be Equal As Integers    ${result1.rc}    0
    # Second backup without changes (should skip)
    ${result2} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}
    Should Be Equal As Integers    ${result2.rc}    0
    Should Contain    ${result2.stdout}    No changes detected
    # Force backup
    ${result3} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}    --force
    Should Be Equal As Integers    ${result3.rc}    0
    [Teardown]    Cleanup Valid Test Environment

GitBackup Should Handle Verbose Mode
    [Documentation]    Verify that GitBackup verbose mode provides detailed output
    [Setup]    Setup Valid Test Environment
    Create Test Files
    ${result} =    Run GitBackup    --config    ${TEST_CONFIG_FILE}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    Configuration loaded from:
    Should Contain    ${result.stdout}    Source:
    Should Contain    ${result.stdout}    Backup:
    Should Contain    ${result.stdout}    Git User:
    Should Contain    ${result.stdout}    Exclude patterns:
    [Teardown]    Cleanup Valid Test Environment
