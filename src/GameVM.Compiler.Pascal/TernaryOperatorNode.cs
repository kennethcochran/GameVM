namespace GameVM.Compiler.Pascal
{
    public class TernaryOperatorNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode TrueExpression { get; set; }
        public required PascalASTNode FalseExpression { get; set; }
    }
}