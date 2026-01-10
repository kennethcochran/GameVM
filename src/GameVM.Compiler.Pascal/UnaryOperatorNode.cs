namespace GameVM.Compiler.Pascal
{
    public class UnaryOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Operand { get; set; }
    }
}