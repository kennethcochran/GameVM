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
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to IR.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code into high-level IR (legacy OOP interface)
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>High-level IR representation</returns>
#pragma warning disable S1133
        [System.Obsolete("Use ParseToSlab for DOD pipeline. Will be removed in future version.")]
        HighLevelIR Parse(string sourceCode);
#pragma warning restore S1133

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

        /// <summary>
        /// Convert high-level IR to mid-level IR (legacy OOP interface)
        /// </summary>
        /// <param name="hlir">High-level IR to convert</param>
        /// <returns>Mid-level IR representation</returns>
#pragma warning disable S1133
        [System.Obsolete("Use ConvertToHlirSlab for DOD pipeline. Will be removed in future version.")]
        MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir);
#pragma warning restore S1133
    }
}