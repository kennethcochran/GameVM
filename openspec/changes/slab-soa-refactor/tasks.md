## 1. SoA Primitives & Core Types

- [ ] 1.1 Create `InstIndex`, `BlockId`, `SymbolId`, `SlotIndex` readonly struct handles in `GameVM.Compiler.Core/IR/Soa/`
- [ ] 1.2 Create `InstFlags` enum (Terminator=1, Diagnostic=2, reserved bits)
- [ ] 1.3 Create `InstList` readonly struct with parallel arrays:
  - `tags: byte[]` (instruction kind enum)
  - `flags: ushort[]` (bitwise flags)
  - `argCounts: ushort[]` (arg count per instruction)
  - `fixedOps: uint[]` (flat array: `MAX_FIXED_OPS * count`, MAX_FIXED_OPS=4)
  - `extra: uint[]` (variable-length operand pool)
  - `extraOffsets: uint[]` / `extraLengths: uint[]` (per-instruction overflow tracking)
  - `blockIds: int[]` (per-instruction CFG block mapping, 0 = unassigned)
  - `count: int` / `extraUsed: uint`
- [ ] 1.4 Add `InstListBuilder` for incremental construction (Append/Add methods, auto-resize)
- [ ] 1.5 Add `InstList.GetOperands(instIdx)` returning `ReadOnlySpan<uint>` (handles fast path + extra pool)
- [ ] 1.5 Add `InstList.CompactExtra()` for pool defragmentation (optional)
- [ ] 1.6 Unit tests for `InstList`/`InstListBuilder` (append, get operands, fast/slow paths)

## 2. AST Stage — Pascal Frontend

- [ ] 2.1 Update `PascalFrontend.ParseToSlab` to return `InstList` (AST stage) instead of `uint[]`
- [ ] 2.2 Update `PascalToSlabVisitor` to emit into `InstListBuilder` (AST stage) using tagged-union AoS per Odin
- [ ] 2.3 Keep `AstSlabToHlirSlabTransformer` input as `InstList` (AST); output `InstList` (HLIR)
- [ ] 2.4 Update `PascalFrontend.ConvertToHlirSlab` signature: `InstList ConvertToHlirSlab(InstList astSlab)`
- [ ] 2.5 Update `PascalFrontend.StringPool` integration with new `InstList` extra pool
- [ ] 2.6 Update `PascalToSlabVisitorTests` to assert on `InstList` field arrays

## 3. HLIR Stage — AstSlabToHlirSlabTransformer

- [ ] 3.1 Rewrite `AstSlabToHlirSlabTransformer.Transform` to take/return `InstList`
- [ ] 3.2 Replace `InstructionMetadata.Encode/Decode` with direct `instList.Tags[]`/`Flags[]`/`ArgCounts[]`/`FixedOps[]` access
- [ ] 3.3 Rewrite `ProcessAssignment`, `ProcessIfStatement`, `ProcessWhileStatement`, `ProcessReturnStatement`, etc. to append to `InstListBuilder`
- [ ] 3.4 Use `InstIndex` handles for operand references; `BlockId` for CFG
- [ ] 3.5 Update `HlirSlabToMlirSlabTransformerTests` to assert on `InstList` field arrays
- [ ] 3.6 Update `AstSlabToHlirSlabTransformerTests` to construct/assert on `InstList`

## 4. HLIR → MLIR — HlirSlabToMlirSlabTransformer

- [ ] 4.1 Rewrite `HlirSlabToMlirSlabTransformer.Transform` to take/return `InstList`
- [ ] 4.2 Linear iteration over `sourceInstList.Tags[]` with switch on kind
- [ ] 4.3 Operand references use `InstIndex`; branch targets use `BlockId`; local slots use `SlotIndex`
- [ ] 4.4 Update `HlirSlabToMlirSlabTransformerTests` for `InstList` input/output

## 5. MLIR Optimizer — DefaultMidLevelOptimizer

- [ ] 5.1 Rewrite `OptimizeSlab` to take/return `InstList` (MLIR stage)
- [ ] 5.2 Replace `InstructionMetadata.Decode*` with direct `instList.Tags[i]`, `instList.ArgCounts[i]`, `instList.FixedOps[i*MAX_FIXED_OPS + ...]`
- [ ] 5.3 Rewrite `ProcessInstruction`, `ProcessAssign`, `ProcessLabel`, `ProcessBranch`, `ProcessCall`, `ProcessReturn` to operate on `InstListBuilder`
- [ ] 5.4 Iteration: `for (int i = 0; i < instList.Count; i++)` stride-only on `Tags[]`, `FixedOps[]`
- [ ] 5.5 Update `MidLevelOptimizerTests` to construct/assert on `InstList`

## 6. MLIR → LLIR — MidToLowLevelTransformer

- [ ] 6.1 Rewrite `MidToLowLevelTransformer.TransformSlab` to take/return `InstList` (LLIR stage)
- [ ] 6.2 Linear iteration over `sourceInstList.Tags[]` with switch on `MlirInstructionKind`
- [ ] 6.3 Operands use `InstIndex`; branch targets `BlockId`; local slots `SlotIndex`
- [ ] 6.4 Update `MLIRToLLIRTransformerTests`, `MidToLowLevelTransformerTests` for `InstList`

