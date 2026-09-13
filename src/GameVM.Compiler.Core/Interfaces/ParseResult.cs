using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Interfaces;

/// <summary>
/// Result of parsing source code to HLIR. Bundles the semantic tree with
/// the symbol table populated during the same parse pass.
/// </summary>
public readonly record struct ParseResult(HlirTree Hlir, SymbolTable Symbols);
