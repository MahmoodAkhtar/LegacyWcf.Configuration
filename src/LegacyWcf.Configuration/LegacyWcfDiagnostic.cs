namespace LegacyWcf.Configuration;

public sealed class LegacyWcfDiagnostic
{
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

    public LegacyWcfDiagnosticSeverity Severity { get; }

    public string Message { get; }

    public string? Code { get; }

    public string? SourceFilePath { get; }

    public int? LineNumber { get; }
}
