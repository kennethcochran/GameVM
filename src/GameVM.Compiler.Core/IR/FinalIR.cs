using System.Collections.Generic;

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
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// The final instructions or tokens to be emitted
        /// </summary>
        public List<string> AssemblyLines { get; set; } = new();

        /// <summary>
        /// The actual binary bytecode
        /// </summary>
        public byte[] Bytecode { get; set; } = System.Array.Empty<byte>();
    }
}
