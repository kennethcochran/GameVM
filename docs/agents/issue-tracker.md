# Issue tracker: filesystem (Three-Pocket)

The repo's **issue tracker is the filesystem** — organised as the Three-Pocket Strategy. GitHub Issues is the **intake inbox only**: a place for external users to report bugs and suggest ideas. Everything that lives long-term lives as markdown files in this repo, versioned and diffable.

## The three pockets

| Pocket | Where | What goes there |
|---|---|---|
| **Implemented reality** | `CONTEXT.md` | glossary + current architecture + invariants |
| **Planned / aspirational** | `openspec/specs/**` | specs for features not yet built |
| **Decisions** | `docs/adr/**` | architectural decision records |

The engineering skills (`to-spec`, `to-tickets`, `domain-modeling`, `wayfinder`, `triage`) all operate on these files. They never put specs or ADRs into GitHub issues — see "Data flow" below.

## Ephemeral ideation — `.scratch/`

The **fuzzy frontend** (wayfinder maps, ideation, scratch) is ephemeral working space under `.scratch/<effort>/`, gitignored. This is where a big fuzzy idea is broken into the map and decision tickets before it crystallises:

- **Map**: `.scratch/<effort>/map.md` — the Notes / Decisions-so-far / Fog body.
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, with the question in the body. `Type:` line (`research`/`prototype`/`grilling`/`task`); `Status:` line (`claimed`/`resolved`).
- **Blocking**: a `Blocked by: NN, NN` line near the top. Unblocked when every listed file is `resolved`.
- **Frontier**: scan `.scratch/<effort>/issues/` for open, unblocked, unclaimed files; first by number wins.
- **Claim**: set `Status: claimed` and save before any work.
- **Resolve**: append the answer under an `## Answer` heading, set `Status: resolved`, then append a context pointer to the map's Decisions-so-far in `map.md`.

When an idea crystallises, it is **promoted** out of `.scratch/` into its proper pocket: a new term to `CONTEXT.md`, a planned feature to `openspec/specs/`, a decision to `docs/adr/`. `.scratch`'s business is working, its contents are throwaway until promoted.

## GitHub Issues — the intake inbox

GitHub Issues is **only** for external intake: user-reported bugs and suggested ideas. The `gh` CLI runs against GitHub (kennethcochran/GameVM).

**When a skill says "publish to the issue tracker", it means the filesystem.** Write the spec/ADR/term file to its pocket (`openspec/specs/`, `docs/adr/`, `CONTEXT.md`), not to a GitHub issue.

**When a skill says "fetch the relevant ticket":** read the file at the referenced path.

### Userbase intake workflow

An incoming GitHub issue is not the persistent artifact — it is the *seed*. When triaged, it is distilled into a filesystem artifact, then GitHub is closed with a pointer (never kept open to mirror state — that would create a second copy to drift):

1. User files bug / idea → GitHub issue (unlabeled or `needs-triage`).
2. `/triage` categorises (bug/enhancement), moves state roles; if `wontfix`, the issue is closed and (for a rejected idea) logged to `.out-of-scope/`).
3. If real, distill into a filesystem artifact:
   - planned feature → `openspec/specs/<feature>/spec.md`
   - decision → `docs/adr/<NNN>-*.md`
   - clarified term → `CONTEXT.md`
4. **Close the GitHub issue** with a pointer to the file that now owns it (e.g. "Spec: `openspec/specs/itc-dispatch/spec.md`").

The GitHub issue is closed; the filesystem artifact is the live truth.

## Triage labels

The `triage` skill's five state roles map to GitHub issue labels on the intake inbox. See `docs/agents/triage-labels.md` for the mapping.

## When a skill's tracker instructions seem to target GitHub

The engineering skills offer a GitHub mode (publish via the `gh` CLI to a remote issue tracker) and a local-markdown mode. **This repo uses the local-markdown/filesystem interpretation** — the skills should treat the "issue tracker" as the filesystem pockets + `.scratch/` for wayfinding, per the data flow above. Where the publicly shipped copy of a skill leans toward GitHub, follow this file instead.