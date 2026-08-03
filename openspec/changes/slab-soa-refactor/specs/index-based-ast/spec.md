## MODIFIED Requirements

### Requirement: Offset-based References
**Modified:** All stages (AST, HLIR, MLIR, LLIR) use `InstIndex` handles for instruction references instead of 32-bit slab offsets.

#### Scenario: Building a binary expression
- **WHEN** a parser processes an addition operation (`A + B`)
- **THEN** it stores the `InstIndex` of `A` and `B` inside the `BinaryOp` node block in the AST `InstList`; all stages use `InstIndex` handles uniformly.

### Requirement: Enum-based Node Typing
**Modified:** `InstList.Tags` array uses `AstNodeKind` / `MlirInstructionKind` / `LlirInstructionKind` enums directly (u8) instead of metadata bit-packing, uniformly across all stages.

#### Scenario: Dispatching based on node type
- **WHEN** an evaluator or code generator processes a node
- **THEN** it reads `instList.Tags[i]` (u8 enum) and switches on it instead of relying on virtual `Accept(Visitor)` dispatch or bit-packed metadata decode.