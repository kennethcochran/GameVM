using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Backend.Atari2600
{
    public static class M6502Emitter
    {
        public static byte[] Emit(List<string> assembly)
        {
            var binary = new List<byte>();

            foreach (var line in assembly)
            {
                var trimmed = line.Trim();
                if (trimmed.EndsWith(':')) continue; // Skip labels for now

                if (IsLdaInstruction(trimmed))
                {
                    ProcessLdaInstruction(trimmed, binary);
                }
                else if (IsStaInstruction(trimmed))
                {
                    ProcessStaInstruction(trimmed, binary);
                }
            }

            return binary.ToArray();
        }

        private static bool IsLdaInstruction(string trimmed)
        {
            return trimmed.StartsWith("LDA #");
        }

        private static bool IsStaInstruction(string trimmed)
        {
            return trimmed.StartsWith("STA ");
        }

        private static void ProcessLdaInstruction(string trimmed, List<byte> binary)
        {
            binary.Add(0xA9);
            var valStr = trimmed.Substring(5).Trim();
            var value = ParseValue(valStr);
            binary.Add(value);
        }

        private static void ProcessStaInstruction(string trimmed, List<byte> binary)
        {
            var addrStr = trimmed.Substring(4).Trim();
            var addr = ParseAddress(addrStr);

            if (addr < 0x100)
            {
                // Zero Page Addressing
                binary.Add(0x85);
                binary.Add((byte)addr);
            }
            else
            {
                // Absolute Addressing
                binary.Add(0x8D);
                binary.Add((byte)(addr & 0xFF));
                binary.Add((byte)((addr >> 8) & 0xFF));
            }
        }

        private static byte ParseValue(string valStr)
        {
            if (valStr.StartsWith('$'))
            {
                return (byte)Convert.ToInt32(valStr.Substring(1), 16);
            }
            else if (byte.TryParse(valStr, out byte b))
            {
                return b;
            }
            else if (int.TryParse(valStr, out int val))
            {
                return (byte)val;
            }
            throw new ArgumentException($"Unable to parse value: {valStr}");
        }

        private static int ParseAddress(string addrStr)
        {
            if (addrStr.StartsWith('$'))
            {
                return Convert.ToInt32(addrStr.Substring(1), 16);
            }
            else
            {
                int.TryParse(addrStr, out int addr);
                return addr;
            }
        }
    }
}
