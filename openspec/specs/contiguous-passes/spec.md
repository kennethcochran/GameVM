# contiguous-passes Specification

## Purpose
All semantic analysis and optimization passes operate on Struct-of-Arrays `InstList` instances using stride-only linear iteration over parallel arrays (`Tags`, `Flags`, `ArgCounts`, `FixedOps`, `Extra`, `ExtraOffsets`, `BlockIds`). There are no self-describing metadata headers, no bit-packed blocks, and no visitor-pattern traversals.

## Requirements
### Requirement: Linear slab processing
Semantic analysis and optimization passes MUST iterate through the instruction slab linearly by reading parallel field arrays (`Tags`, `Flags`, `ArgCounts`, `BlockIds`) sequentially, instead of traversing logical tree hierarchies recursively.

#### Scenario: Applying a constant folding pass
- **WHEN** constant folding is executed
- **THEN** it iterates `for (int i = 0; i < instList.Count; i++)` reading `Tags[i]`, `Flags[i]`, `ArgCounts[i]` to identify operations where both operands are resolved constants, without tree traversal.

### Requirement: Elimination of standard visitors
The compiler passes MUST NOT use the `ASTVisitor` object-oriented pattern, and `Accept/Visit` methods MUST be removed.

#### Scenario: Refactoring PascalToHlirTransformer
- **WHEN** migrating `PascalToHlirTransformer`
- **THEN** it is rewritten to use a linear loop over the slab with a switch statement on decoded instruction kinds rather than inheriting from `ASTVisitor`.

