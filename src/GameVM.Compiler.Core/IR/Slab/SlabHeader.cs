using System;

namespace GameVM.Compiler.Core.IR.Slab
{
    /// <summary>
    /// Standardized 6-index slab header prefix. Owns the global metadata and versioning
    /// fields that every DOD instruction slab reserves at its start:
    /// [0] magic, [1] major version, [2] minor version, [3] IR stage, [4] element count, [5] symbol table offset.
    /// Value type so it composes with serialization-ready DOD structures.
    /// </summary>
    public readonly struct SlabHeader
    {
        private readonly uint[] _values;

        public struct HeaderIndex
        {
            public const int MagicIndex = 0;
            public const int MajorIndex = 1;
            public const int MinorIndex = 2;
            public const int IrStageIndex = 3;
            public const int ElementCountIndex = 4;
            public const int SymbolTableOffsetIndex = 5;
            public const int Length = 6;
        }

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

        public uint MagicNumber => _values[HeaderIndex.MagicIndex];
        public uint Major => _values[HeaderIndex.MajorIndex];
        public uint Minor => _values[HeaderIndex.MinorIndex];
        public uint IrStage => _values[HeaderIndex.IrStageIndex];
        public uint ElementCount => _values[HeaderIndex.ElementCountIndex];
        public uint SymbolTableOffset => _values[HeaderIndex.SymbolTableOffsetIndex];

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

        public bool HasValidMagic() => MagicNumber == Magic;

        public void Validate(uint slabLength)
        {
            if (!HasValidMagic())
                throw new InvalidOperationException($"Invalid slab magic 0x{MagicNumber:X8}; expected 0x{Magic:X8}");
            if (HeaderIndex.Length + ElementCount > slabLength)
                throw new InvalidOperationException(
                    $"Element count {ElementCount} exceeds slab length {slabLength} (header is {HeaderIndex.Length})");
        }

        public static SlabHeader ForStage(uint irStage, uint elementCount, uint symbolTableOffset = 0)
        {
            return new SlabHeader(Magic, CurrentMajorVersion, CurrentMinorVersion, irStage, elementCount, symbolTableOffset);
        }

        public const uint Magic = 0x4741564D;
        public const uint CurrentMajorVersion = 1;
        public const uint CurrentMinorVersion = 0;
    }
}