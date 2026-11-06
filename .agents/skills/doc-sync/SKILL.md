---
name: doc-sync
description: Sync documentation with code changes. Use after making code changes to ensure the affected docs are updated, tagged, and consistent. Triggers from AGENTS.md "Documentation Update Rules". Use when asked to "sync docs", "update documentation", "check doc mapping", or after any code edit that touches IR, frontends, backends, optimizers, public APIs, or the build.
---

# Doc-Sync Skill

Ensures every code change is accompanied by the documentation updates it requires.

## When to Use

- After editing code in `src/` (or tests that exercise documented behavior).
- When starting a task that modifies compiler architecture, IR, dispatch, capability profiles, or public API.
- Whenever a user asks to "sync docs", "update docs for X", or "check the documentation".

## Workflow

### 1. Determine which docs are affected

Consult `docs/DOC-IMPACT.md` to decide which documentation a change requires. The common triggers:

| Code Change | Docs |
|-------------|------|
| LLIR instruction | `docs/compiler/LLIR_ISA.md`, `docs/compiler/LLIR.md` |
| HLIR/MLIR construct | `docs/compiler/HLIR.md`, `docs/compiler/MLIR.md` |
| Backend/platform | `docs/platforms/README.md`, `docs/platforms/specs/` |
| Optimizer pass | `docs/optimization.md` |
| Frontend/language | `docs/compiler/Parser.md`, `TypeSystem.md`, `LanguageIntegration.md` |
| Public API | `CONTEXT.md` + XML doc comments |
| Architecture/pipeline | `CONTEXT.md`, `docs/architecture/` |
| Dispatch strategy | `docs/code-generation.md` |
| Capability profile | `docs/platforms/CapabilityProfiles.md` |
| Build/tooling | `docs/compiler/BuildSystem.md` |

### 2. Update each affected doc

For each affected doc:

1. **Read** the file first — never guess its current content.
2. **Tag each section** with the status convention:
   - `[implemented]` — feature exists in code
   - `[aspirational]` — planned, not built (tracked under `.scratch/`, not `docs/`)
   - `[outdated]` — built differently than documented (or removed); describe the replacement, don't delete
3. **Update** code examples and API references to match the current implementation.
4. **Verify** relative links still resolve and headers still exist.
5. **Update** the document's changelog section (if present).

Implemented behavior lands in `CONTEXT.md`/`docs/adr/`/`docs/` in the same commit; planned work lives as failing test files (NUnit for unit behavior, Reqnroll for acceptance/end-to-end).

### 3. Verify the updates

Re-read each updated doc and confirm the tags are accurate, the links resolve, and the headers still exist. The doc-sync gate that used to verify this was disabled 2026-11-06; the script remains in `.github/scripts/` for a later readdress.

## Rules of Thumb

- **Err on the side of updating docs.** If in doubt, update.
- **When NOT to update**: pure refactors with no behavior/API change (verify by reading the docs), test-only coverage additions, dependency bumps with no API change. Note this explicitly rather than skipping silently.
- **Never silently delete coverage** — tag `[outdated]` and describe the replacement.
- **Keep tags accurate**: check the actual source in `src/` and tests in `test/` before deciding `[implemented]` vs `[aspirational]` vs `[outdated]`.

## Output

Report what you updated, e.g.:

```
Updated docs/compiler/LLIR.md (added X instruction, tagged [implemented])
Updated docs/compiler/LLIR_ISA.md (opcode table, tagged [implemented])
```
