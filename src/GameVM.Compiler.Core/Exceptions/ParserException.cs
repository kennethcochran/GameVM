using System;

namespace GameVM.Compiler.Core.Exceptions;

public class ParserException : CompilerException
{
    public ParserException(string message) : base(message)
    {
    }

    public ParserException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
