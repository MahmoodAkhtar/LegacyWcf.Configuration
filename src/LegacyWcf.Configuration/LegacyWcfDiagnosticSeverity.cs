namespace LegacyWcf.Configuration;

/// <summary>
/// Represents the severity of a <see cref="LegacyWcfDiagnostic"/>.
/// </summary>
public enum LegacyWcfDiagnosticSeverity
{
    /// <summary>
    /// Indicates an informational diagnostic that does not require action.
    /// </summary>
    Info,

    /// <summary>
    /// Indicates a warning diagnostic for a configuration issue that may require review.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates an error diagnostic for a read or configuration issue that prevents a usable result.
    /// </summary>
    Error
}