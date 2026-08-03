using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Service for validating capability profiles against IR.
    /// </summary>
    public interface ICapabilityValidatorService
    {
        /// <summary>
        /// Validates that the HLIR slab conforms to the specified capability profile.
        /// </summary>
        /// <param name="hlirSlab">The HLIR slab to validate.</param>
        /// <param name="profile">The target capability profile.</param>
        /// <param name="systemExtensions">List of enabled system extensions.</param>
        /// <returns>A list of validation error messages. Empty if validation succeeds.</returns>
        IEnumerable<string> Validate(uint[] hlirSlab, CapabilityLevel profile, List<string> systemExtensions);
    }
}
