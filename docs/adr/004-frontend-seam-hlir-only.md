# ADR-004: Frontend Seam Exposes HLIR, Not AST

`ILanguageFrontend` exposes a single method: `string → HlirTree`. The parse tree (`AstTree`) is frontend-internal and does not cross the interface boundary.

## Context

HLIR is the universal IR — the shared type system and ABI for all modules regardless of source language. Every frontend produces `HlirTree`; the middle end, optimizers, and backends operate exclusively on HLIR and its lowered derivatives (MLIR, LLIR). This is the shared-IR architecture: same pattern as GCC's `tree`, GraalVM's `Node`, Roslyn's `SyntaxTree`.

The original interface exposed two steps:

```csharp
AstTree ParseToSlab(string sourceCode);
HlirTree ConvertToHlirSlab(AstTree astTree);
```

This put `AstTree` (a parse-tree type) in the interface contract of a shared-IR compiler. Nothing downstream of the frontend reads `AstTree` — only the frontend's own transformer does. The two-step design leaked a frontend-internal type into `CompileUseCase` and the interface boundary.

## Decision

Collapse the interface to a single method:

```csharp
HlirTree ParseToHlir(string sourceCode);
IReadOnlyList<string>? LastParseErrors { get; }
StringPool? StringPool { get; }
```

Each frontend owns its parse tree internally (`AstTree`, or any other type) and transforms it to `HlirTree` before returning. The parse tree never crosses the interface boundary.

## Why not alternatives

- **Two-step interface** (`ParseToSlab` + `ConvertToHlirSlab`): exposes `AstTree` in the contract. `CompileUseCase` holds an intermediate type it never uses downstream. Tests that need parse-tree inspection should test the frontend directly, not through the interface.
- **`string → InstList`** (MLIR): skips the semantic tree, losing the type information and tree structure that HLIR carries. The whole point of HLIR is to be the universal semantic layer before lowering to instruction streams.

## Consequences

- **Clean seam**: the interface boundary is `string → HlirTree`. Frontend internals (parse tree format, visitor, transformer) are fully encapsulated.
- **CompileUseCase simplifies**: one fewer intermediate variable, one fewer branch on empty result.
- **Parse-tree tests move to frontend assemblies**: tests that inspect `AstTree` structure test the frontend's parser directly, not through `ILanguageFrontend`. This is correct — the parse tree is a frontend concern.
- **Trade-off lost**: `CompileUseCase` can no longer inspect the parse tree for debugging. This is the same trade-off Clang makes (AST is frontend-internal, LLVM only sees `Module`). The parse tree is available via the frontend's public API for tests and tooling, just not through the compilation pipeline interface.
