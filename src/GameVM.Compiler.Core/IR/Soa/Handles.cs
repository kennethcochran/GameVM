using System;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Typed handle for an instruction index in an <see cref="InstList"/>.
/// Value 0 = valid first instruction; -1 = invalid.
/// </summary>
public readonly struct InstIndex : IEquatable<InstIndex>, IComparable<InstIndex>
{
    private readonly int _value;

    private InstIndex(int value)
    {
        _value = value;
    }

    /// <summary>Invalid instruction index sentinel.</summary>
    public static InstIndex Invalid => new InstIndex(-1);

    /// <summary>Creates an instruction index from a raw integer value.</summary>
    public static InstIndex FromInt(int value)
    {
        if (value < -1)
            throw new ArgumentOutOfRangeException(nameof(value), "InstIndex must be >= -1");
        return new InstIndex(value);
    }

    /// <summary>The raw integer value (0 = first instruction, -1 = invalid).</summary>
    public int Value => _value;

    /// <summary>True if this is a valid instruction index (not Invalid).</summary>
    public bool IsValid => _value >= 0;

    /// <summary>Implicit conversion to int for array indexing.</summary>
    public static implicit operator int(InstIndex index) => index._value;

    /// <summary>Explicit conversion from int to InstIndex.</summary>
    public static explicit operator InstIndex(int value) => FromInt(value);

    public bool Equals(InstIndex other) => _value == other._value;
    public override bool Equals(object? obj) => obj is InstIndex other && Equals(other);
    public override int GetHashCode() => _value;
    public int CompareTo(InstIndex other) => _value.CompareTo(other._value);

    public static bool operator ==(InstIndex left, InstIndex right) => left._value == right._value;
    public static bool operator !=(InstIndex left, InstIndex right) => left._value != right._value;
    public static bool operator <(InstIndex left, InstIndex right) => left._value < right._value;
    public static bool operator >(InstIndex left, InstIndex right) => left._value > right._value;
    public static bool operator <=(InstIndex left, InstIndex right) => left._value <= right._value;
    public static bool operator >=(InstIndex left, InstIndex right) => left._value >= right._value;

    public override string ToString() => IsValid ? $"InstIndex({_value})" : "InstIndex.Invalid";
}

