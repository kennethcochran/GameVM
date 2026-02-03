namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Source code location information
/// </summary>
public class IRSourceLocation
{
    public string File { get; set; } = string.Empty;
    public int Line { get; set; } = 1;
    public int Column { get; set; } = 1;
}