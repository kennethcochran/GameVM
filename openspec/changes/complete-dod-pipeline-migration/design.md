## Context

The compiler currently uses a hybrid approach: DOD for slab generation (AST, HLIR, MLIR, LLIR) but OOP object hierarchies for the optimization passes and code generation. The existing DOD slab methods on `DefaultMidLevelOptimizer.OptimizeSlab`, `MidToLowLevelTransformer.TransformSlab`, and `DefaultLowLevelOptimizer.OptimizeSlab` exist but are not wired into the `CompileUseCase` pipeline.

Current pipeline: Source → AST slab → HLIR objects → MLIR objects → LLIR objects → Bytecode

Target pipeline: Source → AST slab → HLIR slab → MLIR slab → LLIR slab → Bytecode

## Goals / Non-Goals

**Goals:**
- Full DOD pipeline from AST slab to bytecode with no OOP IR objects in the hot path
- All optimization passes use linear iteration with switch-based dispatch
- Interfaces updated to support slab-based methods as primary API
- `CompileUseCase` orchestrates DOD pipeline end-to-end

**Non-Goals:**
- Complete removal of OOP IR classes (they may be needed for debugging/inspection)
- Changes to ANTLR parsing or AST slab generation (already DOD)
- Changes to capability validation system

## Decisions

### Decision 1: Interface Contract Updates
**Choice**: Modify existing interfaces to include slab methods alongside OOP methods, with slab methods becoming the primary path.
**Rationale**: Maintains backward compatibility for any external consumers while establishing DOD as primary. The `CompileUseCase` will be updated to use slab methods exclusively.

**Alternative Considered**: Create entirely new interface versions (e.g., `IMidLevelOptimizerV2`). Rejected due to added complexity and need for dual maintenance.

### Decision 2: Slab Stage Identifiers
**Choice**: Use existing IR stage identifiers (AST=0, HLIR=1, MLIR=2, LLIR=3) consistently across all slab processing.
**Rationale**: Already established in `SlabHeader` and used by existing transformers. Maintains consistency with `HlirSlabToMlirSlabTransformer` and `MidToLowLevelTransformer`.

### Decision 3: HLIR Slab Format
**Choice**: Extend existing HLIR slab format used by `CSharpAstToHlirTransformer` and `PascalAstToHlirTransformer` to be the standard for mid-level optimizer input.
**Rationale**: These transformers already produce HLIR slabs consumed by `CompileUseCase`. The `DefaultMidLevelOptimizer.OptimizeSlab` can process this format directly.

### Decision 4: LLIR Slab to Bytecode
**Choice**: Update `Atari2600CodeGenerator` to accept LLIR slabs directly, using the same linear iteration pattern as the low-level optimizer.
**Rationale**: The `DefaultLowLevelOptimizer.OptimizeSlab` already processes LLIR slabs. The code generator should consume the same format.

### Decision 5: CompileUseCase Pipeline Refactor
**Choice**: Refactor `CompileUseCase.CompileInternal` to chain slab methods:
```
astSlab = frontend.ParseToSlab(sourceCode)
hlirSlab = frontend.ConvertToHlirSlab(astSlab)  // or frontend produces HLIR directly
mlirSlab = midLevelOptimizer.OptimizeSlab(hlirSlab)  // HLIR→MLIR transform + optimize
llirSlab = mlirToLlir.TransformSlab(mlirSlab)  // MLIR→LLIR transform
optimizedLlirSlab = lowLevelOptimizer.OptimizeSlab(llirSlab)
bytecode = codeGenerator.GenerateSlab(optimizedLlirSlab)
```

### Decision 6: StringPool Threading (Identifier Resolution)
**Choice**: Thread a single shared `StringPool` through the entire DOD pipeline, from frontend through codegen. All IR stages reference identifiers by StringPool offset (integer handle) instead of string hashes.
**Rationale**: Modeled after production DOD compilers studied as references:
- **Carbon**: `SharedValueStores.IdentifierStore` threaded through lexer → check → SemIR → lower
- **Zig**: `InternPool` in `Zcu` shared across ZIR → Sema → AIR → backend
- **Odin**: `InternedString` u32 offsets into a global arena used from parser through codegen
- **LLVM/Clang**: `StringMap` + `IdentifierTable` interning, `MCSymbol` interning in `MCContext`

**Key changes:**
- `ILanguageFrontend.StringPool` property exposes the pool created during `ParseToSlab`
- `AstSlabToHlirSlabTransformer` stores `_stringPool.Intern(name)` offsets (not `.GetHashCode()`) in HLIR instructions
- `HlirSlabToMlirSlabTransformer` passes pool offsets through to MLIR unchanged
- `MidToLowLevelTransformer.TransformSlab` resolves pool offsets back to names, then maps names → zero-page addresses via `MapToAddress`
- `Atari2600CodeGenerator.GenerateFromSlab` emits actual numeric addresses/values (not hashes)

**Why not hashes**: `string.GetHashCode()` is non-reversible, so downstream stages (register allocation, address mapping, symbol emission) cannot recover the original identifier. The StringPool offset is a compact, reversible integer handle — matching the pattern of every reference compiler studied.

### Decision 7: Flat (Non-Nested) Slab Iteration
**Choice**: All transformers iterate the slab as a flat instruction stream, with `MLIR_LABEL`/`LLIR_LABEL` marking function boundaries rather than nesting bodies inside a METHOD_DECLARATION container.
**Rationale**: The HLIR slab produced by `AstSlabToHlirSlabTransformer` emits `HLIR_LABEL` followed by a flat sequence of body instructions. The mid-level optimizer, MLIR→LLIR transformer, and low-level optimizer must all iterate this flat stream and stop at the next label, rather than expecting a self-contained function body block. This matches the linear-iteration DOD pattern and avoids the empty-slab bug where the body was skipped.

## Risks / Trade-offs

[Risk] Breaking external consumers of OOP interfaces → Mitigation: Keep OOP methods on interfaces, mark as legacy, add deprecation warnings
[Risk] HLIR slab format differences between C# and Pascal → Mitigation: Ensure both frontends produce compatible HLIR slab format
[Risk] Performance regression if slab operations aren't optimized → Mitigation: Benchmark before/after; slab operations should be faster due to cache locality
[Risk] Increased complexity in debugging without OOP objects → Mitigation: Retain `SlabPrinter` utility; add debug mode that materializes objects on demand
[Risk] MLIR→LLIR transformation needs to work with slabs → Mitigation: `MidToLowLevelTransformer.TransformSlab` already exists; extend as needed