# dod-mlir-transformation Specification

## Purpose
TBD - created by archiving change redesign-compiler-pipeline-dod. Update Purpose after archive.
## Requirements
### Requirement: DOD MLIR Structure
The Mid-Level Intermediate Representation (MLIR) MUST use a single unified memory slab with self-describing instruction blocks instead of object-oriented node hierarchies.

#### Scenario: Creating MLIR instructions
- **WHEN** the `HlirToMlirTransformer` processes HLIR nodes
- **THEN** it allocates MLIR instruction blocks in the slab and returns their offsets

### Requirement: Offset and Block-based MLIR References
MLIR instructions within a local block MUST reference operands using 32-bit offsets within the slab, while control flow branch/jump instructions MUST reference stable Basic Block IDs.

#### Scenario: Building MLIR control flow
- **WHEN** transforming HLIR control structures (if, while) to MLIR
- **THEN** branch instructions store stable Basic Block IDs rather than absolute slab offsets or label object references.

### Requirement: Local Slot Cross-Block References
MLIR instructions MUST use local slot indices for cross-block dependencies instead of SSA virtual registers or raw slab offsets.

#### Scenario: Cross-block value dependencies
- **WHEN** an MLIR instruction needs a value computed in a different basic block
- **THEN** it references the value using a local slot index (abstract stack/local variable array) rather than SSA virtual register ID or raw slab offset.

### Requirement: Linear MLIR Transformation
The `HlirToMlirTransformer` MUST process HLIR slab using linear iteration with switch statements instead of recursive tree traversal with visitor patterns.

#### Scenario: Processing HLIR modules
- **WHEN** transforming HLIR modules to MLIR
- **THEN** the transformer iterates linearly through the HLIR slab using a switch on decoded node types

### Requirement: MLIR Instruction Enum
MLIR instructions MUST use an enum discriminator (`MlirInstructionKind`) encoded in the metadata header to denote instruction type rather than relying on class polymorphism and `is`/`as` operators.

#### Scenario: Dispatching MLIR instruction processing
- **WHEN** an optimizer processes MLIR instructions
- **THEN** it decodes the metadata header to get `instruction.Kind` (the enum value) and switches on it instead of using virtual method dispatch

### Requirement: Contiguous MLIR Optimization
Mid-level optimization passes MUST iterate through the MLIR slab linearly whenever the operation is order-independent, instead of traversing logical control flow graphs recursively.

#### Scenario: Applying constant propagation
- **WHEN** constant propagation is executed on MLIR
- **THEN** it iterates sequentially through the slab, reading each instruction's metadata header to identify assignments with constant sources, without graph traversal

