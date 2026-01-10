namespace GameVM.Compiler.Pascal
{
    public class IfNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode ThenBlock { get; set; }
        public PascalASTNode? ElseBlock { get; set; }
    }
}