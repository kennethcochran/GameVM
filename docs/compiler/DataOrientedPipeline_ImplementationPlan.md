# Data-Oriented Pipeline — Implementation Plan

Status: draft
Branch: feature/dod-pipeline
Design: see `docs/compiler/DataOrientedPipeline.md`

Purpose
-------
A concrete, sequenced, testable plan to build the data-oriented (DOD) compilation pipeline via
**branch by abstraction**, as specified in the design doc. The plan is organized into phases;
each phase is a self-contained, reviewable PR that keeps the repository building, keeps every
existing test green, and leaves the **legacy pipeline as the default** until the very end.

Guiding rules for every phase
-----------------------------
- **Never edit the working pipeline in place.** New DOD code is added alongside the legacy code.
  The legacy classes (`PascalFrontend`, `HlirToMlirTransformer`, `DefaultMidLevelOptimizer`,
  `MidToLowLevelTransformer`, `DefaultLowLevelOptimizer`, `Atari2600CodeGenerator`) are not
  modified except for additive DI registration.
- **Legacy stays the default.** `CompilationOptions.DataOriented` defaults to `false`. A DOD run
  only happens when explicitly selected.
- **Green build + green tests on every PR.** Existing tests must remain untouched and passing;
  new code ships with its own tests.
- **Respect the quality gates.** The repo enforces CRAP-score/coverage gates via Husky.NET and
  SonarCloud; new code must meet them.
- **Parity is the acceptance criterion.** DOD stages are accepted when their output matches the
  legacy output (or is a documented, intended improvement) under differential tests.

Prerequisite (P0): unblock the build
------------------------------------
The base branch currently fails to compile — unrelated to this effort, but it blocks CI and
local `dotnet test`, so it must be resolved first (separately or as the first PR in this series):

- `src/GameVM.Compiler.Core/Interfaces/ISemanticAnalyzer.cs` — missing
  `using System.Collections.Generic;` (`List<>`, `Dictionary<,>` unresolved) and a duplicate
  `Success` member on `SemanticAnalysisResult`.
- `src/GameVM.Compiler.Core/SemanticAnalysis/BasicSemanticAnalyzer.cs` — references a type
  `AssignmentInstruction` that does not exist on `HighLevelIR`, plus the same missing-`using`.

Acceptance: `dotnet build` and `dotnet test` succeed on the base branch across the CI matrix
(ubuntu/windows/macos). Until this is fixed, treat DOD CI failures caused by these errors as
pre-existing.

Overall phase map
-----------------
```
P0  Fix pre-existing build break (prerequisite)
P1  Shared Blob infrastructure          (Core/IR/Blob)
P2  IR blob data models                 (HLIRBlob / MLIRBlob / LLIRBlob)
P3  DOD stage interfaces                (IDodFrontend, IDodIRTransformer, IDodOptimizer, IDodCodeGenerator)
P4  Differential test harness           (compare legacy vs DOD; skipped until stages exist)
P5  Frontend DOD                        (PascalDodAstVisitor -> AstBlob -> DodHlirBuilder -> HLIRBlob)
P6  HLIR->MLIR DOD                       (DodHlirToMlirTransformer)
P7  Mid-level optimizer DOD             (DodMidLevelOptimizer)
P8  MLIR->LLIR DOD                       (DodMidToLowLevelTransformer)
P9  Low-level optimizer DOD             (DodLowLevelOptimizer)
P10 Code generation DOD                 (DodAtari2600CodeGenerator + DOD M6502 path)
P11 Capability validation DOD           (HLIR-table scan)
P12 Serialization                       (BlobWriter/BlobReader + golden tests)
P13 Pipeline composition & selection    (DOD ICompileUseCase wiring behind the flag)
P14 End-to-end parity + benchmarks + CI
P15 Flip default to DOD
P16 Remove legacy pipeline
```
Dependencies: P1 -> P2 -> P3 gate everything. Stage phases P5..P11 each depend on the adjacent
blob model and the relevant interface, and can otherwise proceed in the listed order (a natural
producer/consumer order). P13 requires all of P5..P11. P14 requires P13. P15 requires P14 green
over the corpus. P16 requires P15 to have shipped and soaked.

