# ADR-010: Capability Profiles — Provisional Levels, Settled by Emulated Performance Testing

Date: 2026-11-07

## Status

Accepted

## Context

GameVM compiles games against a capability level, not against individual
consoles: a game written for a level recompiles and runs on any console
meeting that level, and levels are strict supersets with behavioral
equivalence. The level governs stdlib/HAL/third-party availability, and the
capability profile itself stays compiler-internal. The IR redesign
(ADR-008, ADR-009) gives LLIR a virtual ISA, so the question this record
settles is which consoles sit at which level, and how those assignments are
ultimately decided.

Two things were true when this decision was made:

- The per-console specs in `docs/platforms/specs/` each declare a
  `Profile: GV.Spec.Ln`, and the hardware analysis in
  `docs/platforms/CapabilityProfilesReport.md` groups consoles into L1–L7 —
  but neither is an approved level assignment. The specs and the report
  disagree with each other in several places.
- No emulated performance harness exists yet, so no level assignment can be
  proven by measurement today. The levels recorded here are working
  definitions, not locked facts.

## Decision

**Provisional levels.** The working definitions, provisional and not locked:

- **L1** — the most resource-constrained 2nd-generation consoles: Atari
  2600, Magnavox Odyssey², Fairchild Channel F, Bally Astrocade.
- **L2** — runs L1 games with a simple recompile: Intellivision,
  ColecoVision, Atari 5200, Vectrex. L2 is a strict superset of L1.

What instructions the ISA must support at L1 vs L2 follows from this: the
L1 instruction set must be implementable on the most constrained L1
hardware.

**Levels are ultimately determined by runtime performance, through emulated
testing.** That testing is performance testing, not functional testing.
Building and running the emulated performance harness is separate later
work, not part of the IR redesign.

**ISA mobility.** The compiler must allow part of the virtual ISA to move
up and down the level supersets until the right mix of instructions is
found. Anything simply infeasible at L1 gets bumped to L2+. Per-level ISA
membership is data, not hardcoded in the kind definitions, so moving an
instruction between levels is a data change; compiling for a level with an
instruction above that level fails loudly.

## Consequences

- **Positive**: L1/L2 have a written, approved starting point instead of
  three conflicting sources (specs, report, design discussion).
- **Positive**: ISA mobility keeps the level boundaries adjustable by data
  change while measurement is still pending, instead of baking a guess into
  the instruction definitions.
- **Neutral**: The per-console spec files are not corrected in this change.
  They are corrected as follow-on work once emulated testing settles the
  levels.

## Open Discrepancies

Recorded here so the follow-on correction work knows exactly what to
settle; none of these is resolved by this ADR:

- `bally_astrocade.md` declares L2; this ADR provisionally places the
  Bally Astrocade at L1.
- `atari_5200.md` declares L3; this ADR provisionally places the Atari
  5200 at L2.
- `vectrex.md` declares L1; this ADR provisionally places the Vectrex
  at L2.
- The `CapabilityProfilesReport.md` L2 grouping (NES, Master System,
  ColecoVision, SG-1000) differs from the L2 list above. Where NES,
  Master System, and SG-1000 sit in this scheme is deferred to the
  performance-testing phase.

## Alternatives Considered

- **Treat the per-console spec declarations as the levels**: rejected —
  the specs disagree with each other and with the report, and none of them
  was ever approved as a level assignment.
- **Treat the report's L1–L7 groupings as the levels**: rejected — the
  report is a hardware analysis, not a compiler contract; its groupings
  were never approved, and its L2 already conflicts with the provisional
  list above.
- **Lock the levels now**: rejected — without emulated performance
  measurements there is no evidence for a locked assignment, and locking
  would turn a guess into a contract.
