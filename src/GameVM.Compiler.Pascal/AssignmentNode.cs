namespace GameVM.Compiler.Pascal
{
    public class AssignmentNode : PascalAstNode
    {
        public required PascalAstNode Left { get; set; }
        public required PascalAstNode Right { get; set; }
    }
}