--------------------------------------------------------------------------------
Phase 1 — Shared Blob infrastructure
--------------------------------------------------------------------------------
Deliverables (`src/GameVM.Compiler.Core/IR/Blob/`):
- `Ids.cs` — `readonly struct` id types: `StringId, TypeId, SymbolId, ModuleId, FunctionId,
  BlockId, InstrId, ValueId` (each wraps an `int`; distinct types; `Invalid` sentinel).
- `Range32.cs` — `readonly struct { int Start; int Count; }` with enumerator helpers.
- `StringPool.cs` — dedup store; `StringId Intern(ReadOnlySpan<char>/string)`, `string Get(StringId)`.
- `TypeTable.cs` — `TypeRecord` vector + accessors.
- `SymbolTable.cs` — `SymbolRecord` vector (name id, type id, flags, initial-value slot).
- `InstructionStore.cs` — SoA store: `byte[] Opcodes`, operand pool `int[]`, `TypeId[] Types`,
  source/debug side-table; append + range APIs; capacity reservation.
- `Arena.cs` — append-only bump allocator with reserve + bulk reset.
- `VersionInfo.cs` — magic, schema version, endianness, counts, checksum.

Tests (`test/IRBlobTests/`): round-trip/intern dedup, id distinctness, range enumeration,
store append/read invariants, arena reset. No pipeline wiring.

Acceptance: new project/tests build and pass; nothing else changes.

--------------------------------------------------------------------------------
Phase 2 — IR blob data models
--------------------------------------------------------------------------------
Deliverables (`Core/IR/Blob/`): `HLIRBlob.cs`, `MLIRBlob.cs`, `LLIRBlob.cs` as specified in the
design (module tables, function structs, SoA instruction/node stores, string pool, version).
Include builder-style helpers that reserve capacity and append.

Tests: construct each blob programmatically; assert structural invariants (ranges in bounds,
ids resolvable, counts consistent).

Acceptance: models compile and are unit-tested; no pipeline uses them yet.

--------------------------------------------------------------------------------
Phase 3 — DOD stage interfaces
--------------------------------------------------------------------------------
Deliverables (`Core/IR/Interfaces/`): introduce DOD-specific abstractions where the legacy
shapes don't fit blob types:
- `IDodFrontend` — `string source -> HLIRBlob` (+ error reporting).
- `IDodIRTransformer<TIn, TOut>` — e.g. `HLIRBlob -> MLIRBlob`, `MLIRBlob -> LLIRBlob`.
- `IDodOptimizer<T>` — `(T, OptimizationLevel) -> T` over a blob.
- `IDodCodeGenerator` — `LLIRBlob, CodeGenOptions -> byte[]`.

(Reuse an existing interface only where a blob genuinely fits it; otherwise prefer the new one.)

Tests: none beyond compilation (interfaces only).

Acceptance: interfaces compile; no implementers yet.

--------------------------------------------------------------------------------
Phase 4 — Differential test harness
--------------------------------------------------------------------------------
Deliverables (`test/DodParityTests/`): a harness that, given Pascal source, compiles it via the
legacy pipeline and (once available) the DOD pipeline and compares results — final ROM bytes for
end-to-end, and per-stage instruction streams where a stage-level comparison is meaningful.
Provide a small, growing corpus of Pascal programs (start from existing test inputs / lit tests).

Until DOD stages exist, DOD-side assertions are marked pending/skipped so the harness lands
without failing CI. Each subsequent stage phase flips on the relevant comparisons.

Acceptance: harness builds; legacy-only path exercised; DOD comparisons skipped.

