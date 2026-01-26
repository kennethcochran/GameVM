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
    }
}
