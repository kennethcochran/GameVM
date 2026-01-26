namespace GameVM.Compiler.Pascal
{
    public class ConstantDeclarationNode : PascalAstNode
    {
        public required string Name { get; set; }
        public required ExpressionNode Value { get; set; }
    }
}
