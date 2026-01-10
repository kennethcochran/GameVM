namespace GameVM.Compiler.Pascal
{
    public class RangeNode : PascalASTNode
    {
        public required ExpressionNode Low { get; set; }
        public required ExpressionNode High { get; set; }
    }
}