namespace GameVM.Compiler.Core.IR;

/// <summary>
/// Base class for all IR nodes
/// </summary>
public abstract class IRNode
{
    public string SourceFile { get; set; } = string.Empty;

    protected IRNode() { }
    protected IRNode(string sourceFile)
    {
        SourceFile = sourceFile;
    }

    /// <summary>
    /// Source location information
    /// </summary>
    public IRSourceLocation Location { get; set; } = new IRSourceLocation { File = string.Empty, Line = 1, Column = 1 };
}