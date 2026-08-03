namespace GameVM.Compiler.Pascal
{
    public class SetRangeNode : ExpressionNode
    {
        public required ExpressionNode Low { get; set; }
        public required ExpressionNode High { get; set; }
    }
}