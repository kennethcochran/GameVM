namespace GameVM.Compiler.Pascal
{
    public class RepeatNode : PascalAstNode
    {
        public required PascalAstNode Block { get; set; }
        public required PascalAstNode Condition { get; set; }
    }
}