using System.Collections.Generic;

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
        public string SourceFile { get; set; } = string.Empty;

        public List<LLInstruction> Instructions { get; set; } = new();

        public abstract class LLInstruction { }

        public class LLLoad : LLInstruction
        {
            public string Register { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        public class LLStore : LLInstruction
        {
            public string Address { get; set; } = string.Empty;
            public string Register { get; set; } = string.Empty;
        }

        public class LLCall : LLInstruction
        {
            public string Label { get; set; } = string.Empty;
        }

        public class LLLabel : LLInstruction
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