/// <summary>
/// Typed handle for a basic block ID in a CFG.
/// Value 0 = unassigned; -1 = invalid.
/// </summary>
public readonly struct BlockId : IEquatable<BlockId>, IComparable<BlockId>
{
    private readonly int _value;

    private BlockId(int value)
    {
        _value = value;
    }

    /// <summary>Unassigned block ID sentinel.</summary>
    public static BlockId Unassigned => new BlockId(0);

    /// <summary>Invalid block ID sentinel.</summary>
    public static BlockId Invalid => new BlockId(-1);

    /// <summary>Creates a block ID from a raw integer value.</summary>
    public static BlockId FromInt(int value)
    {
        if (value < -1)
            throw new ArgumentOutOfRangeException(nameof(value), "BlockId must be >= -1");
        return new BlockId(value);
    }

    /// <summary>The raw integer value (0 = unassigned, -1 = invalid).</summary>
    public int Value => _value;

    /// <summary>True if this block ID is assigned (not Unassigned or Invalid).</summary>
    public bool IsAssigned => _value > 0;

    /// <summary>True if this is a valid block ID (not Invalid).</summary>
    public bool IsValid => _value >= 0;

    /// <summary>Implicit conversion to int for array indexing.</summary>
    public static implicit operator int(BlockId id) => id._value;

    /// <summary>Explicit conversion from int to BlockId.</summary>
    public static explicit operator BlockId(int value) => FromInt(value);

    public bool Equals(BlockId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is BlockId other && Equals(other);
    public override int GetHashCode() => _value;
    public int CompareTo(BlockId other) => _value.CompareTo(other._value);

    public static bool operator ==(BlockId left, BlockId right) => left._value == right._value;
    public static bool operator !=(BlockId left, BlockId right) => left._value != right._value;
    public static bool operator <(BlockId left, BlockId right) => left._value < right._value;
    public static bool operator >(BlockId left, BlockId right) => left._value > right._value;
    public static bool operator <=(BlockId left, BlockId right) => left._value <= right._value;
    public static bool operator >=(BlockId left, BlockId right) => left._value >= right._value;

    public override string ToString() => _value switch
    {
        -1 => "BlockId.Invalid",
        0 => "BlockId.Unassigned",
        _ => $"BlockId({_value})"
    };
}

/// <summary>
/// Typed handle for a symbol ID in a symbol table.
/// Value -1 = invalid.
/// </summary>
public readonly struct SymbolId : IEquatable<SymbolId>, IComparable<SymbolId>
{
    private readonly int _value;

    private SymbolId(int value)
    {
        _value = value;
    }

    /// <summary>Invalid symbol ID sentinel.</summary>
    public static SymbolId Invalid => new SymbolId(-1);

    /// <summary>Creates a symbol ID from a raw integer value.</summary>
    public static SymbolId FromInt(int value)
    {
        if (value < -1)
            throw new ArgumentOutOfRangeException(nameof(value), "SymbolId must be >= -1");
        return new SymbolId(value);
    }

    /// <summary>The raw integer value (-1 = invalid).</summary>
    public int Value => _value;

    /// <summary>True if this is a valid symbol ID (not Invalid).</summary>
    public bool IsValid => _value >= 0;

    /// <summary>Implicit conversion to int for array indexing.</summary>
    public static implicit operator int(SymbolId id) => id._value;

    /// <summary>Explicit conversion from int to SymbolId.</summary>
    public static explicit operator SymbolId(int value) => FromInt(value);

    public bool Equals(SymbolId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is SymbolId other && Equals(other);
    public override int GetHashCode() => _value;
    public int CompareTo(SymbolId other) => _value.CompareTo(other._value);

    public static bool operator ==(SymbolId left, SymbolId right) => left._value == right._value;
    public static bool operator !=(SymbolId left, SymbolId right) => left._value != right._value;
    public static bool operator <(SymbolId left, SymbolId right) => left._value < right._value;
    public static bool operator >(SymbolId left, SymbolId right) => left._value > right._value;
    public static bool operator <=(SymbolId left, SymbolId right) => left._value <= right._value;
    public static bool operator >=(SymbolId left, SymbolId right) => left._value >= right._value;

    public override string ToString() => IsValid ? $"SymbolId({_value})" : "SymbolId.Invalid";
}

/// <summary>
/// Typed handle for a local slot index (abstract stack/local-variable array position).
/// Value 0 = valid first slot; -1 = invalid.
/// </summary>
public readonly struct SlotIndex : IEquatable<SlotIndex>, IComparable<SlotIndex>
{
    private readonly int _value;

    private SlotIndex(int value)
    {
        _value = value;
    }

    /// <summary>Invalid slot index sentinel.</summary>
    public static SlotIndex Invalid => new SlotIndex(-1);

    /// <summary>Creates a slot index from a raw integer value.</summary>
    public static SlotIndex FromInt(int value)
    {
        if (value < -1)
            throw new ArgumentOutOfRangeException(nameof(value), "SlotIndex must be >= -1");
        return new SlotIndex(value);
    }

    /// <summary>The raw integer value (0 = first slot, -1 = invalid).</summary>
    public int Value => _value;

    /// <summary>True if this is a valid slot index (not Invalid).</summary>
    public bool IsValid => _value >= 0;

    /// <summary>Implicit conversion to int for array indexing.</summary>
    public static implicit operator int(SlotIndex index) => index._value;

    /// <summary>Explicit conversion from int to SlotIndex.</summary>
    public static explicit operator SlotIndex(int value) => FromInt(value);

    public bool Equals(SlotIndex other) => _value == other._value;
    public override bool Equals(object? obj) => obj is SlotIndex other && Equals(other);
    public override int GetHashCode() => _value;
    public int CompareTo(SlotIndex other) => _value.CompareTo(other._value);

    public static bool operator ==(SlotIndex left, SlotIndex right) => left._value == right._value;
    public static bool operator !=(SlotIndex left, SlotIndex right) => left._value != right._value;
    public static bool operator <(SlotIndex left, SlotIndex right) => left._value < right._value;
    public static bool operator >(SlotIndex left, SlotIndex right) => left._value > right._value;
    public static bool operator <=(SlotIndex left, SlotIndex right) => left._value <= right._value;
    public static bool operator >=(SlotIndex left, SlotIndex right) => left._value >= right._value;

    public override string ToString() => IsValid ? $"SlotIndex({_value})" : "SlotIndex.Invalid";
}