--------------------------------------------------------------------------------
Phase 5 — Frontend DOD
--------------------------------------------------------------------------------
Deliverables (`src/GameVM.Compiler.Pascal/`):
- `PascalDodAstVisitor.cs` — a **new** `PascalBaseVisitor<...>` descendant (sibling of the
  existing `AstVisitor`) that walks the ANTLR parse tree from `parser.program()` and populates an
  `AstBlob` (SoA). Generated types are untouched; the parse tree is read, not stored.
- `AstBlob` (in Core/IR/Blob or Pascal) — SoA AST node storage.
- `DodHlirBuilder.cs` — `AstBlob -> HLIRBlob` (reserve capacities, intern strings once).
- `PascalDodFrontend.cs : IDodFrontend` — wires visitor + builder; produces `HLIRBlob`.

Tests: unit tests for visitor coverage across grammar constructs; HLIR-level differential test
comparing the DOD `HLIRBlob` (via a test-only projection) against the legacy `HighLevelIR` for
the corpus. Turn on the frontend/HLIR comparison in the harness.

Acceptance: DOD frontend produces HLIR equivalent to legacy across the corpus; legacy default
unchanged.

--------------------------------------------------------------------------------
Phase 6 — HLIR -> MLIR DOD
--------------------------------------------------------------------------------
Deliverables: `DodHlirToMlirTransformer : IDodIRTransformer<HLIRBlob, MLIRBlob>` — dense
`NodeKind` loop; expression lowering returns operand ids; emits into MLIR SoA store; constant
folding on ids.

Tests: unit tests mirroring the existing HLIR->MLIR tests; MLIR-level differential comparison
(DOD vs legacy `MidLevelIR`) over the corpus. Enable that comparison in the harness.

Acceptance: MLIR parity across the corpus.

--------------------------------------------------------------------------------
Phase 7 — Mid-level optimizer DOD
--------------------------------------------------------------------------------
Deliverables: `DodMidLevelOptimizer : IDodOptimizer<MLIRBlob>` — constant folding on the
value/operand table; DCE/unreachable via bitset scans; duplicate-assignment removal keyed by id.

Tests: port the semantics of the existing mid-optimizer tests; differential comparison of
optimized MLIR (DOD vs legacy) at each `OptimizationLevel`.

Acceptance: optimized-MLIR parity across corpus and opt levels.

--------------------------------------------------------------------------------
Phase 8 — MLIR -> LLIR DOD
--------------------------------------------------------------------------------
Deliverables: `DodMidToLowLevelTransformer : IDodIRTransformer<MLIRBlob, LLIRBlob>` — address map
keyed by id; SoA LLIR emission; preserve the flattened top-level instruction range as an explicit
range view.

Tests: mirror existing MLIR->LLIR tests; LLIR-level differential comparison.

Acceptance: LLIR parity across corpus.

--------------------------------------------------------------------------------
Phase 9 — Low-level optimizer DOD
--------------------------------------------------------------------------------
Deliverables: `DodLowLevelOptimizer : IDodOptimizer<LLIRBlob>` — peephole (redundant load/store)
over SoA arrays comparing operand ids.

Tests: port existing low-optimizer tests; differential comparison of optimized LLIR.

Acceptance: optimized-LLIR parity.

--------------------------------------------------------------------------------
Phase 10 — Code generation DOD
--------------------------------------------------------------------------------
Deliverables: `DodAtari2600CodeGenerator : IDodCodeGenerator` iterating LLIR SoA by range;
a DOD-friendly `M6502Emitter` path (opcode/operand table before final assembly text). ROM layout
unchanged. Keep `ICapabilityProvider` behavior equivalent.

Tests: mirror existing Atari2600 codegen tests; **byte-exact ROM** differential comparison vs
legacy over the corpus.

Acceptance: identical ROM bytes vs legacy across the corpus (the strongest parity signal).

