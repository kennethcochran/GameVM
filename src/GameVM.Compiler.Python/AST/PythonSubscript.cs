using System;

namespace GameVM.Compiler.Python.AST
{
    public class PythonSubscript : PythonExpression
    {
        public PythonExpression Target { get; }
        public PythonExpression Index { get; }

        public PythonSubscript(PythonExpression target, PythonExpression index, string sourceFile) : base(sourceFile)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Index = index ?? throw new ArgumentNullException(nameof(index));
        }
    }
}
