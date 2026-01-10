namespace GameVM.Compiler.Pascal
{
    public class BinaryOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }
}