# Project documentation index

This folder contains design notes and system reference documents for GameVM.

Quick links

- design/
  - `atari2600_implementation.md` — Implementation notes and strategy for Atari 2600 (cycle-exact, memory constraints).
  - `common_language_features.md` — Language feature constraints and recommendations for target platforms.
  - `HLIR.md` — High-Level Intermediate Representation overview.
  - `MLIR.md` — Mid-Level IR (resource analysis and optimizations).
  - `LLIR.md` — Low-Level IR (instruction mapping and final lowering).
  - `InternalAssemblyAPI.md` — Design for an internal assembly-like API used during compilation.

- systems/
  - `atari2600.md` — Hardware reference for Atari 2600 (6507/6502 family).
  - `channelf.md` — Hardware reference for Fairchild Channel F (F8 CPU).
  - `colecovision.md` — Hardware reference and GameVM notes for ColecoVision (Z80/TMS9928A).
  - `intellivision.md` — Hardware reference for Mattel Intellivision (CP1610/STIC).
  - `nes.md` — Hardware reference for NES/Famicom (Ricoh 2A03 and PPU/APU constraints).
  - `vectrex.md` — Hardware reference for Vectrex (6809, vector display).
  - `TEMPLATE.md` — Template for adding new system docs.

Notes & recommendations

- Many files are concise and focused; they look ready to be cross-linked from a central docs README (this file).
- Some markdown files appear wrapped in code-fences or have inconsistent formatting; consider running a markdown linter and normalizing headings/code fences.
- Consider adding a Table of Contents and explicit ‘last updated’ or author metadata in each doc.

Suggested next steps (I can do these if you want):
- Run a markdown linter and automatically fix formatting issues.
- Normalize front-matter (e.g., title, description, updated date) for each file.
- Generate a consolidated HTML documentation site (MkDocs or similar).

