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
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Ast;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to IR.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code into AST tree (DOD pipeline) - returns AoS AstTree
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>AST tree as AstTree</returns>
        AstTree ParseToSlab(string sourceCode);

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
        /// Convert AST tree to HLIR slab (DOD pipeline) - takes AstTree, returns InstList
        /// </summary>
        /// <param name="astTree">AST tree to convert</param>
        /// <returns>HLIR slab as InstList</returns>
        InstList ConvertToHlirSlab(AstTree astTree);
    }
}