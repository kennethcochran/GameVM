# GameVM Documentation

GameVM is a high-performance **cross-compiler toolchain** for retro gaming development. It compiles modern, high-level languages (Pascal, C#, …) down to optimized native binaries for 2nd–5th generation gaming consoles.

## Documentation model

GameVM documentation keeps agents and humans on the same page and prevents aspirational drift:

| Where | What it holds |
|---|---|
| [`CONTEXT.md`](../CONTEXT.md) | Glossary of canonical terms, current architecture, invariants, stabilized APIs. The "True North" — start here. |
| [`docs/adr/`](adr/) | Architectural Decision Records — immutable `why` records (e.g. SoA over OOP AST). |
| `.scratch/` | In-flight planned work — `.scratch/<feature>/spec.md` specs and `issues/` tickets. Ephemeral and gitignored. |

> `docs/` is strictly **what exists**. Planned work lives under `.scratch/` until implemented. See [`docs/AGENTS.md`](AGENTS.md) and [`docs/DOC-IMPACT.md`](DOC-IMPACT.md).

## Documentation in this directory

- **`adr/`** — Architectural Decision Records.
- **`agents/`** — agent/skill configuration (`issue-tracker.md`, `triage-labels.md`, `domain.md`).
- **`api/`** — API reference. (Currently empty of live content; the implemented API is canonical in `CONTEXT.md`.)
- **`architecture/`** — cross-cutting reference & process guides (behavior spec, documentation standards, SDD workflow).
- **`compiler/`** — reference notes on the IR pipeline and platform implementation reality.
- **`platforms/`** — hardware capability catalog and the per-console system reference specs (`specs/`).

## Where to look first

- **What the code is today** → [`CONTEXT.md`](../CONTEXT.md)
- **What's planned** → `.scratch/` (in-flight, gitignored)
- **Why a decision was made** → [`docs/adr/`](adr/)

## Key references

- **Architecture reality** → [`CONTEXT.md`](../CONTEXT.md) (IR pipeline, `InstList` SoA, LLIR ISA, Atari 2600 codegen).
- **Platform hardware specs** → [`platforms/specs/`](platforms/specs/) (NES, PS1, Atari, Sega, …).
- **Error handling** → encoded in `CONTEXT.md` (categories, `GameVmException`, Diagnostic Journal).

## Contributing

Follow the [Documentation Standards](architecture/DocumentationStandards.md) and the mandatory rules in [`docs/AGENTS.md`](AGENTS.md). Every code change that touches public surface must update the relevant documentation in the same commit — enforced by the `doc-sync-gate` pre-commit hook.