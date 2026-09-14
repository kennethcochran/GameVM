# ADR-006: Duplicate AST Container Types into Each Frontend

`AstNode`/`AstTree`/`AstBuilder` are duplicated verbatim into both frontend assemblies — `GameVM.Compiler.Pascal.Ast` and `GameVM.Compiler.CSharp.Ast` — rather than shared in a single AST assembly or referenced from Core.

## Context

ADR-004 made the parse tree frontend-internal: `ILanguageFrontend` exposes `string → HlirTree`, and the AST never crosses the interface boundary. That left a ownership question for the `AstNode`/`AstTree`/`AstBuilder` container types, which previously lived in `GameVM.Compiler.Core.IR.Ast`.

The type belongs in the frontends (it is a frontend-internal parse-tree container). But there were two structurally different ways to split it across the two frontends: share one copy, or duplicate.

## Decision

Duplicate the container into each frontend. `AstNode.cs` and `AstBuilder.cs` exist identically in `GameVM.Compiler.Pascal` (namespace `GameVM.Compiler.Pascal.Ast`) and `GameVM.Compiler.CSharp` (namespace `GameVM.Compiler.CSharp.Ast`). Core holds only the HLIR tree container (`HlirNode`/`HlirTree`/`HlirBuilder` in `GameVM.Compiler.Core.IR.Hlir`/`Soa`).

## Why not alternatives

- **Shared `GameVM.Compiler.Frontend.Ast` assembly**: rejected. It would force both frontends to reference a third assembly, and the AST container carries no cross-frontend shared logic worth the dependency. The two ASTs diverge only in name and will diverge independently as each frontend evolves.
- **Keep in Core (`GameVM.Compiler.Core.IR.Ast`)**: rejected. Violates ADR-004's rule that parse-tree types are frontend-internal and must not be part of Core's public surface.

## Consequences

- **Duplicated code**: the two `AstNode.cs`/`AstBuilder.cs` are identical today. Changes to the container pattern must be applied in both frontends. This is accepted: the shared-assembly alternative would reintroduce a Core-described cross-frontend dependency that ADR-004 removes.
- **Each frontend owns its parse tree**: no frontend references Core for AST types; no shared AST assembly; frontends are structurally independent.
- **Frontend divergence is a feature, not drift**: if Pascal and C# ASTs evolve differently, no shared contract constrains them. Only a real shared AST concept would justify a shared assembly.