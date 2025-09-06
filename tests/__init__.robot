*** Settings ***
Documentation     GitBackup Test Suite - All Platform Tests
Suite Setup       Global Suite Setup  
Suite Teardown    Global Suite Teardown
Test Timeout      60 seconds

*** Variables ***
${GITBACKUP_VERSION}    Unknown
${TEST_PLATFORM}        Unknown

*** Keywords ***
Global Suite Setup
    [Documentation]    Global setup for all test suites
    Log    Starting GitBackup Test Suite
    ${platform} =    Evaluate    platform.system()    platform
    Set Global Variable    ${TEST_PLATFORM}    ${platform}
    Log    Running tests on platform: ${TEST_PLATFORM}
    
    # Get GitBackup version for reporting
    ${result} =    Run Process    gitbackup    --version    shell=True
    IF    ${result.rc} == 0
        ${version_line} =    Get Lines Containing String    ${result.stdout}    GitBackup version
        ${version} =    Remove String    ${version_line}    GitBackup version${SPACE}
        Set Global Variable    ${GITBACKUP_VERSION}    ${version}
    END
    Log    GitBackup version: ${GITBACKUP_VERSION}

Global Suite Teardown
    [Documentation]    Global teardown for all test suites
    Log    GitBackup Test Suite completed
    Log    Platform: ${TEST_PLATFORM}
    Log    Version tested: ${GITBACKUP_VERSION}
