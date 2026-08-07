# dod-compiler-architecture Specification

## Purpose
The compiler uses a Struct-of-Arrays (SoA) instruction slab architecture across all IR stages (AST, HLIR, MLIR, LLIR). Memory is allocated in contiguous arenas; all IR is represented as `InstList` parallel arrays (`byte[] Tags`, `ushort[] Flags`, `ushort[] ArgCounts`, `uint[] FixedOps`, `uint[] Extra`, `uint[] ExtraOffsets`, `int[] BlockIds`). CFG, symbol table, and slab header use parallel primitive arrays. No bit-packed metadata header; no `SlabRelocator`.

## Requirements
### Requirement: DOD Memory Arenas
The compiler MUST allocate all instruction data in contiguous arena slabs via `ArenaAllocator`, with `InstList` parallel arrays (`Tags`, `Flags`, `ArgCounts`, `FixedOps`, `Extra`, `ExtraOffsets`, `BlockIds`) as the IR representation.

#### Scenario: Allocating a new instruction
- **WHEN** a transformer appends an instruction
- **THEN** it uses `InstListBuilder` which bumps pointers on each parallel array in the arena.

### Requirement: Single Unified Slab
The core IR MUST be viewed as a single `InstList` struct — a Struct-of-Arrays over parallel primitive arrays (`byte[] Tags`, `ushort[] Flags`, `ushort[] ArgCounts`, `uint[] FixedOps`, `uint[] Extra`, `uint[] ExtraOffsets`, `int[] BlockIds`).

#### Scenario: Traversing instructions
- **WHEN** an optimization pass processes instructions
- **THEN** it iterates stride-only over `Tags[i]`, `Flags[i]`, `ArgCounts[i]`, `BlockIds[i]` with `i` from `0` to `Count-1`.

### Requirement: CFG Parallel Arrays
The Control Flow Graph MUST use parallel primitive arrays (`blockOffsets[]`, `cfgEdges[]`, `edgeStart[]`, `edgeCount[]`) keyed by `BlockId` handles. `InstList.blockIds[]` maps instructions to blocks.

#### Scenario: Building the CFG
- **WHEN** the CFG construction pass identifies basic blocks
- **THEN** it assigns sequential `BlockId` handles, fills `InstList.blockIds[]`, and populates `CfgTable` arrays indexed by `BlockId`.

### Requirement: Separate CFG Construction Pass
Basic Block ID allocation MUST occur in a dedicated pass after parsing, not during parsing.

#### Scenario: Assigning Block IDs
- **WHEN** the CFG construction pass processes the instruction stream
- **THEN** it identifies leaders, assigns sequential `BlockId` handles, and builds `CfgTable` arrays.

### Requirement: Five-Stage Pipeline Architecture
The compiler MUST use a five-stage pipeline: ANTLR parse tree → AST `InstList` → HLIR `InstList` → MLIR `InstList` → LLIR `InstList`. Object-oriented AST structures are bypassed entirely.

#### Scenario: Parsing to AST slab
- **WHEN** the ANTLR visitor emits instructions
- **THEN** it appends to an `InstList` (AST stage) via `InstListBuilder`; all stages share the SoA layout.

### Requirement: Diagnostic Journal for Error Handling
Semantic and compilation errors MUST be written to a separate `DiagnosticJournal` array indexed by `InstIndex`, never mixed into the executable instruction slab. The `Flags` array has a `Diagnostic` bit per instruction.

#### Scenario: Recording compilation errors
- **WHEN** the frontend encounters a semantic error
- **THEN** it sets `Flags[idx] |= InstFlags.Diagnostic` and writes to `DiagnosticJournal[instIdx]`.

### Requirement: SlabPrinter Prerequisite
A `SlabPrinter` utility MUST translate `InstList` field arrays into readable pseudo-assembly text for debugging.

#### Scenario: Debugging slab contents
- **WHEN** a developer needs to inspect the instruction slab
- **THEN** `SlabPrinter` iterates the parallel arrays directly for output.

### Requirement: Strict Value-Type Isolation
All DOD structures (`InstList`, `CfgTable`, `HashedSymbolTable`) MUST be strictly composed of value types using structs and primitive arrays. No managed object references, class instances, or strings are stored in core IR structures.

#### Scenario: Serializing compiled library
- **WHEN** saving a compiled library to disk
- **THEN** the system writes the parallel primitive arrays directly to a file stream without complex serialization logic.

### Requirement: Hashed Symbol Table Implementation
The `HashedSymbolTable` MUST use parallel primitive arrays (`uint[] SymbolHashes`, `uint[] SlabOffsets`) instead of string names. String identifiers are converted to 32-bit integers using FNV-1a hashing.

#### Scenario: Resolving exported symbols
- **WHEN** the linking phase needs to resolve an exported function
- **THEN** it computes the FNV-1a hash of the symbol name and looks it up in the `SymbolHashes` array.

### Requirement: Standardized Slab Header Prefix
The serialized format MUST reserve the first six indices for global metadata and versioning:
- Index 0: Magic Number / File Signature
- Index 1: Major Version (breaking IR layout or bit-packing changes)
- Index 2: Minor Version (non-breaking additions like new instruction kinds)
- Index 3: IR Stage Identifier (AST, HLIR, MLIR, LLIR)
- Index 4: Total active elements in the slab
- Index 5: Offset to the Symbol Table

#### Scenario: Loading a compiled library
- **WHEN** the loader reads a compiled library file
- **THEN** it first validates the magic number and version compatibility from the header before processing the `InstList` body.

### Requirement: Type-Length-Value (TLV) Structure
Auxiliary data sections appended to the `InstList` body MUST use a TLV structure to enable safe skipping of unknown chunks.

#### Scenario: Loading with unknown extensions
- **WHEN** an older loader encounters an unknown chunk type
- **THEN** it reads the length field and skips that many bytes without crashing, preserving forward compatibility.

### Requirement: Encapsulated Offset Patching
Offset patching MUST operate on `InstList` field arrays using `InstIndex` handles. There is NO dedicated `SlabRelocator` class.

#### Scenario: Applying structural optimization
- **WHEN** an optimization pass shifts code and updates jump targets
- **THEN** it updates `FixedOps`/`Extra` entries directly using `InstIndex` handles.
