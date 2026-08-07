# GameVM Documentation

GameVM is a high-performance **cross-compiler toolchain** for retro gaming development. It compiles modern, high-level languages (Pascal, C#, …) down to optimized native binaries for 2nd–5th generation gaming consoles.

## The Three-Pocket Strategy

GameVM documentation follows a strict three-pocket layout to keep AI agents and humans on the same page and prevent aspirational drift:

| Pocket | Where | What it holds |
|---|---|---|
| **Implemented reality** | [`CONTEXT.md`](../CONTEXT.md) | Glossary of canonical terms, current architecture, invariants, stabilized APIs. The "True North" — start here. |
| **Planned / aspirational** | [`openspec/specs/`](../openspec/specs/) | OpenSpec capability specs for features that don't exist yet. Prohibited from `docs/` until implemented. |
| **Decisions** | [`docs/adr/`](adr/) | Architectural Decision Records — immutable `why` records (e.g. SoA over OOP AST). |

> `docs/` is strictly **what exists**. Aspirational design goes in `openspec/specs/`. See [`docs/AGENTS.md`](AGENTS.md) and [`docs/DOC-IMPACT.md`](DOC-IMPACT.md).

## Documentation in this directory

- **`adr/`** — Architectural Decision Records (Pocket #3).
- **`agents/`** — agent/skill configuration (`issue-tracker.md`, `triage-labels.md`, `domain.md`).
- **`api/`** — API reference. (Currently empty of live content; the implemented API is canonical in `CONTEXT.md` and planned API surface lives in `openspec/specs/`.)
- **`architecture/`** — cross-cutting reference & process guides (behavior spec, documentation standards, SDD workflow).
- **`compiler/`** — reference notes on the IR pipeline and platform implementation reality.
- **`platforms/`** — hardware capability catalog and the per-console system reference specs (`specs/`).

## Where to look first

- **What the code is today** → [`CONTEXT.md`](../CONTEXT.md)
- **What's planned** → [`openspec/specs/`](../openspec/specs/)
- **Why a decision was made** → [`docs/adr/`](adr/)

## Key references

- **Architecture reality** → [`CONTEXT.md`](../CONTEXT.md) (IR pipeline, `InstList` SoA, LLIR ISA, Atari 2600 codegen).
- **LLIR ISA** → implemented opcode table in `CONTEXT.md`; aspirational wider ISA in `openspec/specs/llir-isa-design/`.
- **Platform hardware specs** → [`platforms/specs/`](platforms/specs/) (NES, PS1, Atari, Sega, …).
- **Error handling** → encoded in `CONTEXT.md` (categories, `GameVmException`, Diagnostic Journal).

## Contributing

Follow the [Documentation Standards](architecture/DocumentationStandards.md) and the mandatory rules in [`docs/AGENTS.md`](AGENTS.md). Every code change that touches public surface must update the relevant pocket in the same commit — enforced by the `doc-sync-gate` pre-commit hook.