*** Settings ***
Documentation     Shared keywords and variables for GitBackup testing
Library           OperatingSystem
Library           Process
Library           Collections
Library           String

*** Variables ***
${GITBACKUP_EXE}        gitbackup
${TEST_ROOT_DIR}        ${CURDIR}${/}test_data${/}source
${TEST_BACKUP_DIR}      ${CURDIR}${/}test_data${/}backup
${TEST_CONFIG_FILE}     ${CURDIR}${/}test_data${/}test_config.ini
${SAMPLE_CONFIG}        ${CURDIR}${/}test_data${/}sample_config.ini

*** Keywords ***
Setup Test Environment
    [Documentation]    Initialize test environment
    Log    Setting up test environment
    Log    GitBackup executable: ${GITBACKUP_EXE}
    Create Directory    ${TEST_ROOT_DIR}
    Create Directory    ${TEST_BACKUP_DIR}
    Create Test Files

Cleanup Test Environment
    [Documentation]    Clean up test environment
    Log    Cleaning up test environment
    # Handle Windows git repository file permissions
    Run Keyword And Ignore Error    Run Process    attrib    -R    ${CURDIR}${/}test_data    /S    shell=True
    Run Keyword And Ignore Error    Remove Directory    ${CURDIR}${/}test_data    recursive=True
    Run Keyword And Ignore Error    Run Process    attrib    -R    C:${/}temp${/}gitbackup_test    /S    shell=True  
    Run Keyword And Ignore Error    Remove Directory    C:${/}temp${/}gitbackup_test    recursive=True
    Run Keyword And Ignore Error    Remove Directory    /tmp/gitbackup_test    recursive=True

Create Test Files
    [Documentation]    Create sample files in the test root directory
    Create File    ${TEST_ROOT_DIR}${/}file1.txt    Test content for file 1
    Create File    ${TEST_ROOT_DIR}${/}file2.txt    Test content for file 2
    Create Directory    ${TEST_ROOT_DIR}${/}subdir
    Create File    ${TEST_ROOT_DIR}${/}subdir${/}file3.txt    Test content for file 3
    Create File    ${TEST_ROOT_DIR}${/}should_exclude.tmp    This should be excluded
    Create File    ${TEST_ROOT_DIR}${/}app.log    This log should be excluded

Run GitBackup
    [Documentation]    Run GitBackup with given arguments
    [Arguments]    @{args}
    Log    Running: ${GITBACKUP_EXE} @{args}
    ${result} =    Run Process    ${GITBACKUP_EXE}    @{args}    shell=True    timeout=30s
    Log    Exit code: ${result.rc}
    Log    Stdout: ${result.stdout}
    Log    Stderr: ${result.stderr}
    RETURN    ${result}

Setup Valid Test Environment
    [Documentation]    Setup a valid test environment with directories and config
    Create Directory    ${TEST_ROOT_DIR}
    Create Directory    ${TEST_BACKUP_DIR}
    Create Valid Config File

Cleanup Valid Test Environment
    [Documentation]    Clean up valid test environment
    # Handle Windows git repository file permissions
    Run Keyword And Ignore Error    Run Process    attrib    -R    ${TEST_ROOT_DIR}    /S    shell=True
    Run Keyword And Ignore Error    Run Process    attrib    -R    ${TEST_BACKUP_DIR}    /S    shell=True
    Run Keyword And Ignore Error    Remove Directory    ${TEST_ROOT_DIR}    recursive=True
    Run Keyword And Ignore Error    Remove Directory    ${TEST_BACKUP_DIR}    recursive=True
    Run Keyword And Ignore Error    Remove File    ${TEST_CONFIG_FILE}

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

Should Contain Any
    [Documentation]    Check if text contains any of the given strings
    [Arguments]    ${text}    @{expected_strings}
    FOR    ${expected}    IN    @{expected_strings}
        ${contains} =    Run Keyword And Return Status    Should Contain    ${text}    ${expected}
        IF    ${contains}    BREAK
    END
    Should Be True    ${contains}    Text should contain one of: ${expected_strings}

Create Test Config
    [Documentation]    Create a test configuration file
    [Arguments]    ${root_dir}    ${backup_dir}    ${config_file}=${TEST_CONFIG_FILE}
    ${config_content} =    Catenate    SEPARATOR=\n
    ...    [GitBackup]
    ...    RootDir=${root_dir}
    ...    BackupDir=${backup_dir}
    ...    GitUserName=Test User
    ...    GitUserEmail=test@example.com
    ...    Exclude:0=*.tmp
    ...    Exclude:1=*.log
    Create File    ${config_file}    ${config_content}
    RETURN    ${config_file}

Setup Linux Test Environment
    [Documentation]    Setup Linux-specific test environment
    Set Suite Variable    ${GITBACKUP_EXE}    ${GITBACKUP_EXE}
    Create Directory    /tmp/gitbackup_test/source
    Create Directory    /tmp/gitbackup_test/backup
    Create File    /tmp/gitbackup_test/source/linux_test.txt    Linux test content

Setup Windows Test Environment
    [Documentation]    Setup Windows-specific test environment
    Set Suite Variable    ${GITBACKUP_EXE}    ${GITBACKUP_EXE}
    Create Directory    C:${/}temp${/}gitbackup_test${/}source
    Create Directory    C:${/}temp${/}gitbackup_test${/}backup
    Create File    C:${/}temp${/}gitbackup_test${/}source${/}windows_test.txt    Windows test content
