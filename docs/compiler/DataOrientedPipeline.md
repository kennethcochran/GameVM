# Data-Oriented Compilation Pipeline — Design

Status: draft
Branch: feature/dod-pipeline

Summary
-------
This document proposes a data-oriented (DOD) redesign of the **entire GameVM compilation
pipeline** — every stage from the language frontend through code generation — moving the
current object-heavy, pointer-rich intermediate representations (IRs) and their transformers
toward compact, POD-like, cache-friendly layouts (Structure-of-Arrays where beneficial).

The goal is high-throughput IR construction, analysis, and lowering; fast, deterministic
serialization; simpler memory management; and measurable performance improvements across the
whole `HLIR -> MLIR -> LLIR -> target` flow.

**Delivery method: branch by abstraction.** We do not mutate the existing pipeline in place.
Instead we build a *parallel* DOD pipeline alongside the current one — a new visitor that turns
the ANTLR parse tree into a DOD AST, and a new implementation of every subsequent pipeline stage
that consumes/produces DOD data.

Crucially, the DOD pipeline is expected to look quite different from the original, so we will
**introduce new interfaces** rather than assume the DOD stages fit the existing ones. Where a
legacy interface still fits the DOD data model, a DOD stage may implement it directly; but where
the shapes diverge (e.g. transformers that operate on blob types and id-based operands instead
of object trees and `string`s), we define new DOD-specific abstractions (e.g.
`IDodIRTransformer<TIn, TOut>`, `IDodFrontend`, `IDodOptimizer<T>`, `IDodCodeGenerator`). The
branch-by-abstraction seam therefore lives at the **pipeline/orchestration level** (a selectable
DOD vs. legacy pipeline, e.g. behind `ICompileUseCase`) as well as at individual stages, so the
two pipelines can differ structurally without one constraining the other. Both are selectable at
composition/runtime. The original pipeline is retained (and remains the default) until the new
one demonstrably reaches feature parity; only then is the legacy path retired. See "Migration
strategy" below.

This supersedes the earlier HLIR-only proposal (`HLIR_DataOriented.md`): the scope is now the
full pipeline rather than a single IR level.

Scope
-----
In scope — the full compilation pipeline as orchestrated by `CompileUseCase.CompileInternal`:

1. Frontend AST -> HLIR construction (hand-written Pascal AST node types and
   `PascalAstToHlirTransformer`, `AstVisitor`, and the per-construct transformers/visitors).
2. HLIR data model (`HighLevelIR`).
3. HLIR -> MLIR lowering (`HlirToMlirTransformer`, exposed via `PascalFrontend.ConvertToMidLevelIR`
   and `MlirBuilder`).
4. MLIR data model (`MidLevelIR`).
5. Mid-level optimization (`IMidLevelOptimizer` / `DefaultMidLevelOptimizer`).
6. MLIR -> LLIR lowering (`IIRTransformer<MidLevelIR, LowLevelIR>` / `MidToLowLevelTransformer`,
   `LlirBuilder`).
7. LLIR data model (`LowLevelIR`).
8. Low-level optimization (`ILowLevelOptimizer` / `DefaultLowLevelOptimizer`).
9. Code generation (`ICodeGenerator` / `Atari2600CodeGenerator`, `M6502Emitter`).
10. Capability validation (`ICapabilityValidatorService`) where it walks IR.

**Explicitly out of scope: ANTLR-generated code.** The lexer, parser, and the *base visitor*
that ANTLR generates from `src/GameVM.Compiler.Pascal/ANTLR/Pascal.g4` (e.g. `PascalLexer`,
`PascalParser`, `PascalBaseVisitor`) are machine-generated artifacts and are **not** to be
refactored to DOD. Note the distinction: the base visitor is *generated*, but its **descendant**
(the hand-written `AstVisitor`) is hand-authored and therefore in scope. Rather than modifying
that existing descendant, branch by abstraction adds a **new sibling visitor** — a second
descendant of the generated `PascalBaseVisitor` — that walks the same generated parse tree and
emits a DOD AST directly. The generated parse tree is read at the boundary but never stored, and
no generated type is edited.

Repository findings (current state)
------------------------------------
The pipeline today is a chain of independent object graphs. Each stage allocates a fresh tree
of reference-typed nodes, and each transformer walks one tree while allocating the next.

- **Frontend.** `PascalFrontend.Parse` runs ANTLR (generated `PascalLexer`/`PascalParser`),
  then an `AstVisitor` produces a hand-written `ProgramNode` AST (`PascalASTNode` subclasses),
  which `PascalAstToHlirTransformer` converts into `HighLevelIR`.
