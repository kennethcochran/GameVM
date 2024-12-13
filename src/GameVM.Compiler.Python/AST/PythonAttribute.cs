using System;

namespace GameVM.Compiler.Python.AST
{
    public class PythonAttribute : PythonExpression
    {
        public PythonExpression Target { get; }
        public string Name { get; }

        public PythonAttribute(PythonExpression target, string name, string sourceFile) : base(sourceFile)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
