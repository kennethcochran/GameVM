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
        public HighLevelIR IR { get; }
        public string SourceFile { get; }
        public Dictionary<string, HighLevelIR.HLType> TypeCache { get; }
        private readonly List<Dictionary<string, IRSymbol>> _symbolTables;
        public Dictionary<string, IRSymbol> SymbolTable => _symbolTables.Last();
        public Stack<HighLevelIR.Function> FunctionScope { get; }
        public List<string> Errors { get; }

        public TransformationContext(string sourceFile, HighLevelIR ir)
        {
            SourceFile = sourceFile ?? "<unknown>";
            IR = ir ?? throw new ArgumentNullException(nameof(ir));
            TypeCache = new Dictionary<string, HighLevelIR.HLType>(StringComparer.OrdinalIgnoreCase);
            _symbolTables = new List<Dictionary<string, IRSymbol>> { new(StringComparer.OrdinalIgnoreCase) };
            FunctionScope = new Stack<HighLevelIR.Function>();
            Errors = new List<string>();
        }

        public void PushScope()
        {
            _symbolTables.Add(new Dictionary<string, IRSymbol>(StringComparer.OrdinalIgnoreCase));
        }

        public void PopScope()
        {
            if (_symbolTables.Count > 1)
            {
                _symbolTables.RemoveAt(_symbolTables.Count - 1);
            }
        }

        public bool TryGetSymbol(string name, out IRSymbol symbol)
        {
            for (int i = _symbolTables.Count - 1; i >= 0; i--)
            {
                if (_symbolTables[i].TryGetValue(name, out symbol))
                {
                    return true;
                }
            }
            symbol = null;
            return false;
        }

        public IRSymbol LookupSymbol(string name)
        {
            for (int i = _symbolTables.Count - 1; i >= 0; i--)
            {
                if (_symbolTables[i].TryGetValue(name, out var symbol))
                    return symbol;
            }
            return null;
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
