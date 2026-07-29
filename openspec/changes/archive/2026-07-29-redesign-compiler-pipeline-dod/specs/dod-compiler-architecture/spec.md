## ADDED Requirements

### Requirement: DOD Memory Arenas
The compiler MUST allocate all nodes in a single contiguous memory slab per compilation unit rather than using individual heap allocations for each node.

#### Scenario: Allocating a new node
- **WHEN** the parser creates a new AST node
- **THEN** it is allocated linearly in the active arena slab, and its offset is returned.

### Requirement: Single Unified Slab
The core IR nodes MUST be represented using a single contiguous memory slab (uint[]) where each instruction is a variable-length, self-describing block.

#### Scenario: Traversing instruction blocks
- **WHEN** an optimization pass processes instructions
- **THEN** it reads the metadata header at each offset to determine block size and kind, then processes the block contiguously before jumping to the next offset.

### Requirement: Self-Describing Instruction Blocks
Each instruction block in the slab MUST start with metadata that encodes the instruction kind, total block size, argument count, terminator flag, and diagnostic present flag, making the block self-describing without external metadata arrays.

#### Scenario: Parsing an instruction
- **WHEN** a pass reads an instruction at a given offset
- **THEN** it decodes the metadata header to determine the instruction kind, block size, argument count, whether it terminates a basic block, and whether diagnostic information is available.

### Requirement: Formalized Metadata Encoding
The 32-bit metadata header MUST use the following bit layout:
- Bits 0-7: Instruction Kind (256 kinds)
- Bits 8-13: Instruction Block Size (max 64 uints)
- Bits 14-19: Argument Count (max 64 arguments)
- Bit 20: Terminator Flag (basic block boundary)
- Bit 21: Diagnostic Present Flag (cold-path debug info available)
- Bits 22-31: Reserved for future flags

#### Scenario: Encoding instruction metadata
- **WHEN** the parser creates a new instruction block
- **THEN** it encodes the instruction kind, size, argument count, terminator flag, and diagnostic flag into a single 32-bit metadata header using the specified bit layout.

### Requirement: CFG Parallel Arrays
The Control Flow Graph MUST be represented using parallel, cache-friendly arrays rather than object-oriented graph structures.

#### Scenario: Building the CFG
- **WHEN** the CFG construction pass identifies basic blocks
- **THEN** it populates `blockOffsets[]` (BlockID → SlabOffset), `cfgEdges[]` (flat adjacency list), and per-block `edgeStart[]` and `edgeCount[]` arrays.

### Requirement: Separate CFG Construction Pass
Basic Block ID allocation MUST occur in a dedicated pass after parsing, not during parsing itself.

#### Scenario: Assigning Block IDs
- **WHEN** the CFG construction pass processes the parsed instruction stream
- **THEN** it identifies leaders (jump targets, post-jump instructions), assigns sequential Block IDs, and builds the CFG tables.

### Requirement: Five-Stage Pipeline Architecture
The compiler MUST use a five-stage pipeline: ANTLR parse tree → AST slab → HLIR slab → MLIR slab → LLIR slab. Object-oriented AST structures are bypassed entirely.

#### Scenario: Parsing to AST slab
- **WHEN** the ANTLR parser generates a parse tree
- **THEN** a manual ANTLR visitor descendant traverses the concrete parse tree and emits instructions directly into the AST slab arena.

### Requirement: Diagnostic Journal for Error Handling
Semantic and compilation errors MUST be written to a separate, lightweight Diagnostic Journal, never mixed into the executable instruction slab.

#### Scenario: Recording compilation errors
- **WHEN** the frontend visitor encounters a semantic or compilation error
- **THEN** it writes the error to the Diagnostic Journal and sets the Diagnostic Present Flag in the instruction metadata header.

### Requirement: SlabPrinter Prerequisite
A SlabPrinter utility MUST be implemented alongside the core architecture to translate raw uint[] arrays into readable pseudo-assembly text format before any optimization passes are written.

#### Scenario: Debugging slab contents
- **WHEN** a developer needs to inspect the instruction slab
- **THEN** the SlabPrinter utility translates the raw uint[] array into human-readable pseudo-assembly format.

### Requirement: Strict Value-Type Isolation
All DOD structures (InstructionSlab, CFGTable, SymbolTable) MUST be strictly composed of value types using structs and primitive arrays. No managed object references, class instances, or strings are stored in core IR structures.

#### Scenario: Serializing compiled library
- **WHEN** saving a compiled library to disk
- **THEN** the system performs a high-speed block copy of primitive arrays directly to a file stream without complex serialization logic.

### Requirement: Hashed Symbol Table Implementation
The SymbolTable MUST use parallel primitive arrays (uint[] SymbolHashes, uint[] SlabOffsets) instead of string names. String identifiers are converted to 32-bit integers using FNV-1a hashing.

#### Scenario: Resolving exported symbols
- **WHEN** the linking phase needs to resolve an exported function
- **THEN** it computes the FNV-1a hash of the symbol name and looks it up in the SymbolHashes array to find the corresponding slab offset.

### Requirement: Standardized Slab Header Prefix
The InstructionSlab MUST reserve the first six indices for global metadata and versioning:
- Index 0: Magic Number / File Signature
- Index 1: Major Version (breaking IR layout or bit-packing changes)
- Index 2: Minor Version (non-breaking additions like new instruction kinds)
- Index 3: IR Stage Identifier (AST, HLIR, MLIR, LLIR)
- Index 4: Total active elements in the slab
- Index 5: Offset to the Symbol Table

#### Scenario: Loading a compiled library
- **WHEN** the loader reads a compiled library file
- **THEN** it first validates the magic number and version compatibility from the header before processing the slab contents.

### Requirement: Type-Length-Value (TLV) Structure
Auxiliary data sections appended to the slab MUST use a Type-Length-Value (TLV) structure to enable safe skipping of unknown chunks.

#### Scenario: Loading with unknown extensions
- **WHEN** an older loader encounters an unknown chunk type
- **THEN** it reads the length field and skips that many bytes without crashing, preserving forward compatibility.

### Requirement: Encapsulated Offset Patching
A dedicated `SlabRelocator` utility class MUST handle all offset patching. Structural optimization passes or branch resolution requiring code shifting or jump target updates must use this central utility.

#### Scenario: Applying structural optimization
- **WHEN** an optimization pass needs to shift code and update jump targets
- **THEN** it uses the SlabRelocator utility to identify and patch all affected offsets rather than implementing localized patching logic.
