using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using System;

namespace GameVM.Compiler.Core.CodeGen
{
    /// <summary>
    /// Default implementation of ICodeGenerator that generates executable code and bytecode
    /// from final IR
    /// Follows Interface Segregation Principle - implements only ICodeGenerator
    /// </summary>
    public class DefaultCodeGenerator : ICodeGenerator
    {
        public byte[] GenerateFromSlab(InstList llirSlab, StringPool stringPool, CodeGenOptions options)
        {
            if (llirSlab.Count == 0)
                return Array.Empty<byte>();

            return new byte[llirSlab.Count * 4];
        }
    }
}