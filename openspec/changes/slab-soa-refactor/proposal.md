## Why

GameVM's current pipeline uses opaque `uint[]` slabs with bit-packed metadata for all IR stages (AST, HLIR, MLIR, LLIR). This forces `DecodeKind/DecodeSize/DecodeArgCount` bit operations on every access, defeating the cache-locality and SIMD benefits that Data-Oriented Design (DOD) exists to provide. Reference compilers (Zig's `MultiArrayList`, Carbon's `BlockValueStore`, Odin's arena allocator) use **Struct of Arrays (SoA)** for IR stages that undergo repeated passes: homogeneous fields in parallel contiguous arrays accessed via integer handles, with variable-length operands in a separate pool. This maximizes cache-line utilization during phase-oriented passes and enables SIMD vectorization.

## What Changes

- **AST Stage**: Keep as `uint[]` (write-once/read-once, high node variance, parsing mutation). No DOD benefit for single-pass. **Note**: Dead OOP AST node hierarchy (`PascalAstNode`, `ASTBuilder`, all `*Node` classes) has been removed as cleanup (separate from slab format decision).
- **HLIR Stage**: **SoA `InstList`** — parallel arrays `tags[]`, `flags[]`, `argCounts[]`, `fixedOps[]`, `extra[]`, `blockIds[]` + typed handles (`InstIndex`, `BlockId`, `SlotIndex`). Tree shape with ≤3 operands for 95% of nodes.
- **MLIR Stage**: **SoA `InstList` + `CfgTable`** — same SoA layout, CFG with `BlockId` handles, local slots via `SlotIndex`. Graph algorithms (dom tree, loops, DF) on `CfgTable`.
- **LLIR Stage**: **SoA `InstList` + `CfgTable`** — linear list + CFG. Linear passes stride `tags[]`/`fixedOps[]`; CFG ops use `BlockId`/`SlotIndex`.
- **Final Lowering**: **Dispatch-strategy-specific emit** — from the accumulator-based LLIR to `Bytecode[]` (future stack VM), `DTC/ITC/STC` instruction tables (contiguous arrays of `Inst` + label addresses), or native machine code.
- **Handles**: Typed `readonly struct` handles (`InstIndex`, `BlockId`, `SymbolId`, `SlotIndex`) replace raw offsets everywhere.
- **Metadata**: Remove `InstructionMetadata.Encode/Decode` bit-packing; direct field access on SoA arrays.
- **Handles invalid sentinel**: `BlockId.Zero = unassigned`; others `-1` invalid.

**BREAKING**: All transformers, optimizers, backends, and printers rewritten against `InstList` SoA API. Tests rewritten to assert on SoA field arrays + `extra` pool.

## Capabilities

### New Capabilities
- `soa-instruction-lists`: SoA `InstList` structure (parallel field arrays + `extra` pool), typed handles, field-access API replacing `InstructionMetadata` bit-packing.
- `hlir-soa-ir`: HLIR as SoA `InstList` with tree-shaped access (≤3 operands for 95% nodes, `extra` pool for overflow).
- `mlir-soa-ir`: MLIR as SoA `InstList` + `CfgTable` with `BlockId`/`SlotIndex` handles.
- `llir-soa-ir`: LLIR as SoA `InstList` + `CfgTable` with linear stride access + `BlockId` handles.
- `dispatch-strategy-emit`: Dispatch-strategy-specific emitters producing contiguous instruction tables.

### Modified Capabilities
- `dod-compiler-architecture`: "Single Unified Slab" → SoA `InstList` per stage; "Self-Describing Blocks" → parallel arrays; "Formalized Metadata Encoding" → removed (direct field access); "CFG Parallel Arrays" → keyed by `BlockId` handles; "Five-Stage Pipeline" → each stage body is `InstList`.
- `index-based-ast`: References change from offsets to `InstIndex` handles; AST may stay `uint[]` (write-once).
- `dod-mlir-transformation`: Operands → `InstIndex`; branches → `BlockId`; slots → `SlotIndex`; linear iteration on `Tags[]`.
- `contiguous-passes`: Linear iteration on `Tags[]`/`FixedOps[]` spans; `SlabIterator` removed.

## Non-Goals
- SIMD vectorization of passes (follow-up; SoA layout is prerequisite).
- SoA in front-end parsing phase (write-once, high variance).
- Change LLIR ISA or Atari 2600 bytecode output.
- New optimization passes.

## Impact
- **Source**: All IR transformers, optimizers, backends, printers, frontends, `CompileUseCase`.
- **Tests**: All test projects construct/assert on `InstList` field arrays + `extra` pool.
- **Docs**: Architecture, IR, ISA docs; API docs for `InstList`, handles, `InstListBuilder`.
- **No external dependencies**.

## Memory Profile
- **Functional pipeline (current)**: Single arena, all slabs live until arena reset. Peak ~300–500 KB for typical programs — negligible on any host.
- **No GC pressure**: Bump-pointer arena, zero per-object overhead.
- **Same data, sequential ownership**: No memory waste vs in-place ping-pong.