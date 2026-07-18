namespace GameVM.Compiler.Core.IR.Slab
{
    /// <summary>
    /// Symbolic reference to a local slot (abstract stack / local-variable array position)
    /// used for cross-block operand dependencies. Per the DOD design, instructions must not
    /// reference operands in other basic blocks via raw slab offsets; instead they carry a
    /// local slot index resolved against the block's local frame. Formal SSA is deferred.
    /// </summary>
    public readonly struct LocalSlotIndex
    {
        /// <summary>Bit width available for the slot number field.</summary>
        private const uint SlotMask = 0x00FFFFFF;

        /// <summary>Bit position of the "is parameter" flag (bit 24).</summary>
        private const int ParamShift = 24;

        /// <summary>Bit position of the "is spilled" flag (bit 25).</summary>
        private const int SpilledShift = 25;

        private readonly uint _packed;

        private LocalSlotIndex(uint packed)
        {
            _packed = packed;
        }

        /// <summary>Zero slot (invalid / uninitialized sentinel).</summary>
        public static LocalSlotIndex None => new LocalSlotIndex(0);

        /// <summary>Creates a local slot reference for the given slot number.</summary>
        public static LocalSlotIndex ForSlot(uint slotNumber)
        {
            if (slotNumber > SlotMask)
                throw new System.ArgumentOutOfRangeException(nameof(slotNumber), $"Slot must be 0-{SlotMask}");
            return new LocalSlotIndex(slotNumber);
        }

        /// <summary>Creates a parameter slot reference (caller-provided argument).</summary>
        public static LocalSlotIndex ForParameter(uint slotNumber)
        {
            if (slotNumber > SlotMask)
                throw new System.ArgumentOutOfRangeException(nameof(slotNumber), $"Slot must be 0-{SlotMask}");
            return new LocalSlotIndex(slotNumber | (1u << ParamShift));
        }

        /// <summary>The raw slot number (ignores param/spill flags).</summary>
        public uint SlotNumber => _packed & SlotMask;

        /// <summary>True if this slot refers to a caller parameter rather than a local.</summary>
        public bool IsParameter => (_packed & (1u << ParamShift)) != 0;

        /// <summary>True if this slot has been spilled to the backing store.</summary>
        public bool IsSpilled => (_packed & (1u << SpilledShift)) != 0;

        /// <summary>Returns a copy of this slot marked as spilled.</summary>
        public LocalSlotIndex AsSpilled()
        {
            return new LocalSlotIndex(_packed | (1u << SpilledShift));
        }

        /// <summary>The 32-bit packed encoding for storage in a uint[] slab operand.</summary>
        public uint Encode() => _packed;

        /// <summary>Decodes a local slot reference from its 32-bit packed form.</summary>
        public static LocalSlotIndex Decode(uint packed) => new LocalSlotIndex(packed);

        /// <summary>True if this is the uninitialized sentinel.</summary>
        public bool IsNone => _packed == 0;
    }
}
