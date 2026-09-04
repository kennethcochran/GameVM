# GameVM Agent Instructions — docs/

This file provides guidance for AI assistants and coding agents working on documentation
and docs-related workflows in the GameVM project. It supplements the [root AGENTS.md](../AGENTS.md).

## Documentation Strategy

GameVM documentation lives in two persistent locations, plus an ephemeral planned-work area:

1.  **`CONTEXT.md` (Implemented Reality):** A single, high-fidelity source of truth at the repo root. It contains the glossary of terms you *must* use, the current high-level architecture diagram (AST → HLIR → MLIR → LLIR), and stabilized API interfaces. It has no "aspirational" sections.
2.  **`docs/adr/` (Architectural Decision Records):** Immutable records of *why* the code was built a certain way (e.g., choosing Struct-of-Arrays over OOP ASTs).

Planned work that does not exist yet lives as `.scratch/<feature>/spec.md` until it is implemented, then moves into `CONTEXT.md`/`docs/`.

## Mandatory Documentation Rules

**1. Never Write Aspirational Docs**
If you are designing a feature that doesn't exist yet, track it as a `.scratch/<feature>/spec.md` spec. Do **not** add files to `docs/` that describe non-existent behavior. `docs/` is strictly for **what exists**.

**2. The "True North" Update Rule**
If your code change alters the public API, the compiler pipeline (AST/HLIR/MLIR/LLIR), or the structural invariants of the compiler, you **must** update `CONTEXT.md` in the exact same session. 
*   Do not just tag sections as `[implemented]`. 
*   Rewrite the section to describe the actual code you just wrote.

**3. Vocabulary Enforcement**
You MUST use the exact terminology defined in the `## Domain Glossary` section of `CONTEXT.md`. Do not invent synonyms. Do not use alternative capitalization or hyphenation.

**4. The Doc Gate**
The `doc-sync-gate.csx` script (run by the husky `validate-docs` commit-msg hook and by CI) verifies that semantic code changes are accompanied by documentation updates. It blocks the commit unless `CONTEXT.md`/`docs/` were touched or an `override-no-doc: <reason>` is present. Never bypass it.

## Starting New Work

New work follows the Matt flow: `/grill-with-docs` → `/to-spec` → `/to-tickets` → `.scratch/`. A feature spec lives at `.scratch/<feature>/spec.md`; once implemented, its architecture/API details move into `CONTEXT.md`.


## Adding Documentation

- Follow the existing documentation structure
- Reference related documents using relative links
- Include code examples where appropriate
- Update `/docs/README.md` if adding new sections
- See `/docs/architecture/DocumentationStandards.md` for style guidelines

## Documentation References

Key documents to consult:
- **The True North**: [CONTEXT.md](../../CONTEXT.md) (Start here! This is the actual state of the project)
- **Decisions/ADRs**: [docs/adr/](adr/) (The logic behind why the code looks the way it does)
- **LLIR Specification**: [LLIR ISA](compiler/LLIR_ISA.md)
- **Inline Assembly**: [Inline Assembly Guide](compiler/InlineAssembly.md)
- **Optimization**: [Optimization Features](optimization.md)
- **Code Generation**: [Code Generation Strategies](code-generation.md)
