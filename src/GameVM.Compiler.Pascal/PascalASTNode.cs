namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Base class for all AST nodes in the Pascal compiler.
    /// </summary>
    public class PascalASTNode
    {
        public virtual IList<PascalASTNode> Children => new List<PascalASTNode>();
    }
}
