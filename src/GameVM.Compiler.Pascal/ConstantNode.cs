namespace GameVM.Compiler.Pascal
{
    public class ConstantNode : ExpressionNode
    {
        public required object Value { get; set; }
    }
}
