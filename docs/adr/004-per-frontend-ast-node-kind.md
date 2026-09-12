# ADR-004: Per-Frontend AstNodeKind Enums

Each language frontend defines its own `AstNodeKind` byte enum (`PascalAstNodeKind`, `CSharpAstNodeKind`) in its own assembly. There is no shared `AstNodeKind` in Core.

## Context

The compiler supports multiple frontends (Pascal, C#) that each produce an `AstTree` from their language's parse tree. The `AstTree` container is shared (Core), but the node vocabulary — what kinds of nodes exist and what they mean — is language-specific. Pascal has `Program`, `Begin`, `Case`, `With`, `Goto`; C# has `Class`, `Struct`, `Interface`, `Lambda`, `Switch`. A shared enum would either be a union of every language's vocabulary (bloated, meaningless for any one language) or force Core to reference the frontend assemblies.

## Decision

- `PascalAstNodeKind : byte` lives in `GameVM.Compiler.Pascal` with Pascal's parse vocabulary.
- `CSharpAstNodeKind : byte` lives in `GameVM.Compiler.CSharp` with C#'s parse vocabulary.
- `AstNode.Kind` is a raw `byte`; each frontend casts it to its own enum when walking the tree.
- The old shared `GameVM.Compiler.Core.IR.Enums.AstNodeKind` (Pascal-flavored: `Program`, `Begin`, `Case`, `With`, `Goto`) is deleted.
- Each frontend's `AstToHlirTransformer` (`PascalAstToHlirTransformer`, `CSharpAstToHlirTransformer`) reads its own kind enum. There is no shared `AstSlabToHlirSlabTransformer`.

## Why not alternatives

- **Shared `AstNodeKind` in Core**: Core cannot reference the frontend assemblies (circular dependency). Even if it could, a shared enum would be either the union of all languages (every frontend pays for every other language's vocabulary) or the intersection (missing language-specific nodes).
- **Polymorphic node types** (OOP hierarchy): breaks DOD, defeats GVM001 value-type enforcement, and loses the fixed-stride flat array.

## Consequences

- **No circular dependencies**: Core defines the container (`AstNode`, `AstTree`, `AstBuilder`); frontends define their own vocabulary. The dependency graph is one-way: Frontend → Core.
- **Clean separation**: adding a new language frontend means adding a new `XxAstNodeKind` enum and a new `XxAstToHlirTransformer`, with zero changes to Core.
- **Trade-off**: `AstNode.Kind` is an untyped `byte` at the container level. Type safety is restored at the transformer boundary by casting to the frontend-specific enum. This is intentional — the container is language-neutral, and type safety at the wrong layer would reintroduce the circular dependency.
