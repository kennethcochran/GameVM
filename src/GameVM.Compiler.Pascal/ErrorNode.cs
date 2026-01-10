namespace GameVM.Compiler.Pascal
{
    public class ErrorNode : PascalASTNode
    {
        public string Message { get; set; }

        public ErrorNode(string message)
        {
            Message = message;
        }
    }
}
