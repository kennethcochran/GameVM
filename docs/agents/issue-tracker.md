# Issue tracker: GitHub Issues

Issues and tickets for this repo live on GitHub Issues (kennethcochran/GameVM) — for external intake and for the project's own work alike. Specs are the failing test files, not markdown. (Decision, 2026-11-12: GitHub is the tracker for more than outside requests and bug reports; this supersedes the former local-markdown tracker under `.scratch/`.)

## Conventions

- One feature or slice per issue. A slice issue carries the slice's narrative: what it delivers, its status, and its spec (the failing tests).
- State is carried by the `kind:` / `state:` label taxonomy on the repo; triage roles use the label strings in `triage-labels.md`.
- Blocking is recorded in the issue body with links to the blocking issues; a ticket is unblocked when every issue it links is closed or merged.
- Comments and conversation history live on the issue.

## Publishing to the issue tracker

Create the issue on GitHub with `gh issue create` (or the web UI), with the appropriate labels.

## Fetching a ticket

Read the GitHub issue (`gh issue view <number>`). The user will normally pass the issue number or link.

## Map operations

The **map** is a GitHub issue with one **child issue** per ticket.

- **Map**: a single issue whose body carries the Notes / Decisions-so-far / Fog and a task list of its child issues.
- **Child ticket**: its own GitHub issue, with the question in the body.
- **Blocking**: recorded in the child issue's body with links to the blocking issues.
- **Frontier**: scan the map's open child issues for ones that are open, unblocked, and unclaimed.
- **Claim**: say so in a comment on the child issue before starting work.
- **Resolve**: record the answer on the child issue, close it, and append a context pointer (gist + link) to the map issue's Decisions-so-far.

## External intake

External reports — user-reported bugs and suggested ideas — arrive as GitHub Issues too, and are triaged in place:

1. User files bug / idea → GitHub issue (unlabeled or `needs-triage`).
2. Triage categorises it (bug/enhancement) and applies state labels; if `wontfix`, the issue is closed.
3. If real, the issue itself becomes the tracked artifact for the work; its spec is failing test files (NUnit for unit behavior, Reqnroll for acceptance and end-to-end), committed red.

The GitHub issue is the live truth; there is no second local copy to drift.
