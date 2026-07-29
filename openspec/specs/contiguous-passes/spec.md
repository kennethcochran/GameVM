# contiguous-passes Specification

## Purpose
TBD - created by archiving change redesign-compiler-pipeline-dod. Update Purpose after archive.
## Requirements
### Requirement: Linear slab processing
Semantic analysis and optimization passes MUST iterate through the instruction slab linearly by reading self-describing blocks sequentially, instead of traversing the logical tree hierarchy recursively.

#### Scenario: Applying a constant folding pass
- **WHEN** constant folding is executed
- **THEN** it iterates sequentially through the slab, reading each instruction's metadata header to identify operations where both operands are resolved constants, without tree traversal.

### Requirement: Elimination of standard visitors
The compiler passes MUST NOT use the `ASTVisitor` object-oriented pattern, and `Accept/Visit` methods MUST be removed.

#### Scenario: Refactoring PascalToHlirTransformer
- **WHEN** migrating `PascalToHlirTransformer`
- **THEN** it is rewritten to use a linear loop over the slab with a switch statement on decoded instruction kinds rather than inheriting from `ASTVisitor`.

