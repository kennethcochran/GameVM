using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Core.HLIR.Nodes;
using GameVM.Compiler.Core.HLIR.Nodes.Expressions;
using GameVM.Compiler.Core.HLIR.Nodes.Statements;
using GameVM.Compiler.Core.HLIR.Symbols;
using GameVM.Compiler.Core.HLIR.Types;

namespace GameVM.Compiler.CSharp.Transformers
{
    /// <summary>
    /// Base class for transforming AST to HLIR
    /// </summary>
    public abstract class AstToHlirTransformer : IParseTreeVisitor<HLIRNode>
    {
        protected readonly SymbolTable SymbolTable = new SymbolTable();
        protected HLIRType CurrentReturnType = HLIRType.Void;

        // Error handling
        protected readonly List<CompilationError> Errors = new List<CompilationError>();
        public IReadOnlyList<CompilationError> CompilationErrors => Errors.AsReadOnly();

        // Abstract methods that must be implemented by language-specific transformers
        public abstract HLIRNode Visit(IParseTree tree);
        public abstract HLIRNode VisitChildren(IRuleNode node);
        public abstract HLIRNode VisitTerminal(ITerminalNode node);
        public abstract HLIRNode VisitErrorNode(IErrorNode node);

        // Helper methods for common transformations
        protected SourceLocation GetSourceLocation(ParserRuleContext context)
        {
            return new SourceLocation(
                file: context.Start.InputStream.SourceName,
                line: context.Start.Line,
                column: context.Start.Column,
                length: context.Stop.StopIndex - context.Start.StartIndex + 1
            );
        }

        protected void Error(string message, ParserRuleContext context)
        {
            Errors.Add(new CompilationError
            {
                Message = message,
                Location = GetSourceLocation(context),
                Severity = ErrorSeverity.Error
            });
        }

        protected void Warning(string message, ParserRuleContext context)
        {
            Errors.Add(new CompilationError
            {
                Message = message,
                Location = GetSourceLocation(context),
                Severity = ErrorSeverity.Warning
            });
        }

        // Type conversion helpers
        protected HLIRType ConvertType(ITypeSymbol typeSymbol)
        {
            // TODO: Implement type conversion from language-specific type to HLIRType
            // This is a placeholder implementation
            return typeSymbol.Name.ToLower() switch
            {
                "integer" => HLIRType.Int32,
                "real" => HLIRType.Real,
                "boolean" => HLIRType.Boolean,
                "char" => HLIRType.Char,
                "string" => HLIRType.String,
                _ => throw new NotSupportedException($"Unsupported type: {typeSymbol.Name}")
            };
        }
    }

    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error
    }

    public class CompilationError
    {
        public string Message { get; set; }
        public SourceLocation Location { get; set; }
        public ErrorSeverity Severity { get; set; }

        public override string ToString()
        {
            var severity = Severity.ToString().ToUpper();
            return $"{Location.File}({Location.Line},{Location.Column}): {severity}: {Message}";
        }
    }

    // Placeholder for language-specific type symbol
    public interface ITypeSymbol
    {
        string Name { get; }
    }
}
