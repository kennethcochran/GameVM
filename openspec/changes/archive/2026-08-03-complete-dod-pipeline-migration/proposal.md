## Why

The current compiler pipeline mixes Data-Oriented Design (DOD) slab processing with Object-Oriented Programming (OOP) intermediate representations. While AST, HLIR, MLIR, and LLIR slabs are generated using DOD principles, the optimization passes and code generation still operate on OOP object hierarchies (HighLevelIR, MidLevelIR, LowLevelIR). This creates impedance mismatches, increases memory overhead due to object allocation, and prevents full realization of DOD benefits like cache locality and predictable performance.

## What Changes

- Update `ILanguageFrontend` interface to return IR slabs instead of OOP object hierarchies
- Modify `IMidLevelOptimizer` and `ILowLevelOptimizer` interfaces to include slab-based optimization methods
- Update `ICodeGenerator` to accept LLIR slabs for bytecode generation
- Modify `CompileUseCase` to orchestrate the full DOD pipeline: AST slab → HLIR slab → MLIR slab → LLIR slab → Bytecode
- Remove or deprecate OOP IR object classes that are no longer needed in the hot path
- Ensure all transformation and optimization steps use linear iteration with switch-based dispatch

## Capabilities

### New Capabilities
- `dod-hlir-pipeline`: HLIR slab generation and optimization using DOD principles
- `dod-mlir-pipeline`: MLIR slab generation and optimization using DOD principles  
- `dod-llir-pipeline`: LLIR slab generation and optimization using DOD principles
- `dod-code-generation`: Bytecode generation directly from LLIR slabs

### Modified Capabilities
- `dod-compiler-architecture`: Updates to require full slab-based pipeline
- `dod-compiler-application`: Updates to reflect new compiler interface contracts

## Impact

- **Core Interfaces**: `ILanguageFrontend`, `IMidLevelOptimizer`, `ILowLevelOptimizer`, `ICodeGenerator` will have modified method signatures
- **CompileUseCase**: Major refactor to use slab-based pipeline methods
- **IR Object Classes**: `HighLevelIR`, `MidLevelIR`, `LowLevelIR` classes may be deprecated or moved to legacy paths
- **Optimization Classes**: `DefaultMidLevelOptimizer`, `DefaultLowLevelOptimizer` slab methods will become primary interfaces
- **Backend**: `MidToLowLevelTransformer` and `Atari2600CodeGenerator` will need updates to work with slabs