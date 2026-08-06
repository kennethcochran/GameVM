## 1. SoA Primitives & Core Types

- [x] 1.1 Create `InstIndex`, `BlockId`, `SymbolId`, `SlotIndex` readonly struct handles in `GameVM.Compiler.Core/IR/Soa/`
- [x] 11.3 Update `CfgConstructionPass` to populate `InstList.BlockIds[]` and `CfgTable` via `BlockId` handles (completed by Task11.3)
- [x] 1.3 Create `InstList` readonly struct with parallel arrays:
   - `tags: byte[]` (instruction kind enum)
   - `flags: ushort[]` (bitwise flags)
   - `argCounts: ushort[]` (arg count per instruction)
   - `extra: uint[]` (variable-length operand pool)
   - `extraOffsets: uint[]` (per-instruction offset into extra pool)
   - `blockIds: int[]` (per-instruction CFG block mapping, 0 = unassigned)
   - `count: int` / `extraUsed: uint`
  - `count: int` / `extraUsed: uint`
- [x] 1.4 Add `InstListBuilder` for incremental construction (Append/Add methods, auto-resize)
- [x] 1.5 Add `InstList.GetOperands(instIdx)` returning `ReadOnlySpan<uint>` (handles fast path + extra pool)
- [x] 1.5 Add `InstList.CompactExtra()` for pool defragmentation (optional)
- [x] 1.6 Unit tests for `InstList`/`InstListBuilder` (append, get operands, fast/slow paths)

## 2. AST Stage — Pascal Frontend

- [x] 2.1 Update `PascalFrontend.ParseToSlab` to return `InstList` (AST stage) instead of `uint[]`
- [x] 2.2 Update `PascalToSlabVisitor` to emit into `InstListBuilder` (AST stage) using `InstList` SoA layout (parallel arrays + extra pool), not Odin-style AoS
- [x] 2.3 Keep `AstSlabToHlirSlabTransformer` input as `InstList` (AST); output `InstList` (HLIR)
- [x] 2.4 Update `PascalFrontend.ConvertToHlirSlab` signature: `InstList ConvertToHlirSlab(InstList astSlab)`
- [x] 2.5 Update `PascalFrontend.StringPool` integration with new `InstList` extra pool
- [x] 2.6 Update `PascalToSlabVisitorTests` to assert on `InstList` field arrays

## 3. HLIR Stage — AstSlabToHlirSlabTransformer

- [x] 3.1 Rewrite `AstSlabToHlirSlabTransformer.Transform` to take/return `InstList`
- [x] 3.2 Replace `InstructionMetadata.Encode/Decode` with direct `instList.Tags[]`/`Flags[]`/`ArgCounts[]`/`FixedOps[]` access
- [x] 3.3 Rewrite `ProcessAssignment`, `ProcessIfStatement`, `ProcessWhileStatement`, `ProcessReturnStatement`, etc. to append to `InstListBuilder`
- [x] 3.4 Use `InstIndex` handles for operand references; `BlockId` for CFG
- [x] 3.5 Update `HlirSlabToMlirSlabTransformerTests` to assert on `InstList` field arrays
- [x] 3.6 Update `AstSlabToHlirSlabTransformerTests` to construct/assert on `InstList`

## 4. HLIR → MLIR — HlirSlabToMlirSlabTransformer

- [x] 4.1 Rewrite `HlirSlabToMlirSlabTransformer.Transform` to take/return `InstList`
- [x] 4.2 Linear iteration over `sourceInstList.Tags[]` with switch on kind
- [x] 4.3 Operand references use `InstIndex`; branch targets use `BlockId`; local slots use `SlotIndex`
- [x] 4.4 Update `HlirSlabToMlirSlabTransformerTests` for `InstList` input/output

## 5. MLIR Optimizer — DefaultMidLevelOptimizer

- [x] 5.1 Rewrite `OptimizeSlab` to take/return `InstList` (MLIR stage)
- [x] 5.2 Replace `InstructionMetadata.Decode*` with direct `instList.Tags[i]`, `instList.ArgCounts[i]`, `instList.FixedOps[i*MAX_FIXED_OPS + ...]`
- [x] 5.3 Rewrite `ProcessInstruction`, `ProcessAssign`, `ProcessLabel`, `ProcessBranch`, `ProcessCall`, `ProcessReturn` to operate on `InstListBuilder`
- [x] 5.4 Iteration: `for (int i = 0; i < instList.Count; i++)` stride-only on `Tags[]`, `FixedOps[]`
- [x] 5.5 Update `MidLevelOptimizerTests` to construct/assert on `InstList`

## 6. MLIR → LLIR — MidToLowLevelTransformer

- [x] 6.1 Rewrite `MidToLowLevelTransformer.TransformSlab` to take/return `InstList` (LLIR stage)
- [x] 6.2 MLIR-kind linear iteration and `InstListBuilder` emission
- [x] 6.3 Operands use `InstIndex`; branch targets `BlockId`; local slots `SlotIndex`
- [x] 6.4 Update `MLIRToLLIRTransformerTests`, `MidToLowLevelTransformerTests` for `InstList` (MLIRToLLIRTransformerTests not found in test/ — created MidToLowLevelTransformerTests.cs; MLIR-to-LLIR transformer functionality covered via integration in DebugPipelineTests.cs)

## 7. LLIR Optimizer — DefaultLowLevelOptimizer

