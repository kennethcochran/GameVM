namespace GameVM.Compiler.Pascal
{
    public class StringTypeNode : SimpleTypeNode
    {
        public required ExpressionNode Length { get; set; }
    }
}