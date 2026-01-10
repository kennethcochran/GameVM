namespace GameVM.Compiler.Pascal
{
    public class ForNode : PascalASTNode
    {
        public required PascalASTNode Variable { get; set; }
        public required PascalASTNode FromExpression { get; set; }
        public required PascalASTNode ToExpression { get; set; }
        public required PascalASTNode Block { get; set; }
    }
}