- **HLIR (`HighLevelIR.cs`).** A large object tree: `List<IRNode> TopLevel`,
  `Dictionary<string, IRSymbol> Globals`, `Dictionary<string, IRType> GlobalTypes`,
  `Dictionary<string, Function> GlobalFunctions`, `List<HlModule> Modules`. Statements and
  expressions are a deep class hierarchy (`Statement`, `Block`, `IfStatement`, `While`,
  `ForStatement`, `Assignment`, `Return`, `Expression`, `BinaryOp`, `UnaryOp`, `Literal`,
  `Identifier`, `FunctionCall`, `ArrayAccess`, ...), each carrying a `SourceFile` string and
  references to child nodes.
- **HLIR -> MLIR (`HlirToMlirTransformer`).** Walks the HLIR tree via `switch` on node runtime
  type and appends to `List<MLInstruction>` per function; strings are the universal operand type
  (`GetExpressionValue` returns a `string`).
- **MLIR (`MidLevelIR.cs`).** `List<MLModule>` -> `List<MLFunction>` -> `List<MLInstruction>`,
  where `MLInstruction` is an abstract base with subclasses `MLLabel`, `MLBranch`, `MLAssign`,
  `MLCall`. Operands are strings (`Target`, `Source`, `Arguments`).
- **Mid optimizer (`DefaultMidLevelOptimizer`).** Rebuilds the whole `MidLevelIR` into a new
  object graph; does string-based constant folding and duplicate-assignment removal.
- **MLIR -> LLIR (`MidToLowLevelTransformer`).** Allocates `LowLevelIR`, maps symbolic targets
  to addresses via a `Dictionary<string,string>`, flattens function instructions to a top-level
  list for test compatibility.
- **LLIR (`LowLevelIR.cs`).** `List<LLModule>` -> `List<LLFunction>` -> `List<LLInstruction>`
  plus a flattened top-level `List<LLInstruction>`. `LLInstruction` subclasses: `LLLoad`,
  `LLStore`, `LLCall`, `LLJump`, `LLLabel`. Operands are strings.
- **Low optimizer (`DefaultLowLevelOptimizer`).** Peephole over the flat instruction list
  (redundant load/store removal), rebuilding a new list.
- **Codegen (`Atari2600CodeGenerator` + `M6502Emitter`).** Iterates LLIR instructions, emits
  assembly strings, then assembles into a `byte[]` ROM.

Common cost drivers: many small heap allocations per node; pointer chasing during every walk;
runtime-type `switch`/`is` dispatch; `string` as the universal operand/type currency (parse +
allocate on nearly every access); full object-graph rebuilds in each pass.

Goals
-----
1. Make every IR level memory- and cache-friendly to accelerate construction, whole-program
   analyses, optimization, and lowering.
2. Preserve language- and target-agnostic semantics: each IR must still represent the same
   modules, types, globals, functions, statements/instructions consumed by the next stage.
3. Provide a smooth migration path via branch by abstraction: a parallel DOD pipeline (with new
   DOD-specific interfaces where the shapes diverge from the legacy ones), kept side by side
   with the legacy pipeline until it reaches feature parity, so the working compiler is never
   broken.
4. Replace `string`-typed operands/types with interned ids to remove per-access parsing and
   allocation.
5. Enable deterministic layout and compact serialization of any IR for cross-process tooling
   (e.g. separate optimization/analysis workers) and golden-file testing.

Design principles
-----------------
- **POD-first:** core IR data (types, signatures, blocks, variables, instructions) are plain
  value structs stored in contiguous vectors.
- **SoA for hot fields:** frequently scanned per-instruction fields (opcode, operand ids, type
  ids) live in parallel arrays for vectorizable, scan-friendly passes.
- **Indices, not references:** replace object references with 32-bit ids
  (`ModuleId`, `FunctionId`, `BlockId`, `InstrId`, `TypeId`, `SymbolId`, `StringId`). Ranges
  are `(start, count)` into shared arrays.
- **Interned strings:** one `StringPool` per compilation replaces the pervasive `SourceFile`
  and operand strings; equality and lookups become integer comparisons.
- **Immutable-by-default / rebuild-free passes:** passes operate on read-only views and emit
  new arrays or in-place edits via arenas rather than allocating fresh object graphs.
