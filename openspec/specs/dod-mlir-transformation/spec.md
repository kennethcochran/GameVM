# dod-mlir-transformation Specification

## Purpose
The HLIR-to-MLIR transformation uses a Struct-of-Arrays InstList representation throughout. IR is represented as parallel arrays (`byte[] Tags`, `ushort[] Flags`, `ushort[] ArgCounts`, `uint[] FixedOps`, `uint[] Extra`, `uint[] ExtraOffsets`, `int[] BlockIds`). Metadata is stored per-index across these arrays, not in a packed header per block.

## Requirements
### Requirement: DOD MLIR Structure
The Mid-Level Intermediate Representation (MLIR) MUST be a Struct-of-Arrays `InstList` with parallel primitive arrays for instruction properties, not object-oriented node hierarchies.

#### Scenario: Creating MLIR instructions
- **WHEN** the `HlirToMlirTransformer` processes HLIR nodes
- **THEN** it appends to the MLIR `InstList` via `InstListBuilder`, writing to `Tags[i]`, `Flags[i]`, `ArgCounts[i]`, `FixedOps[...]`, `Extra[...]`, `ExtraOffsets[i]`, `BlockIds[i]`.

### Requirement: Offset and Block-based MLIR References
MLIR instructions within a local block MUST reference operands using 32-bit offsets within the slab (`InstIndex` or `StringPool` offsets), while control flow branch/jump instructions MUST reference stable Basic Block IDs via `BlockId` handles.

#### Scenario: Building MLIR control flow
- **WHEN** transforming HLIR control structures (if, while) to MLIR
- **THEN** branch instructions store stable `BlockId` handles rather than absolute slab offsets or label object references.

### Requirement: Local Slot Cross-Block References
MLIR instructions MUST use local slot indices for cross-block dependencies instead of SSA virtual registers or raw slab offsets.

#### Scenario: Cross-block value dependencies
- **WHEN** an MLIR instruction needs a value computed in a different basic block
- **THEN** it references the value using a local slot index (abstract stack/local variable array) rather than SSA virtual register ID or raw slab offset.

### Requirement: Linear MLIR Transformation
The `HlirToMlirTransformer` MUST process HLIR slab using linear iteration with switch statements instead of recursive tree traversal with visitor patterns.

#### Scenario: Processing HLIR modules
- **WHEN** transforming HLIR modules to MLIR
- **THEN** the transformer iterates linearly through the HLIR slab using a switch on decoded instruction kinds.

### Requirement: MLIR Instruction Enum
MLIR instructions MUST use an enum discriminator (`MlirInstructionKind`) encoded in the `Tags` array to denote instruction type rather than relying on class polymorphism and `is`/`as` operators.

#### Scenario: Dispatching MLIR instruction processing
- **WHEN** an optimizer processes MLIR instructions
- **THEN** it reads the `Tags` array and switches on the enum value instead of using virtual method dispatch.

### Requirement: Contiguous MLIR Optimization
Mid-level optimization passes MUST iterate through the MLIR slab linearly whenever the operation is order-independent, instead of traversing logical control flow graphs recursively.

#### Scenario: Applying constant propagation
- **WHEN** constant propagation is executed on MLIR
- **THEN** it iterates sequentially through the slab, reading each instruction's `Tags[i]` to identify assignments with constant sources, without graph traversal.

