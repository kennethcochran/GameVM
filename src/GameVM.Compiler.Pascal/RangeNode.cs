namespace GameVM.Compiler.Pascal
{
    public class RangeNode : PascalAstNode
    {
        public required ExpressionNode Low { get; set; }
        public required ExpressionNode High { get; set; }
    }
}