- **Arenas and pools:** bulk-allocate and bulk-free per-compilation data to improve locality
  and simplify ownership.
- **Uniform blob shape across levels:** HLIR, MLIR, and LLIR share the same container idioms
  (module table + SoA instruction/node stores + string pool + version header) so tooling,
  serialization, and tests are reusable.
- **Branch by abstraction:** new DOD stages are added as a parallel pipeline, never as in-place
  edits to the working one. The selection seam sits at the pipeline/orchestration level and, where
  practical, at individual stages. New DOD-specific interfaces are introduced wherever the DOD
  shape diverges from a legacy interface. Old and new run side by side and are compared for parity
  before the old path is removed.

Shared infrastructure (`GameVM.Compiler.Core/IR/Blob`)
------------------------------------------------------
A small set of building blocks reused by all three IR levels:

- **Id types:** `readonly struct StringId`, `TypeId`, `SymbolId`, `ModuleId`, `FunctionId`,
  `BlockId`, `InstrId` — each wrapping an `int` with a distinct type for safety.
- **`Range32`:** `readonly struct { int Start; int Count; }` for slices into arrays.
- **`StringPool`:** deduplicated string storage; `StringId Intern(string)`,
  `string Get(StringId)`.
- **`TypeTable`:** vector of `TypeRecord { TypeKind Kind; int Width; TypeId ElementType;
  int FieldLayoutId; StringId Name; }`.
- **`SymbolTable`:** vector of `SymbolRecord` (name id, type id, flags: constant/global,
  initial-value slot).
- **SoA instruction store:** `byte[] Opcodes`, `int[] Operands` (operand pool referenced by
  range), `TypeId[] Types`, plus a debug/source side-table indexed by `InstrId`.
- **`Arena`:** append-only bump allocator with capacity reservation and bulk reset.
- **`VersionInfo`:** magic, schema version, endianness, counts, checksum — one header per blob.

Per-stage design
================

Each stage below describes the **new** DOD implementation that is added alongside the existing
one. Nothing in this section replaces or edits a working stage in place; the legacy type named in
each heading keeps running until its DOD counterpart reaches parity (see "Migration strategy").


1. Frontend parse tree -> DOD AST -> HLIR (excluding ANTLR-generated code)
-------------------------------------------------------------------------
The generated lexer/parser/base visitor stay as-is. The existing hand-written `AstVisitor` and
`PascalAstToHlirTransformer` are left untouched and keep driving the legacy pipeline. In
parallel we add new components:

- A **new sibling visitor** (a second `PascalBaseVisitor` descendant, e.g.
  `PascalDodAstVisitor`) that walks the ANTLR parse tree produced by `parser.program()` and
  builds an `AstBlob` directly — SoA node storage (`NodeKinds`, child `Range32`s, `StringId`
  names, literal payload slots) instead of one heap object per `PascalASTNode`. The generated
  parse tree is read but never stored.
- A **new AST->HLIR builder** (e.g. `DodHlirBuilder`) that consumes the `AstBlob` and emits an
  `HLIRBlob`, reserving capacities up front and interning every string once.
- A **new frontend** (e.g. `PascalDodFrontend`, implementing `IDodFrontend` — or `ILanguageFrontend`
  if that shape still fits) that wires these together, so the composition root can select the
  legacy `PascalFrontend` or the DOD one.

The existing `PascalFrontend`/`AstVisitor`/`PascalAstToHlirTransformer` remain the default until
the DOD frontend reaches parity.

2. HLIR data model (`HLIRBlob`)
-------------------------------
Top-level container:

- `HLIRBlob`
  - `modules: Vector<HLModule>`
  - `globals: Vector<SymbolRecord>`
  - `types: TypeTable`
  - `functions: Vector<HLFunction>`
  - `nodes: SoA statement/expression store` (module- or blob-level)
  - `strings: StringPool`
  - `errors: Vector<StringId>`
  - `version: VersionInfo`

- `HLModule (struct)`: `NameId`, `TypeRange`, `FunctionRange`, `VariableRange`, `Flags`.
- `HLFunction (struct)`: `NameId`, `Signature` (value type: return `TypeId`, param range),
  `BodyNodeRange`, `LocalRange`, `RequiredLevel`, `RequiredExtensionId (StringId)`, attributes.
- Statements/expressions: SoA node store with a `NodeKind` enum (Block, If, While, For, Repeat,
  Assignment, Return, ExpressionStmt, Literal, Identifier, BinaryOp, UnaryOp, FunctionCall,
  ArrayAccess). Per-node payload uses operand-id slots and child `Range32`s; a `TypeId` column
  carries expression types. Source locations move to a side-table indexed by node id.

