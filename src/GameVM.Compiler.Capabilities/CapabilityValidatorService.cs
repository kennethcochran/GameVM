using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Capabilities
{
    public class CapabilityValidatorService : ICapabilityValidatorService
    {
        public IEnumerable<string> Validate(HighLevelIR hlir, CapabilityLevel profile, List<string> systemExtensions)
        {
            var validator = new CapabilityValidator(profile, systemExtensions);
            var violations = new List<string>();

            // Check Modules
            violations.AddRange(hlir.Modules
                .SelectMany(m => m.Functions)
                .Where(f => !validator.IsAllowed(f.RequiredLevel, f.RequiredExtensionId))
                .Select(f => $"Function '{f.Name}' requires {f.RequiredLevel} (Profile: {profile})"));

            // Check Global Functions (legacy support)
            violations.AddRange(hlir.GlobalFunctions.Values
                .Where(f => !validator.IsAllowed(f.RequiredLevel, f.RequiredExtensionId))
                .Select(f => $"Function '{f.Name}' requires {f.RequiredLevel} (Profile: {profile})"));

            return violations;
        }
    }
}
