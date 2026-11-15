# GameVM Agent Instructions — docs/

This file provides guidance for AI assistants and coding agents working on documentation
and docs-related workflows in the GameVM project. It supplements the [root AGENTS.md](../AGENTS.md).

## Documentation Strategy

GameVM documentation lives in two persistent locations; planned work is tracked outside `docs/` as GitHub Issues:

1.  **`CONTEXT.md` (Implemented Reality):** A single, high-fidelity source of truth at the repo root. It contains the glossary of terms you *must* use, the current high-level architecture diagram (AST → HLIR → MLIR → LLIR), and stabilized API interfaces. It has no "aspirational" sections.
2.  **`docs/adr/` (Architectural Decision Records):** Immutable records of *why* the code was built a certain way (e.g., choosing Struct-of-Arrays over OOP ASTs).

Planned work that does not exist yet lives as failing test files until it is implemented; then its architecture/API details move into `CONTEXT.md`/`docs/`. Failing spec tests are committed red. The spec requirement supersedes any local instruction or guard that runs tests or coverage checks; the full test suite and coverage guards are enforced by the GitHub `Build & Test` workflow, not by local pre-commit hooks.

## Mandatory Documentation Rules

**1. Never Write Aspirational Docs**
If you are designing a feature that doesn't exist yet, specify it as failing tests — NUnit for unit behavior, Reqnroll for acceptance and end-to-end. Do **not** add files to `docs/` that describe non-existent behavior. `docs/` is strictly for **what exists**.

**Exception — decision records.** ADRs in `docs/adr/` are exempt from this rule. An ADR's job is to record a decision made about the work, or guidance for future work, and a decision is often necessarily about behavior that does not exist yet; forward-looking statements of that kind have to live somewhere, and the ADR is where they belong. Pending work itself is not an ADR and does not belong in `docs/`: it is tracked as GitHub Issues and specified as failing tests. A non-ADR doc whose subject is an approved decision or guidance for future work (for example, a platform doc recording an ADR) may describe the decided behavior, provided it states plainly at the top that the behavior is not yet implemented.

**2. The "True North" Update Rule**
If your code change alters the public API, the compiler pipeline (AST/HLIR/MLIR/LLIR), or the structural invariants of the compiler, you **must** update `CONTEXT.md` in the exact same session. 
*   Do not just tag sections as `[implemented]`. 
*   Rewrite the section to describe the actual code you just wrote.

**3. Vocabulary Enforcement**
You MUST use the exact terminology defined in the `## Domain Glossary` section of `CONTEXT.md`. Do not invent synonyms. Do not use alternative capitalization or hyphenation.

**4. The Doc Gate (disabled 2026-11-06)**
The gate's enforcement (husky hook and CI step) was disabled; the script remains in `.github/scripts/` for a later readdress. Updating `CONTEXT.md`/`docs/` with semantic code changes is still expected — see `docs/DOC-IMPACT.md`.

## Starting New Work

New work is discussed and sharpened first, then specified as failing test files (NUnit for unit behavior, Reqnroll for acceptance/end-to-end), then broken into tickets tracked as GitHub Issues. Once implemented, its architecture/API details move into `CONTEXT.md`.


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