--------------------------------------------------------------------------------
Phase 11 — Capability validation DOD
--------------------------------------------------------------------------------
Deliverables: DOD capability validation as a scan over the HLIR function table
(`RequiredLevel`, `RequiredExtensionId`), producing the same violation set as
`ICapabilityValidatorService.Validate` for equivalent inputs.

Tests: reuse `CompileUseCaseCapabilityTests` scenarios against the DOD validator; assert
identical violation messages/decisions.

Acceptance: validation parity (including strict-enforcement rejection cases).

--------------------------------------------------------------------------------
Phase 12 — Serialization
--------------------------------------------------------------------------------
Deliverables (`Core/IR/Serialization/`): `BlobWriter.cs` / `BlobReader.cs` for each IR level with
the versioned header; deterministic layout.

Tests: round-trip (write -> read -> equals) and golden-file tests per IR level.

Acceptance: deterministic round-trip and stable golden files. (Can run in parallel with P6..P11;
listed here to avoid blocking the stage work.)

--------------------------------------------------------------------------------
Phase 13 — Pipeline composition & selection
--------------------------------------------------------------------------------
Deliverables:
- A DOD composition of the stages into a full pipeline, exposed at the orchestration level (an
  alternate `ICompileUseCase` implementation/wiring, or a strategy inside it) selected by
  `CompilationOptions.DataOriented` (default `false`).
- Additive DI registration; legacy wiring unchanged and default.

Tests: end-to-end test that compiling with `DataOriented = true` runs entirely on DOD stages and
produces a valid ROM; `DataOriented = false` is byte-identical to today.

Acceptance: both pipelines selectable; default behavior unchanged.

--------------------------------------------------------------------------------
Phase 14 — End-to-end parity + benchmarks + CI
--------------------------------------------------------------------------------
Deliverables: full end-to-end differential run (legacy vs DOD, byte-exact ROM) over the whole
corpus; microbenchmarks per stage and end to end (BenchmarkDotNet); a CI job running parity +
a bounded benchmark set and reporting regressions.

Acceptance: end-to-end ROM parity across the corpus; benchmarks show the expected wins (or are
documented); CI job green.

--------------------------------------------------------------------------------
Phase 15 — Flip default to DOD
--------------------------------------------------------------------------------
Deliverables: change the default so the DOD pipeline runs unless `DataOriented = false`; keep the
legacy path available behind the flag for one release as a fallback.

Acceptance: full suite green with DOD as default; documented rollback via the flag.

--------------------------------------------------------------------------------
Phase 16 — Remove legacy pipeline
--------------------------------------------------------------------------------
Deliverables: remove the legacy object-model IRs, transformers, optimizers, codegen, the
`AstVisitor`/`PascalAstToHlirTransformer`, and any legacy-only interfaces with no remaining
implementers; delete temporary differential adapters.

Acceptance: only the DOD pipeline remains; all tests green; no dead code.

Risks & mitigations
-------------------
- **Hidden legacy behaviors** (e.g. string-based constant folding quirks, the `_temp` assignment
  convention, flattened top-level LLIR list, ROM vector layout). Mitigation: byte-exact ROM
  differential tests and per-stage comparisons over a growing corpus catch divergences early.
- **Corpus coverage gaps.** Mitigation: seed from existing unit/BDD/lit inputs; add a program
  for each grammar construct and each capability rule; grow the corpus whenever a divergence is
  found.
- **Quality gates (CRAP/coverage).** Mitigation: land infra with tests first; keep stages small
  and well-covered.
- **Scope creep into ANTLR-generated code.** Mitigation: the frontend seam is a *new* sibling
  visitor; generated types are never edited.

Definition of done
------------------
The DOD pipeline is the default, produces byte-identical ROMs to the historical legacy output
across the corpus (or documented improvements), meets the quality gates, is benchmarked, and the
legacy pipeline has been removed.
