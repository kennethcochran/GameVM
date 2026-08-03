## MODIFIED Requirements

### Requirement: Linear slab processing
**Modified:** All passes iterate `InstList.Tags[]` / `FixedOps[]` spans linearly with SIMD-friendly stride; `SlabIterator` replaced by direct `for` loops.

#### Scenario: Applying a constant folding pass
- **WHEN** constant folding is executed
- **THEN** it iterates sequentially through `instList.Tags[]`, reading each instruction's kind and fixed operands to identify operations where both operands are resolved constants, without tree traversal.

### Requirement: Elimination of standard visitors
**Modified:** All `ASTVisitor`/`Accept/Visit` removed. Switch-based dispatch on `instList.Tags[i]` is the only pattern.

#### Scenario: Refactoring PascalToAstTransformer
- **WHEN** migrating `PascalToAstTransformer` (AST stage now emits `InstList` directly)
- **THEN** it is rewritten to use a linear loop over `sourceInstList.Tags[]` with a switch on decoded instruction kinds, appending to target `InstList` via `InstListBuilder`.

#### Scenario: Refactoring PascalToHlirTransformer
- **WHEN** migrating `PascalToHlirTransformer`
- **THEN** it is rewritten to use a linear loop over `sourceInstList.Tags[]` with a switch on decoded instruction kinds, appending to target `InstList` via `InstListBuilder`.