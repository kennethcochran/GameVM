# Issue tracker: Local Markdown

Issues for this repo live as markdown files in `.scratch/`, which is gitignored. Specs are the failing test files, not markdown.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`
- The spec is the failing test files — NUnit for unit behavior, Reqnroll for acceptance and end-to-end.
- Implementation issues are one file per ticket at `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01` — never a single combined tickets file
- Triage state is recorded as a `Status:` line near the top of each issue file (see `triage-labels.md` for the role strings)
- Comments and conversation history append to the bottom of the file under a `## Comments` heading

## Publishing to the issue tracker

Create a new file under `.scratch/<feature-slug>/` (creating the directory if needed).

## Fetching a ticket

Read the file at the referenced path. The user will normally pass the path or the issue number directly.

## Map operations

The **map** is a file with one **child** file per ticket.

- **Map**: `.scratch/<effort>/map.md` — the Notes / Decisions-so-far / Fog body.
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, with the question in the body. A `Type:` line records the ticket type (`research`/`prototype`/`discussion`/`task`); a `Status:` line records `claimed`/`resolved`.
- **Blocking**: a `Blocked by: NN, NN` line near the top. A ticket is unblocked when every file it lists is `resolved`.
- **Frontier**: scan `.scratch/<effort>/issues/` for files that are open, unblocked, and unclaimed; first by number wins.
- **Claim**: set `Status: claimed` and save before any work.
- **Resolve**: append the answer under an `## Answer` heading, set `Status: resolved`, then append a context pointer (gist + link) to the map's Decisions-so-far in `map.md`.

## GitHub Issues — the intake inbox

GitHub Issues is **only** for external intake: user-reported bugs and suggested ideas. The `gh` CLI runs against GitHub (kennethcochran/GameVM).

**Publishing to the issue tracker means `.scratch/`.** Write the ticket file to `.scratch/<feature-slug>/`, not to a GitHub issue.

**Fetching a ticket:** read the file at the referenced path.

### Userbase intake workflow

An incoming GitHub issue is not the persistent artifact — it is the *seed*. When triaged, it is distilled into a `.scratch/` file, then GitHub is closed with a pointer (never kept open to mirror state — that would create a second copy to drift):

1. User files bug / idea → GitHub issue (unlabeled or `needs-triage`).
2. Triage categorises it (bug/enhancement), moves state roles; if `wontfix`, the issue is closed.
3. If real, distill into `.scratch/` and/or failing tests:
   - planned feature → failing test files (the spec); the effort is tracked in `.scratch/<feature>/`
   - implementation ticket → `.scratch/<feature>/issues/<NN>-<slug>.md`
4. **Close the GitHub issue** with a pointer to what now owns it (e.g. "Spec: failing tests in `test/GameVM.Compiler.Specs/`; local tracking notes in `.scratch/itc-dispatch/`").

The GitHub issue is closed; the `.scratch/` file is the live truth.
