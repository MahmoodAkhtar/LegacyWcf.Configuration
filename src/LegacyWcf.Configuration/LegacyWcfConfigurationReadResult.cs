using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

public sealed class LegacyWcfConfigurationReadResult
{
    public LegacyWcfConfigurationReadResult(
        bool success,
        LegacyWcfConfiguration? configuration,
        IReadOnlyList<LegacyWcfDiagnostic>? diagnostics = null)
    {
        Success = success;
        Configuration = configuration;
        Diagnostics = diagnostics ?? Array.Empty<LegacyWcfDiagnostic>();
    }

    public bool Success { get; }

    public LegacyWcfConfiguration? Configuration { get; }

    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; }
}
