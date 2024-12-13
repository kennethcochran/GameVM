/*
 * ILanguageRegistry.cs
 * 
 * Defines the interface for managing language support.
 * Coordinates language-specific components:
 * - Language frontend registration
 * - File extension mapping
 * - Language feature detection
 * - Source file routing
 * - Language configuration
 * - Multi-language coordination
 * 
 * Central hub for language support management.
 */

using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for registering language frontends with the compiler.
    /// </summary>
    public interface ILanguageRegistry
    {
        /// <summary>
        /// Registers a frontend for a specific file extension.
        /// </summary>
        /// <param name="extension">The file extension (e.g., ".py", ".js")</param>
        /// <param name="frontend">The language frontend implementation</param>
        void RegisterFrontend(string extension, ILanguageFrontend frontend);

        /// <summary>
        /// Gets the frontend for a specific file extension.
        /// </summary>
        /// <param name="extension">The file extension (e.g., ".py", ".js")</param>
        /// <returns>The language frontend implementation, or null if not found</returns>
        ILanguageFrontend GetFrontend(string extension);
    }
}
