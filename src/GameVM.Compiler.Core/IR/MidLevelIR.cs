namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// Mid-level intermediate representation.
    /// This is a more optimized form after initial transformations.
    /// </summary>
    public class MidLevelIR : IIntermediateRepresentation
    {
        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        public string SourceFile { get; set; }

        // Add additional properties specific to mid-level IR
    }
}
