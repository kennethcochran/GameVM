# GameVM Agent Instructions — docs/

This file provides guidance for AI assistants and coding agents working on documentation
and docs-related workflows in the GameVM project. It supplements the [root AGENTS.md](../AGENTS.md).

## Documentation Update Rules (MANDATORY)

**Every time you modify code, you MUST also update the affected documentation in the same session.**
Documentation drift is the #1 tracked problem in this repo — do not ship code changes without
syncing docs. When in doubt, err on the side of updating docs.

### Trigger Mapping

Use `.github/doc-mapping.yaml` as the authoritative mapping. The common cases:

| Code Change | Docs to Update |
|-------------|----------------|
| New/updated LLIR instruction | `/docs/compiler/LLIR_ISA.md`, `/docs/compiler/LLIR.md` |
| New/updated HLIR or MLIR construct | `/docs/compiler/HLIR.md`, `/docs/compiler/MLIR.md` |
| New/updated backend or platform target | `/docs/platforms/specs/<platform>.md`, `/docs/platforms/README.md` |
| New/updated optimizer pass | `/docs/optimization.md` |
| New/updated frontend or language feature | `/docs/compiler/Parser.md`, `/docs/compiler/TypeSystem.md`, `/docs/compiler/LanguageIntegration.md` |
| Public API surface change (types/methods/signatures) | `/docs/api/` + XML doc comments |
| Architecture or pipeline change | `/docs/architecture/ArchitectureOverview.md`, `/docs/compiler/compiler_architecture.md` |
| Dispatch strategy change | `/docs/code-generation.md` |
| Capability / platform profile change | `/docs/platforms/CapabilityProfiles.md` |
| IR stage, slab, or transformer change | `/docs/compiler/HLIR.md`, `/docs/compiler/MLIR.md`, `/docs/compiler/LLIR.md` |
| Build system / tooling change | `/docs/compiler/BuildSystem.md` |

### Update Procedure

1. **Read** the affected doc file(s) first (do not guess their current content).
2. **Apply the status-tagging convention**: tag each section with one or more of
   `[implemented]`, `[aspirational]` (planned, not built), or `[outdated]`
   (built differently than documented, or describing removed functionality).
3. **Update** code examples and API references to match the current implementation.
4. **Verify** relative links still resolve and section headers still exist.
5. **Update** the document's changelog section (if present).
6. When an interface, capability, or behavior is *removed*, tag the doc section
   `[outdated]` and describe the replacement — do not silently delete coverage.

### When NOT to Update Docs

- Pure refactors with no behavior/API change (but verify by reading the docs)
- Test-only changes that add coverage without changing behavior
- Dependency version bumps with no API change

### Spec Workflow (OpenSpec)

GameVM uses OpenSpec for spec-driven changes. When working on an active change
under `openspec/changes/`:

- Keep the change's `proposal.md`, `design.md`, and `tasks.md` in sync as you work.
- Add a documentation task to `tasks.md` when the change affects documented behavior:
  `- [ ] Update affected documentation per AGENTS.md trigger mapping`
- When a change ships, its specs are merged into `openspec/specs/` via `openspec sync`.

## Adding Documentation

- Follow the existing documentation structure
- Reference related documents using relative links
- Include code examples where appropriate
- Update `/docs/README.md` if adding new sections
- See `/docs/architecture/DocumentationStandards.md` for style guidelines

## Documentation References

Key documents to consult:
- **Architecture**: [Architecture Overview](architecture/ArchitectureOverview.md)
- **LLIR Specification**: [LLIR ISA](compiler/LLIR_ISA.md)
- **Inline Assembly**: [Inline Assembly Guide](compiler/InlineAssembly.md)
- **Optimization**: [Optimization Features](optimization.md)
- **Code Generation**: [Code Generation Strategies](code-generation.md)
