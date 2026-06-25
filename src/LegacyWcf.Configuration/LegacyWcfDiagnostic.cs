namespace LegacyWcf.Configuration;

/// <summary>
/// Describes an issue or informational outcome encountered while reading legacy WCF configuration.
/// </summary>
/// <remarks>
/// Diagnostics are used to report read and configuration issues without requiring consumers
/// to handle normal read outcomes as exceptions.
/// </remarks>
public sealed class LegacyWcfDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfDiagnostic"/> class.
    /// </summary>
    /// <param name="severity">
    /// The diagnostic severity.
    /// </param>
    /// <param name="message">
    /// A human-readable diagnostic message. When <see langword="null"/>, an empty message is used.
    /// </param>
    /// <param name="code">
    /// An optional stable diagnostic code, such as <c>LWC0001</c>. This may be
    /// <see langword="null"/> when no code is available.
    /// </param>
    /// <param name="sourceFilePath">
    /// The optional source configuration file path associated with the diagnostic.
    /// This may be <see langword="null"/> when the path is unavailable.
    /// </param>
    /// <param name="lineNumber">
    /// The optional one-based source line number associated with the diagnostic.
    /// This may be <see langword="null"/> when line information is unavailable.
    /// </param>
    public LegacyWcfDiagnostic(
        LegacyWcfDiagnosticSeverity severity,
        string message,
        string? code = null,
        string? sourceFilePath = null,
        int? lineNumber = null)
    {
        Severity = severity;
        Message = message ?? string.Empty;
        Code = code;
        SourceFilePath = sourceFilePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the diagnostic severity.
    /// </summary>
    public LegacyWcfDiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the human-readable diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional stable diagnostic code.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when the diagnostic does not have a code.
    /// </remarks>
    public string? Code { get; }

    /// <summary>
    /// Gets the optional source configuration file path associated with the diagnostic.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when the file path is unavailable or not
    /// relevant to the diagnostic.
    /// </remarks>
    public string? SourceFilePath { get; }

    /// <summary>
    /// Gets the optional one-based line number associated with the diagnostic.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when line information is unavailable.
    /// </remarks>
    public int? LineNumber { get; }
}