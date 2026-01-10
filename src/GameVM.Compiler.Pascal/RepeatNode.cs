namespace GameVM.Compiler.Pascal
{
    public class RepeatNode : PascalASTNode
    {
        public required PascalASTNode Block { get; set; }
        public required PascalASTNode Condition { get; set; }
    }
}