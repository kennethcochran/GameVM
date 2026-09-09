using System;

namespace GameVM.Compiler.Core.IR.Hlir;

/// <summary>
/// Discriminates the meaning of <see cref="HlirNode.Payload"/>.
/// </summary>
public enum HlirPayloadKind : byte
{
    /// <summary>Payload is unused (0).</summary>
    None = 0,

    /// <summary>Payload is a StringPool offset (unresolved name or literal text).</summary>
    PoolOffset = 1,

    /// <summary>Payload is a literal integer value (small immediates) or a symbol id.</summary>
    Immediate = 2,
}

/// <summary>
/// Semantic node kinds for the HLIR tree. These are the lowering targets of the
/// Pascal AST — assignments, operations, control flow, calls, returns — with typed
/// payloads. Values are unique and stable; they are independent of both the
/// Pascal-specific AST kinds and the MLIR/LLIR instruction kinds.
/// </summary>
public enum HlirNodeKind : byte
{
    Unknown = 0,
    Nop = 0,

    /// <summary>Assignment: children [target, value], target is an Identifier (pool offset).</summary>
    Assign = 1,

    /// <summary>Binary operation: children [left, right], payload = op char.</summary>
    BinaryOp = 2,

    /// <summary>Unary operation: child [operand], payload = op char.</summary>
    UnaryOp = 3,

    /// <summary>If statement: children [condition, then, (else)]; branch polarity = branch-to-target-when-true.</summary>
    If = 4,

    /// <summary>While statement: children [condition, body].</summary>
    While = 5,

    /// <summary>For statement: children [var, initial, final, body]; payload = direction (0=to, 1=downto).</summary>
    For = 6,

    /// <summary>Function/global call: children [args...], payload = function name pool offset.</summary>
    Call = 7,

    /// <summary>Return statement: child [value?].</summary>
    Return = 8,

    /// <summary>Label for control flow; payload = label pool offset.</summary>
    Label = 9,

    /// <summary>Unconditional jump; payload = target label pool offset.</summary>
    Jump = 10,

    /// <summary>Branch (conditional) to target label when condition (child) is true.</summary>
    Branch = 11,

    /// <summary>Variable declaration: children [identifier, type]; type payload = pool offset of type name.</summary>
    VariableDeclaration = 12,

    /// <summary>Function declaration: payload = name pool offset, children [function body].</summary>
    FunctionDeclaration = 13,

    /// <summary>Expression statement: child [expression].</summary>
    ExpressionStatement = 14,

    /// <summary>Identifier reference; payload = pool offset.</summary>
    Identifier = 15,

    /// <summary>Type reference; payload = pool offset of type name.</summary>
    TypeReference = 16,

    /// <summary>Integer literal; payload = immediate value.</summary>
    LiteralInt = 17,

    /// <summary>String literal; payload = pool offset of the string text.</summary>
    LiteralString = 18,

    /// <summary>Boolean literal; payload = 0 (false) or 1 (true).</summary>
    LiteralBool = 19,

    /// <summary>Built-in output statement (write/writeln); children [args...], payload = 0 (write) or 1 (writeln).</summary>
    Output = 20,

    /// <summary>Block of statements; children are executed in order.</summary>
    Block = 22,
}

/// <summary>
/// A single node in the Array-of-Structures (AoS) HLIR semantic tree.
/// Fixed-size (16 bytes), laid out flat in an <see cref="HlirTree"/>.
///
/// Children are addressed via a contiguous span of child indices stored in a
/// separate side buffer (the same pattern as the AST container):
/// <see cref="FirstChild"/> is an offset into that buffer,
/// <see cref="ChildCount"/> is the span length. The node array holds parents only.
///
/// <see cref="Payload"/> carries a by-kind value whose meaning is discriminated by
/// <see cref="PayloadKind"/>: a StringPool offset for names/literal text, or an
/// immediate value for small literal integers / directions / flags.
/// </summary>
public readonly struct HlirNode
{
    /// <summary>HlirNodeKind tag byte.</summary>
    public readonly byte Kind;

    /// <summary>Bitwise flags (InstructionFlag).</summary>
    public readonly ushort Flags;

    /// <summary>By-kind payload (see <see cref="PayloadKind"/>).</summary>
    public readonly uint Payload;

    /// <summary>Offset of the child span in the parent <see cref="HlirTree"/>'s side buffer, or -1 if none.</summary>
    public readonly int FirstChild;

    /// <summary>Number of child indices in the side-buffer span.</summary>
    public readonly int ChildCount;

    /// <summary>Discriminates the meaning of <see cref="Payload"/>.</summary>
    public readonly HlirPayloadKind PayloadKind;

    public HlirNode(byte kind, ushort flags, uint payload, int firstChild, int childCount, HlirPayloadKind payloadKind = HlirPayloadKind.None)
    {
        Kind = kind;
        Flags = flags;
        Payload = payload;
        FirstChild = firstChild;
        ChildCount = childCount;
        PayloadKind = payloadKind;
    }

    /// <summary>True when this node has no children.</summary>
    public bool IsLeaf => ChildCount == 0;
}

/// <summary>
/// Immutable Array-of-Structures (AoS) HLIR semantic tree.
/// A flat <see cref="HlirNode"/>[] holding every node exactly once, plus a side
/// buffer of child indices so children need not be a contiguous node run. Built via
/// <see cref="HlirBuilder"/> and treated as immutable thereafter.
///
/// An <see cref="HlirTree"/> is empty when <see cref="Count"/> == 0.
/// </summary>
public readonly struct HlirTree
{
    private readonly HlirNode[] _nodes;
    private readonly int[] _childIndices;
    private readonly int _count;

    /// <summary>An empty tree. Equivalent to the default value.</summary>
    public static readonly HlirTree Empty = new HlirTree(Array.Empty<HlirNode>(), Array.Empty<int>(), 0);

    /// <summary>Creates an <see cref="HlirTree"/> from node and child-index buffers. Used primarily by <see cref="HlirBuilder"/>.</summary>
    public HlirTree(HlirNode[] nodes, int[] childIndices, int count)
    {
        _nodes = nodes;
        _childIndices = childIndices;
        _count = count;
    }

    /// <summary>Gets the number of nodes in the tree. 0 means empty.</summary>
    public int Count => _count;

    /// <summary>Gets the node at the specified index.</summary>
    public HlirNode this[int index] => _nodes[index];

    /// <summary>Gets the kind byte of the node at <paramref name="index"/>.</summary>
    public byte GetKind(int index) => _nodes[index].Kind;

    /// <summary>
    /// Gets the contiguous span of child indices for the node at <paramref name="index"/>.
    /// Each value is an index into this tree's node array. Returns an empty span for a
    /// leaf or an out-of-range first-child offset.
    /// </summary>
    public ReadOnlySpan<int> Children(int index)
    {
        HlirNode node = _nodes[index];
        if (node.ChildCount <= 0 || node.FirstChild < 0)
            return ReadOnlySpan<int>.Empty;

        int first = node.FirstChild;
        if (first + node.ChildCount > _childIndices.Length)
            return ReadOnlySpan<int>.Empty;

        return new ReadOnlySpan<int>(_childIndices, first, node.ChildCount);
    }

    /// <summary>Gets the index of the first child of the node at <paramref name="index"/>, or -1 if none.</summary>
    public int FirstChildIndex(int index)
    {
        var children = Children(index);
        return children.IsEmpty ? -1 : children[0];
    }
}