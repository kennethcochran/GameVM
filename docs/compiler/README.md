# Compiler Documentation

The compiler's implemented reality is canonically recorded in [`CONTEXT.md`](../../CONTEXT.md):

- **IR pipeline** — AST `InstList` → HLIR → MLIR → LLIR (all Struct-of-Arrays `InstList`)
- **LLIR ISA** — implemented 6502-adjacent opcode table
- **Atari 2600 codegen** — 4KB ROM emission, zero-page addressing, self-loop termination
- **Error handling** — categories, `GameVmException`, Diagnostic Journal

**Planned / aspirational compiler features** (type system, uniform language integration, inline assembly, modules, module resolution, dynamic loading, build system, CLI, superinstructions/JIT/optimization, LLIR ISA design) live as OpenSpec specs in [`openspec/specs/`](../../openspec/specs/). They are prohibited from `docs/` until implemented — see [`docs/AGENTS.md`](../AGENTS.md).

This directory is intentionally lean: reference documentation for the shipped compiler is folded into `CONTEXT.md`, and aspirational design lives in `openspec/specs/`.