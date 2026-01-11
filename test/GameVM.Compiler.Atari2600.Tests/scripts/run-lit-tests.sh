#!/bin/bash
# run-lit-tests.sh
# Sets up and runs LLVM lit tests for the GameVM compiler on Linux.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../" && pwd)"
VENV_DIR="$SCRIPT_DIR/.venv"
LIT_TEST_DIR="$SCRIPT_DIR/../lit-tests"

echo "=== GameVM Compiler Test Runner (Linux) ==="
echo "Project Root: $PROJECT_ROOT"

echo "Building compiler and dev tools..."
dotnet build "$PROJECT_ROOT/src/GameVM.Compile" -c Debug -q
dotnet build "$PROJECT_ROOT/src/GameVM.DevTools" -c Debug -q
if [ ! -d "$VENV_DIR" ]; then
    echo "Creating Python virtual environment..."
    python3 -m venv "$VENV_DIR"
    if [ $? -ne 0 ]; then
        echo "Error: Failed to create virtual environment"
        exit 1
    fi
    
    echo "Installing lit..."
    "$VENV_DIR/bin/pip" install lit
    if [ $? -ne 0 ]; then
        echo "Error: Failed to install lit"
        exit 1
    fi
fi

# Run lit tests
echo "Running tests..."
"$VENV_DIR/bin/lit" -v "$LIT_TEST_DIR"
EXIT_CODE=$?

if [ $EXIT_CODE -ne 0 ]; then
    echo "Some tests failed"
else
    echo "All tests passed!"
fi

exit $EXIT_CODE
