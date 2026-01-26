namespace GameVM.Compiler.Pascal
{
    public class ExpressionStatementNode : PascalAstNode
    {
        public required PascalAstNode Expression { get; set; }
    }
}