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

        /// <summary>
        /// List of modules in low-level representation
        /// </summary>
        public List<LLModule> Modules { get; set; } = new();

        public class LLModule
        {
            public string Name { get; set; } = string.Empty;
            public List<LLFunction> Functions { get; set; } = new();
        }

        public class LLFunction
        {
            public string Name { get; set; } = string.Empty;
            public List<LLInstruction> Instructions { get; set; } = new();
        }

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

        public class LLJump : LLInstruction
        {
            public string Target { get; set; } = string.Empty;
            public string? Condition { get; set; }
        }

        public class LLLabel : LLInstruction
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}
