using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Exceptions;

namespace GameVM.Compiler.Core.CodeGen
{
    /// <summary>
    /// Default implementation of ICodeGenerator that generates executable code and bytecode
    /// from final IR
    /// </summary>
    public class DefaultCodeGenerator : ICodeGenerator
    {
        public byte[] Generate(LowLevelIR ir, CodeGenOptions options)
        {
            if (ir == null)
                throw new CompilerException("Cannot generate code from null IR");

            return new byte[0];
        }

        public byte[] GenerateBytecode(LowLevelIR ir, CodeGenOptions options)
        {
            if (ir == null)
                throw new CompilerException("Cannot generate bytecode from null IR");

            return new byte[0];
        }
    }
}
