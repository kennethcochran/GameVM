using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Holds shared state during AST transformation
    /// </summary>
    public class TransformationContext
    {
        public string SourceFile { get; }
        public Dictionary<string, HighLevelIR.HLType> TypeCache { get; }
        public Dictionary<string, IRSymbol> SymbolTable { get; }
        public Stack<HighLevelIR.Function> FunctionScope { get; }
        public List<string> Errors { get; }

        public TransformationContext(string sourceFile)
        {
            SourceFile = sourceFile ?? "<unknown>";
            TypeCache = new Dictionary<string, HighLevelIR.HLType>();
            SymbolTable = new Dictionary<string, IRSymbol>();
            FunctionScope = new Stack<HighLevelIR.Function>();
            Errors = new List<string>();
        }

        public HighLevelIR.HLType GetOrCreateBasicType(string typeName)
        {
            if (!TypeCache.TryGetValue(typeName, out var type))
            {
                type = new HighLevelIR.BasicType(SourceFile, typeName);
                TypeCache[typeName] = type;
            }
            return type;
        }

        public void AddError(string message)
        {
            Errors.Add(message);
        }
    }
}
