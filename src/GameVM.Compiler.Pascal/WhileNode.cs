namespace GameVM.Compiler.Pascal
{
    public class WhileNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode Block { get; set; }
    }
}