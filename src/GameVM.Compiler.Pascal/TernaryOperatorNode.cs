namespace GameVM.Compiler.Pascal
{
    public class TernaryOperatorNode : PascalAstNode
    {
        public required PascalAstNode Condition { get; set; }
        public required PascalAstNode TrueExpression { get; set; }
        public required PascalAstNode FalseExpression { get; set; }
    }
}