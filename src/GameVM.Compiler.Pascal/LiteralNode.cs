namespace GameVM.Compiler.Pascal
{
    public class LiteralNode : ExpressionNode
    {
        public required object Value { get; set; }
    }
}