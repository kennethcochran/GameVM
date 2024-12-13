/*
 * IErrorHandler.cs
 * 
 * Defines the interface for compiler error handling.
 * Manages compilation error processing:
 * - Error collection and reporting
 * - Error recovery strategies
 * - Error message formatting
 * - Error severity levels
 * - Error location tracking
 * - Error statistics
 * 
 * Critical for robust error management.
 */

namespace GameVM.Compiler.Core.Interfaces
{
    public interface IErrorHandler
    {
        void HandleError(string message);
        void HandleWarning(string message);
        bool HasErrors { get; }
    }
}
