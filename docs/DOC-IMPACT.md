# Documentation Impact Checklist for Agents

This checklist guides agents on *which* documentation to update when making code changes. Documentation must land in the same commit as the code it describes.

Always ensure your commit includes updates to the relevant documentation unless the change has genuinely no impact.

---

## 1. Change Type → Documentation

| If your change...                            | Update this documentation                                      |
| :------------------------------------------- | :-------------------------------------------------------------------- |
| **Introduces a new public API** (class, method, interface, enum, struct) or **alters an existing one's signature/behavior.**  | `CONTEXT.md` (Core APIs section) + `docs/api/InterfaceSpecification.md` (if external)  |
| **Changes core architectural invariants**, pipeline stages, or fundamental data flow (e.g., HLIR, MLIR, LLIR stages, StringPool behavior).                                  | `CONTEXT.md` (High-Level Architecture section, Key Invariants) + potentially a new ADR in `docs/adr/`    |
| **Adds a new domain term**, clarifies an existing term, or changes an existing term's definition.         | `CONTEXT.md` (Domain Glossary section)                                |
| **Implements a new planned feature** or **makes progress on an existing spec.**                                                     | Planned work is tracked as a `.scratch/<feature>/spec.md` spec (or the feature's ticket); implemented behavior lands in `CONTEXT.md`/`docs/` in the same commit |
| **Fixes a bug, refactors internals, or improves performance** with no observable behavior/API change. | No dedicated doc update required unless it unearths a new architectural insight (then consider ADR) |
| **Adds / modifies a test case.**             | No dedicated doc update required.                                     |
| **Pure file rename/move.**                  | No dedicated doc update required.                                     |

---

## 2. When to Override

In rare cases, a semantic change may genuinely have *no documentation impact* (e.g., refactoring internal code without changing external behavior). To bypass the `doc-sync-gate.csx` hook, you **MUST** add `override-no-doc: <reason>` to your commit message. The `<reason>` must clearly explain why no documentation update is needed. This is auditable and will be reviewed.

**Example commit message with override:**

```
feat: improved opcode dispatch performance

Optimized the internal instruction dispatcher by replacing a linear scan
with a jump table. No change to public API or observable behavior.

override-no-doc: Internal performance refactor, no API or architectural change.
```
