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

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to HLIR.
    /// Single method: string → HlirTree. Parse tree is frontend-internal.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code and transform to HLIR semantic tree (DOD pipeline).
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>HLIR semantic tree</returns>
        HlirTree ParseToHlir(string sourceCode);

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
    }
}