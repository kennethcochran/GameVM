namespace GameVM.Compiler.Pascal
{
    using GameVM.Compiler.Core.IR;

    public class SetRangeNode : ExpressionNode
    {
        public required ExpressionNode Low { get; set; }
        public required ExpressionNode High { get; set; }
    }
}