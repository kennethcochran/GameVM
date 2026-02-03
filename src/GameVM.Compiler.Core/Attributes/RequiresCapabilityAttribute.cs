using System;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Attributes
{
    /// <summary>
    /// Indicates that a member requires a specific hardware capability level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresCapabilityAttribute : Attribute
    {
        public CapabilityLevel Level { get; }

        public RequiresCapabilityAttribute(CapabilityLevel level)
        {
            Level = level;
        }
    }
}
