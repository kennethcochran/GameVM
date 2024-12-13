namespace GameVM.Compiler.Core.IR.Interfaces
{
    /// <summary>
    /// Interface for transforming one IR type to another
    /// </summary>
    public interface IIRTransformer<TInput, TOutput>
        where TInput : IIntermediateRepresentation
        where TOutput : IIntermediateRepresentation
    {
        /// <summary>
        /// Transform from one IR type to another
        /// </summary>
        TOutput Transform(TInput input);
    }
}
