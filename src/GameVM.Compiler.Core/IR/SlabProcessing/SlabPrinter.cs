using System;
using System.IO;
using System.Text;
using GameVM.Compiler.Core.Utilities;

using GameVM.Compiler.Core.IR.Slab;
namespace GameVM.Compiler.Core.IR.SlabProcessing;

/// <summary>
/// Translates a raw <c>uint[]</c> instruction slab into a readable pseudo-assembly text
/// representation. This is a debugging/diagnostic utility mandated by the design as a strict
/// prerequisite before writing optimization passes: debugging raw integer arrays is
/// prohibitively difficult otherwise.
///
/// Layout consumed: a 6-index standardized header (see <see cref="SlabHeader"/>), followed by
/// self-describing instruction blocks. Each block starts with a 32-bit metadata word
/// (<see cref="InstructionMetadata"/>) encoding kind/size/arg-count/flags, followed by
/// <c>size - 1</c> payload uints.
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

    private readonly uint[] _slab;

    /// <summary>Initializes the printer over a raw instruction slab.</summary>
    public SlabPrinter(uint[] slab)
    {
        _slab = slab ?? throw new ArgumentNullException(nameof(slab));
    }

    /// <summary>
    /// Renders the entire slab (header + instruction blocks) to a single string.
    /// </summary>
    public string Print()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        Print(writer);
        return sb.ToString();
    }

    /// <summary>
    /// Renders the slab to the given writer (header summary, then one line per instruction block,
    /// with block boundaries marked after terminator instructions).
    /// </summary>
    public void Print(TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (_slab.Length < HeaderLength)
            throw new ArgumentException($"Slab must contain at least {HeaderLength} indices for the header");

        var header = SlabHeader.Read(_slab);
        header.Validate((uint)_slab.Length);

        writer.WriteLine("; === slab header ===");
        writer.WriteLine($"; magic    = 0x{header.MagicNumber:X8}");
        writer.WriteLine($"; version  = {header.Major}.{header.Minor}");
        writer.WriteLine($"; ir-stage = {header.IrStage} ({IrStageName(header.IrStage)})");
        writer.WriteLine($"; elements = {header.ElementCount}");
        writer.WriteLine($"; symbols  = {(header.SymbolTableOffset == 0 ? "none" : header.SymbolTableOffset.ToString())}");
        writer.WriteLine("; === instructions ===");

        int offset = HeaderLength;
        while (offset < _slab.Length)
        {
            var metadata = _slab[offset];
            var size = MetadataDecoder.DecodeSize(metadata);
            if (size == 0)
                throw new InvalidOperationException($"Block at {offset} has zero size (corrupt slab)");

            var kind = MetadataDecoder.DecodeKind(metadata);
            var argCount = MetadataDecoder.DecodeArgCount(metadata);
            var isTerminator = MetadataDecoder.DecodeIsTerminator(metadata);
            var hasDiagnostic = MetadataDecoder.DecodeHasDiagnostic(metadata);

            var mnemonic = Mnemonic(kind);
            var line = new StringBuilder();
            line.Append($"{offset,5}: {mnemonic}");

            // Remaining payload uints after the metadata word are the instruction arguments.
            var argCountClamped = Math.Min(argCount, (int)size - 1);
            for (int i = 0; i < argCountClamped; i++)
            {
                line.Append($" {_slab[offset + 1 + i]:X8}");
            }

            if (isTerminator)
                line.Append(" [TERM]");
            if (hasDiagnostic)
                line.Append(" [DIAG]");

            writer.WriteLine(line.ToString());

            if (isTerminator)
                writer.WriteLine("; --- basic block boundary ---");

            offset += size;
        }
    }

    private static string Mnemonic(byte kind)
    {
        return kind < KindMnemonics.Length ? KindMnemonics[kind] : $"UNK.{kind}";
    }

    private static string IrStageName(uint stage) => stage switch
    {
        0 => "AST",
        1 => "HLIR",
        2 => "MLIR",
        3 => "LLIR",
        _ => "UNKNOWN",
    };
}
