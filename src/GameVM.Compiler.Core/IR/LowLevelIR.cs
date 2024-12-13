namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// Low-level intermediate representation.
    /// This is closer to the target architecture.
    /// </summary>
    public class LowLevelIR : IIntermediateRepresentation
    {
        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        public string SourceFile { get; set; }

        // Add additional properties specific to low-level IR
    }
}
