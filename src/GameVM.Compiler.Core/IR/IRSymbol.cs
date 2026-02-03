namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Symbol information for variables and constants
/// </summary>
public class IRSymbol : IRNode
{
    public string Name { get; set; } = string.Empty;
    public IRType Type { get; set; } = null!;
    public bool IsConstant { get; set; }
    public object InitialValue { get; set; } = null!;
}