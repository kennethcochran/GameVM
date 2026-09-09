using System;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Ast;

namespace GameVM.Compiler.CSharp.Transformers
{
    /// <summary>
    /// Transforms a C# AstTree to an AoS HLIR semantic tree (HlirTree).
    /// The C# frontend's semantic lowering is not restored in this feature;
    /// this transformer produces a minimal valid HlirTree so the pipeline
    /// contract (<see cref="ILanguageFrontend.ConvertToHlirSlab"/> returning
    /// HlirTree) type-checks. The Pascal path is the supported one.
    /// </summary>
    public sealed class CSharpAstToHlirTransformer
    {
        private readonly StringPool _stringPool;

        public CSharpAstToHlirTransformer(StringPool stringPool)
        {
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        /// <summary>Transforms a C# AstTree to an AoS HLIR semantic tree.</summary>
        public HlirTree Transform(AstTree astTree)
        {
            _ = _stringPool;
            if (astTree.Count == 0)
                return HlirTree.Empty;

            // Minimal: mirror the AST structure as a Nop root so the tree is non-empty.
            var builder = new HlirBuilder();
            builder.Add((byte)HlirNodeKind.Nop);
            return builder.Build();
        }
    }
}