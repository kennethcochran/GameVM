# API Documentation

The compiler's **implemented public API is canonical in [`CONTEXT.md`](../../CONTEXT.md)** under *Core APIs (C#)*: `InstList`, `InstListBuilder`, `StringPool`, and the IR transformer/optimizer/codegen interfaces in `GameVM.Compiler.Core`.

**Planned / aspirational API surface** (HAL interfaces, standard library, language features, runtime/VM interfaces) lives as OpenSpec specs in [`openspec/specs/`](../../openspec/specs/) — see `hal-interfaces/`, `standard-library/`, `common-language-features/`, `runtime-interfaces/`.

This `api/` directory no longer holds separate reference files; implemented API is in `CONTEXT.md`, aspirational API is in `openspec/specs/`.