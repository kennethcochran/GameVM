using GameVM.Compiler.Core.Enums;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.IR.Interfaces
{
    /// <summary>
    /// Represents a capability profile for a target backend
    /// </summary>
    public class CapabilityProfile
    {
        /// <summary>
        /// The base capability level of the target hardware
        /// </summary>
        public CapabilityLevel BaseLevel { get; set; }

        /// <summary>
        /// Hardware extensions supported by this backend
        /// </summary>
        public HashSet<string> Extensions { get; set; } = new();

        /// <summary>
        /// Capabilities injected through hardware extensions
        /// </summary>
        public Dictionary<string, CapabilityLevel> InjectedCapabilities { get; set; } = new();
    }

    /// <summary>
    /// Interface for generating executable code from final IR
    /// Follows Interface Segregation Principle - focused on code generation only
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
    /// Interface for providing capability information about backends
    /// Follows Interface Segregation Principle - focused on capability reporting only
    /// </summary>
    public interface ICapabilityProvider
    {
        /// <summary>
        /// Returns the capability profile for this backend
        /// </summary>
        CapabilityProfile GetCapabilityProfile();

        /// <summary>
        /// Returns the list of supported hardware extensions
        /// </summary>
        IEnumerable<string> GetSupportedExtensions();
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
