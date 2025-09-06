*** Settings ***
Documentation     Platform-specific tests for GitBackup on Windows
Resource          gitbackup_keywords.robot
Suite Setup       Setup Windows Test Environment
Suite Teardown    Cleanup Test Environment
Test Timeout      30 seconds

*** Variables ***
${WINDOWS_TEST_ROOT}    C:${/}temp${/}gitbackup_test${/}source
${WINDOWS_TEST_BACKUP}  C:${/}temp${/}gitbackup_test${/}backup
${WINDOWS_CONFIG}       ${CURDIR}${/}test_data${/}windows_config.ini

*** Test Cases ***
Windows Path Handling Should Work
    [Documentation]    Verify GitBackup handles Windows paths correctly
    [Setup]    Setup Windows Paths Test
    ${result} =    Run GitBackup    --config    ${WINDOWS_CONFIG}    --dry-run    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    Source: ${WINDOWS_TEST_ROOT}
    Should Contain    ${result.stdout}    Backup: ${WINDOWS_TEST_BACKUP}
    [Teardown]    Cleanup Windows Paths Test

Windows Long Paths Should Work
    [Documentation]    Verify GitBackup handles Windows long paths
    [Setup]    Setup Windows Long Path Test
    ${result} =    Run GitBackup    --config    ${WINDOWS_CONFIG}    --dry-run
    Should Be Equal As Integers    ${result.rc}    0
    [Teardown]    Cleanup Windows Long Path Test

Windows File Permissions Should Work
    [Documentation]    Verify GitBackup handles Windows file permissions
    [Setup]    Setup Windows Permissions Test
    Create File    ${WINDOWS_TEST_ROOT}${/}readonly.txt    Read-only content
    Run Process    attrib    +R    ${WINDOWS_TEST_ROOT}${/}readonly.txt    shell=True
    ${result} =    Run GitBackup    --config    ${WINDOWS_CONFIG}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Run Process    attrib    -R    ${WINDOWS_TEST_ROOT}${/}readonly.txt    shell=True
    [Teardown]    Cleanup Windows Permissions Test

*** Keywords ***
Setup Windows Test Environment
    [Documentation]    Setup Windows-specific test environment
    Log    Setting up Windows test environment
    Create Directory    C:${/}temp${/}gitbackup_test

Setup Windows Paths Test
    [Documentation]    Setup test with Windows-specific paths
    Create Directory    ${WINDOWS_TEST_ROOT}
    Create Windows Config File
    Create Test Files In Windows Directory

Setup Windows Long Path Test
    [Documentation]    Setup test with Windows long paths
    ${long_path} =    Set Variable    ${WINDOWS_TEST_ROOT}${/}very${/}long${/}path${/}structure${/}for${/}testing${/}windows${/}path${/}limits
    Create Directory    ${long_path}
    Create File    ${long_path}${/}test_file.txt    Long path test content

Setup Windows Permissions Test  
    [Documentation]    Setup test with Windows file permissions
    Create Directory    ${WINDOWS_TEST_ROOT}
    Create Windows Config File

Cleanup Windows Paths Test
    [Documentation]    Clean up Windows paths test
    Remove Directory    ${WINDOWS_TEST_ROOT}    recursive=True
    Remove Directory    ${WINDOWS_TEST_BACKUP}    recursive=True
    Remove File    ${WINDOWS_CONFIG}

Cleanup Windows Long Path Test
    [Documentation]    Clean up Windows long path test
    Remove Directory    ${WINDOWS_TEST_ROOT}    recursive=True

Cleanup Windows Permissions Test
    [Documentation]    Clean up Windows permissions test  
    Remove Directory    ${WINDOWS_TEST_ROOT}    recursive=True
    Remove Directory    ${WINDOWS_TEST_BACKUP}    recursive=True
    Remove File    ${WINDOWS_CONFIG}

Create Windows Config File
    [Documentation]    Create Windows-specific configuration file
    ${config_content} =    Catenate    SEPARATOR=\n
    ...    [GitBackup]
    ...    RootDir=${WINDOWS_TEST_ROOT}
    ...    BackupDir=${WINDOWS_TEST_BACKUP}
    ...    GitUserName=GitBackup Windows Test
    ...    GitUserEmail=windows-test@gitbackup.local
    ...    Exclude:0=*.tmp
    ...    Exclude:1=Thumbs.db
    ...    Exclude:2=desktop.ini
    Create File    ${WINDOWS_CONFIG}    ${config_content}

Create Test Files In Windows Directory
    [Documentation]    Create test files in Windows directory
    Create File    ${WINDOWS_TEST_ROOT}${/}windows_file.txt    Windows test content
    Create File    ${WINDOWS_TEST_ROOT}${/}Thumbs.db    Should be excluded
    Create File    ${WINDOWS_TEST_ROOT}${/}desktop.ini    Should be excluded