3. HLIR -> MLIR lowering
------------------------
A **new** DOD transformer (e.g. `DodHlirToMlirTransformer`, implementing
`IDodIRTransformer<HLIRBlob, MLIRBlob>` — a new interface if `IIRTransformer<,>` doesn't fit the
blob shape) is added alongside `HlirToMlirTransformer`, which is left untouched:

- Replace runtime-type `switch(node)` with a tight loop over `NodeKinds` (dense switch on a
  byte enum; branch-predictor friendly).
- Expression lowering no longer returns `string`; it returns an operand id (into the MLIR
  operand pool / value table), so constant folding and identifier resolution work on ids.
- Emit instructions straight into the MLIR SoA store; record per-function instruction ranges.
- The DOD frontend (`PascalDodFrontend`) delegates to this new transformer, while the legacy
  `PascalFrontend.ConvertToMidLevelIR`/`MlirBuilder` continue to use the object-based one.

4. MLIR data model (`MLIRBlob`)
-------------------------------
- `MLIRBlob`: `modules: Vector<MLModule>`, `functions: Vector<MLFunction>`,
  `globals: Vector<SymbolRecord>`, SoA instruction store, `strings`, `version`.
- `MLModule (struct)`: `NameId`, `FunctionRange`.
- `MLFunction (struct)`: `NameId`, `InstrRange`.
- Instruction storage (SoA): `Opcodes: MLOpcode[]` (Label, Branch, Assign, Call), operand ids
  in the operand pool. E.g. `MLAssign` -> `{ target: SymbolId/ValueId, source: ValueId }`;
  `MLBranch` -> `{ target: StringId(label), condition: ValueId? }`;
  `MLCall` -> `{ name: StringId, args: Range32 into operand pool }`.

5. Mid-level optimization
-------------------------
A **new** DOD optimizer (e.g. `DodMidLevelOptimizer`, implementing `IDodOptimizer<MLIRBlob>` or
`IMidLevelOptimizer` if it fits) operates over the MLIR SoA store, added beside
`DefaultMidLevelOptimizer`:

- Constant folding works on the value/operand table (ids + a small constant pool), not on
  parsing `"(5 + 3)"` substrings.
- Dead-code / unreachable-block elimination becomes a scan over `Opcodes` with a compact
  bitset marking live instructions; output is a filtered range, not a rebuilt object graph.
- Duplicate-assignment removal uses a `SymbolId -> lastInstr` map keyed by int.

6. MLIR -> LLIR lowering
------------------------
A **new** DOD transformer (e.g. `DodMidToLowLevelTransformer`, implementing
`IDodIRTransformer<MLIRBlob, LLIRBlob>`) consumes `MLIRBlob` and emits `LLIRBlob`, added beside
`MidToLowLevelTransformer`:

- Address map keyed by `SymbolId`/`StringId` instead of `Dictionary<string,string>`.
- Emits into the LLIR SoA store; keeps the flattened top-level instruction range for current
  codegen/test expectations (as an explicit range view, not a duplicated list).

7. LLIR data model (`LLIRBlob`)
-------------------------------
- `LLIRBlob`: `modules`, `functions`, SoA instruction store, a top-level `InstrRange`,
  `strings`, `version`.
- `LLFunction (struct)`: `NameId`, `InstrRange`.
- Instruction storage (SoA): `Opcodes: LLOpcode[]` (Load, Store, Call, Jump, Label); operands
  as ids: `LLLoad -> { reg: RegId, value: ValueId }`, `LLStore -> { addr: AddrId, reg: RegId }`,
  `LLJump -> { target: StringId, condition: ValueId? }`, etc.

8. Low-level optimization
-------------------------
A **new** DOD optimizer (e.g. `DodLowLevelOptimizer`, implementing `IDodOptimizer<LLIRBlob>` or
`ILowLevelOptimizer` if it fits), added beside `DefaultLowLevelOptimizer`, scans the LLIR
`Opcodes`/operand arrays directly (e.g. redundant load/store detection compares operand ids in
adjacent slots), emitting a filtered instruction range.

