using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Holds shared state during AST transformation (DOD pipeline - slab-based)
    /// </summary>
    public class TransformationContext
    {
        public uint[] IrSlab { get; }
        public StringPool StringPool { get; }
        public string SourceFile { get; }
        public Dictionary<string, string> TypeCache { get; }
        private readonly List<Dictionary<string, IRSymbol>> _symbolTables;
        public Dictionary<string, IRSymbol> SymbolTable => _symbolTables[_symbolTables.Count - 1];
        public Stack<uint> FunctionScope { get; }
        public List<string> Errors { get; }

        public TransformationContext(string sourceFile, uint[] irSlab, StringPool stringPool)
        {
            SourceFile = sourceFile ?? "<unknown>";
            IrSlab = irSlab ?? throw new ArgumentNullException(nameof(irSlab));
            StringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
            TypeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _symbolTables = new List<Dictionary<string, IRSymbol>> { new(StringComparer.OrdinalIgnoreCase) };
            FunctionScope = new Stack<uint>();
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

        public bool TryGetSymbol(string name, out IRSymbol? symbol)
        {
            for (int i = _symbolTables.Count - 1; i >= 0; i--)
            {
                if (_symbolTables[i].TryGetValue(name, out var foundSymbol))
                {
                    symbol = foundSymbol;
                    return true;
                }
            }
            symbol = null;
            return false;
        }

        public IRSymbol? LookupSymbol(string name)
        {
            for (int i = _symbolTables.Count - 1; i >= 0; i--)
            {
                if (_symbolTables[i].TryGetValue(name, out var symbol))
                    return symbol;
            }
            return null;
        }

        public void AddError(string message)
        {
            Errors.Add(message);
        }
    }
}
