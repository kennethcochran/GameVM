namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Base class for all AST nodes in the Pascal compiler.
    /// </summary>
    public class PascalAstNode
    {
        public virtual IList<PascalAstNode> Children => new List<PascalAstNode>();
    }
}
