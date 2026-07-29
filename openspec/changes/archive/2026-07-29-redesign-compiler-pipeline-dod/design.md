## Context

The current compiler pipeline for GameVM (from `GameVM.Compiler.Pascal` and `GameVM.Compiler.CSharp` frontends down to `GameVM.Compiler.Backend.Atari2600`) relies on an object-oriented parse tree and visitor patterns (e.g., `ASTVisitor`, `PascalToHlirTransformer`). This architecture causes poor cache locality due to heap-allocated nodes, pointer chasing, and indirect virtual calls, severely limiting compilation throughput and creating GC pressure.

The current pipeline follows a three-tier IR architecture:
1. **HLIR (High-Level IR)** - Language-independent representation from source parsing
2. **MLIR (Mid-Level IR)** - Optimized form with resource analysis and basic block structure
3. **LLIR (Low-Level IR)** - Hardware-agnostic virtual machine ISA for code generation

The pipeline includes semantic analysis, two-tier optimization (mid-level and low-level), and code generation phases—all of which currently operate on object-oriented structures.

## Goals / Non-Goals

**Goals:**
- Transition the entire AST and three-tier Intermediate Representation (HLIR/MLIR/LLIR) to a Data-Oriented Design (DOD).
- Store nodes in contiguous memory arrays (e.g., Arena Allocation, Struct-of-Arrays).
- Use 32-bit indices rather than 64-bit object references to link nodes.
- Replace virtual dispatch and standard visitor patterns with linear, contiguous sweeps over node arrays, using switch statements over node type enums.
- Improve cache locality and compilation speed across the entire pipeline.
- Transform semantic analysis to operate on index-based HLIR arrays.
- Refactor two-tier optimization pipeline (MidLevel and LowLevel) for DOD structures.
- Update code generation to process DOD LLIR for bytecode output.

**Non-Goals:**
- We are not changing the language syntax or semantics of C# or Pascal for GameVM.
- We are not introducing new optimization passes yet; the goal is structural transformation of the existing passes.
- We are not writing a new backend; just migrating the existing ones to consume DOD structures.
- We are not changing the fundamental three-tier IR architecture (HLIR→MLIR→LLIR).

## Decisions

- **Single Unified Slab**: Instead of parallel arrays or struct arrays, we will use a single contiguous memory slab (`uint[]`) per compilation unit where all instruction data is stored. This eliminates heap allocation, GC scanning, and pointer-chasing overhead.

- **Self-Describing Instruction Blocks**: Each instruction in the slab is a variable-length block starting with a metadata header that encodes the instruction kind, block size, and argument count. This makes instructions self-describing without external metadata arrays.

- **Block-Level CFG Targets vs. Raw Offsets**:
  - Raw offsets within the slab are only used for local, linear instruction traversal within a Basic Block (`offset += size`).
  - Control flow targets (branches, jumps) reference a stable **Basic Block ID** rather than a raw, absolute slab offset.
  - The Control Flow Graph (CFG) maintains parallel, cache-friendly arrays:
    - `blockOffsets[]`: Maps `BlockID -> SlabOffset` for lookup
    - `cfgEdges[]`: Flat adjacency list storing all CFG edges
    - `edgeStart[]` and `edgeCount[]`: Per-block indices into `cfgEdges[]`
  - This eliminates the need for complex instruction-level relocation maps during optimization passes and keeps CFG traversal cache-friendly.

- **Pipeline-Driven Compaction**: 
  - Analysis and rewriting passes (e.g., constant propagation or dead code elimination) are non-structural and use `NOP` tombstoning (overwriting headers with `NOP` kinds in-place) for $O(1)$ deletion.
  - Lowering and emission passes (e.g., SSA destruction or final code generation) are inherently structural; they read sequentially from the old slab and stream out compacted code to the new representation, providing slab compaction for free.

- **C# Array Type (`uint[]`) & Offset 0 Sentinel**:
  - The slab is typed as `uint[]` to keep bit-packing, masking, and shifting predictable without sign-extension bugs.
  - Offset `0` is reserved as a dummy header or `NOP` instruction, acting as a sentinel value for "null" or "invalid" references, eliminating the need for negative integers.

- **Formalized Metadata Encoding**: The 32-bit metadata header uses the following bit layout:
  - Bits 0-7: Instruction Kind (256 kinds)
  - Bits 8-13: Instruction Block Size (max 64 uints)
  - Bits 14-19: Argument Count (max 64 arguments)
  - Bit 20: Terminator Flag (basic block boundary)
  - Bit 21: Diagnostic Present Flag (cold-path debug info available)
  - Bits 22-31: Reserved for future flags

- **Arena Allocation**: Per-compilation-unit memory uses dedicated bump-pointer Arenas. The slab grows linearly and is discarded as a whole upon compilation completion.

