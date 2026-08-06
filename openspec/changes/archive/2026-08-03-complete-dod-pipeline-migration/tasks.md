## 1. Interface Updates

- [x] 1.1 Update `ILanguageFrontend` to include slab-based methods
- [x] 1.2 Update `IMidLevelOptimizer` to prioritize slab-based methods
- [x] 1.3 Update `ILowLevelOptimizer` to prioritize slab-based methods
- [x] 1.4 Update `ICodeGenerator` to include slab-based generation method

## 2. Compiler Pipeline Refactor

- [x] 2.1 Refactor `CompileUseCase` to use slab-based pipeline
- [x] 2.2 Update C# frontend to produce HLIR slabs consistently (ParseToSlab, ConvertToHlirSlab)
- [x] 2.3 Update Pascal frontend to produce HLIR slabs consistently (ConvertToHlirSlab)
- [x] 2.4 Verify HLIR slab format compatibility between frontends

## 3. Transformer Updates

- [x] 3.1 Enhance `MidToLowLevelTransformer.TransformSlab` to handle full MLIR→LLIR transformation
- [x] 3.2 Ensure `CSharpAstToHlirTransformer` and `PascalAstToHlirTransformer` produce compatible HLIR slabs (both now use AstSlabToHlirSlabTransformer for DOD pipeline)
- [x] 3.5 Validate slab-based transformers work in isolation

## 4. Code Generator Updates

- [x] 4.1 Update `Atari2600CodeGenerator` to accept LLIR slabs
- [x] 4.2 Verify codegen output produces correct 6502 instructions for all spec test scenarios
- [x] 4.3 Add zero-page addressing for addresses < 0x100 and JMP self-loop for program termination

## 5. Integration & Validation

- [x] 5.1 Update `CompileUseCase` to orchestrate full DOD slab pipeline
- [x] 5.2 Run existing test suite to ensure no regressions (590/590 tests pass)
- [x] 5.3 Add performance benchmarks comparing OOP vs slab pipeline
- [x] 5.4 Verify debuggability with `SlabPrinter` and other tools

## 6. Cleanup & Deprecation

- [x] 6.1 Mark OOP interface methods as legacy/deprecated
- [x] 6.2 Document migration path for external consumers
- [x] 6.3 Consider moving unused OOP IR classes to legacy namespace

## 7. StringPool Threading (per DOD reference compiler analysis)

- [x] 7.1 Thread `StringPool` through entire pipeline: Frontend → AstSlabToHlirTransformer → HlirSlabToMlirTransformer → MidToLowLevelTransformer → CodeGenerator
- [x] 7.2 Fix `AstSlabToHlirSlabTransformer` to store StringPool offsets (not hash codes) for variable names
- [x] 7.3 Fix `HlirSlabToMlirSlabTransformer` to pass StringPool offsets through to MLIR
- [x] 7.4 Fix `MidToLowLevelTransformer` to resolve StringPool offsets to addresses via MapToAddress
- [x] 7.5 Fix flat slab iteration in all transformers (MLIR→LLIR and HLIR→MLIR) to handle the non-nested instruction format
