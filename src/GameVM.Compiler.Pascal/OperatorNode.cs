namespace GameVM.Compiler.Pascal
{
    public class OperatorNode : PascalAstNode
    {
        public required string Operator { get; set; }
        public required PascalAstNode Left { get; set; }
        public required PascalAstNode Right { get; set; }
    }
}