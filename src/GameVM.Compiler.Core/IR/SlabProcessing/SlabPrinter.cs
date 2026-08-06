using System;
using System.IO;
using System.Text;

using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
namespace GameVM.Compiler.Core.IR.SlabProcessing;

/// <summary>
/// Translates an <see cref="InstList"/> instruction slab into a readable pseudo-assembly
/// representation. This is a debugging/diagnostic utility mandated by the design as a strict
/// prerequisite before writing optimization passes: debugging raw integer arrays is
/// prohibitively difficult otherwise.
/// </summary>
public sealed class SlabPrinter
{
    private const int HeaderLength = 6;

    /// <summary>Maps instruction kind (0-255) to its pseudo-assembly mnemonic.</summary>
    /// <remarks>
    /// The DOD design does not yet define a canonical opcode table; this printer uses a small
    /// known set (NOP = 0 is reserved as the slab sentinel) and renders any unknown kind as
    /// <c>UNK.&lt;kind&gt;</c>. The table is intentionally data-only (no strings stored in IR
    /// structures) and exists purely for human-readable rendering.
    /// </remarks>
    private static readonly string[] KindMnemonics =
    {
        "NOP",    // 0  - reserved sentinel / tombstone
        "LOAD",   // 1
        "STORE",  // 2
        "MOV",    // 3
        "ADD",    // 4
        "SUB",    // 5
        "MUL",    // 6
        "DIV",    // 7
        "MOD",    // 8
        "AND",    // 9
        "OR",     // 10
        "XOR",    // 11
        "NOT",    // 12
        "SHL",    // 13
        "SHR",    // 14
        "CMP",    // 15
        "TEST",   // 16
        "JUMP",   // 17
        "JZ",     // 18
        "JNZ",    // 19
        "JLT",    // 20
        "JGT",    // 21
        "CALL",   // 22
        "RET",    // 23
        "CAST",   // 24
        "MEMCPY", // 25
        "MEMSET", // 26
        "MEMCMP", // 27
    };

    private readonly InstList _slab;

    /// <summary>Initializes the printer over an InstList instruction slab.</summary>
    public SlabPrinter(InstList slab)
    {
        _slab = slab;
    }

    /// <summary>
    /// Renders the entire slab to a single string.
    /// </summary>
    public string Print()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        Print(writer);
        return sb.ToString();
    }

    /// <summary>
    /// Renders the slab to the given writer (one line per instruction block,
    /// with block boundaries marked after terminator instructions).
    /// </summary>
    public void Print(TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        writer.WriteLine("; === instructions ===");

        int offset = HeaderLength;
        for (int i = 0; i < _slab.Count; i++)
        {
            byte kind = _slab.GetKind(i);
            ushort argCount = _slab.GetArgCount(i);
            ReadOnlySpan<uint> operands = _slab.GetOperands(i);
            ushort flags = _slab.GetFlags(i);
            bool isTerminator = (flags & (ushort)InstructionFlag.Terminator) != 0;
            bool hasDiagnostic = (flags & (ushort)InstructionFlag.Diagnostic) != 0;

            var mnemonic = Mnemonic(kind);
            var line = new StringBuilder();
            line.Append($"{offset,5}: {mnemonic}");

            for (int j = 0; j < argCount; j++)
            {
                line.Append($" {operands[j]:X8}");
            }

            if (isTerminator)
                line.Append(" [TERM]");
            if (hasDiagnostic)
                line.Append(" [DIAG]");

            writer.WriteLine(line.ToString());

            if (isTerminator)
                writer.WriteLine("; --- basic block boundary ---");

            // Each instruction occupies 1 (header slot) + argCount slots in the flat layout
            offset += 1 + argCount;
        }
    }

    private static string Mnemonic(byte kind)
    {
        return kind < KindMnemonics.Length ? KindMnemonics[kind] : $"UNK.{kind}";
    }
}