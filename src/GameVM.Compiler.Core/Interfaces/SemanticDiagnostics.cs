using System.Collections.Generic;
using System.Text;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// UI-neutral DTO carrying structured diagnostics from a compilation pass.
    /// Any UI can format these without parsing a string. The <see cref="Summary"/>
    /// is provided for backward-compatible rendering (CLI: <c>line:column: message</c>).
    /// </summary>
    public sealed class SemanticDiagnostics
    {
        /// <summary>Every semantic violation from the pass.</summary>
        public IReadOnlyList<SemanticError> Errors { get; }

        /// <summary>Pre-rendered <c>line:column: message</c> lines, one per error.</summary>
        public string Summary { get; }

        public SemanticDiagnostics(IReadOnlyList<SemanticError> errors)
        {
            Errors = errors;
            var sb = new StringBuilder();
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var e = errors[i];
                sb.Append(e.Line);
                sb.Append(':');
                sb.Append(e.Column);
                sb.Append(": ");
                sb.Append(e.Message);
            }
            Summary = sb.ToString();
        }

        /// <summary>Convenience: true when no violations were found.</summary>
        public bool IsEmpty => Errors.Count == 0;
    }
}