*** Settings ***
Documentation     Platform-specific tests for GitBackup on Linux
Resource          gitbackup_tests.robot
Suite Setup       Setup Linux Test Environment
Suite Teardown    Cleanup Test Environment  
Test Timeout      30 seconds

*** Variables ***
${LINUX_TEST_ROOT}      /tmp/gitbackup_test/source
${LINUX_TEST_BACKUP}    /tmp/gitbackup_test/backup
${LINUX_CONFIG}         ${CURDIR}/test_data/linux_config.ini

*** Test Cases ***
Linux Path Handling Should Work
    [Documentation]    Verify GitBackup handles Linux paths correctly
    [Setup]    Setup Linux Paths Test
    ${result} =    Run GitBackup    --config    ${LINUX_CONFIG}    --dry-run    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    Should Contain    ${result.stdout}    Source: ${LINUX_TEST_ROOT}
    Should Contain    ${result.stdout}    Backup: ${LINUX_TEST_BACKUP}
    [Teardown]    Cleanup Linux Paths Test

Linux File Permissions Should Work
    [Documentation]    Verify GitBackup handles Linux file permissions correctly
    [Setup]    Setup Linux Permissions Test
    Create File    ${LINUX_TEST_ROOT}/executable.sh    #!/bin/bash\necho "test"
    Run Process    chmod    +x    ${LINUX_TEST_ROOT}/executable.sh
    ${result} =    Run GitBackup    --config    ${LINUX_CONFIG}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    [Teardown]    Cleanup Linux Permissions Test

Linux Hidden Files Should Be Handled
    [Documentation]    Verify GitBackup handles Linux hidden files correctly
    [Setup]    Setup Linux Hidden Files Test
    Create File    ${LINUX_TEST_ROOT}/.hidden_file    Hidden file content
    Create File    ${LINUX_TEST_ROOT}/.gitignore    *.tmp\n*.log
    ${result} =    Run GitBackup    --config    ${LINUX_CONFIG}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    [Teardown]    Cleanup Linux Hidden Files Test

Linux Symlinks Should Be Handled
    [Documentation]    Verify GitBackup handles Linux symbolic links
    [Setup]    Setup Linux Symlinks Test
    Create File    ${LINUX_TEST_ROOT}/target.txt    Target file content
    Run Process    ln    -s    ${LINUX_TEST_ROOT}/target.txt    ${LINUX_TEST_ROOT}/symlink.txt
    ${result} =    Run GitBackup    --config    ${LINUX_CONFIG}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    [Teardown]    Cleanup Linux Symlinks Test

Linux Case Sensitivity Should Work
    [Documentation]    Verify GitBackup respects Linux case sensitivity
    [Setup]    Setup Linux Case Sensitivity Test
    Create File    ${LINUX_TEST_ROOT}/File.txt    Uppercase F
    Create File    ${LINUX_TEST_ROOT}/file.txt    Lowercase f
    ${result} =    Run GitBackup    --config    ${LINUX_CONFIG}    --verbose
    Should Be Equal As Integers    ${result.rc}    0
    [Teardown]    Cleanup Linux Case Sensitivity Test

*** Keywords ***
Setup Linux Test Environment
    [Documentation]    Setup Linux-specific test environment
    Log    Setting up Linux test environment
    Run Process    mkdir    -p    /tmp/gitbackup_test

Setup Linux Paths Test
    [Documentation]    Setup test with Linux-specific paths
    Run Process    mkdir    -p    ${LINUX_TEST_ROOT}
    Create Linux Config File
    Create Test Files In Linux Directory

Setup Linux Permissions Test
    [Documentation]    Setup test with Linux file permissions
    Run Process    mkdir    -p    ${LINUX_TEST_ROOT}
    Create Linux Config File

Setup Linux Hidden Files Test
    [Documentation]    Setup test with Linux hidden files
    Run Process    mkdir    -p    ${LINUX_TEST_ROOT}
    Create Linux Config File

Setup Linux Symlinks Test
    [Documentation]    Setup test with Linux symbolic links
    Run Process    mkdir    -p    ${LINUX_TEST_ROOT}
    Create Linux Config File

Setup Linux Case Sensitivity Test
    [Documentation]    Setup test with Linux case sensitivity
    Run Process    mkdir    -p    ${LINUX_TEST_ROOT}
    Create Linux Config File

Cleanup Linux Paths Test
    [Documentation]    Clean up Linux paths test
    Run Process    rm    -rf    ${LINUX_TEST_ROOT}
    Run Process    rm    -rf    ${LINUX_TEST_BACKUP}
    Remove File    ${LINUX_CONFIG}

Cleanup Linux Permissions Test
    [Documentation]    Clean up Linux permissions test
    Run Process    rm    -rf    ${LINUX_TEST_ROOT}
    Run Process    rm    -rf    ${LINUX_TEST_BACKUP}
    Remove File    ${LINUX_CONFIG}

Cleanup Linux Hidden Files Test
    [Documentation]    Clean up Linux hidden files test
    Run Process    rm    -rf    ${LINUX_TEST_ROOT}
    Run Process    rm    -rf    ${LINUX_TEST_BACKUP}
    Remove File    ${LINUX_CONFIG}

Cleanup Linux Symlinks Test
    [Documentation]    Clean up Linux symlinks test
    Run Process    rm    -rf    ${LINUX_TEST_ROOT}
    Run Process    rm    -rf    ${LINUX_TEST_BACKUP}
    Remove File    ${LINUX_CONFIG}

Cleanup Linux Case Sensitivity Test
    [Documentation]    Clean up Linux case sensitivity test
    Run Process    rm    -rf    ${LINUX_TEST_ROOT}
    Run Process    rm    -rf    ${LINUX_TEST_BACKUP}
    Remove File    ${LINUX_CONFIG}

Create Linux Config File
    [Documentation]    Create Linux-specific configuration file
    ${config_content} =    Catenate    SEPARATOR=\n
    ...    [GitBackup]
    ...    RootDir=${LINUX_TEST_ROOT}
    ...    BackupDir=${LINUX_TEST_BACKUP}
    ...    GitUserName=GitBackup Linux Test
    ...    GitUserEmail=linux-test@gitbackup.local
    ...    Exclude:0=*.tmp
    ...    Exclude:1=*.log
    ...    Exclude:2=.DS_Store
    ...    Exclude:3=core
    Create File    ${LINUX_CONFIG}    ${config_content}

Create Test Files In Linux Directory
    [Documentation]    Create test files in Linux directory
    Create File    ${LINUX_TEST_ROOT}/linux_file.txt    Linux test content
    Create File    ${LINUX_TEST_ROOT}/.DS_Store    Should be excluded on Linux too
    Create File    ${LINUX_TEST_ROOT}/core    Should be excluded core dump
