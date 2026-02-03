using System.Collections.Generic;

namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Type information in IR
/// </summary>
public class IRType : IRNode
{
    public required string Name { get; set; }
    public bool IsBuiltin { get; set; }
    public List<IRField> Fields { get; set; } = new();
    public List<IRFunction> Methods { get; set; } = new();
}