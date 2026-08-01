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

Run the check against your changed files:

```bash
python3 .github/scripts/doc-sync.py <changed file 1> <changed file 2> ...
```

The authoritative mapping is `.github/doc-mapping.yaml` — read it to understand
which docs map to which code paths. The common triggers:

| Code Change | Docs |
|-------------|------|
| LLIR instruction | `docs/compiler/LLIR_ISA.md`, `docs/compiler/LLIR.md` |
| HLIR/MLIR construct | `docs/compiler/HLIR.md`, `docs/compiler/MLIR.md` |
| Backend/platform | `docs/platforms/README.md`, `docs/platforms/specs/` |
| Optimizer pass | `docs/optimization.md` |
| Frontend/language | `docs/compiler/Parser.md`, `TypeSystem.md`, `LanguageIntegration.md` |
| Public API | `docs/api/` + XML doc comments |
| Architecture/pipeline | `docs/architecture/ArchitectureOverview.md`, `compiler_architecture.md` |
| Dispatch strategy | `docs/code-generation.md` |
| Capability profile | `docs/platforms/CapabilityProfiles.md` |
| Build/tooling | `docs/compiler/BuildSystem.md` |

### 2. Update each affected doc

For each doc flagged by the script (or mapped in the YAML):

1. **Read** the file first — never guess its current content.
2. **Tag each section** with the status convention:
   - `[implemented]` — feature exists in code
   - `[aspirational]` — planned, not built
   - `[outdated]` — built differently than documented (or removed); describe the replacement, don't delete
3. **Update** code examples and API references to match the current implementation.
4. **Verify** relative links still resolve and headers still exist.
5. **Update** the document's changelog section (if present).

### 3. Verify

Re-run the check with your changed files (code + updated docs). It should report
`OK`. Example:

```bash
python3 .github/scripts/doc-sync.py \
  src/GameVM.Compiler.Core/IR/LowLevelIR.cs \
  docs/compiler/LLIR_ISA.md \
  docs/compiler/LLIR.md
# => doc-sync: OK - all code changes are accompanied by documentation updates.
```

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
doc-sync check: OK
```
