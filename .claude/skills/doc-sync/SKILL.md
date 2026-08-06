---
name: doc-sync
description: Enforce documentation updates using gstack document-release. This skill acts as a wrapper to ensure all code changes are accompanied by relevant documentation updates in CONTEXT.md and ADRs.  It is triggered by AGENTS.md "Documentation Update Rules" and should be run before committing any significant code changes.
---

# Doc-Sync Skill (powered by gstack document-release)

This skill ensures every code change is accompanied by the necessary documentation updates, leveraging `gstack document-release` for enforcement and reporting. It follows the **Three-Pocket Documentation Strategy** defined in `docs/AGENTS.md`.

## When to Use

- **Before committing major code changes**: Run this skill before creating a pull request or merging work to ensure all documentation is in sync.
- **After any code modification** in `src/` that alters behavior, APIs, or architectural invariants.
- **When prompted by a user** to "sync docs", "update documentation", or "check documentation consistency".

## Workflow

1.  **Run `gstack document-release`**:

    ```bash
    gstack document-release
    ```

    This command automatically:
    *   Scans the git diff for changed code files.
    *   Identifies affected documentation (`CONTEXT.md`, `docs/adr/`, and other relevant files based on internal Diataxis mapping).
    *   Generates a coverage map, identifying documentation gaps or inconsistencies.
    *   Reports any missing updates or drift. If the build pipeline is configured to fail on `document-release` errors, this will prevent commits until docs are in sync.

2.  **Address reported issues**:
    *   If `gstack document-release` reports missing updates, you **must** update the identified documentation files (`CONTEXT.md`, ADRs, etc.) according to the **Three-Pocket Documentation Strategy**.
    *   Specifically, ensure `CONTEXT.md` reflects the **implemented reality**, moving aspirational content to `openspec/specs/` if necessary.
    *   For architectural decisions, create or update an ADR in `docs/adr/`.

3.  **Re-run `gstack document-release`**:
    *   Repeat step 1 until the command reports `OK`, indicating all documentation is in sync with the code changes.

## Rules of Thumb


## Output

Expect output similar to `gstack document-release`:

```
# gstack document-release output

✔ Checking for doc drift against git diff...
✔ Updating docs/AGENTS.md with Three-Pocket Strategy guidelines.
✔ Analyzing code changes in src/GameVM.Compiler.Core/IR/InstList.cs

Missing documentation updates:


Documentation status: WARNING - Some code changes require documentation updates.
```

Once all issues are resolved, the output will indicate success:

```
# gstack document-release output

✔ Checking for doc drift against git diff...
✔ All documentation is up-to-date with code changes.

Documentation status: OK
```
