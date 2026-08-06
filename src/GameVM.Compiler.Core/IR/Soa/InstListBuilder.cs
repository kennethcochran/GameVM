using System;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Builder for incrementally constructing an <see cref="InstList"/>.
/// Automatically resizes internal arrays as needed.
/// </summary>
public sealed class InstListBuilder
{
    private const int InitialCapacity = 16;
    private const int InitialExtraCapacity = 1024;

    private byte[] _tags;
    private ushort[] _flags;
    private ushort[] _argCounts;
    private uint[] _fixedOps;
    private uint[] _extra;
    private uint[] _extraOffsets;
    private int[] _blockIds;

    private int _count;
    private uint _extraUsed;

    public InstListBuilder() : this(InitialCapacity)
    {
    }

    public InstListBuilder(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _tags = new byte[capacity];
        _flags = new ushort[capacity];
        _argCounts = new ushort[capacity];
        _fixedOps = new uint[capacity * InstConstants.MAX_FIXED_OPS];
        _extra = new uint[Math.Max(InitialExtraCapacity, capacity)];
        _extraOffsets = new uint[capacity];
        _blockIds = new int[capacity];
        _count = 0;
        _extraUsed = 0;
    }

    /// <summary>Current number of instructions in the builder.</summary>
    public int Count => _count;

    /// <summary>Current usage of the extra pool in uints.</summary>
    public uint ExtraUsed => _extraUsed;

    /// <summary>Total capacity of the instruction arrays.</summary>
    public int Capacity => _tags.Length;

    /// <summary>Clears all instructions, resetting the builder to empty.</summary>
    public void Clear()
    {
        _count = 0;
        _extraUsed = 0;
    }

    /// <summary>
    /// Appends an instruction with the specified properties.
    /// Returns the index of the newly added instruction.
    /// </summary>
    public int Append(byte kind, InstructionFlag flags, ushort argCount, int blockId, ReadOnlySpan<uint> operands)
    {
        if (operands.Length != argCount)
            throw new ArgumentException($"Operand count ({operands.Length}) does not match argCount ({argCount})");

        EnsureCapacity(_count + 1);

        int index = _count;
        _tags[index] = kind;
        _flags[index] = (ushort)flags;
        _argCounts[index] = argCount;
        _blockIds[index] = blockId;

        if (argCount <= InstConstants.MAX_FIXED_OPS)
        {
            int fixedOpOffset = index * InstConstants.MAX_FIXED_OPS;
            for (int i = 0; i < argCount; i++)
                _fixedOps[fixedOpOffset + i] = operands[i];

            _extraOffsets[index] = 0;
        }
        else
        {
            // Slow path: store ALL operands contiguously in extra pool, and cache first MAX_FIXED_OPS in fixed slots for fast access
            int fixedOpOffset = index * InstConstants.MAX_FIXED_OPS;
            for (int i = 0; i < Math.Min((int)argCount, InstConstants.MAX_FIXED_OPS); i++)
                _fixedOps[fixedOpOffset + i] = operands[i];

            uint extraOffset = _extraUsed;
            EnsureExtraCapacity(_extraUsed + (uint)argCount);
            for (int i = 0; i < argCount; i++)
                _extra[_extraUsed++] = operands[i];

            _extraOffsets[index] = extraOffset;
        }

        _count++;
        return index;
    }

    /// <summary>Adds an instruction with no operands.</summary>
    public int Add(byte kind, InstructionFlag flags = InstructionFlag.None, int blockId = 0)
    {
        return Append(kind, flags, 0, blockId, ReadOnlySpan<uint>.Empty);
    }

    /// <summary>Adds an instruction with one operand.</summary>
    public int Add(byte kind, InstructionFlag flags, int blockId, uint operand0)
    {
        Span<uint> ops = stackalloc uint[1];
        ops[0] = operand0;
        return Append(kind, flags, 1, blockId, ops);
    }

    /// <summary>Adds an instruction with two operands.</summary>
    public int Add(byte kind, InstructionFlag flags, int blockId, uint operand0, uint operand1)
    {
        Span<uint> ops = stackalloc uint[2];
        ops[0] = operand0;
        ops[1] = operand1;
        return Append(kind, flags, 2, blockId, ops);
    }

    /// <summary>Adds an instruction with three operands.</summary>
    public int Add(byte kind, InstructionFlag flags, int blockId, uint operand0, uint operand1, uint operand2)
    {
        Span<uint> ops = stackalloc uint[3];
        ops[0] = operand0;
        ops[1] = operand1;
        ops[2] = operand2;
        return Append(kind, flags, 3, blockId, ops);
    }

