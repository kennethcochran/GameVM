namespace GameVM.Compiler.Pascal
{
    public class ErrorNode : PascalAstNode
    {
        public string Message { get; set; }

        public ErrorNode(string message)
        {
            Message = message;
        }
    }
}
