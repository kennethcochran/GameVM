---
name: doc-sync
description: Enforce documentation updates using the doc-sync-gate.csx hook. This skill ensures all code changes are accompanied by relevant documentation updates in CONTEXT.md and docs/adr/. It is triggered by AGENTS.md "Documentation Update Rules" and should be run before committing any significant code changes.
---

# Doc-Sync Skill

This skill ensures every code change is accompanied by the necessary documentation updates, verified by the `doc-sync-gate.csx` hook. It follows the documentation model defined in `docs/AGENTS.md`.

## When to Use

- **Before committing major code changes**: Run this skill before creating a pull request or merging work to ensure all documentation is in sync.
- **After any code modification** in `src/` that alters behavior, APIs, or architectural invariants.
- **When prompted by a user** to "sync docs", "update documentation", or "check documentation consistency".

## Workflow

1.  **Run the doc gate**:

    ```bash
    dotnet tool run dotnet-script .github/scripts/doc-sync-gate.csx -- origin/main
    ```

    or through husky:

    ```bash
    dotnet husky exec .github/scripts/doc-sync-gate.csx --args origin/main
    ```

    This command automatically:
    *   Diffs the staged/semantic code changes.
    *   Determines whether they touch the doc surface (`CONTEXT.md`/`docs/`).
    *   Blocks (exit 1) if a semantic change has no documentation update and no `override-no-doc: <reason>`.

2.  **Address reported issues**:
    *   If the gate reports missing updates, update the identified documentation files (`CONTEXT.md`, `docs/adr/`, `docs/`) per `docs/DOC-IMPACT.md`.
    *   Ensure `CONTEXT.md` reflects the **implemented reality**; planned work lives under `.scratch/<feature>/spec.md`, not `docs/`.
    *   For architectural decisions, create or update an ADR in `docs/adr/`.

3.  **Re-run the gate**:
    *   Repeat step 1 until it exits `0` (PASS), indicating all documentation is in sync with the code changes.

## Rules of Thumb

- **Err on the side of updating docs.** If in doubt, update.
- **When NOT to update**: pure refactors with no behavior/API change, test-only coverage additions, dependency bumps with no API change. Note this explicitly rather than skipping silently, or add `override-no-doc: <reason>` to the commit message.
- **Never silently delete coverage** — tag `[outdated]` and describe the replacement.
- **Never bypass the hook** — fix the root cause instead.

## Output

Report what you updated, e.g.:

```
Updated CONTEXT.md (reflected implemented pipeline)
Updated docs/adr/0012-slab-soa.md (recorded the decision)
doc-sync gate: PASS
```
