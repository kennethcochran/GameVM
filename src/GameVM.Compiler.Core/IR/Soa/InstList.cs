using System;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Struct of Arrays (SoA) representation of an instruction list.
/// Contains parallel arrays for instruction properties and a variable-length operand pool.
/// </summary>
public readonly struct InstList
{
    private readonly byte[] _tags;          // Instruction kind enum (u8)
    private readonly ushort[] _flags;       // Bitwise flags (Terminator, Diagnostic, etc.)
    private readonly ushort[] _argCounts;   // Number of operands per instruction
    private readonly uint[] _fixedOps;      // Fixed operand slots (MAX_FIXED_OPS * count)
    private readonly uint[] _extra;         // Variable-length operand pool
    private readonly uint[] _extraOffsets;  // Per-instruction offset into extra pool
    private readonly int[] _blockIds;       // CFG block ID per instruction (0 = unassigned)
    private readonly int _count;
    private readonly uint _extraUsed;

    /// <summary>Gets the number of instructions in this <see cref="InstList"/>.</summary>
    public int Count => _count;

    /// <summary>Total uints used in the extra operand pool.</summary>
    public uint ExtraUsed => _extraUsed;

    /// <summary>Gets the instruction at the specified index.</summary>
    public InstMetadata this[int index] => new InstMetadata(
        _tags[index],
        _flags[index],
        _argCounts[index],
        _blockIds[index],
        index
    );

    /// <summary>Gets the total size in uints of the fixed operand array.</summary>
    public int FixedOpSize => _fixedOps.Length;

    /// <summary>Gets the total size in uints of the extra operand pool.</summary>
    public uint ExtraPoolSize => (uint)_extra.Length;

    /// <summary>Gets the total size in uints of the arena allocation.</summary>
    public int TotalSize => _tags.Length;

    /// <summary>
    /// Creates a new <see cref="InstList"/> from raw arrays.
    /// Used primarily by builders.
    /// </summary>
    public InstList(
        byte[] tags,
        ushort[] flags,
        ushort[] argCounts,
        uint[] fixedOps,
        uint[] extra,
        uint[] extraOffsets,
        int[] blockIds,
        int count,
        uint extraUsed)
    {
        _tags = tags;
        _flags = flags;
        _argCounts = argCounts;
        _fixedOps = fixedOps;
        _extra = extra;
        _extraOffsets = extraOffsets;
        _blockIds = blockIds;
        _count = count;
        _extraUsed = extraUsed;
    }

    /// <summary>Gets the instruction kind at the specified index.</summary>
    public byte GetKind(int instIndex)
    {
        CheckIndex(instIndex);
        return _tags[instIndex];
    }

    /// <summary>Gets the instruction flags at the specified index.</summary>
    public ushort GetFlags(int instIndex)
    {
        CheckIndex(instIndex);
        return _flags[instIndex];
    }

    /// <summary>Gets the operand count at the specified index.</summary>
    public ushort GetArgCount(int instIndex)
    {
        CheckIndex(instIndex);
        return _argCounts[instIndex];
    }

    /// <summary>Gets the CFG block ID at the specified index (0 = unassigned).</summary>
    public int GetBlockId(int instIndex)
    {
        CheckIndex(instIndex);
        return _blockIds[instIndex];
    }

    /// <summary>Sets the CFG block ID at the specified index. Accepts a BlockId handle value
    /// (0 = unassigned via BlockId.Unassigned, 1+ = assigned).</summary>
    public void SetBlockId(int instIndex, int blockId)
    {
        CheckIndex(instIndex);
        _blockIds[instIndex] = blockId;
    }

    /// <summary>
    /// Gets the operand span for the instruction at <paramref name="instIndex"/>.
    /// Fast path (argCount &lt;= MAX_FIXED_OPS): span over the fixed operand slots.
    /// Slow path (argCount &gt; MAX_FIXED_OPS): span over the contiguous extra pool region.
    /// </summary>
    public ReadOnlySpan<uint> GetOperands(int instIndex)
    {
        CheckIndex(instIndex);

        int argCount = _argCounts[instIndex];
        int fixedOpIndex = instIndex * InstConstants.MAX_FIXED_OPS;

        if (argCount <= InstConstants.MAX_FIXED_OPS)
        {
            return new ReadOnlySpan<uint>(_fixedOps, fixedOpIndex, argCount);
        }

        // Slow path: all operands are stored contiguously in the extra pool.
        return new ReadOnlySpan<uint>(_extra, (int)_extraOffsets[instIndex], argCount);
    }

    /// <summary>
    /// Returns the absolute index into <c>fixedOps</c> (fast path) or <c>extra</c> (slow path)
    /// for the given operand slot of an instruction. Used at codegen time for address resolution.
    /// </summary>
    public int GetOperandOffset(int instIndex, int operandIndex)
    {
        CheckIndex(instIndex);

        int argCount = _argCounts[instIndex];
        if (operandIndex < 0 || operandIndex >= argCount)
            throw new ArgumentOutOfRangeException(nameof(operandIndex));

        if (argCount <= InstConstants.MAX_FIXED_OPS)
        {
            return instIndex * InstConstants.MAX_FIXED_OPS + operandIndex;
        }

        return (int)_extraOffsets[instIndex] + operandIndex;
    }

    /// <summary>
    /// Reads a single operand value for an instruction, transparently handling the
    /// fast path (fixedOps) and slow path (extra pool).
    /// </summary>
    public uint GetOperand(int instIndex, int operandIndex)
    {
        int argCount = GetArgCount(instIndex);
        if (operandIndex < 0 || operandIndex >= argCount)
            throw new ArgumentOutOfRangeException(nameof(operandIndex));

        if (argCount <= InstConstants.MAX_FIXED_OPS)
        {
            return _fixedOps[instIndex * InstConstants.MAX_FIXED_OPS + operandIndex];
        }

        return _extra[(int)_extraOffsets[instIndex] + operandIndex];
    }

    /// <summary>
    /// Compacts the extra operand pool to reduce fragmentation.
    /// Returns a new <see cref="InstList"/> with a defragmented extra pool.
    /// </summary>
    public InstList CompactExtra()
    {
        if (_extraUsed == 0)
            return this;

        var compacted = new uint[_extraUsed];
        var newOffsets = new uint[_count];

        uint cursor = 0;
        for (int i = 0; i < _count; i++)
        {
            if (_argCounts[i] <= InstConstants.MAX_FIXED_OPS)
            {
                newOffsets[i] = 0;
                continue;
            }

            uint srcOffset = _extraOffsets[i];
            uint length = (uint)_argCounts[i];

            Array.Copy(_extra, (int)srcOffset, compacted, (int)cursor, (int)length);
            newOffsets[i] = cursor;
            cursor += length;
        }

        return new InstList(
            _tags,
            _flags,
            _argCounts,
            _fixedOps,
            compacted,
            newOffsets,
            _blockIds,
            _count,
            cursor);
    }

    /// <summary>
    /// Returns a new <see cref="InstList"/> with updated block IDs.
    /// Used by CFG construction to assign block IDs after building the slab.
    /// </summary>
    public InstList WithBlockIds(ReadOnlySpan<int> blockIds)
    {
        if (blockIds.Length != _count)
            throw new ArgumentException("Block IDs length must match instruction count", nameof(blockIds));

        var newBlockIds = new int[_count];
        blockIds.CopyTo(newBlockIds);

        return new InstList(
            _tags,
            _flags,
            _argCounts,
            _fixedOps,
            _extra,
            _extraOffsets,
            newBlockIds,
            _count,
            _extraUsed);
    }

    /// <summary>
    /// Exposes the raw tags array for stride-only iteration.
    /// </summary>
    public ReadOnlySpan<byte> Tags => _tags.AsSpan(0, _count);

    /// <summary>
    /// Exposes the raw flags array for stride-only iteration.
    /// </summary>
    public ReadOnlySpan<ushort> Flags => _flags.AsSpan(0, _count);

    /// <summary>
    /// Exposes the raw arg counts array for stride-only iteration.
    /// </summary>
    public ReadOnlySpan<ushort> ArgCounts => _argCounts.AsSpan(0, _count);

    /// <summary>
    /// Exposes the raw block IDs array for stride-only iteration.
    /// </summary>
    public ReadOnlySpan<int> BlockIds => _blockIds.AsSpan(0, _count);

    /// <summary>
    /// Exposes the fixed operand array as a flat span
    /// (length = MAX_FIXED_OPS * count).
    /// </summary>
    public ReadOnlySpan<uint> FixedOps => _fixedOps.AsSpan(0, _count * InstConstants.MAX_FIXED_OPS);

    /// <summary>
    /// Exposes the extra operand pool as a flat span (length = ExtraUsed).
    /// </summary>
    public ReadOnlySpan<uint> Extra => _extra.AsSpan(0, (int)_extraUsed);

    private void CheckIndex(int instIndex)
    {
        if (instIndex < 0 || instIndex >= _count)
            throw new ArgumentOutOfRangeException(nameof(instIndex), $"Instruction index {instIndex} out of range 0-{_count - 1}");
    }
}

