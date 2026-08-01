using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Core.CodeGen
{
    /// <summary>
    /// Default implementation of ICodeGenerator that generates executable code and bytecode
    /// from final IR
    /// Follows Interface Segregation Principle - implements only ICodeGenerator
    /// </summary>
    public class DefaultCodeGenerator : ICodeGenerator
    {
        public byte[] GenerateFromSlab(uint[] llirSlab, StringPool stringPool, CodeGenOptions options)
        {
            if (llirSlab == null || llirSlab.Length == 0)
                return Array.Empty<byte>();

            // Convert LLIR slab to bytecode - simple pass-through for now
            // In a real implementation, this would decode the LLIR slab and emit machine code
            var byteArray = new byte[llirSlab.Length * 4];
            Buffer.BlockCopy(llirSlab, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}