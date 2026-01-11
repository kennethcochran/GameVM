using System.Collections.Generic;

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
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// List of functions in mid-level representation
        /// </summary>
        public Dictionary<string, MLFunction> Functions { get; set; } = new();

        public class MLFunction
        {
            public string Name { get; set; } = string.Empty;
            public List<MLInstruction> Instructions { get; set; } = new();
        }

        public abstract class MLInstruction { }

        public class MLAssign : MLInstruction
        {
            public string Target { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty; // Simplification: everything is a string/identifier/literal for now
        }

        public class MLCall : MLInstruction
        {
            public string Name { get; set; } = string.Empty;
            public List<string> Arguments { get; set; } = new();
        }
    }
}
