import sys

def hexdump(filename):
    with open(filename, 'rb') as f:
        offset = 0
        while True:
            chunk = f.read(16)
            if not chunk:
                break
            
            hex_bytes = ' '.join(f'{b:02x}' for b in chunk)
            # Match hexdump -C output roughly
            print(f'{offset:08x}  {hex_bytes:<47}')
            offset += 16

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python hexdump.py <file>")
        sys.exit(1)
    hexdump(sys.argv[1])
