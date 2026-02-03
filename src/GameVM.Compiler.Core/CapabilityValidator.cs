using GameVM.Compiler.Core.Enums;
using System.Collections.Generic;

namespace GameVM.Compiler.Core
{
    /// <summary>
    /// Validates that required hardware capabilities are met by the project profile.
    /// </summary>
    public class CapabilityValidator
    {
        private readonly CapabilityLevel _projectProfile;
        private readonly HashSet<string> _extensions;

        public CapabilityValidator(CapabilityLevel projectProfile, IEnumerable<string>? extensions = null)
        {
            _projectProfile = projectProfile;
            _extensions = extensions != null ? new HashSet<string>(extensions) : new HashSet<string>();
        }

        /// <summary>
        /// Checks if a specific capability level (and optional extension) is allowed.
        /// </summary>
        public bool IsAllowed(CapabilityLevel requiredLevel, string? requiredExtensionId = null)
        {
            // Tier-based check
            if ((int)_projectProfile >= (int)requiredLevel)
            {
                return true;
            }

            // Extension-based check (Hardware Injection)
            if (!string.IsNullOrEmpty(requiredExtensionId) && _extensions.Contains(requiredExtensionId))
            {
                return true;
            }

            return false;
        }
    }
}
