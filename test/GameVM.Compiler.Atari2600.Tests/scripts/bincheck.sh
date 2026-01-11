#!/bin/bash
# bincheck.sh <binary_file> <check_file>
# Converts binary to hex and verifies it using our custom FileCheck

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <binary_file> <check_file>"
    exit 1
fi

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Detect python
if command -v python3 &> /dev/null; then
    PYTHON_CMD="python3"
else
    PYTHON_CMD="python"
fi

# Convert binary to hex strings and pipe to filecheck
$PYTHON_CMD "$SCRIPT_DIR/hexdump.py" "$1" | $PYTHON_CMD "$SCRIPT_DIR/filecheck.py" "$2"
