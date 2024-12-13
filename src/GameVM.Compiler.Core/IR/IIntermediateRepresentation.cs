namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// Base interface for all intermediate representations
    /// </summary>
    public interface IIntermediateRepresentation
    {
        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        string SourceFile { get; set; }
    }
}
