namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Field in a type definition
/// </summary>
public class IRField : IRNode
{
    public required string Name { get; set; }
    public required IRType Type { get; set; }
    public bool IsStatic { get; set; }
}