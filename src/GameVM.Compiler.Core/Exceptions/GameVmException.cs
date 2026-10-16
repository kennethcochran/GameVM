using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Exceptions;

/// <summary>
/// The compiler's structured loud-failure exception. Carries a machine-readable
/// <see cref="ErrorCode"/>, a severity category, and a context dictionary with
/// whatever was available at the throw site (construct name, slab index, …).
/// Source positions are included when the throwing stage has them; IR stages
/// below the frontend generally only have slab indexes.
/// </summary>
public class GameVmException : Exception
{
    /// <summary>Gets the machine-readable error code.</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>Gets the severity category.</summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the throw-site context (construct name, slab index, …).
    /// Never null; may be empty when nothing was available.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Context { get; }

    /// <summary>Initializes a new instance as an <see cref="ErrorSeverity.Error"/>.</summary>
    public GameVmException(ErrorCode errorCode, string message)
        : this(errorCode, message, ErrorSeverity.Error, null, null)
    {
    }

    /// <summary>Initializes a new instance with an explicit severity.</summary>
    public GameVmException(ErrorCode errorCode, string message, ErrorSeverity severity)
        : this(errorCode, message, severity, null, null)
    {
    }

    /// <summary>Initializes a new instance with throw-site context.</summary>
    public GameVmException(
        ErrorCode errorCode,
        string message,
        ErrorSeverity severity,
        IReadOnlyDictionary<string, object?>? context)
        : this(errorCode, message, severity, context, null)
    {
    }

    /// <summary>Initializes a new instance with throw-site context and an inner exception.</summary>
    public GameVmException(
        ErrorCode errorCode,
        string message,
        ErrorSeverity severity,
        IReadOnlyDictionary<string, object?>? context,
        Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Severity = severity;
        Context = context ?? new Dictionary<string, object?>();
    }
}