    /// <summary>Adds an instruction with four operands.</summary>
    public int Add(byte kind, InstructionFlag flags, int blockId, uint operand0, uint operand1, uint operand2, uint operand3)
    {
        Span<uint> ops = stackalloc uint[4];
        ops[0] = operand0;
        ops[1] = operand1;
        ops[2] = operand2;
        ops[3] = operand3;
        return Append(kind, flags, 4, blockId, ops);
    }

    /// <summary>
    /// Adds an instruction with an arbitrary number of operands (uses extra pool when
    /// the operand count exceeds <see cref="InstConstants.MAX_FIXED_OPS"/>).
    /// </summary>
    public int Add(byte kind, InstructionFlag flags, int blockId, ReadOnlySpan<uint> operands)
    {
        if (operands.Length > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(operands), "Operand count exceeds ushort range.");
        return Append(kind, flags, (ushort)operands.Length, blockId, operands);
    }

/// <summary>
        /// Builds an immutable <see cref="InstList"/> from the current builder state.
        /// </summary>
        public InstList Build()
        {
            var trimmedTags = new byte[_count];
            var trimmedFlags = new ushort[_count];
            var trimmedArgCounts = new ushort[_count];
            var trimmedFixedOps = new uint[_count * InstConstants.MAX_FIXED_OPS];
            var trimmedExtra = new uint[_extraUsed];
            var trimmedExtraOffsets = new uint[_count];
            var trimmedBlockIds = new int[_count];

            Array.Copy(_tags, 0, trimmedTags, 0, _count);
            Array.Copy(_flags, 0, trimmedFlags, 0, _count);
            Array.Copy(_argCounts, 0, trimmedArgCounts, 0, _count);
            Array.Copy(_fixedOps, 0, trimmedFixedOps, 0, _count * InstConstants.MAX_FIXED_OPS);
            Array.Copy(_extra, 0, trimmedExtra, 0, (int)_extraUsed);
            Array.Copy(_extraOffsets, 0, trimmedExtraOffsets, 0, _count);
            Array.Copy(_blockIds, 0, trimmedBlockIds, 0, _count);

            return new InstList(
                trimmedTags,
                trimmedFlags,
                trimmedArgCounts,
                trimmedFixedOps,
                trimmedExtra,
                trimmedExtraOffsets,
                trimmedBlockIds,
                _count,
                _extraUsed);
        }

    /// <summary>Ensures the instruction arrays have capacity for at least <paramref name="minCount"/>.</summary>
    private void EnsureCapacity(int minCount)
    {
        if (_tags.Length >= minCount)
            return;

        int newCapacity = Math.Max(_tags.Length * 2, minCount);
        ResizeArrays(newCapacity);
    }

    /// <summary>Ensures the extra pool has capacity for at least <paramref name="minExtraUsed"/> uints.</summary>
    private void EnsureExtraCapacity(uint minExtraUsed)
    {
        if (_extra.Length >= minExtraUsed)
            return;

        int newExtraCapacity = Math.Max(_extra.Length * 2, (int)minExtraUsed);
        var newExtra = new uint[newExtraCapacity];
        Array.Copy(_extra, 0, newExtra, 0, (int)_extraUsed);
        _extra = newExtra;
    }

    /// <summary>Resizes all per-instruction arrays to the specified capacity.</summary>
    private void ResizeArrays(int newCapacity)
    {
var newTags = new byte[newCapacity];
        var newFlags = new ushort[newCapacity];
        var newArgCounts = new ushort[newCapacity];
        var newFixedOps = new uint[newCapacity * InstConstants.MAX_FIXED_OPS];
        var newExtraOffsets = new uint[newCapacity];
        var newBlockIds = new int[newCapacity];

        Array.Copy(_tags, 0, newTags, 0, _count);
        Array.Copy(_flags, 0, newFlags, 0, _count);
        Array.Copy(_argCounts, 0, newArgCounts, 0, _count);
        Array.Copy(_fixedOps, 0, newFixedOps, 0, _count * InstConstants.MAX_FIXED_OPS);
        Array.Copy(_extraOffsets, 0, newExtraOffsets, 0, _count);
        Array.Copy(_blockIds, 0, newBlockIds, 0, _count);

        _tags = newTags;
        _flags = newFlags;
        _argCounts = newArgCounts;
        _fixedOps = newFixedOps;
        _extraOffsets = newExtraOffsets;
        _blockIds = newBlockIds;
    }
}