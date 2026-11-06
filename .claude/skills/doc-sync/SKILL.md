---
name: doc-sync
description: Ensure code changes are accompanied by relevant documentation updates in CONTEXT.md and docs/adr/. It is triggered by AGENTS.md "Documentation Update Rules" and should be run before committing any significant code changes.
---

# Doc-Sync Skill

This skill ensures every code change is accompanied by the necessary documentation updates. It follows the documentation model defined in `docs/AGENTS.md`.

## When to Use

- **Before committing major code changes**: Run this skill before creating a pull request or merging work to ensure all documentation is in sync.
- **After any code modification** in `src/` that alters behavior, APIs, or architectural invariants.
- **When prompted by a user** to "sync docs", "update documentation", or "check documentation consistency".

## Workflow

1.  **Determine which docs are affected**: consult `docs/DOC-IMPACT.md` to decide which documentation a change requires. (The doc-sync gate that used to verify this was disabled 2026-11-06; the script remains in `.github/scripts/` for a later readdress.)

2.  **Update the identified docs**:
    *   Update the documentation files (`CONTEXT.md`, `docs/adr/`, `docs/`) per `docs/DOC-IMPACT.md`.
    *   Ensure `CONTEXT.md` reflects the **implemented reality**; planned work lives as failing test files, not `docs/`.
    *   For architectural decisions, create or update an ADR in `docs/adr/`.

3.  **Verify**:
    *   Re-read the updated docs and confirm links resolve and tags are accurate.

## Rules of Thumb

- **Err on the side of updating docs.** If in doubt, update.
- **When NOT to update**: pure refactors with no behavior/API change, test-only coverage additions, dependency bumps with no API change. Note this explicitly rather than skipping silently.
- **Never silently delete coverage** — tag `[outdated]` and describe the replacement.

## Output

Report what you updated, e.g.:

```
Updated CONTEXT.md (reflected implemented pipeline)
Updated docs/adr/0012-slab-soa.md (recorded the decision)
```
