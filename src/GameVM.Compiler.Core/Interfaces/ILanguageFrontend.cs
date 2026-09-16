/*
 * ILanguageFrontend.cs
 * 
 * Defines the interface for language-specific frontends.
 * Handles source language processing:
 * - Lexical analysis
 * - Syntax parsing
 * - Semantic analysis
 * - Source-level optimizations
 * - High-level IR generation
 * - Language-specific features
 * 
 * Enables multi-language support in GameVM.
 */

using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to HLIR.
    /// Single method: string → ParseResult (HlirTree + SymbolTable). Parse tree is frontend-internal.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code and transform to HLIR semantic tree with symbol table (DOD pipeline).
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>Parse result containing HLIR semantic tree and symbol table</returns>
        ParseResult ParseToHlir(string sourceCode);

        /// <summary>
        /// Gets the syntax error messages from the last parse attempt (DOD pipeline).
        /// Populated when ParseToHlir encounters syntax errors.
        /// </summary>
        IReadOnlyList<string>? LastParseErrors { get; }

        /// <summary>
        /// Gets the string pool from the last parse attempt (DOD pipeline).
        /// Populated after successful ParseToHlir.
        /// </summary>
        StringPool? StringPool { get; }

        /// <summary>
        /// Gets the structured semantic errors (with source positions) from the last
        /// parse attempt. Populated when the semantic-analysis pass rejects the program.
        /// </summary>
        IReadOnlyList<SemanticError>? SemanticErrors { get; }
    }
}