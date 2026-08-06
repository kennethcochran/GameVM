# GameVM Agent Instructions — docs/

This file provides guidance for AI assistants and coding agents working on documentation
and docs-related workflows in the GameVM project. It supplements the [root AGENTS.md](../AGENTS.md).

## Documentation Strategy: The Three-Pocket System

To solve documentation drift and keep AI agents focused, GameVM uses a strict "Three-Pocket" strategy:

1.  **`CONTEXT.md` (Implemented Reality):** A single, high-fidelity source of truth at the repo root. It contains the glossary of terms you *must* use, the current high-level architecture diagram (AST → HLIR → MLIR → LLIR), and stabilized API interfaces. It has no "aspirational" sections.
2.  **`openspec/specs/<id>/spec.md` (Aspirational/Planned):** Features that do not exist yet (e.g., the Hardware Abstraction Layer, Package Management, or the VM Runtime). They are **prohibited** from living in the `docs/` folder until they are implemented.
3.  **`docs/adr/` (Architectural Decision Records):** Immutable records of *why* the code was built a certain way (e.g., choosing Struct-of-Arrays over OOP ASTs).

## Mandatory Documentation Rules

**1. Never Write Aspirational Docs**
If you are designing a feature that doesn't exist yet, write a design document or a spec in `openspec/`. Do **not** add files to `docs/` that describe non-existent behavior. `docs/` is strictly for **what exists**.

**2. The "True North" Update Rule**
If your code change alters the public API, the compiler pipeline (AST/HLIR/MLIR/LLIR), or the structural invariants of the compiler, you **must** update `CONTEXT.md` in the exact same session. 
*   Do not just tag sections as `[implemented]`. 
*   Rewrite the section to describe the actual code you just wrote.

**3. Vocabulary Enforcement**
You MUST use the exact terminology defined in the `## Domain Glossary` section of `CONTEXT.md`. Do not invent synonyms. Do not use alternative capitalization or hyphenation.

**4. The `gstack document-release` Gate**
Before completing a coding session, you must run `gstack document-release`. 
This tool scans the git diff, compares it against the Diataxis coverage map (reference / how-to / tutorial / explanation) of your `CONTEXT.md` and related docs, and flags any drift. If it catches a missing update, you must fix it before committing your final solution.

## The OpenSpec Workflow for New Work

When working on a new feature under `openspec/changes/`:
*   The feature starts as a spec in `openspec/specs/`.
*   Once the implementation is complete, **copy the relevant architecture/API details from the feature's spec and paste them into `CONTEXT.md`**, converting them from aspirational to implemented.
*   Delete or archive the feature's spec once it is merged into `CONTEXT.md`.

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
