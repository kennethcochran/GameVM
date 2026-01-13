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
        /// List of modules in mid-level representation
        /// </summary>
        public List<MLModule> Modules { get; set; } = new();

        /// <summary>
        /// Global variables (kept for backward compatibility)
        /// </summary>
        public Dictionary<string, IRSymbol> Globals { get; set; } = new();

        public class MLModule
        {
            public string Name { get; set; } = string.Empty;
            public List<MLFunction> Functions { get; set; } = new();
        }

        public class MLFunction
        {
            public string Name { get; set; } = string.Empty;
            public List<MLInstruction> Instructions { get; set; } = new();
        }

        public abstract class MLInstruction { }

        public class MLLabel : MLInstruction
        {
            public string Name { get; set; } = string.Empty;
        }

        public class MLBranch : MLInstruction
        {
            public string Target { get; set; } = string.Empty;
            public string? Condition { get; set; }
        }

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
