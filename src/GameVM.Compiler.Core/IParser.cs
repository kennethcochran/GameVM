using System.IO;

namespace GameVM.Compiler.Core
{
    public interface IParser
    {
        object Parse(Stream stream);
    }
}