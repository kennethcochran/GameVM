namespace GameVM.Compiler.Pascal
{
    public class ForNode : PascalAstNode
    {
        public required PascalAstNode Variable { get; set; }
        public required PascalAstNode FromExpression { get; set; }
        public required PascalAstNode ToExpression { get; set; }
        public required PascalAstNode Block { get; set; }
    }
}