## MODIFIED Requirements

### Requirement: DOD MLIR Structure
**Modified:** MLIR body is an `InstList` with `tags[]`, `flags[]`, `argCounts[]`, `fixedOps[]`, `extra[]`, `blockIds[]`. Operand references use `InstIndex` handles; branch targets use `BlockId` handles. Local slots use separate `SlotIndex` handles.

#### Scenario: Creating MLIR instructions
- **WHEN** the `HlirToMlirTransformer` processes HLIR nodes
- **THEN** it allocates MLIR instruction blocks in the target `InstList` and returns `InstIndex` handles.

### Requirement: Offset and Block-based MLIR References
**Modified:** Operands use `InstIndex` handles; branch/jump targets use `BlockId` handles; SSA virtual registers replaced by `SlotIndex` handles.

#### Scenario: Building MLIR control flow
- **WHEN** transforming HLIR control structures (if, while) to MLIR
- **THEN** branch instructions store stable `BlockId` handles rather than absolute slab offsets or label object references.

### Requirement: Local Slot Cross-Block References
**Modified:** Cross-block value dependencies use `SlotIndex` handles (index into a per-function slot array) instead of SSA virtual registers or raw slab offsets.

#### Scenario: Cross-block value dependencies
- **WHEN** an MLIR instruction needs a value computed in a different basic block
- **THEN** it references the value using a `SlotIndex` handle (index into per-function slot array) rather than SSA virtual register ID or raw slab offset.

### Requirement: Linear MLIR Transformation
**Modified:** The `HlirToMlirTransformer` MUST process HLIR `InstList` using linear iteration with switch statements instead of recursive tree traversal with visitor patterns.

#### Scenario: Processing HLIR modules
- **WHEN** transforming HLIR modules to MLIR
- **THEN** the transformer iterates linearly through `sourceInstList.Tags[]` with a switch on decoded instruction kinds, appends to target `InstList`.

### Requirement: MLIR Instruction Enum
**Modified:** MLIR instructions use `MlirInstructionKind` enum stored directly in `InstList.Tags[]` (u8) rather than relying on class polymorphism and `is`/`as` operators.

#### Scenario: Dispatching MLIR instruction processing
- **WHEN** an optimizer processes MLIR instructions
- **THEN** it reads `instList.Tags[i]` (u8 `MlirInstructionKind`) and switches on it instead of using virtual method dispatch.

### Requirement: Contiguous MLIR Optimization
**Modified:** Mid-level optimization passes iterate `InstList.Tags[]` / `FixedOps[]` spans linearly with SIMD-friendly stride.

#### Scenario: Applying constant propagation
- **WHEN** constant propagation is executed on MLIR
- **THEN** it iterates sequentially through `instList.Tags[]`, reading each instruction's kind and fixed operands to identify assignments with constant sources, without graph traversal.