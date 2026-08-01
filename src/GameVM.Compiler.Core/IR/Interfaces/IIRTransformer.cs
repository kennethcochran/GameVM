using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.IR.Interfaces
{
    /// <summary>
    /// Interface for transforming one IR type to another
    /// </summary>
    public interface IIRTransformer<in TInput, out TOutput>
        where TInput : IIntermediateRepresentation
        where TOutput : IIntermediateRepresentation
    {
        /// <summary>
        /// Transform from one IR type to another
        /// </summary>
        TOutput Transform(TInput input);

        /// <summary>
        /// Transform from one IR slab to another IR slab (DOD pipeline)
        /// </summary>
        /// <param name="inputSlab">Input IR as slab</param>
        /// <param name="stringPool">String pool for identifier resolution</param>
        /// <returns>Output IR as slab</returns>
        uint[] TransformSlab(uint[] inputSlab, StringPool stringPool);
    }
}