## 7. LLIR Optimizer — DefaultLowLevelOptimizer

- [ ] 7.1 Rewrite `OptimizeSlab` to take/return `InstList` (LLIR stage)
- [ ] 7.2 Stride-only iteration over `Tags[]`, `FixedOps[]` spans
- [ ] 7.3 Rewrite peephole passes (`EliminateDeadLoad`, etc.) using `InstListBuilder`
- [ ] 7.4 Update `LowLevelOptimizerTests` for `InstList`

## 8. Backends — Atari2600CodeGenerator & MidToLowLevelTransformer

- [ ] 8.1 Update `Atari2600CodeGenerator.GenerateFromSlab` to accept `InstList` (LLIR)
- [ ] 8.2 Rewrite codegen to iterate `instList.Tags[]`, `instList.FixedOps[]` spans
- [ ] 8.3 Use `instList.GetOperandOffset(instIdx, operandIdx)` for codegen-time address resolution
- [ ] 8.4 Update `Atari2600CodeGeneratorTests`, `Atari2600CapabilityTests` for `InstList`

## 9. Application Layer — CompileUseCase

- [ ] 9.1 Update `CompileUseCase.CompileInternal` to pipeline `InstList` through stages
- [ ] 9.2 Update `IMidLevelOptimizer.OptimizeSlab`, `ILowLevelOptimizer.OptimizeSlab`, `IIRSlabTransformer.TransformSlab` signatures to `InstList`
- [ ] 9.3 Update `CompileUseCaseTests`, `CapabilityEnforcementTests`, `CompileUseCaseCapabilityTests` mocks to return `InstList`

## 10. SlabPrinter & Diagnostics

- [ ] 10.1 Rewrite `SlabPrinter.Print` to iterate `InstList.Tags[]`, `Flags[]`, `FixedOps[]`, `Extra[]` directly
- [ ] 10.2 Update `DiagnosticJournal` to index by `InstIndex` handle
- [ ] 10.3 Update `SlabPrinterTests` for `InstList` output

## 11. Symbol Table & CFG

- [ ] 11.1 Update `SymbolTable` to use parallel arrays keyed by `SymbolId` handle
- [ ] 11.2 Update `CfgTable` to use `BlockId` handles; add `InstList.BlockIds[]` mapping
- [ ] 11.3 Update `CfgConstructionPass` to populate `InstList.BlockIds[]` and `CfgTable` via `BlockId` handles

## 12. Cleanup Obsolete Types

- [ ] 12.1 Delete `InstructionMetadata`, `InstructionMetadataFlags`, `SlabIterator`, `SlabCompactionUtility` (replaced by `InstList` + `InstListBuilder`)
- [ ] 12.2 Remove `uint[]` slab parsing/creation helpers (`SlabHeader.ForStage` stays for file format)
- [ ] 12.3 Update `ArenaAllocator` to add `AllocateArray<T>(count)` helper

## 13. Tests — Full Suite Migration

- [ ] 13.1 Update all test projects (`Core.Tests`, `Pascal.Tests`, `Optimizers.MidLevel.Tests`, `Optimizers.LowLevel.Tests`, `Backend.Atari2600.Tests`, `Application.Tests`, `Compile.Tests`) to construct/assert on `InstList` field arrays + `extra` pool
- [ ] 13.2 Verify all test suites pass with new `InstList` API

## 14. Documentation

- [ ] 14.1 Update `docs/compiler/HLIR.md`, `MLIR.md`, `LLIR.md` with SoA `InstList` layout
- [ ] 14.2 Update `docs/compiler/LLIR_ISA.md` for new operand access patterns
- [ ] 14.3 Update `docs/api/` and XML doc comments for changed public API (`IMidLevelOptimizer`, `ILowLevelOptimizer`, `IIRSlabTransformer`, `InstList`, handles)
- [ ] 14.4 Update `docs/architecture/` for new SoA pipeline layout

## 15. CI & Verification

- [ ] 15.1 Verify `dotnet build` succeeds with zero errors/warnings
- [ ] 15.2 Verify all test suites pass (`dotnet test`)
- [ ] 15.3 Verify SonarQube quality gate passes (no new warnings)
- [ ] 15.4 Update `.github/workflows/` if any CI scripts reference old slab format

## 16. Cleanup — Legacy OOP AST Removal (Completed)

- [x] 16.1 Delete dead `PascalAstNode` hierarchy (~61 files in `src/GameVM.Compiler.Pascal/`)
- [x] 16.2 Delete dead `ASTBuilder.cs` factory
- [x] 16.3 Delete dead `TransformationContext.cs` 
- [x] 16.4 Delete `test/GameVM.Compiler.Pascal.Tests/ASTBuilderTests.cs` (8 tests)
- [x] 16.5 Delete `test/GameVM.Compiler.Pascal.Tests/TransformationContextTests.cs` (4 tests)
- [x] 16.6 Update `docs/compiler/semantic-analysis-report.md` to mark OOP AST sections as `[outdated]`
- [x] 16.7 Verify build succeeds and all tests pass (445 tests green)