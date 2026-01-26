namespace GameVM.Compiler.Pascal
{
    public class MultiplicativeOperatorNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required PascalAstNode Left { get; set; }
        public required PascalAstNode Right { get; set; }
    }
}