/// <summary>
/// Metadata for a single instruction in an <see cref="InstList"/>.
/// Provides convenient access to instruction properties.
/// </summary>
public readonly struct InstMetadata
{
    private readonly byte _kind;
    private readonly ushort _flags;
    private readonly ushort _argCount;
    private readonly int _blockId;
    private readonly int _index;

    internal InstMetadata(byte kind, ushort flags, ushort argCount, int blockId, int index)
    {
        _kind = kind;
        _flags = flags;
        _argCount = argCount;
        _blockId = blockId;
        _index = index;
    }

    public byte Kind => _kind;
    public ushort Flags => _flags;
    public ushort ArgCount => _argCount;
    public int BlockId => _blockId;

    /// <summary>The position of this instruction within its parent <see cref="InstList"/>.</summary>
    public InstIndex Index => InstIndex.FromInt(_index);

    public bool IsTerminator => (_flags & (ushort)InstructionFlag.Terminator) != 0;
    public bool IsDiagnostic => (_flags & (ushort)InstructionFlag.Diagnostic) != 0;

    /// <summary>
    /// Gets the operand span for this instruction from the parent InstList.
    /// </summary>
    public ReadOnlySpan<uint> GetOperands(InstList list) => list.GetOperands(_index);
}

/// <summary>Shared constants for the SoA instruction list design.</summary>
public static class InstConstants
{
    /// <summary>
    /// Maximum number of operands stored in the fixed array per instruction.
    /// Operand counts above this overflow into the extra pool.
    /// </summary>
    public const int MAX_FIXED_OPS = 4;
}