using System;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Standardized 6-index slab header prefix. Owns the global metadata and versioning
    /// fields that every DOD instruction slab reserves at its start:
    /// [0] magic, [1] major version, [2] minor version, [3] IR stage, [4] element count, [5] symbol table offset.
    /// Value type so it composes with serialization-ready DOD structures.
    /// </summary>
    public readonly struct SlabHeader
    {
        /// <summary>Well-known file signature ("GAVM" as big-endian ASCII).</summary>
        public const uint Magic = 0x4741564D;

        /// <summary>Current major version. Bump on breaking IR layout or bit-packing changes.</summary>
        public const uint CurrentMajorVersion = 1;

        /// <summary>Current minor version. Bump on non-breaking additions (new instruction kinds).</summary>
        public const uint CurrentMinorVersion = 0;

        /// <summary>Index of each header field within the slab prefix.</summary>
        public static class HeaderIndex
        {
            public const int MagicIndex = 0;
            public const int MajorIndex = 1;
            public const int MinorIndex = 2;
            public const int IrStageIndex = 3;
            public const int ElementCountIndex = 4;
            public const int SymbolTableOffsetIndex = 5;
            public const int Length = 6;
        }

        /// <summary>The six header values, in prefix order.</summary>
        private readonly uint[] _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlabHeader"/> struct from raw prefix values.
        /// </summary>
        public SlabHeader(uint magic, uint majorVersion, uint minorVersion, uint irStage, uint elementCount, uint symbolTableOffset)
        {
            _values = new uint[HeaderIndex.Length];
            _values[HeaderIndex.MagicIndex] = magic;
            _values[HeaderIndex.MajorIndex] = majorVersion;
            _values[HeaderIndex.MinorIndex] = minorVersion;
            _values[HeaderIndex.IrStageIndex] = irStage;
            _values[HeaderIndex.ElementCountIndex] = elementCount;
            _values[HeaderIndex.SymbolTableOffsetIndex] = symbolTableOffset;
        }

        /// <summary>Magic number / file signature.</summary>
        public uint MagicNumber => _values[HeaderIndex.MagicIndex];

        /// <summary>Major version (breaking IR layout or bit-packing changes).</summary>
        public uint Major => _values[HeaderIndex.MajorIndex];

        /// <summary>Minor version (non-breaking additions).</summary>
        public uint Minor => _values[HeaderIndex.MinorIndex];

        /// <summary>IR stage identifier (0=AST, 1=HLIR, 2=MLIR, 3=LLIR).</summary>
        public uint IrStage => _values[HeaderIndex.IrStageIndex];

        /// <summary>Total active elements in the slab (excludes the 6 header indices).</summary>
        public uint ElementCount => _values[HeaderIndex.ElementCountIndex];

        /// <summary>Offset to the symbol table section, or 0 if none.</summary>
        public uint SymbolTableOffset => _values[HeaderIndex.SymbolTableOffsetIndex];

        /// <summary>Reads a header from the first six indices of a slab.</summary>
        public static SlabHeader Read(uint[] slab)
        {
            if (slab == null)
                throw new ArgumentNullException(nameof(slab));
            if (slab.Length < HeaderIndex.Length)
                throw new ArgumentException($"Slab must contain at least {HeaderIndex.Length} indices for the header", nameof(slab));

            return new SlabHeader(
                slab[HeaderIndex.MagicIndex],
                slab[HeaderIndex.MajorIndex],
                slab[HeaderIndex.MinorIndex],
                slab[HeaderIndex.IrStageIndex],
                slab[HeaderIndex.ElementCountIndex],
                slab[HeaderIndex.SymbolTableOffsetIndex]);
        }

        /// <summary>Writes the six header indices into the start of a slab.</summary>
        public void WriteTo(uint[] slab)
        {
            if (slab == null)
                throw new ArgumentNullException(nameof(slab));
            if (slab.Length < HeaderIndex.Length)
                throw new ArgumentException($"Slab must contain at least {HeaderIndex.Length} indices for the header", nameof(slab));

            slab[HeaderIndex.MagicIndex] = _values[HeaderIndex.MagicIndex];
            slab[HeaderIndex.MajorIndex] = _values[HeaderIndex.MajorIndex];
            slab[HeaderIndex.MinorIndex] = _values[HeaderIndex.MinorIndex];
            slab[HeaderIndex.IrStageIndex] = _values[HeaderIndex.IrStageIndex];
            slab[HeaderIndex.ElementCountIndex] = _values[HeaderIndex.ElementCountIndex];
            slab[HeaderIndex.SymbolTableOffsetIndex] = _values[HeaderIndex.SymbolTableOffsetIndex];
        }

        /// <summary>Returns true if the magic number matches the expected signature.</summary>
        public bool HasValidMagic() => MagicNumber == Magic;

        /// <summary>
        /// Validates structural invariants: correct magic and a non-increasing version
        /// contract is not enforced here (consumers may target older minor versions), but
        /// the magic must match and element count must be within slab bounds.
        /// </summary>
        public void Validate(uint slabLength)
        {
            if (!HasValidMagic())
                throw new InvalidOperationException($"Invalid slab magic 0x{MagicNumber:X8}; expected 0x{Magic:X8}");
            if (HeaderIndex.Length + ElementCount > slabLength)
                throw new InvalidOperationException(
                    $"Element count {ElementCount} exceeds slab length {slabLength} (header is {HeaderIndex.Length})");
        }

        /// <summary>Creates a header for a freshly built slab of the given IR stage.</summary>
        public static SlabHeader ForStage(uint irStage, uint elementCount, uint symbolTableOffset = 0)
        {
            return new SlabHeader(Magic, CurrentMajorVersion, CurrentMinorVersion, irStage, elementCount, symbolTableOffset);
        }
    }
}
