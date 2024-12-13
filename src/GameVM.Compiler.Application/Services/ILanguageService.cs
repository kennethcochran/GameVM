using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Service for managing language frontends
    /// </summary>
    public interface ILanguageService
    {
        ILanguageFrontend GetFrontend(string extension);
    }
}
