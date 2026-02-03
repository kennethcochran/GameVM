using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Service for validating capability profiles against IR.
    /// </summary>
    public interface ICapabilityValidatorService
    {
        /// <summary>
        /// Validates that the High-Level IR conforms to the specified capability profile.
        /// </summary>
        /// <param name="hlir">The High-Level IR to validate.</param>
        /// <param name="profile">The target capability profile.</param>
        /// <param name="systemExtensions">List of enabled system extensions.</param>
        /// <returns>A list of validation error messages. Empty if validation succeeds.</returns>
        IEnumerable<string> Validate(HighLevelIR hlir, CapabilityLevel profile, List<string> systemExtensions);
    }
}
