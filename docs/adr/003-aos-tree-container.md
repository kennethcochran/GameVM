# ADR-003: AoS Tree Container with Zig-Style Child-Index Side Buffer

Tree structures in the compiler use a flat Array-of-Structures layout: fixed-size node struct + a separate `int[]` side buffer for child indices. Children are referenced by index span, not by contiguous node placement. This pattern is used for both parse trees (frontend-private) and the HLIR semantic tree (Core, universal IR).

## Context

The original tree stages were flattened into a stream-shaped `InstList` slab during the OOP→DOD migration. A tree is not a stream: post-order emission of legal tree shapes (e.g. a `Block` containing a nested `Block`) made children non-contiguous, and `AstBuilder` hard-asserted contiguity, crashing with `Debug.Fail` on valid input.

The question was how to represent a tree in a flat, cache-efficient, DOD-compatible way without ordering constraints on the builder.

## Decision

Adopt the Zig `Ast.zig` `extra_data`/`SubRange` pattern for all tree types:

- **Node struct** is fixed-size (16 bytes): `{ Kind u8, Flags u16, Payload u32, FirstChild int, ChildCount int }`. Fixed stride, one flat array.
- **Child indices** live in a separate `int[]` side buffer. `FirstChild` is an offset into that buffer; `ChildCount` is the span length. `Children(int parentIndex) → ReadOnlySpan<int>` returns the contiguous span.
- **Builder.Add(kind, flags, payload, childIndices…)** appends child indices to the side buffer. **No ordering constraint at call sites** — children can be emitted before, after, or interleaved with parents. The old contiguity `Debug.Assert` is removed.
- **Tree value type** wraps `{ Node[], int[], int count }`. Immutable after `Build()`. Surface: `Count`, `this[int]`, `Children(int)`, `GetKind(int)`.
- **Payload** is by-kind: `StringPool` offset for identifiers/literals, operator char for `BinaryOp`, direction for `ForStatement`.

This pattern is instantiated twice:
- **HLIR** (`HlirNode`/`HlirTree`/`HlirBuilder` in Core) — the universal IR, shared across all frontends
- **AST** (e.g. `AstNode`/`AstTree`/`AstBuilder`) — parse-tree containers, owned by each frontend

## Why not alternatives

- **Contiguous children** (the prior design): requires children to be an adjacent run in the node array. Post-order emission of nested trees (Block→Block→Statement) makes this impossible without reordering. Crashed on legal input.
- **Pointer-based tree** (classic OOP `Left`/`Right`/`Children` refs): breaks DOD, defeats cache locality, and isn't GVM001-enforceable (value types only in IR namespaces).
- **`InstList` slab** (the degraded state): stream-shaped, loses tree locality, can't express parent→child relationships without string-soup.

## Consequences

- **Every legal tree shape is encodable** without builder-side reordering. Post-order emission just works.
- **Cache-friendly**: walking the node array touches one contiguous buffer with uniform 16-byte stride. Child lookups hit a contiguous span in the side buffer.
- **GVM001-enforced**: `.editorconfig` enforces value types in IR namespaces, so node structs can never become classes.
- **Reference**: Zig `lib/std/zig/Ast.zig` (`SubRange`/`extra_data`), Carbon `toolchain/parse/tree.h` (flat postorder array with implicit children).
