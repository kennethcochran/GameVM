# Architecture Documentation

GameVM's **implemented architecture** is canonical in [`CONTEXT.md`](../../CONTEXT.md) (high-level pipeline, key invariants, core C# APIs, current platform reality). See the Three-Pocket Strategy in [`../AGENTS.md`](../AGENTS.md).

**Planned / aspirational architecture** (HAL, VM runtime, package management, performance, maintenance, versioning, test/verification) lives as OpenSpec specs in [`openspec/specs/`](../../openspec/specs/) — see `hal-interfaces/`, `package-management/`, `performance-guidelines/`, `performance-specs/`, `maintenance/`, `versioning-strategy/`, `testing-verification/`, `test-specification/`.

This `architecture/` directory retains the **process / reference** guides that are still current:

- [BehaviorSpecification.md](BehaviorSpecification.md) — behavior specification guidelines
- [DocumentationStandards.md](DocumentationStandards.md) — documentation style & process
- [SDDGuide.md](SDDGuide.md) — Software Design Document guide
- [SDDWorkflow.md](SDDWorkflow.md) — SDD workflow

Related: [Compiler](../compiler/README.md) · [Platforms](../platforms/README.md) · [API](../api/README.md).