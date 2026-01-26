namespace GameVM.Compiler.Pascal
{
    public class WhileNode : PascalAstNode
    {
        public required PascalAstNode Condition { get; set; }
        public required PascalAstNode Block { get; set; }
    }
}