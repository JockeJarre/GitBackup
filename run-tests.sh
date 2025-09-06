#!/bin/bash
# GitBackup Robot Framework Test Execution Script for Linux/macOS
# This script runs all Robot Framework tests

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
TEST_SUITE="all"
OUTPUT_DIR="test_results"
VERBOSE=false
DRY_RUN=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--suite)
            TEST_SUITE="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -s, --suite SUITE    Test suite to run (all|core|linux) [default: all]"
            echo "  -o, --output DIR     Output directory [default: test_results]"
            echo "  -v, --verbose        Enable verbose output"
            echo "  --dry-run            Show what would be executed without running"
            echo "  -h, --help           Show this help"
            echo ""
            echo "Examples:"
            echo "  $0                   # Run all tests"
            echo "  $0 -s core          # Run only core tests"
            echo "  $0 -v --dry-run     # Show verbose dry run"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}GitBackup Robot Framework Test Runner${NC}"
echo -e "${GREEN}====================================${NC}"
echo ""

# Check if Robot Framework is installed
echo -e "${CYAN}Checking Robot Framework installation...${NC}"
if ! python3 -m robot.libdoc --version >/dev/null 2>&1; then
    echo -e "${YELLOW}Robot Framework not found. Installing...${NC}"
    python3 -m pip install --upgrade pip
    python3 -m pip install -r tests/requirements.txt
    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to install Robot Framework dependencies${NC}"
        exit 1
    fi
else
    RF_VERSION=$(python3 -m robot.libdoc --version 2>&1 | head -1)
    echo -e "${GREEN}Robot Framework found: $RF_VERSION${NC}"
fi

# Build the application
echo -e "${CYAN}Building GitBackup application...${NC}"
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

# Find the executable
EXE_PATH="bin/Release/net8.0/GitBackup"
if [ ! -f "$EXE_PATH" ]; then
    echo -e "${RED}GitBackup executable not found at: $EXE_PATH${NC}"
    exit 1
fi
echo -e "${GREEN}GitBackup executable found at: $EXE_PATH${NC}"

# Make executable available in PATH
export PATH="$(pwd)/bin/Release/net8.0:$PATH"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Determine which tests to run
TEST_FILES=()
case "${TEST_SUITE,,}" in
    "all")
        TEST_FILES+=("tests/gitbackup_tests.robot")
        if [[ "$OSTYPE" == "linux-gnu"* ]]; then
            TEST_FILES+=("tests/linux_tests.robot")
        elif [[ "$OSTYPE" == "darwin"* ]]; then
            TEST_FILES+=("tests/linux_tests.robot")  # macOS uses similar tests as Linux
        fi
        ;;
    "core")
        TEST_FILES+=("tests/gitbackup_tests.robot")
        ;;
    "linux")
        TEST_FILES+=("tests/linux_tests.robot")
        ;;
    *)
        TEST_FILES+=("$TEST_SUITE")
        ;;
esac

echo -e "${CYAN}Test files to execute:${NC}"
for file in "${TEST_FILES[@]}"; do
    echo -e "  ${YELLOW}- $file${NC}"
done
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}DRY RUN MODE - Would execute the following command:${NC}"
    ROBOT_CMD="python3 -m robot --outputdir $OUTPUT_DIR --timestampoutputs"
    if [ "$VERBOSE" = true ]; then
        ROBOT_CMD="$ROBOT_CMD --loglevel DEBUG"
    fi
    ROBOT_CMD="$ROBOT_CMD ${TEST_FILES[*]}"
    echo -e "${CYAN}$ROBOT_CMD${NC}"
    exit 0
fi

# Execute Robot Framework tests
echo -e "${CYAN}Running Robot Framework tests...${NC}"

ROBOT_ARGS=(
    "--outputdir" "$OUTPUT_DIR"
    "--timestampoutputs"
    "--reporttitle" "GitBackup Test Report"
    "--logtitle" "GitBackup Test Log"
    "--variable" "GITBACKUP_EXE:$EXE_PATH"
)

if [ "$VERBOSE" = true ]; then
    ROBOT_ARGS+=("--loglevel" "DEBUG")
fi

ROBOT_ARGS+=("${TEST_FILES[@]}")

echo -e "${CYAN}Executing: python3 -m robot ${ROBOT_ARGS[*]}${NC}"
python3 -m robot "${ROBOT_ARGS[@]}"
TEST_RESULT=$?

# Display results
echo ""
if [ $TEST_RESULT -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
else
    echo -e "${RED}Some tests failed (exit code: $TEST_RESULT)${NC}"
fi

echo ""
echo -e "${CYAN}Test results available in:${NC}"
echo -e "  ${YELLOW}- HTML Report: $OUTPUT_DIR/report.html${NC}"
echo -e "  ${YELLOW}- Log File: $OUTPUT_DIR/log.html${NC}"
echo -e "  ${YELLOW}- XML Output: $OUTPUT_DIR/output.xml${NC}"

exit $TEST_RESULT
