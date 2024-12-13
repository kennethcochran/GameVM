/*
 * ILogger.cs
 * 
 * Defines the interface for compiler logging.
 * Provides logging functionality:
 * - Log level filtering
 * - Message categorization
 * - Timestamp tracking
 * - Output formatting
 * - Log destination control
 * - Performance metrics
 * 
 * Essential for debugging and monitoring.
 */

namespace GameVM.Compiler.Core.Interfaces
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
        void LogWarning(string message);
    }
}
