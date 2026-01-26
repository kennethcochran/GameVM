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

                if (trimmed.StartsWith('L') && trimmed.StartsWith("LDA #"))
                {
                    binary.Add(0xA9);
                    var valStr = trimmed.Substring(5).Trim();
                    
                    if (valStr.StartsWith('$'))
                    {
                        binary.Add((byte)Convert.ToInt32(valStr.Substring(1), 16));
                    }
                    else if (byte.TryParse(valStr, out byte b))
                    {
                        binary.Add(b);
                    }
                    else if (int.TryParse(valStr, out int val))
                    {
                        binary.Add((byte)val);
                    }
                }
                else if (trimmed.StartsWith('S') && trimmed.StartsWith("STA "))
                {
                    var addrStr = trimmed.Substring(4).Trim();
                    int addr = 0;

                    if (addrStr.StartsWith('$'))
                    {
                        addr = Convert.ToInt32(addrStr.Substring(1), 16);
                    }
                    else
                    {
                        int.TryParse(addrStr, out addr);
                    }

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
            }

            return binary.ToArray();
        }
    }
}
