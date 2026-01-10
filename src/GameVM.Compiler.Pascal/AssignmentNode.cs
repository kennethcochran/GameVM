namespace GameVM.Compiler.Pascal
{
    public class AssignmentNode : PascalASTNode
    {
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }
}