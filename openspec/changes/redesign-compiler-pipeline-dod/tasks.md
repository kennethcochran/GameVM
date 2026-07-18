## 1. Core Architecture

- [x] 1.1 Implement `ArenaAllocator` for contiguous memory slab allocation
- [x] 1.2 Define formalized 32-bit metadata encoding (bits 0-7: kind, 8-13: size, 14-19: arg count, 20: terminator, 21: diagnostic, 22-31: reserved)
- [x] 1.3 Implement slab iteration utilities for reading self-describing instruction blocks
- [x] 1.4 Create metadata encoding/decoding helper functions with bit manipulation utilities
- [x] 1.5 Implement parallel CFG arrays (blockOffsets, cfgEdges, edgeStart, edgeCount)
- [x] 1.6 Implement CFG construction pass to identify leaders and assign Block IDs
- [x] 1.7 Define local slot index system for cross-block dependencies (defer SSA to future work)
- [x] 1.8 Implement Diagnostic Journal for error handling separate from instruction slab
- [x] 1.9 Implement SlabPrinter utility to translate uint[] arrays to pseudo-assembly text (prerequisite before optimization passes)
- [ ] 1.10 Enforce strict value-type isolation in all DOD structures (InstructionSlab, CFGTable, SymbolTable) using only structs and primitive arrays
- [x] 1.11 Implement hashed SymbolTable using FNV-1a algorithm with parallel uint[] arrays (SymbolHashes, SlabOffsets)
- [x] 1.12 Implement standardized slab header prefix (6 indices: magic, major version, minor version, IR stage, element count, symbol table offset)
- [x] 1.13 Implement Type-Length-Value (TLV) structure for auxiliary data sections
- [x] 1.14 Implement SlabRelocator utility class for centralized offset patching

## 2. CSharp Frontend Migration

- [ ] 2.1 Implement manual ANTLR visitor descendant to traverse CSharp parse tree and emit directly into AST slab
- [ ] 2.2 Remove existing CSharp OOP AST transformation code
- [ ] 2.3 Rewrite CSharp-to-HLIR transformation to process AST slab using offset lookups and linear iteration

## 3. Pascal Frontend Migration

- [ ] 3.1 Implement manual ANTLR visitor descendant to traverse Pascal parse tree and emit directly into AST slab
- [ ] 3.2 Remove existing Pascal OOP AST transformation code (abandon current AST visitor refactoring)
- [ ] 3.3 Delete the deprecated `ASTVisitor`, `DeclarationVisitor`, `ExpressionVisitor`, and `StatementVisitor` files
- [ ] 3.4 Rewrite Pascal-to-HLIR transformation to process AST slab using offset lookups and linear iteration

## 4. HLIR to MLIR Transformation Migration

- [ ] 4.1 Refactor `HlirToMlirTransformer` to process DOD HLIR slab using linear iteration
- [ ] 4.2 Implement DOD MLIR slab with self-describing instruction blocks
- [ ] 4.3 Update MLIR instruction processing to use switch statements on decoded metadata instead of virtual dispatch
- [ ] 4.4 Create MLIR instruction enum (`MlirInstructionKind`) for type discrimination

## 5. Semantic Analysis Migration

- [ ] 5.1 Refactor `BasicSemanticAnalyzer` to process DOD HLIR slab using linear iteration
- [ ] 5.2 Update type checking to operate on offset-based HLIR structures
- [ ] 5.3 Update symbol resolution to use offset-based symbol tables
- [ ] 5.4 Remove tree traversal patterns from semantic analysis

## 6. Mid-Level Optimization Migration

- [ ] 6.1 Refactor `DefaultMidLevelOptimizer` to process DOD MLIR slab using linear iteration
- [ ] 6.2 Update constant propagation to use switch-based instruction processing on decoded metadata
- [ ] 6.3 Implement tombstoning (NOP encoding) for dead code elimination
- [ ] 6.4 Implement out-of-place transformations for structural optimizations
- [ ] 6.5 Remove visitor patterns from mid-level optimization

## 7. Low-Level Optimization Migration

- [ ] 7.1 Refactor `DefaultLowLevelOptimizer` to process DOD LLIR slab using linear iteration
- [ ] 7.2 Update register allocation for contiguous LLIR slab
- [ ] 7.3 Update peephole optimization to use switch-based instruction processing on decoded metadata
- [ ] 7.4 Implement tombstoning for redundant instruction elimination
- [ ] 7.5 Remove visitor patterns from low-level optimization

## 8. Backend & Code Generation Migration

- [ ] 8.1 Refactor `MidToLowLevelTransformer` to process DOD MLIR slab using linear iteration
- [ ] 8.2 Update `GameVM.Compiler.Backend.Atari2600` to consume the DOD offset-based IR slab
- [ ] 8.3 Refactor `DefaultCodeGenerator` to process DOD LLIR slab for bytecode generation
- [ ] 8.4 Remove the legacy OOP node class hierarchies across the entire codebase

## 9. Integration Testing

- [ ] 9.1 Create end-to-end tests for complete DOD pipeline (AST→HLIR→MLIR→LLIR→Bytecode)
- [ ] 9.2 Validate three-tier IR transformations with DOD slab structures
- [ ] 9.3 Performance benchmarking comparing OOP vs DOD slab pipeline
- [ ] 9.4 Memory usage analysis for DOD slab vs OOP approaches
- [ ] 9.5 Validate tombstoning behavior in optimization passes
- [ ] 9.6 Validate out-of-place transformation correctness
