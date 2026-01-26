namespace GameVM.Compiler.Pascal
{
    public class UnaryOperatorNode : PascalAstNode
    {
        public required string Operator { get; set; }
        public required PascalAstNode Operand { get; set; }
    }
}