using System.Collections.Generic;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Models
{
    /// <summary>
    /// Represents the contents of a gamevm.yaml project file.
    /// </summary>
    public class ProjectConfig
    {
        /// <summary>
        /// The target hardware profile (e.g., GV.Spec.L3)
        /// </summary>
        public CapabilityLevel Profile { get; set; } = CapabilityLevel.L1;

        /// <summary>
        /// The enforcement mode (Strict or Advisory)
        /// </summary>
        public EnforcementLevel Enforcement { get; set; } = EnforcementLevel.Strict;

        /// <summary>
        /// List of hardware extensions/injections enabled for this project.
        /// </summary>
        public List<string> Extensions { get; set; } = new List<string>();

        /// <summary>
        /// The target architecture (e.g. NES, Atari2600)
        /// </summary>
        public Architecture Target { get; set; }
    }
}