9. Code generation
------------------
A **new** DOD code generator (e.g. `DodAtari2600CodeGenerator`, implementing `IDodCodeGenerator`
or `ICodeGenerator` if it fits) iterates the LLIR SoA store by `InstrRange`, added beside
`Atari2600CodeGenerator`. Assembly emission can move from building
`List<string>` toward an opcode/operand table consumed by `M6502Emitter`, reducing
per-instruction string formatting until the final assembly text is needed. ROM layout logic is
unchanged. The MOS 6502 mnemonics/tables are target data, not generated code, so they remain in
scope for a data-oriented representation.

Capability validation
----------------------
`ICapabilityValidatorService.Validate(hlir, ...)` becomes a scan over the HLIR function table
(`RequiredLevel`, `RequiredExtensionId` columns) instead of a tree walk.

Migration strategy (branch by abstraction)
------------------------------------------
We migrate by building a parallel DOD pipeline behind the existing stage interfaces, never by
editing a working stage in place. The legacy pipeline stays the default and fully functional
until the DOD pipeline reaches feature parity end to end.

1. Land shared `Blob` infrastructure (ids, ranges, string pool, SoA store, arena) with tests.
2. Add the DOD data model for each IR level (`HLIRBlob`, `MLIRBlob`, `LLIRBlob`).
3. Define the DOD stage abstractions. Reuse a legacy interface only where the DOD data model
   genuinely fits it; otherwise introduce a new DOD-specific interface (the expected common
   case, since the DOD stages operate on blobs and id-based operands). Candidate abstractions:
   - `IDodFrontend` — parse-tree -> `HLIRBlob` (may or may not resemble `ILanguageFrontend`),
   - `IDodIRTransformer<TIn, TOut>` — e.g. `HLIRBlob -> MLIRBlob`, `MLIRBlob -> LLIRBlob`,
   - `IDodOptimizer<T>` — e.g. over `MLIRBlob` / `LLIRBlob`,
   - `IDodCodeGenerator` — `LLIRBlob -> byte[]`.
   These live beside (not replacing) `ILanguageFrontend`, `IIRTransformer<,>`,
   `IMidLevelOptimizer`, `ILowLevelOptimizer`, `ICodeGenerator`.
4. Implement a **new, parallel** component for each stage against those abstractions:
   `PascalDodFrontend`, `DodHlirToMlirTransformer`, `DodMidLevelOptimizer`,
   `DodMidToLowLevelTransformer`, `DodLowLevelOptimizer`, `DodAtari2600CodeGenerator`.
5. Compose them into a **DOD pipeline** selectable against the legacy one at the orchestration
   level (e.g. an alternate `ICompileUseCase` wiring, chosen via DI / an option such as
   `CompilationOptions.DataOriented`). The two pipelines never interleave: a compilation runs
   entirely on one path or the other. (Optional narrow adapters between a legacy IR and a blob
   may be used only as temporary scaffolding for differential testing, not as a permanent
   coupling.)
6. Drive the DOD pipeline to parity: run both paths over the same inputs and compare outputs
   (see "Tooling, testing, benchmarks") until the DOD path matches the legacy path (or is a
   documented, intended improvement) across the test corpus.
7. Flip the default to the DOD pipeline once parity holds; keep the legacy path available behind
   the flag for one release as a fallback.
8. Only after the DOD path has proven itself, remove the legacy object-model pipeline, its IR
   types, and any legacy-only interfaces that no longer have implementers.

Because each stage is independently swappable behind its interface, stages can be built and
validated in any convenient order; a natural progression is frontend -> HLIR/MLIR ->
mid optimizer -> LLIR -> low optimizer -> codegen, but a fully DOD end-to-end run is only
possible once every stage has a DOD implementation.

Serialization & versioning
--------------------------
- Each blob serializes to a compact binary format: header (`magic`, `schema version`,
  `endianness`, counts, checksum) followed by the string pool and SoA arrays.
- Deterministic layout enables golden-file tests and cross-process tooling (analysis/opt
  workers).
- Provide backward-compatible readers keyed off the schema version.

Passes, analyses, opportunities (enabled by the layout)
-------------------------------------------------------
- Opcode histograms / statistics: vector scans over `Opcodes`.
- Liveness/dataflow: compact bitsets indexed by `ValueId`/`InstrId`; SIMD-friendly set ops.
- Type/width analysis: scans over `TypeTable`.
- Hot-field grouping and deterministic serialization for LLIR lowering and codegen.

Tooling, testing, benchmarks
----------------------------
- **Differential (parity) tests** are the core gate for branch by abstraction: run the legacy
  and DOD implementations of a stage (or the whole pipeline) over the same inputs and assert
  equivalent outputs — e.g. identical generated ROM bytes end to end, or equivalent instruction
  streams per stage. Any intended divergence must be explicitly whitelisted.
