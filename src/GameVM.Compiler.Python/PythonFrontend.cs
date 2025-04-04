/*
 * PythonFrontend.cs
 * 
 * This class serves as the primary interface between the GameVM compiler infrastructure
 * and the Python-specific compilation components. It coordinates the overall compilation
 * process for Python source code and integrates with the larger compilation pipeline.
 * 
 * Key responsibilities:
 * 1. Initializes and configures the Python compiler components
 * 2. Manages compiler options and settings
 * 3. Provides error handling and reporting
 * 4. Integrates with the GameVM compilation pipeline
 * 
 * Dependencies:
 * - PythonLanguageParser.cs: Main parser entry point
 * - PythonParserRegistration.cs: Parser registration with GameVM
 * - GameVM.Compiler.Core.Interfaces: Core compiler interfaces
 * 
 * This class implements ILanguageFrontend to integrate with the GameVM compiler.
 */

using System;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Python.AST;
using GameVM.Compiler.Python.ANTLR;
using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Python
{
    /// <summary>
    /// Python language frontend implementation
    /// </summary>
    public class PythonFrontend : ILanguageFrontend
    {
        private readonly PythonParseTreeToAst _astBuilder;

        public PythonFrontend(PythonParseTreeToAst astBuilder)
        {
            _astBuilder = astBuilder ?? throw new ArgumentNullException(nameof(astBuilder));
        }

        /// <summary>
        /// Parse Python source code into HLIR
        /// </summary>
        public HighLevelIR Parse(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
                throw new ArgumentNullException(nameof(sourceCode));

            // Create input stream from source code
            var inputStream = new AntlrInputStream(sourceCode);

            // Create lexer and parser
            var lexer = new PythonLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PythonParser(tokenStream);

            // Add error listener to capture syntax errors
            var errorListener = new ParserErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            // Parse to AST
            var parseTree = parser.file_input();

            // Check for syntax errors
            if (errorListener.HasErrors)
            {
                throw new ParserException(errorListener.GetErrorMessage());
            }

            var ast = _astBuilder.Visit(parseTree);

            if (ast is not ModuleNode _)
                throw new InvalidOperationException("Failed to generate Python AST");

            // Convert AST to HLIR
            var hlirConverter = new PythonASTToHLIR();
            return hlirConverter.Convert();
        }

        /// <summary>
        /// Convert high-level IR to mid-level IR
        /// </summary>
        public MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir)
        {
            if (hlir == null)
                throw new ArgumentNullException(nameof(hlir));

            // TODO: Implement conversion to MLIR
            var mlirBuilder = new MLIRBuilder();
            return mlirBuilder.Transform(hlir);
        }
    }
}
