#!/bin/bash
# bincheck.sh <binary_file> <check_file>
# Converts binary to hex and verifies it using our custom FileCheck

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <binary_file> <check_file>"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
hexdump -C "$1" | python3 "$SCRIPT_DIR/filecheck.py" "$2"
