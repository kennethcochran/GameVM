# Capability Profiles

Status: provisional — records ADR-010. The capability profile system
(the L1–L7 `GV.Spec` hardware-contract mechanism) is not yet implemented
in code; only the Atari 2600 backend exists today (see
[`CONTEXT.md`](../../CONTEXT.md)). Levels below are working definitions,
not locked assignments.

## What a capability profile is

A game is written against a declared target level, not against a console
and not against fine-grained capability queries. A game recompiles and
runs on any console meeting the level. Levels are strict supersets with
behavioral equivalence: a game written for L1 plays identically on L2.
The level governs stdlib/HAL/third-party availability. Capability sets
are semver-versioned and reclassifiable lower. The profile stays
compiler-internal — developers may supply a custom stdlib/HAL, or none.

Backend capability profiles feed HLIR-stage optimization, 2600-first:
design and backend work targets the most resource-constrained system
first and scales up, never the reverse.

## Provisional levels (ADR-010)

### L1 — the most resource-constrained 2nd-generation consoles

- Atari 2600 ([spec](specs/atari_2600.md))
- Magnavox Odyssey² ([spec](specs/odyssey_2.md))
- Fairchild Channel F ([spec](specs/channel_f.md))
- Bally Astrocade ([spec](specs/bally_astrocade.md))

The L1 instruction set must be implementable on the most constrained L1
hardware. The LLIR register set (accumulator plus 6 general-purpose
registers, ADR-009) was sized against exactly these four CPUs: 7 is the
largest virtual set every L1 backend can support with a spare cell. What
a "register" is differs per CPU. On the Z80 (Bally Astrocade) they are
physical registers: A plus B, C, D, E, H, L. On the Atari 2600 they are
not CPU registers at all — the 6507 has only A, X, and Y — they are seven
zero-page memory locations. On the Odyssey² (Intel 8048) they are A plus
six of R0–R7 in internal RAM; on the Channel F (Fairchild F8) they are
the accumulator plus six directly addressable scratchpad cells. On the
memory-backed machines the seven registers are a RAM budget line, not
register-file space.

### L2 — runs L1 games with a simple recompile

- Intellivision ([spec](specs/intellivision.md))
- ColecoVision ([spec](specs/colecovision.md))
- Atari 5200 ([spec](specs/atari_5200.md))
- Vectrex ([spec](specs/vectrex.md))

L2 is a strict superset of L1.

### L3–L7 — not yet defined

No level above L2 is defined by this document. The hardware analysis in
[CapabilityProfilesReport.md](CapabilityProfilesReport.md) groups consoles
into L1–L7, but those groupings are a hardware analysis, not approved
level definitions. Where NES, Master System, and SG-1000 sit in this
scheme is deferred to the performance-testing phase.

## How the levels are settled

Levels are ultimately determined by runtime performance, through emulated
testing. That testing is performance testing, not functional testing.
Building and running the emulated performance harness is separate later
work, not part of the IR redesign.

Until that testing runs, the L1/L2 lists above are provisional. They will
be corrected — along with the per-console spec files — as follow-on work
once testing settles the levels.

## ISA mobility across levels

The compiler must allow part of the virtual ISA to move up and down the
level supersets until the right mix of instructions is found. Anything
simply infeasible at L1 gets bumped to L2+. Per-level ISA membership is
data, not hardcoded in the kind definitions, so moving an instruction
between levels is a data change; compiling for a level with an
instruction above that level fails loudly.

## Current spec declarations vs the provisional lists

Each per-console spec declares a `Profile: GV.Spec.Ln`. The table below
records what the specs declare today for the eight consoles in scope,
against the provisional ADR-010 placement. The three disagreements are
open discrepancies, to be settled by emulated performance testing — the
spec files are deliberately not corrected in this change.

| Console | Spec declares | ADR-010 (provisional) | Agrees? |
| :------ | :------------ | :-------------------- | :------ |
| Atari 2600 | L1 | L1 | Yes |
| Magnavox Odyssey² | L1 | L1 | Yes |
| Fairchild Channel F | L1 | L1 | Yes |
| Bally Astrocade | L2 | L1 | No — spec says L2 |
| Intellivision | L2 | L2 | Yes |
| ColecoVision | L2 | L2 | Yes |
| Atari 5200 | L3 | L2 | No — spec says L3 |
| Vectrex | L1 | L2 | No — spec says L1 |

The `CapabilityProfilesReport.md` L2 grouping (NES, Master System,
ColecoVision, SG-1000) also differs from the provisional L2 list above;
that grouping is part of the same unsettled question.

## Related documentation

- [ADR-010](../adr/010-capability-profiles.md) — the decision record for
  the provisional levels, ISA mobility, and the open discrepancies.
- [Platform Specifications](specs/README.md) — per-console hardware
  reference specs, each declaring its current `Profile: GV.Spec.Ln`.
- [CapabilityProfilesReport.md](CapabilityProfilesReport.md) — detailed
  hardware capability analysis (2nd–5th generation) behind the
  L1–L7 groupings.
- [Platform Overview](README.md) — the hardware capability catalog and
  its implementation status.
