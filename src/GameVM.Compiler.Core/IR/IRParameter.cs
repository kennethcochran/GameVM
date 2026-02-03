namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Function parameter
/// </summary>
public class IRParameter : IRNode
{
    public string Name { get; set; } = string.Empty;
    public IRType Type { get; set; } = null!;
    public bool HasDefaultValue { get; set; }
    public object DefaultValue { get; set; } = null!;

    protected IRParameter() { }
}