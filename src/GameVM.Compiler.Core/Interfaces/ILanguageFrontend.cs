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

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for language frontends that can compile source code to IR.
    /// </summary>
    public interface ILanguageFrontend
    {
        /// <summary>
        /// Parse source code into high-level IR
        /// </summary>
        /// <param name="sourceCode">Source code to parse</param>
        /// <returns>High-level IR representation</returns>
        HighLevelIR Parse(string sourceCode);

        /// <summary>
        /// Convert high-level IR to mid-level IR
        /// </summary>
        /// <param name="hlir">High-level IR to convert</param>
        /// <returns>Mid-level IR representation</returns>
        MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir);
    }
}
