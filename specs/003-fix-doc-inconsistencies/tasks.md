---

description: "Task list for Fix Documentation Inconsistencies"
---

# Tasks: Fix Documentation Inconsistencies

**Input**: Design documents from `/specs/003-fix-doc-inconsistencies/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: The examples below include test tasks. Tests are OPTIONAL - only include them if explicitly requested in the feature specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 1: Setup

**Purpose**: Project initialization and basic structure

- [x] T001 Review `plan.md` in `specs/003-fix-doc-inconsistencies/plan.md` to confirm understanding of the implementation strategy.
- [x] T002 Review `spec.md` in `specs/003-fix-doc-inconsistencies/spec.md` to confirm understanding of user stories and priorities.
- [x] T003 Review `data-model.md` in `specs/003-fix-doc-inconsistencies/data-model.md` to understand the conceptual entities.
- [x] T004 Review `research.md` in `specs/003-fix-doc-inconsistencies/research.md` to understand decisions on tooling for link checking and terminology.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Identify and install a suitable Markdown parser library for C#/.NET for processing documentation files (e.g., `Markdig`).
- [x] T006 Set up a project or script to encapsulate documentation scanning logic. This could be a new C# project in `src/GameVM.Docs.Tools/`
- [x] T007 Implement basic file scanning functionality to read all `.md` files in the `docs/` directory.

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Resolve Cross-Reference Issues (Priority: P1) 🎯 MVP

**Goal**: Resolve cross-reference inconsistencies and missing file references so that the documentation is internally consistent and all links point to existing files.

**Independent Test**: Can be fully tested by verifying all cross-references in documentation resolve to existing files, and the documentation index accurately reflects available content.

### Implementation for User Story 1

- [x] T008 [US1] Implement functionality to extract all internal and external links from Markdown files using the chosen Markdown parser. (e.g. `src/GameVM.Docs.Tools/LinkExtractor.cs`)
- [x] T009 [US1] Implement functionality to validate internal links by checking if the target file or anchor exists within the project's `docs/` directory. (e.g. `src/GameVM.Docs.Tools/LinkValidator.cs`)
- [x] T010 [P] [US1] Implement functionality to validate external links by making HTTP requests (with appropriate timeouts and error handling). (e.g. `src/GameVM.Docs.Tools/LinkValidator.cs`)
- [x] T011 [US1] Generate a report of broken internal and external links, including file path, line number, and a description of the issue. (e.g. `src/GameVM.Docs.Tools/LinkReportGenerator.cs`)
- [x] T012 [US1] Manually fix all identified broken links in `docs/` according to the generated report. (This is a manual step for the user, but a task to acknowledge it)
- [x] T013 [US1] Verify all cross-references in documentation resolve to existing files using the link validation tool.

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Standardize Documentation Terminology (Priority: P1)

**Goal**: Standardize terminology and descriptions across all documentation files so that the architecture is described consistently throughout the project.

**Independent Test**: Can be fully tested by reviewing all documentation files to ensure consistent use of technical terms and architectural concepts.

### Implementation for User Story 2

- [x] T014 [US2] Define a controlled vocabulary (glossary) for key terms in a configuration file (e.g. `docs/terminology.json` or `docs/glossary.md`).
- [x] T015 [US2] Implement functionality to extract all terms from documentation files. (e.g. `src/GameVM.Docs.Tools/TerminologyExtractor.cs`)
- [x] T016 [US2] Implement functionality to compare extracted terms against the controlled vocabulary and identify inconsistencies (e.g., misspellings, unapproved variations). (e.g. `src/GameVM.Docs.Tools/TerminologyChecker.cs`)
- [x] T017 [US2] Generate a report of terminology inconsistencies, including file path, line number, and suggested corrections. (e.g. `src/GameVM.Docs.Tools/TerminologyReportGenerator.cs`)
- [x] T018 [US2] Manually update terminology in `docs/` according to the generated report and the controlled vocabulary.
- [x] T019 [US2] Verify consistent use of technical terms across documentation files using the terminology checking tool.

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Create Missing Referenced Files (Priority: P2)

**Goal**: Create missing files that are referenced by existing documentation so that all cross-references resolve to actual content.

**Independent Test**: Can be fully tested by verifying all cross-references resolve to existing files, and no broken links remain in the documentation.

### Implementation for User Story 3

- [ ] T020 [US3] Extend the link validation tool to specifically identify internal links that point to non-existent files. (e.g. `src/GameVM.Docs.Tools/LinkValidator.cs`)
- [ ] T021 [US3] Generate a report of internal links pointing to missing files. (e.g. `src/GameVM.Docs.Tools/LinkReportGenerator.cs`)
- [ ] T022 [US3] For each missing referenced file identified, create a placeholder Markdown file (e.g., `docs/path/to/missing-file.md`) with a "TODO: Content to be added" message.
- [ ] T023 [US3] Manually fill in the content for newly created placeholder files in `docs/`.
- [ ] T024 [US3] Verify that all previously identified missing files now exist and cross-references resolve correctly.

**Checkpoint**: All user stories should now be independently functional

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T025 Integrate documentation tools (link checker, terminology checker) into the CI/CD pipeline to automate future consistency checks.
- [ ] T026 Update `GEMINI.md` and `CODE_OF_CONDUCT.md` if any changes in documentation practices warrant it. (Placeholder - not explicitly required by spec, but good practice)
- [ ] T027 Document the usage of the new documentation tools and processes. (e.g. `docs/tooling/documentation-tools.md`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (if tests requested):
Task: "T009 [US1] Implement functionality to validate internal links by checking if the target file or anchor exists within the project's docs/ directory. (e.g. src/GameVM.Docs.Tools/LinkValidator.cs)"
Task: "T010 [P] [US1] Implement functionality to validate external links by making HTTP requests (with appropriate timeouts and error handling). (e.g. src/GameVM.Docs.Tools/LinkValidator.cs)"

# Launch all models for User Story 1 together:
Task: "T008 [US1] Implement functionality to extract all internal and external links from Markdown files using the chosen Markdown parser. (e.g. src/GameVM.Docs.Tools/LinkExtractor.cs)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence