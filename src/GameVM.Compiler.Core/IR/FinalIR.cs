namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// Final intermediate representation.
    /// This is the last IR form before code generation.
    /// </summary>
    public class FinalIR : IIntermediateRepresentation
    {
        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        public string SourceFile { get; set; }

        // Add additional properties specific to final IR
    }
}
