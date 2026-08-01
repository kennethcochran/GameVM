using GameVM.Compiler.Core.IR;
#pragma warning disable CS0618 // Type or member is obsolete

namespace GameVM.Compiler.Core.Pipeline
{
    /// <summary>
    /// Interface for optimization pipeline that can optimize different IR levels
    /// </summary>
    public interface IOptimizationPipeline
    {
        /// <summary>
        /// Optimize mid-level IR
        /// </summary>
        void OptimizeMidLevel(MidLevelIR ir);

        /// <summary>
        /// Optimize low-level IR
        /// </summary>
        void OptimizeLowLevel(LowLevelIR ir);
    }
}
