# ADR-003: AoS AST Container with Zig-Style Child-Index Side Buffer

The AST is a flat Array-of-Structures: fixed 16-byte node struct + a separate `int[]` side buffer for child indices. Children are referenced by index span, not by contiguous node placement.

## Context

The original AST was flattened from a tree into a stream-shaped `InstList` slab during the OOP→DOD migration. This made every tree stage (AST, HLIR) share the same stream container, but a tree is not a stream: post-order emission of legal tree shapes (e.g. a `Block` containing a nested `Block`) made children non-contiguous, and `AstBuilder` hard-asserted contiguity, crashing with `Debug.Fail` on valid input.

The question was how to represent a tree in a flat, cache-efficient, DOD-compatible way without ordering constraints on the builder.

## Decision

Adopt the Zig `Ast.zig` `extra_data`/`SubRange` pattern:

- **`AstNode`** is a 16-byte readonly struct: `{ Kind u8, Flags u16, Payload u32, FirstChild int, ChildCount int }`. Fixed stride, one flat `AstNode[]` array.
- **Child indices** live in a separate `int[]` side buffer. `FirstChild` is an offset into that buffer; `ChildCount` is the span length. `AstTree.Children(int parentIndex) → ReadOnlySpan<int>` returns the contiguous span.
- **`AstBuilder.Add(kind, flags, payload, childIndices…)`** appends child indices to the side buffer. **No ordering constraint at call sites** — children can be emitted before, after, or interleaved with parents. The old contiguity `Debug.Assert` is removed.
- **`AstTree`** is a readonly value type wrapping `{ AstNode[], int[], int count }`. Immutable after `Build()`. Surface: `Count`, `this[int]`, `Children(int)`, `GetKind(int)`.
- **`Payload`** is by-kind: `StringPool` offset for identifiers/literals, operator char for `BinaryOp`, direction for `ForStatement`.

## Why not alternatives

- **Contiguous children** (the prior design): requires children to be a adjacent run in the node array. Post-order emission of nested trees (Block→Block→Statement) makes this impossible without reordering. Crashed on legal input.
- **Pointer-based tree** (classic OOP `Left`/`Right`/`Children` refs): breaks DOD, defeats cache locality, and isn't GVM001-enforceable (value types only in IR namespaces).
- **`InstList` slab** (the degraded state): stream-shaped, loses tree locality, can't express parent→child relationships without string-soup.

## Consequences

- **Every legal tree shape is encodable** without builder-side reordering. Post-order emission just works.
- **Cache-friendly**: walking the node array touches one contiguous buffer with uniform 16-byte stride. Child lookups hit a contiguous span in the side buffer.
- **GVM001-enforced**: `.editorconfig` enforces value types in `GameVM.Compiler.Core.IR.Ast`, so `AstNode` can never become a class.
- **Shared container**: the same pattern is reused for the HLIR semantic tree (`HlirNode`/`HlirTree`/`HlirBuilder`), making AST→HLIR a structural tree-to-tree morph.
- **Reference**: Zig `lib/std/zig/Ast.zig` (`SubRange`/`extra_data`), Carbon `toolchain/parse/tree.h` (flat postorder array with implicit children).
