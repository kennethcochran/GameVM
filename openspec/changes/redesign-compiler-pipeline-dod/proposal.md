## Why

Currently, the compiler pipeline (from frontend parsing to code generation) uses an object-oriented parse tree and visitor patterns (e.g., `PascalToHlirTransformer`, `ASTVisitor`). This approach relies on heavy heap allocations and pointer chasing which is very cache-unfriendly. Transitioning the pipeline to a Data-Oriented Design (DOD) approach using a single unified memory slab with self-describing instruction blocks will significantly improve cache locality, reduce memory pressure, and greatly accelerate compilation speed. This becomes critical as the codebase and target language complexity scales.

## What Changes

- **BREAKING**: Replace object-oriented AST nodes with a single unified memory slab using arena allocation.
- **BREAKING**: Remove standard OOP Visitor patterns across all compiler passes (Semantic Analysis, Optimization, Code Generation).
- **BREAKING**: Abandon current OOP AST visitor refactoring; implement five-stage pipeline (ANTLR → AST slab → HLIR slab → MLIR slab → LLIR slab).
- Replace tree traversals with contiguous slab iteration over self-describing instruction blocks.
- Introduce data-oriented intermediate representations (HLIR, MLIR, LLIR) using offset-based references in a single slab with Basic Block ID-based control flow.
- Implement parallel CFG arrays (blockOffsets, cfgEdges, edgeStart, edgeCount) for cache-friendly control flow graph traversal.
- Add separate CFG construction pass after parsing to identify basic blocks and assign Block IDs.
- Encode block boundaries and diagnostic flags in instruction metadata using formalized 32-bit layout.
- Use symbolic cross-block references (local slot indices) instead of SSA virtual registers; defer formal SSA to future work.
- Implement Diagnostic Journal for error handling separate from instruction slab.
- Implement SlabPrinter utility as prerequisite for debugging before optimization passes.
- Enforce strict value-type isolation in all DOD structures (InstructionSlab, CFGTable, SymbolTable) using only structs and primitive arrays for serialization readiness.
- Implement hashed symbol table using FNV-1a algorithm for integer-based symbol resolution instead of string names.
- Add standardized slab header prefix (6 indices) for magic number, versioning, IR stage identifier, element count, and symbol table offset.
- Implement Type-Length-Value (TLV) structure for auxiliary data sections to enable safe skipping of unknown chunks.
- Implement SlabRelocator utility class for centralized offset patching in optimization passes and future linking phases.
- Rewrite `PascalFrontend`, `CSharp` parsers to emit directly into AST slab from ANTLR parse tree.
- Transform semantic analysis to operate on HLIR slab using linear iteration.
- Refactor two-tier optimization pipeline (MidLevel and LowLevel) for DOD slab processing.
- Update code generation to process DOD LLIR slab for bytecode output.
- Target 90% reduction in heap allocations and 3-5x compilation throughput improvement.

## Capabilities

### New Capabilities
- `dod-compiler-architecture`: Establishing data-oriented memory structures, arena allocators, and index-based representations for the entire compiler.
- `index-based-ast`: The frontend translation mapping source code to index-based flat AST arrays instead of objects.
- `contiguous-passes`: Semantic analysis and optimization passes executed over contiguous arrays rather than node visitors.
- `dod-mlir-transformation`: Converting HLIR to MLIR using index-based transformations.
- `dod-optimization-pipeline`: Two-tier optimization (mid-level and low-level) operating on DOD IR arrays.

### Modified Capabilities

## Impact

- All frontends (`GameVM.Compiler.CSharp`, `GameVM.Compiler.Pascal`) and their ASTs.
- The intermediate representation passes (`GameVM.Compiler.Core`, `GameVM.Compiler.Optimizers.LowLevel`, `GameVM.Compiler.Optimizers.MidLevel`).
- Semantic analysis (`GameVM.Compiler.Core.SemanticAnalysis`).
- Transformers (`HlirToMlirTransformer`, `MidToLowLevelTransformer`).
- Backends, specifically `GameVM.Compiler.Backend.Atari2600`.
- Code generation (`GameVM.Compiler.Core.CodeGen`).
- All testing suites verifying parse tree properties and compilation stages.
