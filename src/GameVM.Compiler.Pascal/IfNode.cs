namespace GameVM.Compiler.Pascal
{
    public class IfNode : PascalAstNode
    {
        public required PascalAstNode Condition { get; set; }
        public required PascalAstNode ThenBlock { get; set; }
        public PascalAstNode? ElseBlock { get; set; }
    }
}