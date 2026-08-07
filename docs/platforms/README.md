# Platform Documentation

This directory holds the **hardware capability catalog** for the retro platforms GameVM targets.

## Contents

- **`specs/`** — per-console hardware reference specifications (Atari, Nintendo, Sega, NEC, SNK, and obscure systems). Each is a factual hardware/feasibility reference.
- **`specs/TEMPLATE.md`** — template for authoring new platform reference specs.
- **`CapabilityProfilesReport.md`** — hardware capability comparison / analysis.

## Status

The hardware catalog is reference material (category D of the Three-Pocket strategy): it stays in `docs/`. The **Capability Profile *system*** (the L1–L7 `GV.Spec` hardware-contract mechanism) is planned and lives as an OpenSpec spec in [`openspec/specs/capability-profiles/`](../../openspec/specs/capability-profiles/).

## Current implementation reality

Only the **Atari 2600** backend is implemented today (see [`CONTEXT.md`](../../CONTEXT.md)). All other systems in `specs/` are researched hardware targets — not yet generating ROMs.

## Adding a platform

1. Add/have a reference spec in `specs/`.
2. Track the target's build in OpenSpec (see [`openspec/specs/`](../../openspec/specs/)).
3. Implement the backend; update `CONTEXT.md` when it ships.

See [Compiler docs](../compiler/README.md) for how the pipeline produces platform binaries.