- Unit tests for each new DOD stage and for blob invariants.
- Golden serialization tests per IR level.
- Microbenchmarks per stage and end to end: compare legacy vs DOD (build IR, optimize, lower,
  generate code).
- CI job running the parity + benchmark suites on a limited dataset and reporting regressions.
- The existing pipeline tests stay green unchanged, because the legacy path is never modified.

Roadmap & checklist (initial)
-----------------------------
- [ ] Write this pipeline-wide design doc (this file)
- [ ] Implement shared `Blob` infrastructure (ids, `Range32`, `StringPool`, `TypeTable`,
      SoA instruction store, `Arena`, `VersionInfo`) + tests
- [ ] HLIR/MLIR/LLIR blob data models
- [ ] Define DOD stage interfaces where shapes diverge (`IDodFrontend`,
      `IDodIRTransformer<TIn, TOut>`, `IDodOptimizer<T>`, `IDodCodeGenerator`)
- [ ] New frontend: `PascalDodAstVisitor` (sibling `PascalBaseVisitor` descendant) +
      `DodHlirBuilder` + `PascalDodFrontend`
- [ ] New `DodHlirToMlirTransformer` (`HLIRBlob -> MLIRBlob`)
- [ ] New `DodMidLevelOptimizer` (over `MLIRBlob`)
- [ ] New `DodMidToLowLevelTransformer` (`MLIRBlob -> LLIRBlob`)
- [ ] New `DodLowLevelOptimizer` (over `LLIRBlob`)
- [ ] New `DodAtari2600CodeGenerator` (+ DOD-friendly `M6502Emitter` path)
- [ ] DOD capability validation over the HLIR table
- [ ] DOD pipeline composition + selection (`CompilationOptions.DataOriented`) at the
      orchestration level (legacy vs DOD)
- [ ] Differential/parity tests (per-stage and end-to-end) + golden serialization tests
- [ ] Microbenchmarks + CI job
- [ ] Flip default to DOD once parity holds; keep legacy behind the flag for one release
- [ ] Remove the legacy object-model pipeline and IR types after the DOD path is proven

Initial file & code layout suggestions
--------------------------------------
- docs/compiler/DataOrientedPipeline.md (this file)
- src/GameVM.Compiler.Core/IR/Blob/ (Ids.cs, Range32.cs, StringPool.cs, TypeTable.cs,
  SymbolTable.cs, InstructionStore.cs, Arena.cs, VersionInfo.cs)
- src/GameVM.Compiler.Core/IR/Blob/HLIRBlob.cs, MLIRBlob.cs, LLIRBlob.cs
- src/GameVM.Compiler.Pascal/ (PascalDodAstVisitor.cs, DodHlirBuilder.cs, PascalDodFrontend.cs)
- src/GameVM.Compiler.Core/IR/Interfaces/ (IDodFrontend.cs, IDodIRTransformer.cs, IDodOptimizer.cs,
  IDodCodeGenerator.cs — the DOD stage abstractions)
- src/GameVM.Compiler.Core/IR/Transformers/DodHlirToMlirTransformer.cs (and MLIR->LLIR, optimizer,
  codegen DOD equivalents in their existing projects)
- src/GameVM.Compiler.Core/IR/Serialization/ (BlobWriter.cs, BlobReader.cs)
- tests/IRBlobTests/
- benchmarks/pipeline_microbench/

Open questions
--------------
1. Do we want one global string pool per compilation, or per-IR-level pools, to balance
   locality vs. dedup and cross-stage id stability?
2. Which instruction fields are "hot" per level and should be first-class SoA columns
   (opcode, type id, operand0/operand1 are likely candidates)?
3. How should the value/operand model unify literals, identifiers, and computed temporaries
   across HLIR/MLIR/LLIR (single `ValueId` space vs. per-level)?
4. Should the flattened top-level LLIR instruction list remain, or can codegen/tests move to
   per-function ranges (removing the duplication in `MidToLowLevelTransformer`)?
5. What non-pipeline consumers (DevTools, external analysis) need compatibility, and for how
   long?

Next steps
----------
- Land the shared `Blob` infrastructure and the HLIR/MLIR/LLIR blob data models, then build the
  parallel DOD stages behind their existing interfaces (starting with `PascalDodFrontend`),
  gating each on differential parity tests against the legacy pipeline before flipping the
  default.
