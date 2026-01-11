import re
import sys
import argparse

def main():
    parser = argparse.ArgumentParser(description="Lightweight FileCheck equivalent for GameVM")
    parser.add_argument('check_file', help="The test file containing CHECK: directives")
    parser.add_argument('input_file', nargs='?', type=argparse.FileType('r'), default=sys.stdin, 
                        help="The output to check (default: stdin)")
    args = parser.parse_args()

    # Read check directives from the test file
    with open(args.check_file, 'r') as f:
        check_content = f.read()

    # Extract CHECK: and CHECK-NEXT: directives
    # We support // CHECK:, ; CHECK:, { CHECK: style comments
    checks = []
    for line in check_content.splitlines():
        # Handle various comment styles: //, ;, { ... }
        match = re.search(r'(?://|;|\{)\s*CHECK:\s*([^}]*)', line)
        if match:
            checks.append(('CHECK', match.group(1).strip()))
            continue
            
        match = re.search(r'(?://|;|\{)\s*CHECK-NEXT:\s*([^}]*)', line)
        if match:
            checks.append(('CHECK-NEXT', match.group(1).strip()))
            continue

    if not checks:
        print(f"Error: No CHECK: directives found in {args.check_file}", file=sys.stderr)
        sys.exit(1)

    # Read input lines to check against
    input_lines = args.input_file.read().splitlines()
    
    current_input_idx = 0
    passed = True

    for i, (check_type, pattern) in enumerate(checks):
        found = False
        
        # Simple substring match for now
        if check_type == 'CHECK':
            for j in range(current_input_idx, len(input_lines)):
                if pattern in input_lines[j]:
                    current_input_idx = j + 1
                    found = True
                    break
        elif check_type == 'CHECK-NEXT':
            if current_input_idx < len(input_lines):
                if pattern in input_lines[current_input_idx]:
                    current_input_idx += 1
                    found = True
                else:
                    print(f"FAILED: CHECK-NEXT mismatch at line {current_input_idx + 1}", file=sys.stderr)
                    print(f"  Expected: {pattern}", file=sys.stderr)
                    print(f"  Actual:   {input_lines[current_input_idx]}", file=sys.stderr)
            else:
                print(f"FAILED: CHECK-NEXT reached EOF", file=sys.stderr)

        if not found:
            if check_type == 'CHECK':
                print(f"FAILED: Could not find CHECK: {pattern} (starting from line {current_input_idx + 1})", file=sys.stderr)
            passed = False
            break

    if passed:
        print("FileCheck PASSED")
        sys.exit(0)
    else:
        sys.exit(1)

if __name__ == '__main__':
    main()
