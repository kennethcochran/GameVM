using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR.Interfaces
{
    /// <summary>
    /// Interface for generating executable code from final IR
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Generate executable code from low-level IR
        /// </summary>
        byte[] Generate(LowLevelIR ir, CodeGenOptions options);

        /// <summary>
        /// Generate bytecode from low-level IR
        /// </summary>
        byte[] GenerateBytecode(LowLevelIR ir, CodeGenOptions options);
    }

    /// <summary>
    /// Options for code generation
    /// </summary>
    public class CodeGenOptions
    {
        /// <summary>
        /// Target architecture to generate code for
        /// </summary>
        public Architecture Target { get; set; }

        /// <summary>
        /// Code dispatch strategy to use
        /// </summary>
        public DispatchStrategy DispatchStrategy { get; set; }

        /// <summary>
        /// Whether to generate debug information
        /// </summary>
        public bool GenerateDebugInfo { get; set; }

        /// <summary>
        /// Whether to optimize the generated code
        /// </summary>
        public bool Optimize { get; set; }
    }
}
