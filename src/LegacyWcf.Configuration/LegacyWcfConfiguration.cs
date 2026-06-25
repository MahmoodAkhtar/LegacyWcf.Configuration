using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

public sealed class LegacyWcfConfiguration
{
    public LegacyWcfConfiguration(
        LegacyWcfElement rawSystemServiceModel,
        IReadOnlyList<LegacyWcfDiagnostic>? diagnostics = null)
    {
        RawSystemServiceModel = rawSystemServiceModel ?? throw new ArgumentNullException(nameof(rawSystemServiceModel));
        Diagnostics = diagnostics ?? Array.Empty<LegacyWcfDiagnostic>();
    }

    public LegacyWcfElement RawSystemServiceModel { get; }

    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; }
}