- **Separate CFG Construction Pass**: Basic Block ID allocation is handled as a dedicated pass after parsing, not during parsing itself. This allows the parser to focus on language semantics while the CFG pass identifies leaders (jump targets, post-jump instructions) and assigns Block IDs sequentially.

- **Terminator Flag in Metadata**: Block boundaries are explicitly encoded in the instruction metadata header using a dedicated `IsTerminator` flag. Instructions like branches, jumps, and returns have this flag set, enabling O(1) block boundary detection without repeated control-flow analysis.

- **Symbolic Cross-Block References**: Instructions must not reference operands in different basic blocks using raw slab offsets. Cross-block dependencies use explicit local slot indices (abstract stack/local variable array) rather than SSA virtual registers. Formal SSA construction and destruction are deferred to future optimization work.

- **Five-Stage Pipeline Architecture**: The compiler uses a five-stage pipeline: ANTLR parse tree → AST slab → HLIR slab → MLIR slab → LLIR slab. The current OOP AST visitor refactoring is abandoned. A manual ANTLR visitor descendant traverses the concrete parse tree and emits instructions directly into the AST slab arena, bypassing OOP AST entirely.

- **Diagnostic Journal for Error Handling**: Semantic and compilation errors are never mixed into the executable instruction slab. Errors are written to a separate, lightweight Diagnostic Journal. The Diagnostic Present Flag (bit 21) indicates when cold-path debug information exists for a specific offset, enabling lookup in a separate dictionary mapping slab offset to source span.

- **SlabPrinter Prerequisite**: A SlabPrinter utility must be implemented alongside the core architecture to translate raw uint[] arrays into readable pseudo-assembly text format. This is a strict prerequisite before writing any optimization passes, as debugging raw integer arrays is prohibitively difficult.

- **Performance Targets**: Success targets are 90% reduction in heap allocations during middle-end optimization passes and 3-5x increase in raw compilation throughput, measured against current architecture baseline on Intel Core i7-6820HQ.

- **Strict Value-Type Isolation**: All DOD structures (InstructionSlab, CFGTable, SymbolTable) must be strictly composed of value types using structs and primitive arrays. No managed object references, class instances, or strings are stored in core IR structures. This guarantees serialization via high-speed block copy of primitive arrays to file streams.

- **Hashed Symbol Table Implementation**: SymbolTable uses parallel primitive arrays (uint[] SymbolHashes, uint[] SlabOffsets) instead of string names. String identifiers are converted to 32-bit integers using FNV-1a hashing. The SymbolTable maps hashes to function starting offsets within the slab, enabling linking phases to match integer hashes rather than parse string data.

- **Standardized Slab Header Prefix**: The InstructionSlab reserves the first six indices for global metadata and versioning:
  - Index 0: Magic Number / File Signature
  - Index 1: Major Version (breaking IR layout or bit-packing changes)
  - Index 2: Minor Version (non-breaking additions like new instruction kinds)
  - Index 3: IR Stage Identifier (AST, HLIR, MLIR, LLIR)
  - Index 4: Total active elements in the slab
  - Index 5: Offset to the Symbol Table
  Auxiliary data sections use Type-Length-Value (TLV) structure for safe skipping of unknown chunks.

- **Encapsulated Offset Patching**: A dedicated `SlabRelocator` utility class handles all offset patching. Structural optimization passes or branch resolution requiring code shifting or jump target updates must use this central utility. Centralizing offset identification enables reuse for sliding imported libraries during future linking phases.

- **Three-Tier IR Preservation**: The HLIR→MLIR→LLIR transformation chain will be maintained, but each IR level will use the single slab architecture with block-level CFG tracking.

- **Semantic Analysis Integration**: The `BasicSemanticAnalyzer` will process the HLIR slab using linear iteration over local instruction blocks.

- **Code Generation**: The `DefaultCodeGenerator` will process the LLIR slab by reading the self-describing blocks to emit the final bytecode.

## Risks / Trade-offs

- **[Risk] Debugging Difficulty**: Looking at raw arrays and integer indices in a debugger is much harder than expanding an object graph.
  - *Mitigation*: Build custom debugger visualizers (`.natvis` or similar) or dump methods that reconstruct tree views for developer use.
- **[Risk] Massive Diff & Pipeline Breakage**: Changing the core AST affects everything at once.
  - *Mitigation*: Start by creating the DOD IR alongside the OOP one, add a conversion step (OOP -> DOD), and then port backend and optimization passes. Finally, port the parsers to generate DOD AST directly and remove the OOP tree entirely.
- **[Risk] Three-Tier IR Complexity**: Maintaining consistency across HLIR→MLIR→LLIR transformations with DOD structures adds complexity.
  - *Mitigation*: Create comprehensive transformation tests that validate each IR level independently and the complete pipeline end-to-end.
