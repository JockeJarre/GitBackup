*** Settings ***
Documentation     GitBackup Application Tests
Library           OperatingSystem
Library           Process
Library           Collections
Library           String
Suite Setup       Setup Test Environment
Suite Teardown    Cleanup Test Environment
Test Timeout      30 seconds

*** Variables ***
${GITBACKUP_EXE}        gitbackup
${TEST_ROOT_DIR}        ${CURDIR}${/}test_data${/}source
${TEST_BACKUP_DIR}      ${CURDIR}${/}test_data${/}backup
${TEST_CONFIG_FILE}     ${CURDIR}${/}test_data${/}test_config.ini
${SAMPLE_CONFIG}        ${CURDIR}${/}test_data${/}sample_config.ini

*** Test Cases ***
GitBackup Should Display Help
    [Documentation]    Verify that GitBackup displays help information
    ${result} =    Run GitBackup    --help
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    GitBackup - Git-based Directory Backup Tool
    Should Contain    ${result.stdout}    --config
    Should Contain    ${result.stdout}    --help

GitBackup Should Display Version
    [Documentation]    Verify that GitBackup displays version information
    ${result} =    Run GitBackup    --version
    Should Be Equal As Integers    ${result.rc}    0
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

*** Keywords ***
Setup Test Environment
    [Documentation]    Initialize test environment
    Log    Setting up test environment
    Create Directory    ${CURDIR}${/}test_data
    Set Suite Variable    ${TEST_TIMEOUT}    30s
    
Cleanup Test Environment
    [Documentation]    Clean up test environment
    Log    Cleaning up test environment
    Remove Directory    ${CURDIR}${/}test_data    recursive=True
    
Setup Valid Test Environment
    [Documentation]    Setup a valid test environment with directories and config
    Create Directory    ${TEST_ROOT_DIR}
    Create Directory    ${TEST_BACKUP_DIR}
    Create Valid Config File

Cleanup Valid Test Environment
    [Documentation]    Clean up valid test environment
    Remove Directory    ${TEST_ROOT_DIR}    recursive=True
    Remove Directory    ${TEST_BACKUP_DIR}    recursive=True
    Remove File    ${TEST_CONFIG_FILE}

Create Valid Config File
    [Documentation]    Create a valid configuration file for testing
    ${config_content} =    Catenate    SEPARATOR=\n
    ...    [GitBackup]
    ...    RootDir=${TEST_ROOT_DIR}
    ...    BackupDir=${TEST_BACKUP_DIR}
    ...    GitUserName=GitBackup Test
    ...    GitUserEmail=test@gitbackup.local
    ...    Exclude:0=*.tmp
    ...    Exclude:1=*.log
    ...    Exclude:2=.git/
    Create File    ${TEST_CONFIG_FILE}    ${config_content}

Create Invalid Config File
    [Documentation]    Create an invalid configuration file for testing
    ${config_content} =    Catenate    SEPARATOR=\n
    ...    [GitBackup]
    ...    RootDir=
    ...    BackupDir=
    Create File    ${TEST_CONFIG_FILE}    ${config_content}

Create Test Files
    [Documentation]    Create sample files in the test root directory
    Create File    ${TEST_ROOT_DIR}${/}file1.txt    Test content for file 1
    Create File    ${TEST_ROOT_DIR}${/}file2.txt    Test content for file 2
    Create Directory    ${TEST_ROOT_DIR}${/}subdir
    Create File    ${TEST_ROOT_DIR}${/}subdir${/}file3.txt    Test content for file 3
    Create File    ${TEST_ROOT_DIR}${/}should_exclude.tmp    This should be excluded
    Create File    ${TEST_ROOT_DIR}${/}app.log    This log should be excluded

Run GitBackup
    [Documentation]    Run GitBackup with specified arguments
    [Arguments]    @{args}
    ${result} =    Run Process    ${GITBACKUP_EXE}    @{args}    shell=True    timeout=${TEST_TIMEOUT}
    Log    Command: ${GITBACKUP_EXE} ${args}
    Log    Return Code: ${result.rc}
    Log    STDOUT: ${result.stdout}
    Log    STDERR: ${result.stderr}
    RETURN    ${result}

Should Contain Any
    [Documentation]    Check if text contains any of the given strings
    [Arguments]    ${text}    @{expected_strings}
    FOR    ${expected}    IN    @{expected_strings}
        ${contains} =    Run Keyword And Return Status    Should Contain    ${text}    ${expected}
        IF    ${contains}    BREAK
    END
    Should Be True    ${contains}    Text should contain one of: ${expected_strings}
