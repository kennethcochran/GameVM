using System.Collections.Generic;

namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Function definition in IR
/// </summary>
public class IRFunction : IRNode
{
    public string Name { get; set; } = string.Empty;
    public IRType ReturnType { get; set; } = null!;
    public List<IRParameter> Parameters { get; set; } = new();
    public List<IRNode> Body { get; set; } = new();

    protected IRFunction() { }
}