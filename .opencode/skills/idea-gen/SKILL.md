---
name: idea-gen
description: >
  Generate candidate features, improvements, and research topics for the GameVM compiler.
  Use when brainstorming, exploring the roadmap, or the user says "ideas", "what's next",
  "what could we build", "what's missing", or "help me plan".
---

# GameVM Idea Generation

Produce concrete, ranked candidate ideas grounded in the actual compiler state.
The output is always a short list, each idea carrying why it matters and what work it implies.

## Context anchors

Read these before generating:

- **`CONTEXT.md`** at repo root — glossary, current platform reality, aspirational items.
- **`docs/adr/`** — architectural decisions that constrain what's sensible.
- **`src/` structure** — what already exists (frontends, optimizers, backends).
- **`docs/platforms/specs/`** — hardware specs that define opportunity surfaces.
- **`.scratch/`** — what's already being tracked; don't regenerate closed work.

If a file doesn't exist, skip it silently — don't flag absence.

## Process

### 1. Identify the pressure points

Scan the context anchors for gaps between what the compiler can do and what the targets need. Look for:

- **Unimplemented platforms** — consoles in `docs/platforms/specs/` without a backend in `src/`.
- **Missing optimization passes** — MLIR/LLIR stages where `IMidLevelOptimizer` or `ILowLevelOptimizer` runs a single default pass.
- **Frontend coverage** — languages mentioned in specs but absent from `src/` (e.g. C per `AGENTS.md`).
- **Dispatch strategy gaps** — TTCC/ITC/DTC/STC listed in `src/AGENTS.md`; which are built vs stubbed.
- **Constraint violations** — places where the 6502-adjacent ISA can't express a target's native instruction cleanly.
- **Test debt** — areas in `test/` flagged as sparse relative to `src/` coverage.

### 2. Score candidates against GameVM's design axes

For each candidate, assess:

| Axis | What to look for |
|---|---|
| **Target reach** | Does it open a new console or deepen an existing one? |
| **Compiler leverage** | Does it reuse the IR pipeline, or require a new stage? |
| **Constraint fit** | Does it respect memory/cycle budgets of 8-16 bit targets? |
| **Incremental value** | Can it ship a partial win without full feature? |
| **Agent-navigability** | Is the work sliceable into `.scratch/` tickets? |

Prefer ideas that score high on leverage and incremental value. Deprioritise anything requiring a new IR stage unless the target demands it.

### 3. Surface the frontier, not the obvious

Skip re-stating what `CONTEXT.md` already labels aspirational without adding analysis. Dig into:

- **Under-optimised paths** — e.g. zero-page promotion in Atari2600; does it generalise?
- **Superinstruction opportunities** — methods marked `[Super]` that could be tighter.
- **Inline assembly hooks** — `docs/compiler/InlineAssembly.md` references hand-optimized paths; where are the gaps?
- **Cross-backend commonality** — refactors that let NES + Genesis share LLIR lowering logic.

### 4. Format the output

Present results as a ranked list. Each entry:

```markdown
### <N>. <Title>

**Why now:** <one sentence on the pressure point>

**Scope:** <which stage — frontend / MLIR / LLIR / backend — and which platform(s)>

**Work slices:**
- Ticket 01: <first implementable unit>
- Ticket 02: <next unit>
- Ticket 03: <final unit>

**Rough effort:** <small / medium / large> — based on existing patterns to follow
```

Cap the list at **5 ideas**. Fewer is sharper; more dilutes attention.

## Completion criterion

Done when the list exists, every idea cites a real pressure point from the context anchors, and each has at least one ticket-sized work slice. Stop before writing specs — that's a downstream skill.

## Output destination

Write the list to the conversation. If the user asks to track an idea, create the corresponding `.scratch/<feature>/spec.md` or `.scratch/<feature>/issues/` ticket using the issue-tracker convention from `docs/agents/issue-tracker.md`.
