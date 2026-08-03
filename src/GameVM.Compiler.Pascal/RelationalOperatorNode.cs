namespace GameVM.Compiler.Pascal
{
    public class RelationalOperatorNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required ExpressionNode Left { get; set; }
        public required ExpressionNode Right { get; set; }
    }
}