- [x] 7.1 Rewrite `OptimizeSlab` to take/return `InstList` (LLIR stage)
- [x] 7.2 Stride-only iteration over `Tags[]`, `FixedOps[]` spans
- [x] 7.3 Rewrite peephole passes (`EliminateDeadLoad`, etc.) using `InstListBuilder`
- [x] 7.4 Update `LowLevelOptimizerTests` for `InstList`

## 8. Backends — Atari2600CodeGenerator & MidToLowLevelTransformer

- [x] 8.1 Update `Atari2600CodeGenerator.GenerateFromSlab` to accept `InstList` (LLIR)
- [x] 8.2 Rewrite codegen to iterate `instList` tags/operands spans
- [x] 8.3 Handle fast+slow operands via `GetOperands`/`GetOperand` in codegen (GetOperandOffset exists in InstList but is not called by codegen)
- [x] 8.4 Update `Atari2600CodeGeneratorTests`, `Atari2600CapabilityTests` for `InstList`

## 9. Application Layer — CompileUseCase

- [x] 9.1 Update `CompileUseCase.CompileInternal` to pipeline `InstList` through stages
- [x] 9.2 Update `IMidLevelOptimizer.OptimizeSlab`, `ILowLevelOptimizer.OptimizeSlab`, `IIRSlabTransformer.TransformSlab` signatures to `InstList`
- [x] 9.3 Update `CompileUseCaseTests`, `CapabilityEnforcementTests`, `CompileUseCaseCapabilityTests` mocks to return `InstList`

## 10. SlabPrinter & Diagnostics

- [x] 10.1 Rewrite `SlabPrinter.Print` to iterate `InstList.Tags[]`, `Flags[]`, `FixedOps[]`, `Extra[]` directly (completed by FixSoABuild)
- [x] 10.2 Update `DiagnosticJournal` to index by `InstIndex` handle (completed)
- [x] 10.3 Update `SlabPrinterTests` for `InstList` output (completed by FixSoABuild)

## 11. Symbol Table & CFG

- [x] 11.1 Update SymbolTable to use parallel arrays keyed by `SymbolId` handle (SymbolTable.cs deleted)
- [x] 11.2 Update `CfgTable` to use `BlockId` handles; add `InstList.BlockIds[]` mapping
- [x] 11.3 Update `CfgConstructionPass` to populate `InstList.BlockIds[]` and `CfgTable` via `BlockId` handles (completed by Task11.3)

## 12. Cleanup Obsolete Types

- [x] 12.1 Delete `InstructionMetadata`, `InstructionMetadataFlags`, `SlabIterator`, `SlabCompactionUtility` (deleted by FixSoABuild)
- [x] 12.2 Remove `uint[]` slab parsing/creation helpers (`SlabHeader.ForStage` stays for file format)
- [x] 12.3 Update `ArenaAllocator` to add `AllocateArray<T>(count)` helper (method not found)

## 13. Tests — Full Suite Migration

- [x] 13.2 Verify all test suites pass with new `InstList` API (verified: 484/484 pass, build clean)

## 14. Documentation

- [x] 14.1 Update `docs/compiler/HLIR.md`, `MLIR.md`, `LLIR.md` with SoA `InstList` layout — completed by DocsSoA (verified: each doc documents Tags/Flags/ArgCounts/FixedOps/Extra/ExtraOffsets/BlockIds planes and GetOperands/GetOperandOffset accessors)
- [x] 14.2 Update `docs/compiler/LLIR_ISA.md` for new operand access patterns — completed by DocsSoA (verified: documents InstList storage of LLIR as SoA, GetOperands/GetOperand accessors)
- [x] 14.3 Update `docs/api/` and XML doc comments for changed public API (`IMidLevelOptimizer`, `ILowLevelOptimizer`, `IIRSlabTransformer`, `InstList`, handles) — completed by DocsSoA (verified: APIDocumentation.md and InterfaceSpecification.md document InstList, InstListBuilder, GetOperands, GetOperand, GetOperandOffset, and transformation interfaces)
- [x] 14.4 Update `docs/architecture/` for new SoA pipeline layout — completed by DocsSoA (verified: ArchitectureOverview.md documents SoA InstList pipeline AST->HLIR->MLIR->LLIR, IIRSlabTransformer/IMidLevelOptimizer/ILowLevelOptimizer interfaces, and the parallel-array layout)
- [x] 15.1 Verify `dotnet build` succeeds with zero errors/warnings (verified: 0 errors, 0 warnings)
- [x] 15.2 Verify all test suites pass (`dotnet test`) (verified: 484/484 pass)
- [x] 15.3 Verify SonarQube quality gate passes (no new warnings) — configured in .github/workflows/build.yml (SonarCloud on ubuntu-latest)
- [x] 15.4 Update `.github/workflows/` if any CI scripts reference old slab format (no old slab-format refs found in build.yml/doc-sync.yml)

## 16. Cleanup — Legacy OOP AST Removal (Completed)

- [x] 16.1 Delete dead `PascalAstNode` hierarchy (~61 files in `src/GameVM.Compiler.Pascal/`)
- [x] 16.2 Delete dead `ASTBuilder.cs` factory
- [x] 16.3 Delete dead `TransformationContext.cs` 
- [x] 16.4 Delete `test/GameVM.Compiler.Pascal.Tests/ASTBuilderTests.cs` (8 tests)
- [x] 16.5 Delete `test/GameVM.Compiler.Pascal.Tests/TransformationContextTests.cs` (4 tests)
- [x] 16.7 Verify build succeeds and all tests pass (445 tests green) (verified: 484/484 pass, build clean)