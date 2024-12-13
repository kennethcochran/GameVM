namespace GameVM.Compiler.Python
{
    using Antlr4.Runtime.Tree;
    using GameVM.Compiler.Core.IR;
    using GameVM.Compiler.Python.AST;


    /// <summary>
    /// Interface for building a Python AST from a parse tree.
    /// </summary>
    public interface IPythonASTBuilder
    {
        
        /// <summary>
        /// Uses the parse tree to generate a Python AST.
        /// </summary>
        /// <param name="parseTree">The parse tree to visit.</param>
        /// <returns>The root node of the generated Python AST.</returns>
        PythonASTNode Build(IParseTree parseTree);
    }
}
