## MODIFIED Requirements

### Requirement: DOD Memory Arenas
**Modified:** The arena allocator continues to provide raw contiguous memory for `InstList` parallel arrays, but the concept of a "unified slab" changes from a flat `uint[]` of self-describing blocks to a structured `InstList` view.

#### Scenario: Allocating a new node
- **WHEN** a transformer needs memory for a new `InstList`
- **THEN** it calls `ArenaAllocator.AllocateArray<byte>(count)` for each parallel array, and constructs the `InstList` view over the allocated buffers.

### Requirement: Single Unified Slab
**Modified:** The "slab" is now an `InstList` struct (parallel field arrays + `extra` pool) instead of a flat `uint[]` of self-describing blocks. The header prefix (`SlabHeader`) is retained for file format; the body is the `InstList` view.

#### Scenario: Traversing instruction blocks
- **WHEN** an optimization pass processes instructions
- **THEN** it iterates `for (int i = 0; i < instList.Count; i++) { var kind = instList.Tags[i]; var flags = instList.Flags[i]; ... }` — stride-only linear iteration on homogeneous arrays.

### Requirement: Self-Describing Instruction Blocks
**Modified:** "Self-describing" is achieved by parallel arrays instead of a bit-packed metadata header per block. Each instruction's metadata is spread across homogeneous field arrays at the same index.

#### Scenario: Parsing an instruction
- **WHEN** a pass reads an instruction at index `i`
- **THEN** it reads `Tags[i]`, `Flags[i]`, `ArgCounts[i]`, `FixedOps[i * MAX_FIXED_OPS + ...]` — no decode function required.

### Requirement: Formalized Metadata Encoding
**Modified:** The 32-bit bit-packed metadata header is REMOVED. Its fields become individual SoA arrays:
- `tags[]` (u8) — replaces bits 0-7 (Instruction Kind)
- `flags[]` (u16) — replaces bits 8-13 (Size), 14-19 (ArgCount), 20 (Terminator), 21 (Diagnostic)
- `argCounts[]` (u16) — replaces bits 14-19 (ArgCount)
- Bits 22-31 (Reserved) moved to `flags[]` reserved bits

#### Scenario: Encoding instruction metadata
- **WHEN** creating a new instruction
- **THEN** the system writes to `Tags[idx]`, `Flags[idx]`, `ArgCounts[idx]` directly — no `Encode` function.

### Requirement: CFG Parallel Arrays
**Modified:** CFG tables remain parallel arrays (`blockOffsets[]`, `cfgEdges[]`, `edgeStart[]`, `edgeCount[]`) but are now keyed by `BlockId` (int handle) instead of raw slab offset indices. `InstList.blockIds[]` provides the instruction→block mapping.

#### Scenario: Building the CFG
- **WHEN** the CFG construction pass identifies basic blocks
- **THEN** it assigns sequential `BlockId` handles, populates `InstList.blockIds[]` per instruction, and fills `CfgTable` arrays indexed by `BlockId`.

### Requirement: Separate CFG Construction Pass
**Modified:** Unchanged — CFG construction still happens in a dedicated pass after instruction emission, assigning `BlockId` handles instead of raw indices.

### Requirement: Five-Stage Pipeline Architecture
**Modified:** Pipeline stages remain (AST → HLIR → MLIR → LLIR → bytecode) but each stage's body is now an `InstList` SoA view instead of a `uint[]` slab. Stage transitions (transformers) produce/consume `InstList`.

#### Scenario: Parsing to AST slab
- **WHEN** the ANTLR visitor emits AST instructions
- **THEN** it appends to an `InstList` (AST stage) via `InstListBuilder`; the AST stage now uses the same `InstList` SoA layout (parallel arrays + `extra` pool) as all subsequent stages, ensuring uniformity and eliminating the legacy bit-packed `uint[]` slab entirely.

### Requirement: Diagnostic Journal
**Modified:** The `flags[]` array includes a `Diagnostic` bit (replaces bit 21). When set, the instruction's diagnostic info is in a separate `DiagnosticJournal` array indexed by `InstIndex`.

#### Scenario: Recording compilation errors
- **WHEN** the frontend encounters a semantic error
- **THEN** it sets `Flags[idx] |= InstFlags.Diagnostic` and writes to `DiagnosticJournal[instIdx]`.

### Requirement: SlabPrinter Prerequisite
**Modified:** `SlabPrinter` now iterates `InstList` field arrays directly for human-readable output, no decode step.

### Requirement: Strict Value-Type Isolation
**Modified:** `InstList` is a `readonly struct` composed entirely of value types (`byte[]`, `ushort[]`, `uint[]`, `InstIndex`, `BlockId`, `SymbolId`). No managed references in core IR.

### Requirement: Hashed Symbol Table
**Modified:** Symbol table uses parallel arrays (`SymbolHashes[]`, `SlabOffsets[]`, `BlockIds[]`) indexed by `SymbolId` handle. String-to-hash conversion uses FNV-1a.

### Requirement: Standardized Slab Header Prefix
**Modified:** Unchanged — `SlabHeader` (magic/version/stage/count/symbolTableOffset) remains the 6-index prefix of the serialized file format. The `InstList` body follows the header in the on-disk format.

### Requirement: Type-Length-Value (TLV) Structure
**Modified:** Unchanged — auxiliary sections after the `InstList` body still use TLV for forward compatibility.

### Requirement: Encapsulated Offset Patching
**Modified:** `SlabRelocator` operates on `InstList` field arrays and `extra` pool using `InstIndex` handles. Offset patching updates `fixedOps`/`extra` entries.

## REMOVED Requirements

### Requirement: InstructionMetadata Encode/Decode API
**Reason:** Superseded by direct SoA field access. Bit-packing decode/encode overhead eliminated.
**Migration:** All passes rewritten to use `instList.Tags[i]`, `instList.Flags[i]`, etc.

### Requirement: InstructionMetadataFlags Enum (as bit-packed flags)
**Reason:** Flags are now individual `flags[]` array (u16) with direct bitwise access.
**Migration:** `InstFlags.Terminator`, `InstFlags.Diagnostic` etc. become `Flags[idx] |= InstFlags.Terminator`.

### Requirement: SlabIterator Utility
**Reason:** Replaced by direct `for (int i = 0; i < instList.Count; i++)` loops over SoA arrays.
**Migration:** Remove `SlabIterator`; passes use direct indexing.

### Requirement: SlabCompactionUtility
**Reason:** `InstList` arrays are already compact by design; no separate compaction needed.
**Migration:** Remove `SlabCompactionUtility`; `InstListBuilder.Compact()` handles reallocation if needed.

### Requirement: CfgTable (as separate struct with raw indices)
**Reason:** `CfgTable` is retained but keyed by `BlockId` handles; `InstList.blockIds[]` provides instruction→block mapping.
**Migration:** Update `CfgTable` methods to accept/return `BlockId` handles.