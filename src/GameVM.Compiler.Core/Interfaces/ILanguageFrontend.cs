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

using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to IR.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code into AST slab (DOD pipeline)
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>AST slab as uint array</returns>
        uint[] ParseToSlab(string sourceCode);

        /// <summary>
        /// Gets the syntax error messages from the last parse attempt (DOD pipeline).
        /// Populated when ParseToSlab encounters syntax errors.
        /// </summary>
        IReadOnlyList<string>? LastParseErrors { get; }

        /// <summary>
        /// Gets the string pool from the last parse attempt (DOD pipeline).
        /// Populated after successful ParseToSlab.
        /// </summary>
        StringPool? StringPool { get; }

        /// <summary>
        /// Convert AST slab to HLIR slab (DOD pipeline)
        /// </summary>
        /// <param name="astSlab">AST slab to convert</param>
        /// <returns>HLIR slab as uint array</returns>
        uint[] ConvertToHlirSlab(uint[] astSlab);
    }
}